using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Network;
using Sandbox.Services;
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

	// Player profiles (synced across network for displaying avatars/backgrounds)
	public Dictionary<string, PlayerProfileData> PlayerProfiles { get; private set; } = new();
	public Action OnProfilesUpdated;

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

		// Track chat message stats
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer != null )
		{
			tamer.ChatMessagesSent++;
			AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.ChatMessagesSent, tamer.ChatMessagesSent );
			Stats.SetValue( "chat-sent", tamer.ChatMessagesSent );
		}

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

		// Track beast showcase achievement
		AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.BeastShowcased, 1 );

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
	/// Show off your tamer card in chat
	/// </summary>
	public void SendTamerCardShowcase()
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		var achievementCount = tamer.Achievements?.Values.Count( p => p.IsUnlocked ) ?? 0;
		var title = GetTamerTitle( tamer.Level );

		var message = new ChatMessage
		{
			SteamId = LocalSteamId,
			PlayerName = LocalPlayerName,
			Content = $"{LocalPlayerName} shared their Tamer Card!",
			Type = ChatMessageType.TamerCardShowcase,
			CardName = tamer.Name,
			CardLevel = tamer.Level,
			CardTitle = title,
			CardArenaRank = tamer.ArenaRank ?? "Unranked",
			CardArenaPoints = tamer.ArenaPoints,
			CardFavoriteSpeciesId = tamer.FavoriteMonsterSpeciesId ?? "",
			CardAchievementCount = achievementCount,
			CardWinRate = tamer.WinRate,
			CardGender = tamer.Gender.ToString(),
			CardFavoriteExpeditionId = tamer.FavoriteExpeditionId ?? "",
			CardArenaWins = tamer.ArenaWins,
			CardArenaLosses = tamer.ArenaLosses,
			CardMonstersCaught = tamer.TotalMonstersCaught,
			CardBattlesWon = tamer.ArenaWins,
			CardMonstersBred = tamer.TotalMonstersBred,
			CardMonstersEvolved = tamer.TotalMonstersEvolved,
			CardHighestExpedition = tamer.HighestExpeditionCleared
		};
		AddMessage( message );

		if ( GameNetworkSystem.IsActive )
		{
			BroadcastTamerCardShowcase(
				LocalConnectionId.ToString(), LocalSteamId, LocalPlayerName,
				tamer.Name, tamer.Level, title,
				tamer.ArenaRank ?? "Unranked", tamer.ArenaPoints,
				tamer.FavoriteMonsterSpeciesId ?? "", achievementCount,
				tamer.WinRate, tamer.Gender.ToString(),
				tamer.FavoriteExpeditionId ?? "",
				tamer.ArenaWins, tamer.ArenaLosses,
				tamer.TotalMonstersCaught, tamer.ArenaWins,
				tamer.TotalMonstersBred, tamer.TotalMonstersEvolved,
				tamer.HighestExpeditionCleared,
				message.Timestamp.Ticks
			);
		}
	}

	[Rpc.Broadcast]
	public void BroadcastTamerCardShowcase(
		string senderConnectionId, long steamId, string playerName,
		string cardName, int cardLevel, string cardTitle,
		string arenaRank, int arenaPoints,
		string favoriteSpeciesId, int achievementCount,
		float winRate, string gender,
		string favoriteExpeditionId,
		int arenaWins, int arenaLosses,
		int monstersCaught, int battlesWon,
		int monstersBred, int monstersEvolved,
		int highestExpedition,
		long timestampTicks )
	{
		if ( senderConnectionId == LocalConnectionId.ToString() ) return;

		var message = new ChatMessage
		{
			SteamId = steamId,
			PlayerName = playerName,
			Content = $"{playerName} shared their Tamer Card!",
			Type = ChatMessageType.TamerCardShowcase,
			CardName = cardName,
			CardLevel = cardLevel,
			CardTitle = cardTitle,
			CardArenaRank = arenaRank,
			CardArenaPoints = arenaPoints,
			CardFavoriteSpeciesId = favoriteSpeciesId,
			CardAchievementCount = achievementCount,
			CardWinRate = winRate,
			CardGender = gender,
			CardFavoriteExpeditionId = favoriteExpeditionId,
			CardArenaWins = arenaWins,
			CardArenaLosses = arenaLosses,
			CardMonstersCaught = monstersCaught,
			CardBattlesWon = battlesWon,
			CardMonstersBred = monstersBred,
			CardMonstersEvolved = monstersEvolved,
			CardHighestExpedition = highestExpedition,
			Timestamp = new DateTime( timestampTicks, DateTimeKind.Utc )
		};
		AddMessage( message );
	}

	private static string GetTamerTitle( int level )
	{
		if ( level >= 80 ) return "Legendary Tamer";
		if ( level >= 60 ) return "Master Tamer";
		if ( level >= 40 ) return "Expert Tamer";
		if ( level >= 20 ) return "Skilled Tamer";
		if ( level >= 10 ) return "Apprentice Tamer";
		return "Novice Tamer";
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
		// Assign deterministic name color if not already set
		if ( string.IsNullOrEmpty( message.NameColor ) )
		{
			message.NameColor = ChatMessage.GetColorForSteamId( message.SteamId );
		}

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

	// ═══════════════════════════════════════════════════════════════
	// PLAYER PROFILE SYNC
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Broadcast local player's profile (gender, favorite expedition) to all players.
	/// Called when player loads a save and enters the game.
	/// </summary>
	public void SendPlayerProfile()
	{
		if ( !GameNetworkSystem.IsActive ) return;

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		var gender = tamer.Gender.ToString();
		var favExpId = tamer.FavoriteExpeditionId ?? "";
		var title = GetDisplayTitle( tamer );
		var titleColor = GetDisplayTitleColor( tamer );
		var level = tamer.Level;
		var favMonster = tamer.FavoriteMonsterSpeciesId ?? "";
		var arenaRank = CompetitiveManager.GetRankFromPoints( tamer.ArenaPoints );
		var arenaPoints = tamer.ArenaPoints;
		var arenaWins = tamer.ArenaWins;
		var arenaLosses = tamer.ArenaLosses;
		var monstersCaught = tamer.TotalMonstersCaught;
		var highestExp = tamer.HighestExpeditionCleared;
		var battlesWon = tamer.TotalBattlesWon;
		var monstersBred = tamer.TotalMonstersBred;
		var monstersEvolved = tamer.TotalMonstersEvolved;
		var totalExp = tamer.TotalExpeditionsCompleted;
		var totalTrades = tamer.TotalTradesCompleted;
		var playTime = (int)tamer.TotalPlayTime.TotalMinutes;
		var achCount = tamer.Achievements?.Values.Count( p => p.IsUnlocked ) ?? 0;

		// Store locally
		var connId = LocalConnectionId.ToString();
		PlayerProfiles[connId] = new PlayerProfileData
		{
			Gender = gender,
			FavoriteExpeditionId = favExpId,
			Title = title,
			TitleColor = titleColor,
			Level = level,
			FavoriteMonsterSpeciesId = favMonster,
			ArenaRank = arenaRank,
			ArenaPoints = arenaPoints,
			ArenaWins = arenaWins,
			ArenaLosses = arenaLosses,
			MonstersCaught = monstersCaught,
			HighestExpedition = highestExp,
			BattlesWon = battlesWon,
			MonstersBred = monstersBred,
			MonstersEvolved = monstersEvolved,
			TotalExpeditionsCompleted = totalExp,
			TotalTradesCompleted = totalTrades,
			TotalPlayTimeMinutes = playTime,
			AchievementCount = achCount
		};

		// Broadcast to all
		BroadcastPlayerProfile( connId, gender, favExpId, title, titleColor, level, favMonster, arenaRank, arenaPoints,
			arenaWins, arenaLosses, monstersCaught, highestExp, battlesWon, monstersBred, monstersEvolved, totalExp, totalTrades, playTime, achCount );
	}

	[Rpc.Broadcast]
	public void BroadcastPlayerProfile( string senderConnectionId, string gender, string favoriteExpeditionId, string title, string titleColor,
		int level = 0, string favoriteMonsterSpeciesId = "", string arenaRank = "Unranked", int arenaPoints = 0, int arenaWins = 0, int arenaLosses = 0, int monstersCaught = 0, int highestExpedition = 0,
		int battlesWon = 0, int monstersBred = 0, int monstersEvolved = 0, int totalExpeditionsCompleted = 0, int totalTradesCompleted = 0, int totalPlayTimeMinutes = 0, int achievementCount = 0 )
	{
		// Store the profile (including our own, which we already stored locally)
		PlayerProfiles[senderConnectionId] = new PlayerProfileData
		{
			Gender = gender,
			FavoriteExpeditionId = favoriteExpeditionId,
			Title = title ?? "Tamer",
			TitleColor = titleColor ?? "#a78bfa",
			Level = level,
			FavoriteMonsterSpeciesId = favoriteMonsterSpeciesId ?? "",
			ArenaRank = arenaRank ?? "Unranked",
			ArenaPoints = arenaPoints,
			ArenaWins = arenaWins,
			ArenaLosses = arenaLosses,
			MonstersCaught = monstersCaught,
			HighestExpedition = highestExpedition,
			BattlesWon = battlesWon,
			MonstersBred = monstersBred,
			MonstersEvolved = monstersEvolved,
			TotalExpeditionsCompleted = totalExpeditionsCompleted,
			TotalTradesCompleted = totalTradesCompleted,
			TotalPlayTimeMinutes = totalPlayTimeMinutes,
			AchievementCount = achievementCount
		};

		OnProfilesUpdated?.Invoke();
	}

	private string GetDisplayTitle( Tamer tamer )
	{
		if ( !string.IsNullOrEmpty( tamer.ActiveTitleId ) )
		{
			var cosmetic = CosmeticDatabase.GetTitle( tamer.ActiveTitleId );
			if ( cosmetic != null ) return cosmetic.Title;
		}
		if ( !string.IsNullOrEmpty( tamer.ActiveLevelTitle ) )
			return tamer.ActiveLevelTitle;
		var level = tamer.Level;
		if ( level >= 80 ) return "Legendary Tamer";
		if ( level >= 60 ) return "Master Tamer";
		if ( level >= 40 ) return "Expert Tamer";
		if ( level >= 20 ) return "Skilled Tamer";
		if ( level >= 10 ) return "Apprentice Tamer";
		return "Tamer";
	}

	private string GetDisplayTitleColor( Tamer tamer )
	{
		if ( !string.IsNullOrEmpty( tamer.ActiveTitleId ) )
		{
			var cosmetic = CosmeticDatabase.GetTitle( tamer.ActiveTitleId );
			if ( cosmetic != null ) return cosmetic.TitleColor;
		}
		return "#a78bfa";
	}

	/// <summary>
	/// Look up a player's profile data by their connection ID string.
	/// </summary>
	public PlayerProfileData GetProfileByConnectionId( string connectionId )
	{
		if ( string.IsNullOrEmpty( connectionId ) ) return null;
		return PlayerProfiles.TryGetValue( connectionId, out var profile ) ? profile : null;
	}

	// INetworkListener implementation
	void INetworkListener.OnActive( Connection connection )
	{
		// Player joined
		if ( connection != Connection.Local )
		{
			AddSystemMessage( $"{connection.DisplayName} joined the game.", ChatMessageType.Join );
		}

		// Re-broadcast our profile so the new player gets it
		SendPlayerProfile();
	}

	void INetworkListener.OnDisconnected( Connection connection )
	{
		// Clean up disconnected player's profile
		var connId = connection.Id.ToString();
		if ( PlayerProfiles.ContainsKey( connId ) )
		{
			PlayerProfiles.Remove( connId );
			OnProfilesUpdated?.Invoke();
		}
	}
}

/// <summary>
/// Profile data synced across the network for player tiles
/// </summary>
public class PlayerProfileData
{
	public string Gender { get; set; } = "Male";
	public string FavoriteExpeditionId { get; set; } = "";
	public string Title { get; set; } = "Tamer";
	public string TitleColor { get; set; } = "#a78bfa";
	public int Level { get; set; }
	public string FavoriteMonsterSpeciesId { get; set; } = "";
	public string ArenaRank { get; set; } = "Unranked";
	public int ArenaPoints { get; set; }
	public int ArenaWins { get; set; }
	public int ArenaLosses { get; set; }
	public int MonstersCaught { get; set; }
	public int HighestExpedition { get; set; }
	public int BattlesWon { get; set; }
	public int MonstersBred { get; set; }
	public int MonstersEvolved { get; set; }
	public int TotalExpeditionsCompleted { get; set; }
	public int TotalTradesCompleted { get; set; }
	public int TotalPlayTimeMinutes { get; set; }
	public int AchievementCount { get; set; }
}
