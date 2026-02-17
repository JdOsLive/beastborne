using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Network;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// Voice component that filters voice by room membership.
/// Only transmits to and receives from players in the same voice room.
/// </summary>
public sealed class RoomVoiceFilter : Voice
{
	protected override IEnumerable<Connection> ExcludeFilter()
	{
		var vcm = VoiceChatManager.Instance;
		if ( vcm?.CurrentRoom == null )
		{
			// Not in a room: exclude everyone (don't transmit)
			return Connection.All.Where( c => c != Connection.Local ).ToList();
		}

		// In a room: only exclude connections NOT in our room
		var excluded = new List<Connection>();
		foreach ( var c in Connection.All )
		{
			if ( c == Connection.Local ) continue;
			if ( !vcm.CurrentRoom.HasMember( c.Id.ToString() ) )
				excluded.Add( c );
		}
		return excluded;
	}

	protected override bool ShouldHearVoice( Connection connection )
	{
		var vcm = VoiceChatManager.Instance;
		// No manager = no voice
		if ( vcm == null ) return false;
		// Not in a room = don't hear anyone
		if ( vcm.CurrentRoom == null ) return false;

		var connId = connection.Id.ToString();

		// Local mute check
		if ( vcm.IsPlayerMuted( connId ) ) return false;

		// Only hear players who are in our room
		return vcm.CurrentRoom.HasMember( connId );
	}
}

/// <summary>
/// Manages voice chat rooms with room-scoped voice communication.
/// Players can join the open lobby or create/join custom rooms.
/// </summary>
public sealed class VoiceChatManager : Component, Component.INetworkListener
{
	public static VoiceChatManager Instance { get; private set; }

	private const int MAX_ROOMS = 10;
	private const int MAX_ROOM_SIZE = 8;
	private const int MAX_ROOM_NAME_LENGTH = 30;
	private const string LOBBY_ROOM_ID = "lobby";

	// Voice component for room-scoped audio
	private RoomVoiceFilter _voice;

	// State
	private List<VoiceRoom> _rooms = new();
	public IReadOnlyList<VoiceRoom> Rooms => _rooms;
	public VoiceRoom CurrentRoom { get; private set; }
	public bool IsMicActive { get; private set; } = false;

	private HashSet<string> _locallyMutedPlayers = new();

	private string LocalConnectionId => Connection.Local?.Id.ToString() ?? "";
	private string LocalPlayerName => Connection.Local?.DisplayName ?? "Player";
	private long LocalSteamId => Connection.Local?.SteamId ?? 0;

	// Events
	public Action OnRoomsUpdated;
	public Action OnCurrentRoomUpdated;
	public Action<string> OnKickedFromRoom;
	public Action<string, string> OnPlayerJoinedRoom;
	public Action<string, string> OnPlayerLeftRoom;

	// Helpers
	public bool IsInRoom => CurrentRoom != null;
	public bool IsRoomOwner => CurrentRoom?.IsOwner( LocalConnectionId ) ?? false;
	public bool IsInLobby => CurrentRoom?.IsLobby ?? false;

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			EnsureLobbyExists();
			SetupVoiceComponent();
			Log.Info( "VoiceChatManager initialized" );
		}
		else
		{
			Destroy();
		}
	}

	private void SetupVoiceComponent()
	{
		_voice = GameObject.Components.GetOrCreate<RoomVoiceFilter>();
		_voice.Mode = Voice.ActivateMode.Manual;
		_voice.WorldspacePlayback = false;
		_voice.IsListening = false;
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;
		var go = scene.CreateObject();
		go.Name = "VoiceChatManager";
		go.Flags = GameObjectFlags.DontDestroyOnLoad;
		go.Components.Create<VoiceChatManager>();
	}

	private void EnsureLobbyExists()
	{
		if ( _rooms.Any( r => r.IsLobby ) ) return;
		_rooms.Insert( 0, new VoiceRoom
		{
			RoomId = LOBBY_ROOM_ID,
			RoomName = "Open Lobby",
			OwnerConnectionId = "",
			OwnerName = "",
			IsLobby = true,
			MaxMembers = 50
		} );
	}

	// Voice routing is handled by RoomVoiceFilter's ExcludeFilter/ShouldHearVoice overrides

	// ═══════════════════════════════════════════════════════════════
	// INetworkListener
	// ═══════════════════════════════════════════════════════════════

	void INetworkListener.OnActive( Connection connection )
	{
		// Sync our room state to the new player
		if ( CurrentRoom != null )
		{
			BroadcastRoomState(
				LocalConnectionId,
				CurrentRoom.RoomId,
				CurrentRoom.RoomName,
				CurrentRoom.OwnerConnectionId,
				CurrentRoom.OwnerName,
				SerializeMembers( CurrentRoom.Members ),
				CurrentRoom.MaxMembers,
				CurrentRoom.IsLobby
			);
		}
	}

	void INetworkListener.OnDisconnected( Connection connection )
	{
		var connId = connection.Id.ToString();

		foreach ( var room in _rooms.ToList() )
		{
			var member = room.GetMember( connId );
			if ( member == null ) continue;

			room.Members.Remove( member );

			if ( !room.IsLobby && room.IsOwner( connId ) )
			{
				if ( room.Members.Count > 0 )
				{
					room.OwnerConnectionId = room.Members[0].ConnectionId;
					room.OwnerName = room.Members[0].PlayerName;
				}
				else
				{
					_rooms.Remove( room );
				}
			}
		}

		_locallyMutedPlayers.Remove( connId );

		OnRoomsUpdated?.Invoke();
		if ( CurrentRoom != null )
		{
			OnCurrentRoomUpdated?.Invoke();
			OnPlayerLeftRoom?.Invoke( connId, connection.DisplayName ?? "Player" );
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// PUBLIC API
	// ═══════════════════════════════════════════════════════════════

	public void JoinLobby()
	{
		var lobby = _rooms.FirstOrDefault( r => r.IsLobby );
		if ( lobby != null )
			JoinRoom( lobby.RoomId );
	}

	public bool CreateRoom( string roomName )
	{
		if ( CurrentRoom != null ) return false;
		if ( !GameNetworkSystem.IsActive ) return false;
		if ( _rooms.Count( r => !r.IsLobby ) >= MAX_ROOMS ) return false;

		roomName = roomName?.Trim() ?? "";
		if ( string.IsNullOrEmpty( roomName ) )
			roomName = $"{LocalPlayerName}'s Room";
		if ( roomName.Length > MAX_ROOM_NAME_LENGTH )
			roomName = roomName.Substring( 0, MAX_ROOM_NAME_LENGTH );

		var room = new VoiceRoom
		{
			RoomName = roomName,
			OwnerConnectionId = LocalConnectionId,
			OwnerName = LocalPlayerName,
			MaxMembers = MAX_ROOM_SIZE
		};

		room.Members.Add( new VoiceRoomMember
		{
			ConnectionId = LocalConnectionId,
			SteamId = LocalSteamId,
			PlayerName = LocalPlayerName
		} );

		_rooms.Add( room );
		CurrentRoom = room;
		EnableMic();

		BroadcastRoomCreated(
			LocalConnectionId, room.RoomId, roomName,
			LocalPlayerName, LocalSteamId, MAX_ROOM_SIZE
		);

		OnRoomsUpdated?.Invoke();
		OnCurrentRoomUpdated?.Invoke();
		return true;
	}

	public bool JoinRoom( string roomId )
	{
		if ( CurrentRoom != null ) return false;

		var room = _rooms.Find( r => r.RoomId == roomId );
		if ( room == null ) return false;
		if ( room.Members.Count >= room.MaxMembers ) return false;

		room.Members.Add( new VoiceRoomMember
		{
			ConnectionId = LocalConnectionId,
			SteamId = LocalSteamId,
			PlayerName = LocalPlayerName
		} );
		CurrentRoom = room;
		EnableMic();

		BroadcastRoomJoin( LocalConnectionId, LocalSteamId, LocalPlayerName, roomId );

		OnRoomsUpdated?.Invoke();
		OnCurrentRoomUpdated?.Invoke();
		return true;
	}

	public void LeaveRoom()
	{
		if ( CurrentRoom == null ) return;

		var roomId = CurrentRoom.RoomId;
		var wasOwner = CurrentRoom.IsOwner( LocalConnectionId );
		var isLobby = CurrentRoom.IsLobby;

		CurrentRoom.Members.RemoveAll( m => m.ConnectionId == LocalConnectionId );

		if ( !isLobby && wasOwner )
		{
			if ( CurrentRoom.Members.Count > 0 )
			{
				CurrentRoom.OwnerConnectionId = CurrentRoom.Members[0].ConnectionId;
				CurrentRoom.OwnerName = CurrentRoom.Members[0].PlayerName;
			}
			else
			{
				_rooms.Remove( CurrentRoom );
			}
		}

		DisableMic();
		CurrentRoom = null;

		BroadcastRoomLeave( LocalConnectionId, roomId );

		OnRoomsUpdated?.Invoke();
		OnCurrentRoomUpdated?.Invoke();
	}

	public void KickPlayer( string targetConnectionId )
	{
		if ( CurrentRoom == null || CurrentRoom.IsLobby ) return;
		if ( !CurrentRoom.IsOwner( LocalConnectionId ) ) return;
		if ( targetConnectionId == LocalConnectionId ) return;

		BroadcastRoomKick( LocalConnectionId, CurrentRoom.RoomId, targetConnectionId );

		CurrentRoom.Members.RemoveAll( m => m.ConnectionId == targetConnectionId );
		OnCurrentRoomUpdated?.Invoke();
		OnRoomsUpdated?.Invoke();
	}

	// ═══════════════════════════════════════════════════════════════
	// MIC & MUTE
	// ═══════════════════════════════════════════════════════════════

	public void ToggleMic()
	{
		if ( IsMicActive ) DisableMic();
		else EnableMic();
	}

	public void EnableMic()
	{
		if ( CurrentRoom == null ) return;
		if ( _voice != null ) _voice.IsListening = true;
		IsMicActive = true;
		OnCurrentRoomUpdated?.Invoke();
	}

	public void DisableMic()
	{
		if ( _voice != null ) _voice.IsListening = false;
		IsMicActive = false;
		OnCurrentRoomUpdated?.Invoke();
	}

	public void ToggleMutePlayer( string connectionId )
	{
		if ( _locallyMutedPlayers.Contains( connectionId ) )
			_locallyMutedPlayers.Remove( connectionId );
		else
			_locallyMutedPlayers.Add( connectionId );
		OnCurrentRoomUpdated?.Invoke();
	}

	public bool IsPlayerMuted( string connectionId ) => _locallyMutedPlayers.Contains( connectionId );

	public bool IsLocalSpeaking => _voice != null && _voice.IsRecording && IsMicActive;

	// ═══════════════════════════════════════════════════════════════
	// RPC BROADCASTS
	// ═══════════════════════════════════════════════════════════════

	[Rpc.Broadcast]
	private void BroadcastRoomCreated(
		string creatorConnectionId, string roomId, string roomName,
		string creatorName, long creatorSteamId, int maxMembers )
	{
		if ( creatorConnectionId == LocalConnectionId ) return;
		if ( _rooms.Any( r => r.RoomId == roomId ) ) return;

		var room = new VoiceRoom
		{
			RoomId = roomId,
			RoomName = roomName,
			OwnerConnectionId = creatorConnectionId,
			OwnerName = creatorName,
			MaxMembers = maxMembers
		};
		room.Members.Add( new VoiceRoomMember
		{
			ConnectionId = creatorConnectionId,
			SteamId = creatorSteamId,
			PlayerName = creatorName
		} );
		_rooms.Add( room );
		OnRoomsUpdated?.Invoke();
	}

	[Rpc.Broadcast]
	private void BroadcastRoomJoin(
		string joinerConnectionId, long joinerSteamId,
		string joinerName, string roomId )
	{
		if ( joinerConnectionId == LocalConnectionId ) return;

		EnsureLobbyExists();
		var room = _rooms.Find( r => r.RoomId == roomId );
		if ( room == null ) return;

		if ( !room.HasMember( joinerConnectionId ) )
		{
			room.Members.Add( new VoiceRoomMember
			{
				ConnectionId = joinerConnectionId,
				SteamId = joinerSteamId,
				PlayerName = joinerName
			} );
		}

		OnRoomsUpdated?.Invoke();
		if ( CurrentRoom?.RoomId == roomId )
		{
			OnCurrentRoomUpdated?.Invoke();
			OnPlayerJoinedRoom?.Invoke( joinerConnectionId, joinerName );
		}
	}

	[Rpc.Broadcast]
	private void BroadcastRoomLeave( string leaverConnectionId, string roomId )
	{
		if ( leaverConnectionId == LocalConnectionId ) return;

		var room = _rooms.Find( r => r.RoomId == roomId );
		if ( room == null ) return;

		var memberName = room.GetMember( leaverConnectionId )?.PlayerName ?? "Player";
		room.Members.RemoveAll( m => m.ConnectionId == leaverConnectionId );

		if ( !room.IsLobby && room.IsOwner( leaverConnectionId ) )
		{
			if ( room.Members.Count > 0 )
			{
				room.OwnerConnectionId = room.Members[0].ConnectionId;
				room.OwnerName = room.Members[0].PlayerName;
			}
			else
			{
				_rooms.Remove( room );
			}
		}

		OnRoomsUpdated?.Invoke();
		if ( CurrentRoom?.RoomId == roomId )
		{
			OnCurrentRoomUpdated?.Invoke();
			OnPlayerLeftRoom?.Invoke( leaverConnectionId, memberName );
		}
	}

	[Rpc.Broadcast]
	private void BroadcastRoomKick(
		string ownerConnectionId, string roomId, string targetConnectionId )
	{
		if ( targetConnectionId == LocalConnectionId && CurrentRoom?.RoomId == roomId )
		{
			DisableMic();
			CurrentRoom.Members.RemoveAll( m => m.ConnectionId == LocalConnectionId );
			CurrentRoom = null;
			OnKickedFromRoom?.Invoke( "You were kicked from the voice room." );
			OnCurrentRoomUpdated?.Invoke();
		}

		var room = _rooms.Find( r => r.RoomId == roomId );
		if ( room != null )
		{
			room.Members.RemoveAll( m => m.ConnectionId == targetConnectionId );
			if ( !room.IsLobby && room.Members.Count == 0 )
				_rooms.Remove( room );
		}

		OnRoomsUpdated?.Invoke();
		if ( CurrentRoom?.RoomId == roomId )
			OnCurrentRoomUpdated?.Invoke();
	}

	[Rpc.Broadcast]
	private void BroadcastRoomState(
		string senderConnectionId, string roomId, string roomName,
		string ownerConnectionId, string ownerName,
		string membersJson, int maxMembers, bool isLobby )
	{
		if ( senderConnectionId == LocalConnectionId ) return;

		EnsureLobbyExists();
		var existing = _rooms.Find( r => r.RoomId == roomId );
		if ( existing != null )
		{
			existing.Members = DeserializeMembers( membersJson );
			existing.OwnerConnectionId = ownerConnectionId;
			existing.OwnerName = ownerName;
		}
		else
		{
			_rooms.Add( new VoiceRoom
			{
				RoomId = roomId,
				RoomName = roomName,
				OwnerConnectionId = ownerConnectionId,
				OwnerName = ownerName,
				MaxMembers = maxMembers,
				IsLobby = isLobby,
				Members = DeserializeMembers( membersJson )
			} );
		}

		OnRoomsUpdated?.Invoke();
	}

	// ═══════════════════════════════════════════════════════════════
	// SERIALIZATION
	// ═══════════════════════════════════════════════════════════════

	private string SerializeMembers( List<VoiceRoomMember> members )
	{
		return string.Join( ";",
			members.Select( m => $"{m.ConnectionId}|{m.SteamId}|{m.PlayerName}" ) );
	}

	private List<VoiceRoomMember> DeserializeMembers( string data )
	{
		if ( string.IsNullOrEmpty( data ) ) return new();
		return data.Split( ';' )
			.Where( s => !string.IsNullOrEmpty( s ) )
			.Select( s =>
			{
				var parts = s.Split( '|' );
				return new VoiceRoomMember
				{
					ConnectionId = parts[0],
					SteamId = parts.Length > 1 && long.TryParse( parts[1], out var id ) ? id : 0,
					PlayerName = parts.Length > 2 ? parts[2] : "Player"
				};
			} ).ToList();
	}
}
