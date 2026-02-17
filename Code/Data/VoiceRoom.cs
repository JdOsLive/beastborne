using System;
using System.Collections.Generic;

namespace Beastborne.Data;

public class VoiceRoomMember
{
	public string ConnectionId { get; set; }
	public long SteamId { get; set; }
	public string PlayerName { get; set; }
	public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public class VoiceRoom
{
	public string RoomId { get; set; } = Guid.NewGuid().ToString();
	public string RoomName { get; set; }
	public string OwnerConnectionId { get; set; }
	public string OwnerName { get; set; }
	public List<VoiceRoomMember> Members { get; set; } = new();
	public int MaxMembers { get; set; } = 8;
	public bool IsLobby { get; set; } = false;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public bool HasMember( string connectionId )
	{
		return Members.Exists( m => m.ConnectionId == connectionId );
	}

	public VoiceRoomMember GetMember( string connectionId )
	{
		return Members.Find( m => m.ConnectionId == connectionId );
	}

	public bool IsOwner( string connectionId )
	{
		return OwnerConnectionId == connectionId;
	}
}
