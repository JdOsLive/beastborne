using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Network;
using Sandbox.Services;
using Beastborne.Data;
using Beastborne.Systems;

namespace Beastborne.Core;

/// <summary>
/// Manages guild creation, membership, RPCs, raid bosses, and persistence.
/// Data stored on remote API server, with local cache and RPC for real-time sync.
/// </summary>
public sealed class GuildManager : Component, Component.INetworkListener
{
	public static GuildManager Instance { get; private set; }

	// ═══════════════════════════════════════════════════════════════
	// CONSTANTS
	// ═══════════════════════════════════════════════════════════════

	private const string STAT_PREFIX = "guild-";
	private const float SAVE_INTERVAL = 30f;
	public const int GUILD_CREATION_COST = 50000;
	public const int MIN_LEVEL_TO_JOIN = 10;
	public const int MAX_MEMBERS = 30;
	public const int MAX_LOG_ENTRIES = 50;
	public const int GUILD_HOP_COOLDOWN_HOURS = 24;
	public const int MAX_GUILD_LEVEL = 20;
	public const int LEADER_INACTIVE_DAYS = 30;
	public const int MAX_RAID_ATTEMPTS_PER_DAY = 3;

	// Raid boss curated species list (Legendary + Mythic)
	private static readonly string[] RaidBossSpecies = new[]
	{
		"mythweaver", "worldserpent", "voiddragon", "primordius",
		"genesis", "genisoul", "songborne", "namashira",
		"chalkodon", "fujinara", "bluffrost"
	};

	// ═══════════════════════════════════════════════════════════════
	// STATE
	// ═══════════════════════════════════════════════════════════════

	public GuildMembership Membership { get; private set; }
	public GuildDefinition Guild { get; private set; }
	public List<GuildMemberInfo> Members { get; private set; } = new();
	public List<GuildLogEntry> ActivityLog { get; private set; } = new();
	public List<GuildJoinRequest> JoinRequests { get; private set; } = new();
	public List<GuildInvite> PendingInvites { get; private set; } = new();
	public Dictionary<string, GuildAdvertisement> VisibleGuilds { get; private set; } = new();
	public GuildRaidBoss CurrentRaidBoss { get; private set; }

	private float lastSaveTime = 0f;
	private float lastWeeklyCheckTime = 0f;

	// ═══════════════════════════════════════════════════════════════
	// COMPUTED PROPERTIES
	// ═══════════════════════════════════════════════════════════════

	public bool IsInGuild => Membership != null && Guild != null;
	public bool IsLeader => Membership?.Role == GuildRole.Beastlord;
	public bool IsOfficer => Membership?.Role >= GuildRole.Warden;
	public int OnlineCount => Members.Count( m => m.IsOnline );
	public int TotalRP => Members.Sum( m => m.ArenaPoints );

	public string GuildRankTier => TotalRP switch
	{
		>= 120000 => "Legendary",
		>= 80000 => "Master",
		>= 50000 => "Diamond",
		>= 30000 => "Platinum",
		>= 15000 => "Gold",
		>= 5000 => "Silver",
		_ => "Bronze"
	};

	// ═══════════════════════════════════════════════════════════════
	// EVENTS
	// ═══════════════════════════════════════════════════════════════

	public Action OnGuildUpdated;
	public Action OnMembersUpdated;
	public Action OnLogUpdated;
	public Action<GuildInvite> OnInviteReceived;
	public Action<GuildJoinRequest> OnJoinRequestReceived;
	public Action OnGuildJoined;
	public Action OnGuildLeft;
	public Action<string> OnGuildError;

	// ═══════════════════════════════════════════════════════════════
	// LIFECYCLE
	// ═══════════════════════════════════════════════════════════════

	private static string GetKey( string key ) => $"{SaveSlotManager.GetSlotPrefix()}{key}";

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			_ = LoadFromApi();
			Log.Info( "GuildManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;
		var go = scene.CreateObject();
		go.Name = "GuildManager";
		go.Components.Create<GuildManager>();
	}

	protected override void OnUpdate()
	{
		// Heartbeat every 60s — update last_seen and sync player stats to API
		if ( IsInGuild && Time.Now - lastSaveTime > 60f )
		{
			lastSaveTime = Time.Now;
			_ = SendHeartbeat();
		}

		// Check weekly stat reset every 60s
		if ( Time.Now - lastWeeklyCheckTime > 60f )
		{
			lastWeeklyCheckTime = Time.Now;
			CheckWeeklyReset();
		}
	}

	private async Task SendHeartbeat()
	{
		if ( !IsInGuild || Guild == null ) return;
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		var steamId = Connection.Local?.SteamId ?? 0;
		await GuildApiClient.PostAsync( $"guilds/{Guild.Id}/members/{steamId}/heartbeat", new
		{
			level = tamer.Level,
			arenaPoints = tamer.ArenaPoints,
			arenaRank = CompetitiveManager.GetRankFromPoints( tamer.ArenaPoints )
		} );
	}

	// ═══════════════════════════════════════════════════════════════
	// INetworkListener
	// ═══════════════════════════════════════════════════════════════

	void INetworkListener.OnActive( Connection conn )
	{
		// When we connect, broadcast our guild presence
		if ( conn == Connection.Local && IsInGuild )
		{
			BroadcastPresence();
		}

		// When another player connects, re-broadcast so they see us
		if ( conn != Connection.Local && IsInGuild )
		{
			BroadcastPresence();
		}
	}

	void INetworkListener.OnDisconnected( Connection conn )
	{
		// Mark the disconnected player as offline in our member list
		var connId = conn.Id.ToString();
		var member = Members.FirstOrDefault( m => m.ConnectionId == connId );
		if ( member != null )
		{
			member.IsOnline = false;
			member.LastSeen = DateTime.UtcNow;
			OnMembersUpdated?.Invoke();
		}

		// Remove from visible guilds if they were advertising
		VisibleGuilds.Remove( connId );
	}

	// ═══════════════════════════════════════════════════════════════
	// PERSISTENCE
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Load guild data from the remote API server on startup.
	/// </summary>
	private async Task LoadFromApi()
	{
		try
		{
			var steamId = Connection.Local?.SteamId ?? 0;
			if ( steamId == 0 )
			{
				Log.Warning( "GuildManager: No Steam ID, skipping API load" );
				return;
			}

			var result = await GuildApiClient.GetAsync<MyGuildResponse>( $"players/{steamId}/guild" );
			if ( result == null )
			{
				Log.Info( "GuildManager: API unavailable, starting without guild data" );
				return;
			}

			if ( result.Guild == null )
			{
				Log.Info( "GuildManager: Not in a guild" );
				// Load any pending invites for this player
				await LoadPendingInvites();
				return;
			}

			// Map API data to local state
			ApplyApiData( result );

			// Mark all members as offline (will be updated via presence broadcasts)
			foreach ( var m in Members )
				m.IsOnline = false;

			var self = Members.FirstOrDefault( m => m.SteamId == steamId );
			if ( self != null )
			{
				self.IsOnline = true;
				self.LastSeen = DateTime.UtcNow;
			}

			if ( IsInGuild )
				EnsureRaidBoss();

			OnGuildUpdated?.Invoke();
			OnMembersUpdated?.Invoke();

			Log.Info( $"GuildManager loaded from API: InGuild={IsInGuild}, Members={Members.Count}" );
		}
		catch ( Exception e )
		{
			Log.Warning( $"GuildManager: Failed to load from API: {e.Message}" );
		}
	}

	/// <summary>
	/// Refresh all guild data from the API. Call after mutations.
	/// </summary>
	public async Task RefreshGuildData()
	{
		if ( !IsInGuild || Guild == null ) return;

		try
		{
			var result = await GuildApiClient.GetAsync<GuildDetailResponse>( $"guilds/{Guild.Id}" );
			if ( result?.Guild == null ) return;

			ApplyGuildDetail( result );
			OnGuildUpdated?.Invoke();
			OnMembersUpdated?.Invoke();
			OnLogUpdated?.Invoke();
		}
		catch ( Exception e )
		{
			Log.Warning( $"GuildManager: Refresh failed: {e.Message}" );
		}
	}

	/// <summary>
	/// Map API response data to local Guild/Members/Log state.
	/// </summary>
	private void ApplyApiData( MyGuildResponse data )
	{
		var g = data.Guild;
		Guild = new GuildDefinition
		{
			Id = g.Id,
			Name = g.Name,
			Tag = g.Tag,
			Level = g.Level,
			GuildXP = g.GuildXp,
			EmblemColor = g.EmblemColor ?? "#7c3aed",
			EmblemIcon = g.EmblemIcon ?? "dragon",
			EmblemShape = g.EmblemShape ?? "square",
			Description = g.Description ?? "",
			Motd = g.Motd ?? "",
			JoinMode = Enum.TryParse<GuildJoinMode>( g.JoinMode, out var jm ) ? jm : GuildJoinMode.Request,
			MinLevel = g.MinLevel,
			MinRank = g.MinRank ?? "Unranked",
			AutoKickInactive = g.AutoKickInactive == 1,
			InactiveDays = g.InactiveDays,
			MaxMembers = g.MaxMembers,
			OwnerSteamId = long.TryParse( g.OwnerSteamId, out var oid ) ? oid : 0,
			OwnerName = g.OwnerName ?? "",
			TotalCatches = g.TotalCatches,
			TotalArenaWins = g.TotalArenaWins,
			TotalExpeditions = g.TotalExpeditions,
			TotalRaidsCompleted = g.TotalRaidsCompleted,
			CreatedAt = DateTime.TryParse( g.CreatedAt, out var ca ) ? ca : DateTime.UtcNow,
			LastUpdated = DateTime.TryParse( g.UpdatedAt, out var ua ) ? ua : DateTime.UtcNow
		};

		Membership = data.Membership != null ? new GuildMembership
		{
			GuildId = data.Membership.GuildId,
			GuildName = data.Membership.GuildName,
			GuildTag = data.Membership.GuildTag,
			Role = (GuildRole)data.Membership.Role,
			JoinedAt = DateTime.TryParse( data.Membership.JoinedAt, out var mj ) ? mj : DateTime.UtcNow
		} : null;

		Members = data.Members?.Select( m => new GuildMemberInfo
		{
			SteamId = long.TryParse( m.SteamId, out var sid ) ? sid : 0,
			Name = m.Name,
			Role = (GuildRole)m.Role,
			Level = m.Level,
			ArenaPoints = m.ArenaPoints,
			ArenaRank = m.ArenaRank ?? "Unranked",
			JoinedAt = DateTime.TryParse( m.JoinedAt, out var ja ) ? ja : DateTime.UtcNow,
			LastSeen = DateTime.TryParse( m.LastSeen, out var ls ) ? ls : DateTime.UtcNow,
			WeeklyRP = m.WeeklyRp,
			WeeklyGuildXP = m.WeeklyGuildXp,
			WeeklyRaidDamage = m.WeeklyRaidDamage,
			BestRaidScore = m.BestRaidScore
		} ).ToList() ?? new();

		ActivityLog = data.Log?.Select( l => new GuildLogEntry
		{
			Timestamp = DateTime.TryParse( l.Timestamp, out var lt ) ? lt : DateTime.UtcNow,
			Action = l.Action,
			PlayerName = l.PlayerName,
			Details = l.Details
		} ).ToList() ?? new();

		JoinRequests = data.Requests?.Select( r => new GuildJoinRequest
		{
			SteamId = long.TryParse( r.SteamId, out var rid ) ? rid : 0,
			PlayerName = r.PlayerName,
			Level = r.Level,
			ArenaRank = r.ArenaRank ?? "Unranked",
			ArenaPoints = r.ArenaPoints,
			RequestedAt = DateTime.TryParse( r.RequestedAt, out var ra ) ? ra : DateTime.UtcNow
		} ).ToList() ?? new();
	}

	private void ApplyGuildDetail( GuildDetailResponse data )
	{
		var g = data.Guild;
		Guild = new GuildDefinition
		{
			Id = g.Id,
			Name = g.Name,
			Tag = g.Tag,
			Level = g.Level,
			GuildXP = g.GuildXp,
			EmblemColor = g.EmblemColor ?? "#7c3aed",
			EmblemIcon = g.EmblemIcon ?? "dragon",
			EmblemShape = g.EmblemShape ?? "square",
			Description = g.Description ?? "",
			Motd = g.Motd ?? "",
			JoinMode = Enum.TryParse<GuildJoinMode>( g.JoinMode, out var jm ) ? jm : GuildJoinMode.Request,
			MinLevel = g.MinLevel,
			MinRank = g.MinRank ?? "Unranked",
			AutoKickInactive = g.AutoKickInactive == 1,
			InactiveDays = g.InactiveDays,
			MaxMembers = g.MaxMembers,
			OwnerSteamId = long.TryParse( g.OwnerSteamId, out var oid ) ? oid : 0,
			OwnerName = g.OwnerName ?? "",
			TotalCatches = g.TotalCatches,
			TotalArenaWins = g.TotalArenaWins,
			TotalExpeditions = g.TotalExpeditions,
			TotalRaidsCompleted = g.TotalRaidsCompleted,
			CreatedAt = DateTime.TryParse( g.CreatedAt, out var ca ) ? ca : DateTime.UtcNow,
			LastUpdated = DateTime.TryParse( g.UpdatedAt, out var ua ) ? ua : DateTime.UtcNow
		};

		Members = data.Members?.Select( m => new GuildMemberInfo
		{
			SteamId = long.TryParse( m.SteamId, out var sid ) ? sid : 0,
			Name = m.Name,
			Role = (GuildRole)m.Role,
			Level = m.Level,
			ArenaPoints = m.ArenaPoints,
			ArenaRank = m.ArenaRank ?? "Unranked",
			JoinedAt = DateTime.TryParse( m.JoinedAt, out var ja ) ? ja : DateTime.UtcNow,
			LastSeen = DateTime.TryParse( m.LastSeen, out var ls ) ? ls : DateTime.UtcNow,
			WeeklyRP = m.WeeklyRp,
			WeeklyGuildXP = m.WeeklyGuildXp,
			WeeklyRaidDamage = m.WeeklyRaidDamage,
			BestRaidScore = m.BestRaidScore
		} ).ToList() ?? new();

		ActivityLog = data.Log?.Select( l => new GuildLogEntry
		{
			Timestamp = DateTime.TryParse( l.Timestamp, out var lt ) ? lt : DateTime.UtcNow,
			Action = l.Action,
			PlayerName = l.PlayerName,
			Details = l.Details
		} ).ToList() ?? new();

		JoinRequests = data.Requests?.Select( r => new GuildJoinRequest
		{
			SteamId = long.TryParse( r.SteamId, out var rid ) ? rid : 0,
			PlayerName = r.PlayerName,
			Level = r.Level,
			ArenaRank = r.ArenaRank ?? "Unranked",
			ArenaPoints = r.ArenaPoints,
			RequestedAt = DateTime.TryParse( r.RequestedAt, out var ra ) ? ra : DateTime.UtcNow
		} ).ToList() ?? new();

		// Restore online status from RPC-tracked data
		var selfSteamId = Connection.Local?.SteamId ?? 0;
		var self = Members.FirstOrDefault( m => m.SteamId == selfSteamId );
		if ( self != null )
		{
			self.IsOnline = true;
			self.LastSeen = DateTime.UtcNow;
		}

		// Update membership from member record
		var myMember = Members.FirstOrDefault( m => m.SteamId == selfSteamId );
		if ( myMember != null && Membership != null )
		{
			Membership.Role = myMember.Role;
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// GUILD CREATION
	// ═══════════════════════════════════════════════════════════════

	public bool CreateGuild( string name, string tag )
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return false;

		if ( IsInGuild )
		{
			OnGuildError?.Invoke( "You are already in a guild." );
			return false;
		}

		if ( tamer.Level < MIN_LEVEL_TO_JOIN )
		{
			OnGuildError?.Invoke( $"You must be Level {MIN_LEVEL_TO_JOIN} to create a guild." );
			return false;
		}

		if ( tamer.Gold < GUILD_CREATION_COST )
		{
			OnGuildError?.Invoke( $"You need {GUILD_CREATION_COST:N0} gold to create a guild." );
			return false;
		}

		if ( IsOnHopCooldown() )
		{
			OnGuildError?.Invoke( $"You must wait before joining or creating another guild." );
			return false;
		}

		name = name?.Trim() ?? "";
		tag = tag?.Trim().ToUpperInvariant() ?? "";

		if ( name.Length < 3 || name.Length > 24 )
		{
			OnGuildError?.Invoke( "Guild name must be 3-24 characters." );
			return false;
		}

		if ( tag.Length < 2 || tag.Length > 5 )
		{
			OnGuildError?.Invoke( "Guild tag must be 2-5 characters." );
			return false;
		}

		// Deduct gold
		tamer.Gold -= GUILD_CREATION_COST;
		TamerManager.Instance?.OnGoldChanged?.Invoke( tamer.Gold );

		_ = CreateGuildAsync( name, tag, tamer );
		return true;
	}

	private async Task CreateGuildAsync( string name, string tag, Beastborne.Data.Tamer tamer )
	{
		var result = await GuildApiClient.PostAsync<CreateGuildResponse>( "guilds", new
		{
			name,
			tag,
			ownerName = tamer.Name,
			level = tamer.Level,
			arenaPoints = tamer.ArenaPoints,
			arenaRank = CompetitiveManager.GetRankFromPoints( tamer.ArenaPoints )
		} );

		if ( result?.Guild == null )
		{
			// Refund gold on failure
			tamer.Gold += GUILD_CREATION_COST;
			TamerManager.Instance?.OnGoldChanged?.Invoke( tamer.Gold );
			OnGuildError?.Invoke( "Failed to create guild. Please try again." );
			return;
		}

		// Load full guild data from API
		var steamId = Connection.Local?.SteamId ?? 0;
		var fullData = await GuildApiClient.GetAsync<MyGuildResponse>( $"players/{steamId}/guild" );
		if ( fullData != null )
			ApplyApiData( fullData );

		EnsureRaidBoss();
		BroadcastPresence();

		SoundManager.PlaySuccess();
		NotificationManager.Instance?.AddNotification( NotificationType.Success, "Guild Created!", $"Welcome to {name} [{tag}]!", 5f );
		OnGuildJoined?.Invoke();
		OnGuildUpdated?.Invoke();
		OnMembersUpdated?.Invoke();

		ChatManager.Instance?.SendPlayerProfile();
		Log.Info( $"GuildManager: Created guild {name} [{tag}] (ID: {result.Guild.Id})" );
	}

	// ═══════════════════════════════════════════════════════════════
	// LEAVE / DISBAND
	// ═══════════════════════════════════════════════════════════════

	public bool LeaveGuild()
	{
		if ( !IsInGuild ) return false;

		if ( IsLeader && Members.Count > 1 )
		{
			OnGuildError?.Invoke( "Transfer ownership before leaving. You are the leader." );
			return false;
		}

		var steamId = Connection.Local?.SteamId ?? 0;
		var connId = Connection.Local?.Id.ToString() ?? "";
		var guildId = Guild.Id;
		var playerName = TamerManager.Instance?.CurrentTamer?.Name ?? "Unknown";

		_ = LeaveGuildAsync( steamId, connId, guildId, playerName );
		return true;
	}

	private async Task LeaveGuildAsync( long steamId, string connId, string guildId, string playerName )
	{
		var success = await GuildApiClient.DeleteAsync( $"guilds/{guildId}/members/{steamId}" );
		if ( !success )
		{
			OnGuildError?.Invoke( "Failed to leave guild. Please try again." );
			return;
		}

		BroadcastMemberLeft( connId, steamId, guildId, playerName );
		ClearGuildData();
		SetHopCooldown();

		SoundManager.PlayBack();
		NotificationManager.Instance?.AddNotification( NotificationType.Info, "Left Guild", "You have left the guild.", 5f );
		OnGuildLeft?.Invoke();
		ChatManager.Instance?.SendPlayerProfile();
	}

	public bool DisbandGuild()
	{
		if ( !IsInGuild || !IsLeader ) return false;

		var connId = Connection.Local?.Id.ToString() ?? "";
		var guildId = Guild.Id;
		var guildName = Guild.Name;

		_ = DisbandGuildAsync( connId, guildId, guildName );
		return true;
	}

	private async Task DisbandGuildAsync( string connId, string guildId, string guildName )
	{
		// Broadcast disband to online members before deleting
		foreach ( var member in Members.Where( m => m.SteamId != (Connection.Local?.SteamId ?? 0) ) )
		{
			BroadcastMemberKicked( connId, guildId, member.SteamId, member.Name, "Guild disbanded" );
		}

		var success = await GuildApiClient.DeleteAsync( $"guilds/{guildId}" );
		if ( !success )
		{
			OnGuildError?.Invoke( "Failed to disband guild. Please try again." );
			return;
		}

		ClearGuildData();

		SoundManager.PlayBack();
		NotificationManager.Instance?.AddNotification( NotificationType.Warning, "Guild Disbanded", $"{guildName} has been disbanded.", 5f );
		OnGuildLeft?.Invoke();
		ChatManager.Instance?.SendPlayerProfile();
	}

	private void ClearGuildData()
	{
		Membership = null;
		Guild = null;
		Members.Clear();
		ActivityLog.Clear();
		JoinRequests.Clear();
		PendingInvites.Clear();
		CurrentRaidBoss = null;
		OnGuildUpdated?.Invoke();
		OnMembersUpdated?.Invoke();
	}

	// ═══════════════════════════════════════════════════════════════
	// INVITES
	// ═══════════════════════════════════════════════════════════════

	public void InvitePlayer( string targetConnectionId, string targetName, long targetSteamId )
	{
		if ( !IsInGuild || !IsOfficer ) return;
		if ( Members.Count >= MAX_MEMBERS )
		{
			OnGuildError?.Invoke( "Guild is full." );
			return;
		}

		_ = InvitePlayerAsync( targetConnectionId, targetName, targetSteamId );
	}

	private async Task InvitePlayerAsync( string targetConnectionId, string targetName, long targetSteamId )
	{
		var playerName = TamerManager.Instance?.CurrentTamer?.Name ?? "Unknown";
		var steamId = Connection.Local?.SteamId ?? 0;

		// Create invite record in API
		var result = await GuildApiClient.PostAsync( $"guilds/{Guild.Id}/invites", new
		{
			targetSteamId = targetSteamId.ToString(),
			targetName
		} );

		if ( !result )
		{
			OnGuildError?.Invoke( "Failed to send invite." );
			return;
		}

		// Send real-time RPC notification to target
		var connId = Connection.Local?.Id.ToString() ?? "";
		BroadcastGuildInvite( targetConnectionId, connId, Guild.Id, Guild.Name, Guild.Tag, playerName, steamId );
		SoundManager.PlayForward();
	}

	public void AcceptInvite( GuildInvite invite )
	{
		if ( IsInGuild ) return;
		if ( IsOnHopCooldown() )
		{
			OnGuildError?.Invoke( "You must wait before joining another guild." );
			return;
		}

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null || tamer.Level < MIN_LEVEL_TO_JOIN ) return;

		_ = AcceptInviteAsync( invite );
	}

	private async Task AcceptInviteAsync( GuildInvite invite )
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		var steamId = Connection.Local?.SteamId ?? 0;

		// If we have the API ID, use it. Otherwise, join directly via POST /members.
		bool success;
		if ( invite.ApiId > 0 )
		{
			var result = await GuildApiClient.PostAsync<AcceptInviteResponse>( $"invites/{invite.ApiId}/accept", new
			{
				name = tamer.Name,
				level = tamer.Level,
				arenaPoints = tamer.ArenaPoints,
				arenaRank = CompetitiveManager.GetRankFromPoints( tamer.ArenaPoints )
			} );
			success = result?.Success ?? false;
		}
		else
		{
			// Fallback: join guild directly (for RPC-only invites)
			var result = await GuildApiClient.PostAsync<MembersResponse>( $"guilds/{invite.GuildId}/members", new
			{
				name = tamer.Name,
				level = tamer.Level,
				arenaPoints = tamer.ArenaPoints,
				arenaRank = CompetitiveManager.GetRankFromPoints( tamer.ArenaPoints )
			} );
			success = result?.Success ?? (result?.Members?.Count > 0);
		}

		PendingInvites.Remove( invite );

		if ( !success )
		{
			OnGuildError?.Invoke( "Failed to accept invite." );
			return;
		}

		// Load full guild data from API
		var fullData = await GuildApiClient.GetAsync<MyGuildResponse>( $"players/{steamId}/guild" );
		if ( fullData?.Guild != null )
		{
			ApplyApiData( fullData );
			EnsureRaidBoss();
			BroadcastPresence();

			SoundManager.PlaySuccess();
			NotificationManager.Instance?.AddNotification( NotificationType.Success, "Joined Guild!",
				$"Welcome to [{Guild.Tag}] {Guild.Name}!", 5f );
			OnGuildJoined?.Invoke();
			OnGuildUpdated?.Invoke();
			OnMembersUpdated?.Invoke();
			ChatManager.Instance?.SendPlayerProfile();
		}
	}

	public void DeclineInvite( GuildInvite invite )
	{
		PendingInvites.Remove( invite );

		// Delete from API if we have the ID
		if ( invite.ApiId > 0 )
			_ = GuildApiClient.DeleteAsync( $"invites/{invite.ApiId}" );

		SoundManager.PlayBack();
	}

	// ═══════════════════════════════════════════════════════════════
	// JOIN REQUESTS
	// ═══════════════════════════════════════════════════════════════

	public void RequestToJoin( GuildAdvertisement guild )
	{
		if ( IsInGuild ) return;
		if ( IsOnHopCooldown() )
		{
			OnGuildError?.Invoke( "You must wait before joining another guild." );
			return;
		}

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null || tamer.Level < MIN_LEVEL_TO_JOIN ) return;

		if ( tamer.Level < guild.MinLevel )
		{
			OnGuildError?.Invoke( $"Requires Level {guild.MinLevel}." );
			return;
		}

		if ( guild.JoinMode == GuildJoinMode.Open )
		{
			_ = JoinGuildDirectAsync( guild.GuildId, guild.GuildName, guild.GuildTag );
		}
		else if ( guild.JoinMode == GuildJoinMode.Request )
		{
			_ = SubmitJoinRequestAsync( guild.GuildId, guild.GuildName );
		}
		else
		{
			OnGuildError?.Invoke( "This guild is invite only." );
		}
	}

	private async Task JoinGuildDirectAsync( string guildId, string guildName, string guildTag )
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		var steamId = Connection.Local?.SteamId ?? 0;

		var result = await GuildApiClient.PostAsync<MembersResponse>( $"guilds/{guildId}/members", new
		{
			name = tamer.Name,
			level = tamer.Level,
			arenaPoints = tamer.ArenaPoints,
			arenaRank = CompetitiveManager.GetRankFromPoints( tamer.ArenaPoints )
		} );

		if ( result == null )
		{
			OnGuildError?.Invoke( "Failed to join guild." );
			return;
		}

		// Load full guild data from API
		var fullData = await GuildApiClient.GetAsync<MyGuildResponse>( $"players/{steamId}/guild" );
		if ( fullData?.Guild != null )
		{
			ApplyApiData( fullData );
			EnsureRaidBoss();
			BroadcastPresence();

			SoundManager.PlaySuccess();
			NotificationManager.Instance?.AddNotification( NotificationType.Success, "Joined Guild!",
				$"Welcome to [{guildTag}] {guildName}!", 5f );
			OnGuildJoined?.Invoke();
			OnGuildUpdated?.Invoke();
			OnMembersUpdated?.Invoke();
			ChatManager.Instance?.SendPlayerProfile();
		}
	}

	private async Task SubmitJoinRequestAsync( string guildId, string guildName )
	{
		var tamer = TamerManager.Instance?.CurrentTamer;

		var success = await GuildApiClient.PostAsync( $"guilds/{guildId}/requests", new
		{
			playerName = tamer.Name,
			level = tamer.Level,
			arenaPoints = tamer.ArenaPoints,
			arenaRank = CompetitiveManager.GetRankFromPoints( tamer.ArenaPoints )
		} );

		if ( !success )
		{
			OnGuildError?.Invoke( "Failed to submit join request." );
			return;
		}

		// Also broadcast via RPC for real-time notification to online officers
		var connId = Connection.Local?.Id.ToString() ?? "";
		var steamId = Connection.Local?.SteamId ?? 0;
		BroadcastJoinRequest( connId, steamId, tamer.Name, tamer.Level,
			CompetitiveManager.GetRankFromPoints( tamer.ArenaPoints ), tamer.ArenaPoints,
			guildId );

		SoundManager.PlayForward();
		NotificationManager.Instance?.AddNotification( NotificationType.Info, "Request Sent", $"Join request sent to {guildName}.", 5f );
	}

	public void ApproveJoinRequest( GuildJoinRequest request )
	{
		if ( !IsInGuild || !IsOfficer ) return;
		if ( Members.Count >= MAX_MEMBERS )
		{
			OnGuildError?.Invoke( "Guild is full." );
			return;
		}

		_ = ApproveJoinRequestAsync( request );
	}

	private async Task ApproveJoinRequestAsync( GuildJoinRequest request )
	{
		var success = await GuildApiClient.PostAsync( $"guilds/{Guild.Id}/requests/{request.SteamId}/approve" );
		if ( !success )
		{
			OnGuildError?.Invoke( "Failed to approve request." );
			return;
		}

		JoinRequests.Remove( request );
		await RefreshGuildData();
		BroadcastRefreshGuild( Connection.Local?.Id.ToString() ?? "", Guild.Id );

		SoundManager.PlaySuccess();
		NotificationManager.Instance?.AddNotification( NotificationType.Success, "Request Approved",
			$"{request.PlayerName} has joined the guild!", 5f );
	}

	public void DenyJoinRequest( GuildJoinRequest request )
	{
		if ( !IsInGuild || !IsOfficer ) return;
		_ = DenyJoinRequestAsync( request );
	}

	private async Task DenyJoinRequestAsync( GuildJoinRequest request )
	{
		await GuildApiClient.DeleteAsync( $"guilds/{Guild.Id}/requests/{request.SteamId}" );
		JoinRequests.Remove( request );
		OnGuildUpdated?.Invoke();
		SoundManager.PlayBack();
	}

	// ═══════════════════════════════════════════════════════════════
	// MEMBER MANAGEMENT
	// ═══════════════════════════════════════════════════════════════

	public void PromoteMember( long steamId )
	{
		if ( !IsInGuild || !IsLeader ) return;

		var member = Members.FirstOrDefault( m => m.SteamId == steamId );
		if ( member == null || member.Role >= GuildRole.Warden ) return;

		var newRole = (GuildRole)((int)member.Role + 1);
		_ = UpdateMemberRoleAsync( steamId, newRole, member.Name );
	}

	public void DemoteMember( long steamId )
	{
		if ( !IsInGuild || !IsLeader ) return;

		var member = Members.FirstOrDefault( m => m.SteamId == steamId );
		if ( member == null || member.Role <= GuildRole.Wanderer ) return;

		var newRole = (GuildRole)((int)member.Role - 1);
		_ = UpdateMemberRoleAsync( steamId, newRole, member.Name );
	}

	private async Task UpdateMemberRoleAsync( long steamId, GuildRole newRole, string memberName )
	{
		var result = await GuildApiClient.PatchAsync<MembersResponse>( $"guilds/{Guild.Id}/members/{steamId}", new
		{
			role = (int)newRole
		} );

		if ( result == null )
		{
			OnGuildError?.Invoke( "Failed to update role." );
			return;
		}

		await RefreshGuildData();
		BroadcastRefreshGuild( Connection.Local?.Id.ToString() ?? "", Guild.Id );
	}

	public void KickMember( long steamId )
	{
		if ( !IsInGuild || !IsOfficer ) return;

		var member = Members.FirstOrDefault( m => m.SteamId == steamId );
		if ( member == null ) return;

		// Can't kick equal or higher rank
		if ( member.Role >= Membership.Role ) return;

		_ = KickMemberAsync( steamId, member.Name );
	}

	private async Task KickMemberAsync( long steamId, string memberName )
	{
		var connId = Connection.Local?.Id.ToString() ?? "";
		var kickerName = TamerManager.Instance?.CurrentTamer?.Name ?? "Officer";

		// Notify the kicked player via RPC first (so they see it immediately)
		BroadcastMemberKicked( connId, Guild.Id, steamId, memberName, kickerName );

		var success = await GuildApiClient.DeleteAsync( $"guilds/{Guild.Id}/members/{steamId}" );
		if ( !success )
		{
			OnGuildError?.Invoke( "Failed to kick member." );
			return;
		}

		await RefreshGuildData();
	}

	public void TransferOwnership( long steamId )
	{
		if ( !IsInGuild || !IsLeader ) return;

		var member = Members.FirstOrDefault( m => m.SteamId == steamId );
		if ( member == null ) return;

		_ = TransferOwnershipAsync( steamId, member.Name );
	}

	private async Task TransferOwnershipAsync( long targetSteamId, string targetName )
	{
		// PATCH with role=3 (Beastlord) triggers server-side ownership transfer
		var result = await GuildApiClient.PatchAsync<MembersResponse>( $"guilds/{Guild.Id}/members/{targetSteamId}", new
		{
			role = (int)GuildRole.Beastlord
		} );

		if ( result == null )
		{
			OnGuildError?.Invoke( "Failed to transfer ownership." );
			return;
		}

		await RefreshGuildData();
		BroadcastRefreshGuild( Connection.Local?.Id.ToString() ?? "", Guild.Id );

		SoundManager.PlaySuccess();
		NotificationManager.Instance?.AddNotification( NotificationType.Info, "Ownership Transferred",
			$"{targetName} is now the Beastlord.", 5f );
	}

	public void ClaimLeadership()
	{
		if ( !IsInGuild || IsLeader ) return;

		var leader = Members.FirstOrDefault( m => m.Role == GuildRole.Beastlord );
		if ( leader == null ) return;

		// Check if leader has been inactive for 30+ days
		if ( (DateTime.UtcNow - leader.LastSeen).TotalDays < LEADER_INACTIVE_DAYS ) return;

		// Check if caller is the highest-ranking online member
		var selfSteamId = Connection.Local?.SteamId ?? 0;
		var self = Members.FirstOrDefault( m => m.SteamId == selfSteamId );
		if ( self == null ) return;

		var highestOnline = Members
			.Where( m => m.IsOnline && m.SteamId != leader.SteamId )
			.OrderByDescending( m => m.Role )
			.ThenBy( m => m.JoinedAt )
			.FirstOrDefault();

		if ( highestOnline?.SteamId != selfSteamId ) return;

		_ = ClaimLeadershipAsync( selfSteamId, self.Name, leader.Name );
	}

	private async Task ClaimLeadershipAsync( long selfSteamId, string selfName, string oldLeaderName )
	{
		// PATCH with role=3 (Beastlord) triggers server-side ownership transfer
		var result = await GuildApiClient.PatchAsync<MembersResponse>( $"guilds/{Guild.Id}/members/{selfSteamId}", new
		{
			role = (int)GuildRole.Beastlord
		} );

		if ( result == null )
		{
			OnGuildError?.Invoke( "Failed to claim leadership." );
			return;
		}

		await RefreshGuildData();
		BroadcastRefreshGuild( Connection.Local?.Id.ToString() ?? "", Guild.Id );

		SoundManager.PlaySuccess();
		NotificationManager.Instance?.AddNotification( NotificationType.Success, "Leadership Claimed",
			$"You are now the Beastlord!", 5f );
	}

	public bool CanClaimLeadership()
	{
		if ( !IsInGuild || IsLeader ) return false;
		var leader = Members.FirstOrDefault( m => m.Role == GuildRole.Beastlord );
		if ( leader == null ) return false;
		if ( (DateTime.UtcNow - leader.LastSeen).TotalDays < LEADER_INACTIVE_DAYS ) return false;

		var selfSteamId = Connection.Local?.SteamId ?? 0;
		var highestOnline = Members
			.Where( m => m.IsOnline && m.SteamId != leader.SteamId )
			.OrderByDescending( m => m.Role )
			.ThenBy( m => m.JoinedAt )
			.FirstOrDefault();

		return highestOnline?.SteamId == selfSteamId;
	}

	// ═══════════════════════════════════════════════════════════════
	// GUILD SETTINGS
	// ═══════════════════════════════════════════════════════════════

	public void UpdateGuildSettings( GuildJoinMode joinMode, int minLevel, string minRank, bool autoKick, int inactiveDays, string description )
	{
		if ( !IsInGuild || !IsOfficer ) return;
		_ = UpdateGuildSettingsAsync( joinMode, minLevel, minRank, autoKick, inactiveDays, description );
	}

	private async Task UpdateGuildSettingsAsync( GuildJoinMode joinMode, int minLevel, string minRank, bool autoKick, int inactiveDays, string description )
	{
		var result = await GuildApiClient.PutAsync<UpdateGuildResponse>( $"guilds/{Guild.Id}", new
		{
			joinMode = joinMode.ToString(),
			minLevel,
			minRank,
			autoKickInactive = autoKick ? 1 : 0,
			inactiveDays,
			description
		} );

		if ( result?.Guild == null )
		{
			OnGuildError?.Invoke( "Failed to update settings." );
			return;
		}

		await RefreshGuildData();
		BroadcastRefreshGuild( Connection.Local?.Id.ToString() ?? "", Guild.Id );
	}

	public void UpdateMotd( string motd )
	{
		if ( !IsInGuild || !IsOfficer ) return;
		_ = UpdateMotdAsync( motd );
	}

	private async Task UpdateMotdAsync( string motd )
	{
		var result = await GuildApiClient.PutAsync<UpdateGuildResponse>( $"guilds/{Guild.Id}", new
		{
			motd
		} );

		if ( result?.Guild == null )
		{
			OnGuildError?.Invoke( "Failed to update MOTD." );
			return;
		}

		await RefreshGuildData();
		BroadcastRefreshGuild( Connection.Local?.Id.ToString() ?? "", Guild.Id );
	}

	public void UpdateEmblem( string color, string icon, string shape )
	{
		if ( !IsInGuild || !IsOfficer ) return;
		_ = UpdateEmblemAsync( color, icon, shape );
	}

	private async Task UpdateEmblemAsync( string color, string icon, string shape )
	{
		var result = await GuildApiClient.PutAsync<UpdateGuildResponse>( $"guilds/{Guild.Id}", new
		{
			emblemColor = color,
			emblemIcon = icon,
			emblemShape = shape
		} );

		if ( result?.Guild == null )
		{
			OnGuildError?.Invoke( "Failed to update emblem." );
			return;
		}

		await RefreshGuildData();
		BroadcastRefreshGuild( Connection.Local?.Id.ToString() ?? "", Guild.Id );
	}

	// ═══════════════════════════════════════════════════════════════
	// GUILD XP & LEVELING
	// ═══════════════════════════════════════════════════════════════

	public static long GetXPForGuildLevel( int level )
	{
		return 500 + (long)level * level * 50;
	}

	public void AddGuildXP( int amount )
	{
		if ( !IsInGuild || Guild == null ) return;
		_ = AddGuildXPAsync( amount );
	}

	private async Task AddGuildXPAsync( int amount )
	{
		var result = await GuildApiClient.PostAsync<XpResponse>( $"guilds/{Guild.Id}/xp", new
		{
			amount,
			source = "gameplay"
		} );

		if ( result == null ) return;

		if ( result.LeveledUp )
		{
			SoundManager.PlaySuccess();
			NotificationManager.Instance?.AddNotification( NotificationType.Success, "Guild Level Up!",
				$"Your guild is now Level {result.Level}!", 5f );
		}

		// Update local state from API response
		Guild.GuildXP = result.GuildXp;
		Guild.Level = result.Level;

		// Track weekly XP locally (will sync via heartbeat)
		var selfSteamId = Connection.Local?.SteamId ?? 0;
		var self = Members.FirstOrDefault( m => m.SteamId == selfSteamId );
		if ( self != null )
			self.WeeklyGuildXP += amount;

		OnGuildUpdated?.Invoke();
	}

	public void AddWeeklyRP( int amount )
	{
		if ( !IsInGuild || amount <= 0 ) return;
		var selfSteamId = Connection.Local?.SteamId ?? 0;
		var self = Members.FirstOrDefault( m => m.SteamId == selfSteamId );
		if ( self != null )
			self.WeeklyRP += amount;
		// Weekly RP is synced to server via heartbeat
	}

	/// <summary>
	/// Get the guild XP perk multiplier for tamer XP (Lv4: +5%, Lv10: +15%)
	/// </summary>
	public float GetTamerXPMultiplier()
	{
		if ( !IsInGuild || Guild == null ) return 1.0f;
		float bonus = 0f;
		if ( Guild.Level >= 4 ) bonus += 0.05f;
		if ( Guild.Level >= 10 ) bonus += 0.10f;
		return 1.0f + bonus;
	}

	/// <summary>
	/// Get the guild gold perk multiplier (Lv2: +5% expedition, Lv8: +10% all, Lv15: +25% all)
	/// </summary>
	public float GetGoldMultiplier( bool isExpedition = false )
	{
		if ( !IsInGuild || Guild == null ) return 1.0f;
		float bonus = 0f;
		if ( isExpedition && Guild.Level >= 2 ) bonus += 0.05f;
		if ( Guild.Level >= 8 ) bonus += 0.10f;
		if ( Guild.Level >= 15 ) bonus += 0.15f;
		return 1.0f + bonus;
	}

	/// <summary>
	/// Get the guild catch rate bonus (Lv6: +5%)
	/// </summary>
	public float GetCatchRateBonus()
	{
		if ( !IsInGuild || Guild == null ) return 0f;
		if ( Guild.Level >= 6 ) return 5f;
		return 0f;
	}

	/// <summary>
	/// Get the guild beast XP multiplier (Lv12: +10%)
	/// </summary>
	public float GetBeastXPMultiplier()
	{
		if ( !IsInGuild || Guild == null ) return 1.0f;
		if ( Guild.Level >= 12 ) return 1.10f;
		return 1.0f;
	}

	// ═══════════════════════════════════════════════════════════════
	// GUILD ACHIEVEMENTS
	// ═══════════════════════════════════════════════════════════════

	public static readonly List<GuildAchievement> AllAchievements = new()
	{
		new GuildAchievement { Id = "first-hundred", Name = "First Hundred", Description = "Guild catches 100 monsters", Requirement = 100, GuildXPReward = 1000, Icon = "catch" },
		new GuildAchievement { Id = "arena-warriors", Name = "Arena Warriors", Description = "Guild wins 50 arena sets", Requirement = 50, GuildXPReward = 2000, Icon = "arena" },
		new GuildAchievement { Id = "expedition-force", Name = "Expedition Force", Description = "Complete 100 expeditions", Requirement = 100, GuildXPReward = 1500, Icon = "expedition" },
		new GuildAchievement { Id = "raid-slayer", Name = "Raid Slayer", Description = "Complete 5 raid boss periods", Requirement = 5, GuildXPReward = 2500, Icon = "raid" },
		new GuildAchievement { Id = "full-house", Name = "Full House", Description = "Reach 30/30 members", Requirement = 30, GuildXPReward = 500, Icon = "members" }
	};

	public void IncrementAchievement( string type, int amount = 1 )
	{
		if ( !IsInGuild || Guild == null ) return;

		// Update local state immediately for responsiveness
		switch ( type )
		{
			case "catch": Guild.TotalCatches += amount; break;
			case "arena": Guild.TotalArenaWins += amount; break;
			case "expedition": Guild.TotalExpeditions += amount; break;
			case "raid": Guild.TotalRaidsCompleted += amount; break;
		}

		// Sync to API
		_ = GuildApiClient.PostAsync( $"guilds/{Guild.Id}/stats", new { type, amount } );
	}

	public int GetAchievementProgress( string achievementId )
	{
		if ( Guild == null ) return 0;
		return achievementId switch
		{
			"first-hundred" => Guild.TotalCatches,
			"arena-warriors" => Guild.TotalArenaWins,
			"expedition-force" => Guild.TotalExpeditions,
			"raid-slayer" => Guild.TotalRaidsCompleted,
			"full-house" => Members.Count,
			_ => 0
		};
	}

	public bool IsAchievementComplete( string achievementId )
	{
		var achievement = AllAchievements.FirstOrDefault( a => a.Id == achievementId );
		if ( achievement == null ) return false;
		return GetAchievementProgress( achievementId ) >= achievement.Requirement;
	}

	// ═══════════════════════════════════════════════════════════════
	// RAID BOSS
	// ═══════════════════════════════════════════════════════════════

	private static readonly DateTime RaidEpoch = new DateTime( 2026, 1, 1, 0, 0, 0, DateTimeKind.Utc );

	public int GetCurrentPeriodNumber()
	{
		return (int)(DateTime.UtcNow - RaidEpoch).TotalDays / 14;
	}

	public DateTime GetPeriodEndDate()
	{
		var period = GetCurrentPeriodNumber();
		return RaidEpoch.AddDays( (period + 1) * 14 );
	}

	private void EnsureRaidBoss()
	{
		var currentPeriod = GetCurrentPeriodNumber();
		if ( CurrentRaidBoss != null && CurrentRaidBoss.PeriodNumber == currentPeriod )
			return;

		// New period — create new boss locally (API also handles this)
		var bossIndex = currentPeriod % RaidBossSpecies.Length;
		var bossSpeciesId = RaidBossSpecies[bossIndex];
		var species = MonsterManager.Instance?.GetSpecies( bossSpeciesId );

		CurrentRaidBoss = new GuildRaidBoss
		{
			BossSpeciesId = bossSpeciesId,
			BossName = species?.Name ?? bossSpeciesId,
			BossElement = species?.Element ?? ElementType.Neutral,
			BossLevel = 100,
			MaxRounds = 10,
			PeriodNumber = currentPeriod,
			BestScores = new()
		};

		// Load scores from API
		if ( IsInGuild && Guild != null )
			_ = LoadRaidFromApi();

		Log.Info( $"GuildManager: New raid boss: {CurrentRaidBoss.BossName} (Period {currentPeriod})" );
	}

	/// <summary>
	/// Load raid data (scores, attempt count) from the API server.
	/// </summary>
	private async Task LoadRaidFromApi()
	{
		if ( Guild == null ) return;

		var result = await GuildApiClient.GetAsync<RaidResponse>( $"guilds/{Guild.Id}/raid" );
		if ( result == null ) return;

		cachedRaidAttemptsToday = result.TodayAttempts;

		if ( result.Scores != null && CurrentRaidBoss != null )
		{
			CurrentRaidBoss.BestScores.Clear();
			foreach ( var score in result.Scores )
			{
				var sid = long.TryParse( score.SteamId, out var s ) ? s : 0;
				CurrentRaidBoss.BestScores[sid] = new RaidAttemptResult
				{
					SteamId = sid,
					PlayerName = score.PlayerName,
					TotalScore = score.TotalScore,
					RawDamage = score.RawDamage,
					ComboMultiplier = score.ComboMultiplier,
					RoundsUsed = score.RoundsUsed,
					AttemptedAt = DateTime.TryParse( score.AttemptedAt, out var at ) ? at : DateTime.UtcNow
				};
			}
		}

		OnGuildUpdated?.Invoke();
	}

	/// <summary>
	/// Browse guilds from the API server (for players not in a guild).
	/// </summary>
	public async Task<List<GuildAdvertisement>> BrowseGuildsFromApi( string search = "", int page = 1 )
	{
		var path = $"guilds?page={page}";
		if ( !string.IsNullOrEmpty( search ) )
			path += $"&search={Uri.EscapeDataString( search )}";

		var result = await GuildApiClient.GetAsync<GuildListResponse>( path );
		if ( result?.Guilds == null ) return new();

		return result.Guilds.Select( g => new GuildAdvertisement
		{
			GuildId = g.Id,
			GuildName = g.Name,
			GuildTag = g.Tag,
			Level = g.Level,
			MemberCount = g.MemberCount,
			EmblemColor = g.EmblemColor ?? "#7c3aed",
			EmblemIcon = g.EmblemIcon ?? "dragon",
			EmblemShape = g.EmblemShape ?? "square",
			JoinMode = Enum.TryParse<GuildJoinMode>( g.JoinMode, out var jm ) ? jm : GuildJoinMode.Request,
			MinLevel = g.MinLevel,
			MinRank = g.MinRank ?? "Unranked",
			OwnerName = g.OwnerName ?? "",
			Description = g.Description ?? ""
		} ).ToList();
	}

	/// <summary>
	/// Load pending invites for the current player from the API.
	/// </summary>
	public async Task LoadPendingInvites()
	{
		var steamId = Connection.Local?.SteamId ?? 0;
		if ( steamId == 0 ) return;

		var result = await GuildApiClient.GetAsync<InvitesResponse>( $"players/{steamId}/invites" );
		if ( result?.Invites == null ) return;

		PendingInvites = result.Invites.Select( i => new GuildInvite
		{
			ApiId = i.Id,
			GuildId = i.GuildId,
			GuildName = i.GuildName,
			GuildTag = i.GuildTag,
			InviterName = i.InviterName,
			InviterSteamId = long.TryParse( i.InviterSteamId, out var sid ) ? sid : 0,
			SentAt = DateTime.TryParse( i.SentAt, out var sa ) ? sa : DateTime.UtcNow
		} ).ToList();
	}

	/// <summary>
	/// Raid attempts are tracked by the API server. This local cache is updated
	/// after each attempt and on raid data load.
	/// </summary>
	private int cachedRaidAttemptsToday = 0;

	public int GetRaidAttemptsToday()
	{
		return cachedRaidAttemptsToday;
	}

	public bool CanAttemptRaid()
	{
		return IsInGuild && CurrentRaidBoss != null && cachedRaidAttemptsToday < MAX_RAID_ATTEMPTS_PER_DAY;
	}

	/// <summary>
	/// Calculate the best combo multiplier for a team of 3 against the boss.
	/// Returns the name and multiplier of the best combo.
	/// </summary>
	public (string ComboName, float Multiplier) CalculateComboBonus( List<Monster> team )
	{
		if ( team == null || team.Count != 3 || CurrentRaidBoss == null )
			return ("None", 1.0f);

		var bestCombo = ("None", 1.0f);

		// Get species info for each team member
		var species = team.Select( m => MonsterManager.Instance?.GetSpecies( m.SpeciesId ) ).ToList();
		if ( species.Any( s => s == null ) ) return bestCombo;

		var elements = species.Select( s => s.Element ).ToList();
		var rarities = species.Select( s => s.BaseRarity ).ToList();
		var levels = team.Select( m => m.Level ).ToList();
		var bossElement = CurrentRaidBoss.BossElement;

		// Type Advantage: all 3 super-effective vs boss (1.5x)
		bool allSuperEffective = elements.All( e => IsElementSuperEffective( e, bossElement ) );
		if ( allSuperEffective && 1.5f > bestCombo.Item2 )
			bestCombo = ("Type Advantage", 1.5f);

		// Underdog: all 3 are 20+ levels below boss (1.5x)
		bool allUnderdog = levels.All( l => l <= CurrentRaidBoss.BossLevel - 20 );
		if ( allUnderdog && 1.5f > bestCombo.Item2 )
			bestCombo = ("Underdog", 1.5f);

		// Evolution Chain: team includes a full 3-stage evo line (1.4x)
		bool hasEvoChain = CheckEvolutionChain( team );
		if ( hasEvoChain && 1.4f > bestCombo.Item2 )
			bestCombo = ("Evolution Chain", 1.4f);

		// Elemental Unity: all 3 same element (1.3x)
		bool allSameElement = elements.Distinct().Count() == 1;
		if ( allSameElement && 1.3f > bestCombo.Item2 )
			bestCombo = ("Elemental Unity", 1.3f);

		// Rarity Rush: all 3 Epic+ (1.25x)
		bool allEpicPlus = rarities.All( r => r >= Rarity.Epic );
		if ( allEpicPlus && 1.25f > bestCombo.Item2 )
			bestCombo = ("Rarity Rush", 1.25f);

		// Diversity: all 3 different elements (1.2x)
		bool allDifferent = elements.Distinct().Count() == 3;
		if ( allDifferent && 1.2f > bestCombo.Item2 )
			bestCombo = ("Diversity", 1.2f);

		return bestCombo;
	}

	private bool IsElementSuperEffective( ElementType attacker, ElementType defender )
	{
		return BattleAI.GetTypeEffectiveness( attacker, defender ) >= 2.0f;
	}

	private bool CheckEvolutionChain( List<Monster> team )
	{
		// Check if team contains a full 3-stage evolution line
		for ( int i = 0; i < team.Count; i++ )
		{
			var species = MonsterManager.Instance?.GetSpecies( team[i].SpeciesId );
			if ( species == null ) continue;

			// Find the base of this evo line
			var baseId = species.EvolvesFrom ?? species.Id;
			var baseSpecies = MonsterManager.Instance?.GetSpecies( baseId );
			if ( baseSpecies == null ) continue;

			// Check if base evolves to mid
			if ( string.IsNullOrEmpty( baseSpecies.EvolvesTo ) ) continue;
			var midSpecies = MonsterManager.Instance?.GetSpecies( baseSpecies.EvolvesTo );
			if ( midSpecies == null ) continue;

			// Check if mid evolves to final
			if ( string.IsNullOrEmpty( midSpecies.EvolvesTo ) ) continue;

			// Check if all three stages are on the team
			var stageIds = new[] { baseSpecies.Id, midSpecies.Id, midSpecies.EvolvesTo };
			var teamSpeciesIds = team.Select( m => m.SpeciesId ).ToHashSet();
			if ( stageIds.All( id => teamSpeciesIds.Contains( id ) ) )
				return true;
		}
		return false;
	}

	/// <summary>
	/// Complete a raid attempt. Called after battle simulation.
	/// </summary>
	public void CompleteRaidAttempt( int rawDamage, float comboMultiplier, string comboName, int roundsUsed )
	{
		if ( !IsInGuild || CurrentRaidBoss == null ) return;
		_ = CompleteRaidAttemptAsync( rawDamage, comboMultiplier, comboName, roundsUsed );
	}

	private async Task CompleteRaidAttemptAsync( int rawDamage, float comboMultiplier, string comboName, int roundsUsed )
	{
		int totalScore = (int)(rawDamage * comboMultiplier);
		var steamId = Connection.Local?.SteamId ?? 0;
		var playerName = TamerManager.Instance?.CurrentTamer?.Name ?? "Unknown";

		// Submit score to API
		var result = await GuildApiClient.PostAsync<RaidScoreResponse>( $"guilds/{Guild.Id}/raid/score", new
		{
			totalScore,
			rawDamage,
			comboMultiplier,
			roundsUsed,
			playerName
		} );

		if ( result == null )
		{
			OnGuildError?.Invoke( "Failed to submit raid score." );
			return;
		}

		// Update local raid attempt count from server
		cachedRaidAttemptsToday = result.TodayAttempts;

		// Update local state
		if ( CurrentRaidBoss != null )
		{
			CurrentRaidBoss.BestScores[steamId] = new RaidAttemptResult
			{
				SteamId = steamId,
				PlayerName = playerName,
				TotalScore = result.IsNewBest ? totalScore : result.BestScore,
				RawDamage = rawDamage,
				ComboMultiplier = comboMultiplier,
				RoundsUsed = roundsUsed,
				AttemptedAt = DateTime.UtcNow
			};
		}

		// Update weekly tracking
		var self = Members.FirstOrDefault( m => m.SteamId == steamId );
		if ( self != null )
		{
			self.BestRaidScore = result.BestScore;
			self.WeeklyRaidDamage += rawDamage;
		}

		// Award XP and achievement
		AddGuildXP( 50 );
		IncrementAchievement( "raid" );

		// Broadcast to guild via RPC for real-time update
		var connId = Connection.Local?.Id.ToString() ?? "";
		BroadcastRaidScore( connId, Guild.Id, steamId, playerName, totalScore, rawDamage, comboMultiplier, roundsUsed );

		// Submit to leaderboard (leader submits guild total)
		if ( IsLeader )
		{
			Stats.SetValue( "guild-raid-s0", result.GuildTotalScore );
			Stats.SetValue( "guild-rp-s0", TotalRP );
		}

		SoundManager.PlaySuccess();
		NotificationManager.Instance?.AddNotification( NotificationType.Success, "Raid Complete!",
			$"Score: {totalScore:N0} ({comboName} {comboMultiplier:F1}x, {roundsUsed} rounds)", 5f );
	}

	// ═══════════════════════════════════════════════════════════════
	// HOP COOLDOWN
	// ═══════════════════════════════════════════════════════════════

	public bool IsOnHopCooldown()
	{
		var ticksStr = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}leave-time" ), "0" );
		if ( !long.TryParse( ticksStr, out var ticks ) || ticks == 0 ) return false;

		var leaveTime = new DateTime( ticks, DateTimeKind.Utc );
		return (DateTime.UtcNow - leaveTime).TotalHours < GUILD_HOP_COOLDOWN_HOURS;
	}

	public TimeSpan GetHopCooldownRemaining()
	{
		var ticksStr = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}leave-time" ), "0" );
		if ( !long.TryParse( ticksStr, out var ticks ) || ticks == 0 ) return TimeSpan.Zero;

		var leaveTime = new DateTime( ticks, DateTimeKind.Utc );
		var remaining = TimeSpan.FromHours( GUILD_HOP_COOLDOWN_HOURS ) - (DateTime.UtcNow - leaveTime);
		return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
	}

	private void SetHopCooldown()
	{
		Game.Cookies.Set( GetKey( $"{STAT_PREFIX}leave-time" ), DateTime.UtcNow.Ticks.ToString() );
	}

	// ═══════════════════════════════════════════════════════════════
	// WEEKLY RESET
	// ═══════════════════════════════════════════════════════════════

	private void CheckWeeklyReset()
	{
		if ( !IsInGuild ) return;

		// Get the start of this week (Monday UTC)
		var now = DateTime.UtcNow;
		var daysUntilMonday = ((int)now.DayOfWeek - 1 + 7) % 7;
		var thisMonday = now.Date.AddDays( -daysUntilMonday );

		var resetKey = GetKey( $"{STAT_PREFIX}week-reset" );
		var lastResetStr = Game.Cookies.Get<string>( resetKey, "0" );
		if ( !long.TryParse( lastResetStr, out var lastResetTicks ) ) lastResetTicks = 0;

		if ( lastResetTicks == 0 || new DateTime( lastResetTicks, DateTimeKind.Utc ) < thisMonday )
		{
			// Reset weekly stats locally
			foreach ( var m in Members )
			{
				m.WeeklyRP = 0;
				m.WeeklyGuildXP = 0;
				m.WeeklyRaidDamage = 0;
			}

			// Also tell the API to reset (fire-and-forget, server handles idempotency)
			_ = GuildApiClient.PostAsync( $"guilds/{Guild.Id}/weekly-reset" );

			Game.Cookies.Set( resetKey, thisMonday.Ticks.ToString() );
			Log.Info( "GuildManager: Weekly stats reset" );
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// ACTIVITY LOG
	// ═══════════════════════════════════════════════════════════════

	private void AddLogEntry( string action, string playerName, string details )
	{
		ActivityLog.Insert( 0, new GuildLogEntry
		{
			Timestamp = DateTime.UtcNow,
			Action = action,
			PlayerName = playerName,
			Details = details
		} );

		while ( ActivityLog.Count > MAX_LOG_ENTRIES )
			ActivityLog.RemoveAt( ActivityLog.Count - 1 );

		OnLogUpdated?.Invoke();
	}

	// ═══════════════════════════════════════════════════════════════
	// BROADCAST PRESENCE
	// ═══════════════════════════════════════════════════════════════

	private void BroadcastPresence()
	{
		if ( !IsInGuild || !GameNetworkSystem.IsActive ) return;

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		var connId = Connection.Local?.Id.ToString() ?? "";
		var steamId = Connection.Local?.SteamId ?? 0;

		BroadcastGuildPresence( connId, steamId, tamer.Name, Guild.Id, Guild.Name, Guild.Tag,
			(int)Membership.Role, tamer.Level, tamer.ArenaPoints,
			CompetitiveManager.GetRankFromPoints( tamer.ArenaPoints ),
			Membership.JoinedAt.Ticks, Members.Count, OnlineCount, TotalRP,
			Guild.EmblemColor, Guild.EmblemIcon, Guild.EmblemShape,
			(int)Guild.JoinMode, Guild.MinLevel, Guild.MinRank,
			Guild.OwnerName, Guild.Description ?? "", Guild.Level );
	}

	// ═══════════════════════════════════════════════════════════════
	// RPC METHODS
	// ═══════════════════════════════════════════════════════════════

	[Rpc.Broadcast]
	public void BroadcastGuildPresence( string senderConnectionId, long steamId, string name,
		string guildId, string guildName, string guildTag, int role, int level, int arenaPoints,
		string arenaRank, long joinedAtTicks, int memberCount, int onlineCount, int totalRP,
		string emblemColor, string emblemIcon, string emblemShape,
		int joinMode, int minLevel, string minRank, string ownerName, string description, int guildLevel )
	{
		// Update visible guilds for browsing (all players see this)
		VisibleGuilds[senderConnectionId] = new GuildAdvertisement
		{
			GuildId = guildId,
			GuildName = guildName,
			GuildTag = guildTag,
			Level = guildLevel,
			MemberCount = memberCount,
			OnlineCount = onlineCount,
			TotalRP = totalRP,
			EmblemColor = emblemColor,
			EmblemIcon = emblemIcon,
			EmblemShape = emblemShape,
			JoinMode = (GuildJoinMode)joinMode,
			MinLevel = minLevel,
			MinRank = minRank,
			OwnerName = ownerName,
			Description = description
		};

		// If we're in the same guild, update member info
		if ( IsInGuild && Guild?.Id == guildId )
		{
			var member = Members.FirstOrDefault( m => m.SteamId == steamId );
			if ( member != null )
			{
				member.IsOnline = true;
				member.LastSeen = DateTime.UtcNow;
				member.Level = level;
				member.ArenaPoints = arenaPoints;
				member.ArenaRank = arenaRank;
				member.ConnectionId = senderConnectionId;
				OnMembersUpdated?.Invoke();
			}
		}
	}

	[Rpc.Broadcast]
	public void BroadcastGuildInvite( string targetConnectionId, string senderConnectionId,
		string guildId, string guildName, string guildTag, string inviterName, long inviterSteamId )
	{
		var localConnId = Connection.Local?.Id.ToString() ?? "";

		// Only the target player processes this
		if ( localConnId != targetConnectionId ) return;

		// Don't process if already in a guild
		if ( IsInGuild ) return;

		// Reload invites from API to get the invite ID
		_ = LoadPendingInvites();

		SoundManager.PlayNotification();
		NotificationManager.Instance?.AddNotification( NotificationType.Info, "Guild Invite",
			$"{inviterName} invited you to [{guildTag}] {guildName}", 8f );
	}

	[Rpc.Broadcast]
	public void BroadcastJoinRequest( string senderConnectionId, long steamId, string name,
		int level, string arenaRank, int arenaPoints, string targetGuildId )
	{
		// Only guild officers of the target guild see this
		if ( !IsInGuild || !IsOfficer || Guild?.Id != targetGuildId ) return;

		// Refresh from API to get the actual request data
		_ = RefreshGuildData();

		SoundManager.PlayNotification();
		NotificationManager.Instance?.AddNotification( NotificationType.Info, "Join Request",
			$"{name} wants to join your guild", 5f );
	}

	[Rpc.Broadcast]
	public void BroadcastJoinAcceptRequest( string requesterConnectionId, long requesterSteamId,
		string requesterName, int requesterLevel, string requesterArenaRank, int requesterArenaPoints,
		string guildId, string inviterConnectionId )
	{
		// Legacy RPC — joining is now handled via API.
		// Existing guild members just refresh to see the new member.
		if ( !IsInGuild || Guild?.Id != guildId ) return;
		_ = RefreshGuildData();
	}

	[Rpc.Broadcast]
	public void BroadcastJoinAccepted( string targetConnectionId, string senderConnectionId,
		string guildId, string definitionJson, string membersJson, string logJson, string raidJson )
	{
		// Legacy RPC — kept for backward compatibility.
		// New flow: the joining player loads guild from API directly.
	}

	/// <summary>
	/// Tells all online guild members to re-fetch guild data from the API.
	/// Used after mutations (settings, role changes, etc.) to keep everyone in sync.
	/// </summary>
	[Rpc.Broadcast]
	public void BroadcastRefreshGuild( string senderConnectionId, string guildId )
	{
		if ( !IsInGuild || Guild?.Id != guildId ) return;

		// Don't re-fetch if we're the sender (we already refreshed locally)
		var localConnId = Connection.Local?.Id.ToString() ?? "";
		if ( localConnId == senderConnectionId ) return;

		_ = RefreshGuildData();
	}

	[Rpc.Broadcast]
	public void BroadcastGuildUpdate( string senderConnectionId, string guildId, string definitionJson )
	{
		// Legacy RPC — kept for backward compatibility but now triggers API refresh
		if ( !IsInGuild || Guild?.Id != guildId ) return;
		_ = RefreshGuildData();
	}

	[Rpc.Broadcast]
	public void BroadcastRoleChange( string senderConnectionId, string guildId,
		long targetSteamId, int newRole, string targetName, string changerName )
	{
		// Legacy RPC — kept for backward compatibility but now triggers API refresh
		if ( !IsInGuild || Guild?.Id != guildId ) return;
		_ = RefreshGuildData();
	}

	[Rpc.Broadcast]
	public void BroadcastMemberLeft( string senderConnectionId, long steamId, string guildId, string playerName )
	{
		if ( !IsInGuild || Guild?.Id != guildId ) return;

		// Remove from local list immediately for responsiveness
		Members.RemoveAll( m => m.SteamId == steamId );
		OnMembersUpdated?.Invoke();

		// Refresh from API for accurate data
		_ = RefreshGuildData();
	}

	[Rpc.Broadcast]
	public void BroadcastMemberKicked( string senderConnectionId, string guildId,
		long targetSteamId, string targetName, string kickerName )
	{
		if ( !IsInGuild || Guild?.Id != guildId ) return;

		var selfSteamId = Connection.Local?.SteamId ?? 0;

		// If we're the one being kicked
		if ( targetSteamId == selfSteamId )
		{
			ClearGuildData();
			SetHopCooldown();
			SoundManager.PlayDeny();
			NotificationManager.Instance?.AddNotification( NotificationType.Warning, "Kicked",
				$"You were removed from the guild by {kickerName}.", 8f );
			OnGuildLeft?.Invoke();
			ChatManager.Instance?.SendPlayerProfile();
			return;
		}

		// Remove from local list immediately
		Members.RemoveAll( m => m.SteamId == targetSteamId );
		OnMembersUpdated?.Invoke();

		// Refresh from API for accurate data
		_ = RefreshGuildData();
	}

	[Rpc.Broadcast]
	public void BroadcastRaidScore( string senderConnectionId, string guildId,
		long steamId, string playerName, int totalScore, int rawDamage, float comboMultiplier, int roundsUsed )
	{
		if ( !IsInGuild || Guild?.Id != guildId ) return;

		var localSteamId = Connection.Local?.SteamId ?? 0;
		if ( steamId == localSteamId ) return; // Already processed locally

		if ( CurrentRaidBoss == null ) return;

		// Update local cache immediately for responsiveness
		if ( !CurrentRaidBoss.BestScores.TryGetValue( steamId, out var existing ) || totalScore > existing.TotalScore )
		{
			CurrentRaidBoss.BestScores[steamId] = new RaidAttemptResult
			{
				SteamId = steamId,
				PlayerName = playerName,
				TotalScore = totalScore,
				RawDamage = rawDamage,
				ComboMultiplier = comboMultiplier,
				RoundsUsed = roundsUsed,
				AttemptedAt = DateTime.UtcNow
			};
		}

		var member = Members.FirstOrDefault( m => m.SteamId == steamId );
		if ( member != null && totalScore > member.BestRaidScore )
			member.BestRaidScore = totalScore;

		OnGuildUpdated?.Invoke();
	}

	// ═══════════════════════════════════════════════════════════════
	// PERK HELPERS
	// ═══════════════════════════════════════════════════════════════

	public static string GetGuildRankColor( string tier )
	{
		return tier switch
		{
			"Legendary" => "#fbbf24",
			"Master" => "#a855f7",
			"Diamond" => "#60a5fa",
			"Platinum" => "#22d3ee",
			"Gold" => "#f59e0b",
			"Silver" => "#94a3b8",
			"Bronze" => "#cd7c32",
			_ => "#64748b"
		};
	}

	public static string GetRoleDisplayName( GuildRole role )
	{
		return role switch
		{
			GuildRole.Beastlord => "Beastlord",
			GuildRole.Warden => "Warden",
			GuildRole.Tamer => "Tamer",
			GuildRole.Wanderer => "Wanderer",
			_ => "Unknown"
		};
	}

	public static string GetRoleIcon( GuildRole role )
	{
		return role switch
		{
			GuildRole.Beastlord => "👑",
			GuildRole.Warden => "⭐",
			GuildRole.Tamer => "💎",
			GuildRole.Wanderer => "👤",
			_ => "❓"
		};
	}

	public static string GetRoleColor( GuildRole role )
	{
		return role switch
		{
			GuildRole.Beastlord => "#fbbf24",
			GuildRole.Warden => "#a855f7",
			GuildRole.Tamer => "#60a5fa",
			GuildRole.Wanderer => "#94a3b8",
			_ => "#64748b"
		};
	}
}
