using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Network;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// Manages multiplayer chat functionality using s&box networking
/// </summary>
public sealed class ChatManager : Component, Component.INetworkListener
{
	public static ChatManager Instance { get; private set; }

	// Chat message history (limited to prevent memory issues)
	private const int MaxMessages = 100;
	private List<ChatMessage> _messages = new();
	public IReadOnlyList<ChatMessage> Messages => _messages;

	// Events for UI updates
	public Action<ChatMessage> OnMessageReceived;
	public Action OnMessagesCleared;

	// Network state
	public bool IsConnected => GameNetworkSystem.IsActive;
	public int OnlinePlayerCount => GameNetworkSystem.IsActive ? Connection.All.Count : 1;

	// Local player info
	public string LocalPlayerName => Connection.Local?.DisplayName ?? "Player";
	public long LocalSteamId => Connection.Local?.SteamId ?? 0;

	// Unique connection ID (different for each game instance, even with same Steam account)
	public Guid LocalConnectionId => Connection.Local?.Id ?? Guid.Empty;

	protected override void OnAwake()
	{
		// Always set instance - the networked object from the scene should be authoritative
		Instance = this;
		GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
		Log.Info( $"ChatManager initialized (IsProxy: {IsProxy}, NetworkMode: {GameObject.NetworkMode})" );
	}

	protected override void OnStart()
	{
		// Add a welcome message
		AddSystemMessage( "Welcome to Beastborne! Type to chat with other players.", ChatMessageType.System );
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		// Try to find existing ChatManager in scene (added via prefab in editor)
		Instance = scene.GetAllComponents<ChatManager>().FirstOrDefault();
		if ( Instance != null )
		{
			Log.Info( "ChatManager found in scene" );
			return;
		}

		// Fallback: create locally for single player only
		// For multiplayer, ChatManager should be added to scene as a prefab with NetworkMode.Object
		if ( !GameNetworkSystem.IsActive )
		{
			var go = scene.CreateObject();
			go.Name = "ChatManager";
			go.Components.Create<ChatManager>();
			Log.Info( "ChatManager created locally (single player)" );
		}
		else
		{
			Log.Warning( "ChatManager not found in scene! Add ChatManager prefab to scene for multiplayer chat to work." );
		}
	}

	/// <summary>
	/// Send a chat message to all players
	/// </summary>
	public void SendMessage( string content )
	{
		if ( string.IsNullOrWhiteSpace( content ) ) return;

		Log.Info( $"[ChatManager] SendMessage called - Content: {content}, IsNetworkActive: {GameNetworkSystem.IsActive}" );

		// Limit message length
		content = content.Length > 500 ? content.Substring( 0, 500 ) : content;

		// Add message locally
		var message = new ChatMessage
		{
			SteamId = LocalSteamId,
			PlayerName = LocalPlayerName,
			Content = content,
			Type = ChatMessageType.Player
		};
		AddMessage( message );

		// If we're in a networked game, broadcast to others
		if ( GameNetworkSystem.IsActive )
		{
			Log.Info( $"[ChatManager] Broadcasting to others via RPC (ConnectionId: {LocalConnectionId})..." );
			BroadcastToOthers( LocalConnectionId.ToString(), LocalSteamId, LocalPlayerName, content, (int)ChatMessageType.Player, message.Timestamp.Ticks );
		}
	}

	/// <summary>
	/// Broadcast a system message (achievements, catches, etc.)
	/// </summary>
	public void SendSystemAnnouncement( string content, ChatMessageType type = ChatMessageType.Achievement, string iconPath = null )
	{
		var message = new ChatMessage
		{
			SteamId = LocalSteamId,
			PlayerName = LocalPlayerName,
			Content = content,
			Type = type,
			IconPath = iconPath
		};
		AddMessage( message );

		if ( GameNetworkSystem.IsActive )
		{
			BroadcastToOthers( LocalConnectionId.ToString(), LocalSteamId, LocalPlayerName, content, (int)type, message.Timestamp.Ticks );
		}
	}

	/// <summary>
	/// Announce when player catches a monster
	/// </summary>
	public void AnnounceMonsterCaught( string playerName, string monsterName, string speciesId )
	{
		var iconPath = $"ui/monsters/{speciesId}.png";
		SendSystemAnnouncement( $"{playerName} caught a {monsterName}!", ChatMessageType.Achievement, iconPath );
	}

	/// <summary>
	/// Announce when player reaches a milestone
	/// </summary>
	public void AnnounceMilestone( string playerName, string milestone )
	{
		SendSystemAnnouncement( $"{playerName} {milestone}", ChatMessageType.Achievement );
	}

	/// <summary>
	/// Show off a beast in chat with a nice card display
	/// </summary>
	public void SendBeastShowcase( Monster monster, MonsterSpecies species )
	{
		if ( monster == null || species == null ) return;

		var nickname = string.IsNullOrEmpty( monster.Nickname ) ? species.Name : monster.Nickname;
		var genes = monster.Genetics != null
			? monster.Genetics.HPGene + monster.Genetics.ATKGene + monster.Genetics.DEFGene +
			  monster.Genetics.SPDGene + monster.Genetics.SpAGene + monster.Genetics.SpDGene
			: 0;
		var rank = monster.GetVeteranRank();
		var rankText = rank != VeteranRank.Rookie ? rank.ToString() : "";

		// Get trait display names (not IDs)
		var traitNames = "";
		if ( monster.Traits != null && monster.Traits.Count > 0 )
		{
			var names = new List<string>();
			foreach ( var traitId in monster.Traits )
			{
				var traitDef = TraitDatabase.GetTrait( traitId );
				names.Add( traitDef?.Name ?? traitId );
			}
			traitNames = string.Join( ", ", names );
		}

		var message = new ChatMessage
		{
			SteamId = LocalSteamId,
			PlayerName = LocalPlayerName,
			Content = $"Check out my {species.Name}!",
			Type = ChatMessageType.BeastShowcase,
			IconPath = species.IconPath,
			ShowcaseSpeciesId = species.Id,
			ShowcaseNickname = nickname,
			ShowcaseSpeciesName = species.Name,
			ShowcaseLevel = monster.Level,
			ShowcasePower = monster.PowerRating,
			ShowcaseGenes = genes,
			ShowcaseRarity = species.BaseRarity.ToString(),
			ShowcaseVeteranRank = rankText,
			ShowcaseElement = species.Element.ToString(),
			ShowcaseMonsterId = monster.Id,
			// Extended stats
			ShowcaseBattles = monster.BattlesFought,
			ShowcaseKnockouts = monster.TotalKnockouts,
			ShowcaseExpeditions = monster.ExpeditionsCompleted,
			ShowcaseHP = monster.MaxHP,
			ShowcaseATK = monster.ATK,
			ShowcaseDEF = monster.DEF,
			ShowcaseSpA = monster.SpA,
			ShowcaseSpD = monster.SpD,
			ShowcaseSPD = monster.SPD,
			ShowcaseTraits = traitNames
		};
		AddMessage( message );

		if ( GameNetworkSystem.IsActive )
		{
			BroadcastBeastShowcase(
				LocalConnectionId.ToString(), LocalSteamId, LocalPlayerName,
				species.IconPath, species.Id, nickname, species.Name,
				monster.Level, monster.PowerRating, genes,
				species.BaseRarity.ToString(), rankText, species.Element.ToString(),
				message.Timestamp.Ticks,
				monster.BattlesFought, monster.TotalKnockouts, monster.ExpeditionsCompleted,
				monster.MaxHP, monster.ATK, monster.DEF, monster.SpA, monster.SpD, monster.SPD, traitNames
			);
		}
	}

	/// <summary>
	/// Broadcast beast showcase to other players via RPC
	/// </summary>
	[Rpc.Broadcast]
	public void BroadcastBeastShowcase(
		string senderConnectionId, long steamId, string playerName,
		string iconPath, string speciesId, string nickname, string speciesName,
		int level, int power, int genes, string rarity, string veteranRank, string element,
		long timestampTicks,
		int battles, int knockouts, int expeditions,
		int hp, int atk, int def, int spa, int spd, int spd2, string traits )
	{
		if ( senderConnectionId == LocalConnectionId.ToString() ) return;

		var message = new ChatMessage
		{
			SteamId = steamId,
			PlayerName = playerName,
			Content = $"Check out my {speciesName}!",
			Type = ChatMessageType.BeastShowcase,
			IconPath = iconPath,
			ShowcaseSpeciesId = speciesId,
			ShowcaseNickname = nickname,
			ShowcaseSpeciesName = speciesName,
			ShowcaseLevel = level,
			ShowcasePower = power,
			ShowcaseGenes = genes,
			ShowcaseRarity = rarity,
			ShowcaseVeteranRank = veteranRank,
			ShowcaseElement = element,
			Timestamp = new DateTime( timestampTicks, DateTimeKind.Utc ),
			// Extended stats
			ShowcaseBattles = battles,
			ShowcaseKnockouts = knockouts,
			ShowcaseExpeditions = expeditions,
			ShowcaseHP = hp,
			ShowcaseATK = atk,
			ShowcaseDEF = def,
			ShowcaseSpA = spa,
			ShowcaseSpD = spd,
			ShowcaseSPD = spd2,
			ShowcaseTraits = traits
		};
		AddMessage( message );
	}

	/// <summary>
	/// Broadcast message to other players via RPC
	/// </summary>
	[Rpc.Broadcast]
	public void BroadcastToOthers( string senderConnectionId, long steamId, string playerName, string content, int messageType, long timestampTicks )
	{
		Log.Info( $"[ChatManager RPC] BroadcastToOthers called - From: {playerName}, Content: {content}, SenderConnectionId: {senderConnectionId}, LocalConnectionId: {LocalConnectionId}" );

		// Only add if it's from someone else (use ConnectionId, not SteamId, since same account can have multiple instances)
		if ( senderConnectionId == LocalConnectionId.ToString() )
		{
			Log.Info( "[ChatManager RPC] Skipping own message" );
			return;
		}

		var message = new ChatMessage
		{
			SteamId = steamId,
			PlayerName = playerName,
			Content = content,
			Type = (ChatMessageType)messageType,
			Timestamp = new DateTime( timestampTicks, DateTimeKind.Utc )
		};

		AddMessage( message );
		Log.Info( $"[ChatManager RPC] Message added from {playerName}" );
	}

	/// <summary>
	/// Add a message to the history
	/// </summary>
	private void AddMessage( ChatMessage message )
	{
		_messages.Add( message );

		// Trim old messages if we exceed the limit
		while ( _messages.Count > MaxMessages )
		{
			_messages.RemoveAt( 0 );
		}

		OnMessageReceived?.Invoke( message );
	}

	/// <summary>
	/// Add a local system message (not broadcast)
	/// </summary>
	public void AddSystemMessage( string content, ChatMessageType type = ChatMessageType.System )
	{
		var message = new ChatMessage
		{
			SteamId = 0,
			PlayerName = "System",
			Content = content,
			Type = type
		};

		AddMessage( message );
	}

	/// <summary>
	/// Clear all messages
	/// </summary>
	public void ClearMessages()
	{
		_messages.Clear();
		OnMessagesCleared?.Invoke();
	}

	/// <summary>
	/// Get recent messages (for initial load)
	/// </summary>
	public List<ChatMessage> GetRecentMessages( int count = 50 )
	{
		return _messages.TakeLast( count ).ToList();
	}

	// INetworkListener implementation
	void INetworkListener.OnActive( Connection connection )
	{
		// Player joined
		if ( connection != Connection.Local )
		{
			AddSystemMessage( $"{connection.DisplayName} joined the game.", ChatMessageType.Join );
		}
	}

	void INetworkListener.OnDisconnected( Connection connection )
	{
		// Player left - don't announce to avoid spam
	}
}
