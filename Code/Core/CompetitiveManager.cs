using Sandbox;
using Sandbox.Services;
using Sandbox.Network;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// Manages PvP arena matchmaking and rankings (both AI and online)
/// </summary>
public sealed class CompetitiveManager : Component, Component.INetworkListener
{
	public static CompetitiveManager Instance { get; private set; }

	private const string LEADERBOARD_NAME = "beastborne-arena";

	// Arena state
	public bool IsInArena { get; private set; }
	public List<Monster> ArenaTeam { get; private set; } = new();
	public ArenaOpponent CurrentOpponent { get; private set; }

	// Leaderboard cache
	private List<LeaderboardEntry> _leaderboardCache = new();
	private DateTime _lastLeaderboardFetch = DateTime.MinValue;
	private const float LEADERBOARD_CACHE_SECONDS = 60f;

	// ═══════════════════════════════════════════════════════════════
	// ONLINE MATCHMAKING STATE
	// ═══════════════════════════════════════════════════════════════

	public enum MatchmakingState
	{
		None,
		SearchingForMatch,
		MatchFound,
		WaitingForOpponentReady,
		InBattle,
		Disconnected
	}

	public MatchmakingState CurrentMatchmakingState { get; private set; } = MatchmakingState.None;
	public bool IsOnlineMatch { get; private set; } = false;
	public int PlayersInQueue => _playersInQueue?.Count ?? 0;

	// Queue management
	private List<QueuedPlayer> _playersInQueue = new();
	private QueuedPlayer _localQueueEntry;
	private QueuedPlayer _matchedOpponent;
	private DateTime _queueStartTime;
	private const float MATCH_TIMEOUT_SECONDS = 60f;
	private const int RANK_RANGE_INITIAL = 100;
	private const int RANK_RANGE_EXPANSION_PER_10S = 50;

	// Battle sync
	public int BattleSeed { get; private set; }
	private bool _localPlayerReady = false;
	private bool _opponentReady = false;

	// Events
	public Action<ArenaOpponent> OnOpponentFound;
	public Action<bool, int> OnArenaMatchComplete; // won, points gained/lost
	public Action<int> OnQueueUpdate; // players in queue
	public Action<string> OnMatchmakingError;
	public Action OnOpponentDisconnected;
	public Action OnBothPlayersReady; // Fires when both players clicked ready and battle starts
	public Action<string> OnPlayerSearchingRanked; // Fires when another player starts searching

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Log.Info( "CompetitiveManager initialized" );
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
		go.Name = "CompetitiveManager";
		go.Components.Create<CompetitiveManager>();
	}

	/// <summary>
	/// Set the team for arena battles
	/// </summary>
	public void SetArenaTeam( List<Monster> team )
	{
		ArenaTeam = team.Take( 3 ).ToList();
	}

	/// <summary>
	/// Find a random opponent for arena battle
	/// </summary>
	public void FindOpponent()
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		// Generate a simulated opponent based on player's rank
		CurrentOpponent = GenerateOpponent( tamer.ArenaPoints );
		OnOpponentFound?.Invoke( CurrentOpponent );
	}

	private ArenaOpponent GenerateOpponent( int playerPoints )
	{
		var random = new Random();

		// Generate opponent with similar rank (+/- 200 points)
		int opponentPoints = playerPoints + random.Next( -200, 201 );
		opponentPoints = Math.Max( 0, opponentPoints );

		// Determine opponent level based on points
		int baseLevel = 5 + (opponentPoints / 100);
		baseLevel = Math.Clamp( baseLevel, 5, 50 );

		// Generate opponent team
		var team = new List<Monster>();
		var availableSpecies = new[] { "embrik", "droskul", "wispryn", "rootling", "dawnmote", "murkmaw",
									   "charrow", "luracoil", "hollowgale", "cragmaw", "haloveil", "voidweep" };

		int teamSize = random.Next( 2, 4 ); // 2-3 monsters
		for ( int i = 0; i < teamSize; i++ )
		{
			var speciesId = availableSpecies[random.Next( availableSpecies.Length )];
			var level = baseLevel + random.Next( -2, 3 );
			level = Math.Max( 1, level );

			var monster = new Monster
			{
				SpeciesId = speciesId,
				Nickname = MonsterManager.Instance?.GetSpecies( speciesId )?.Name ?? "Monster",
				Level = level,
				Genetics = Genetics.GenerateRandom()
			};

			MonsterManager.Instance?.RecalculateStats( monster );
			monster.FullHeal();

			team.Add( monster );
		}

		// Generate opponent name
		var names = new[] { "Shadowkeeper", "Beastlord", "Mythwalker", "Spiritbinder",
						   "Ashtrainer", "Voidcaller", "Dawnseeker", "Stormtamer",
						   "Flamewhisper", "Deepwatcher", "Windrunner", "Earthshaper" };

		return new ArenaOpponent
		{
			Name = names[random.Next( names.Length )] + random.Next( 100, 999 ),
			ArenaPoints = opponentPoints,
			Team = team,
			Rank = GetRankFromPoints( opponentPoints )
		};
	}

	/// <summary>
	/// Start an arena battle
	/// </summary>
	public void StartArenaBattle()
	{
		Log.Info( $"[Arena] StartArenaBattle called: ArenaTeam={ArenaTeam?.Count ?? 0}, CurrentOpponent={CurrentOpponent?.Name ?? "null"}" );

		if ( ArenaTeam == null || ArenaTeam.Count == 0 )
		{
			Log.Warning( "[Arena] Cannot start battle: No arena team!" );
			return;
		}

		if ( CurrentOpponent == null )
		{
			Log.Warning( "[Arena] Cannot start battle: No opponent!" );
			return;
		}

		// Exit any existing battle first (e.g., if player was in an expedition)
		if ( BattleManager.Instance?.IsInBattle == true )
		{
			Log.Info( "[Arena] Exiting existing battle before starting arena battle" );
			BattleManager.Instance.ExitBattle();
		}

		// Heal player team for battle
		foreach ( var monster in ArenaTeam )
		{
			monster?.FullHeal();
		}

		IsInArena = true;
		// Use manual battle mode for arena (player selects moves, 1v1 with swaps)
		BattleManager.Instance?.StartManualBattle( ArenaTeam, CurrentOpponent.Team, isArena: true );
	}

	/// <summary>
	/// Called when arena battle ends
	/// </summary>
	public void OnBattleComplete( bool playerWon )
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		int pointsChange;

		if ( playerWon )
		{
			// Points gained based on opponent's rank
			int baseGain = 25;
			int rankDiff = CurrentOpponent.ArenaPoints - tamer.ArenaPoints;
			float modifier = 1.0f + (rankDiff / 500f);
			modifier = Math.Clamp( modifier, 0.5f, 2.0f );

			pointsChange = (int)(baseGain * modifier);
			tamer.ArenaPoints += pointsChange;
			tamer.TotalBattlesWon++;
			tamer.ArenaWins++;
		}
		else
		{
			// Points lost
			pointsChange = -15;
			tamer.ArenaPoints = Math.Max( 0, tamer.ArenaPoints + pointsChange );
			tamer.TotalBattlesLost++;
			tamer.ArenaLosses++;
		}

		// Update rank
		tamer.ArenaRank = GetRankFromPoints( tamer.ArenaPoints );

		// Submit to leaderboard
		SubmitScore( tamer.ArenaPoints );

		IsInArena = false;
		OnArenaMatchComplete?.Invoke( playerWon, pointsChange );

		TamerManager.Instance?.SaveToCloud();
	}

	/// <summary>
	/// Get rank title from points
	/// </summary>
	public static string GetRankFromPoints( int points )
	{
		return points switch
		{
			>= 5000 => "Mythic",
			>= 3000 => "Legendary",
			>= 2000 => "Master",
			>= 1500 => "Diamond",
			>= 1000 => "Platinum",
			>= 700 => "Gold",
			>= 400 => "Silver",
			>= 200 => "Bronze",
			_ => "Unranked"
		};
	}

	/// <summary>
	/// Get rank color
	/// </summary>
	public static string GetRankColor( string rank )
	{
		return rank switch
		{
			"Mythic" => "#ec4899",
			"Legendary" => "#fbbf24",
			"Master" => "#a855f7",
			"Diamond" => "#60a5fa",
			"Platinum" => "#22d3ee",
			"Gold" => "#eab308",
			"Silver" => "#9ca3af",
			"Bronze" => "#d97706",
			_ => "#6b7280"
		};
	}

	/// <summary>
	/// Submit score to leaderboard
	/// </summary>
	private async void SubmitScore( int score )
	{
		try
		{
			var board = Leaderboards.Get( LEADERBOARD_NAME );
			board.MaxEntries = 100;
			await board.Refresh();
			// Note: Leaderboard submission in s&box is typically automatic when using Stats
			// For now, we just log the score - the actual submission would use Stats API
			Stats.SetValue( "arena-score", score );
			Log.Info( $"Submitted arena score: {score}" );
		}
		catch ( Exception e )
		{
			Log.Warning( $"Failed to submit leaderboard score: {e.Message}" );
		}
	}

	/// <summary>
	/// Fetch leaderboard entries
	/// </summary>
	public async Task<List<LeaderboardEntry>> GetLeaderboard( int count = 100 )
	{
		// Return cache if fresh
		if ( (DateTime.UtcNow - _lastLeaderboardFetch).TotalSeconds < LEADERBOARD_CACHE_SECONDS )
		{
			return _leaderboardCache;
		}

		try
		{
			var board = Leaderboards.Get( LEADERBOARD_NAME );
			board.MaxEntries = count;

			await board.Refresh();

			_leaderboardCache = board.Entries.Select( ( e, index ) => new LeaderboardEntry
			{
				Rank = index + 1,
				Name = e.DisplayName,
				Score = (int)e.Value,
				RankTitle = GetRankFromPoints( (int)e.Value )
			} ).ToList();

			_lastLeaderboardFetch = DateTime.UtcNow;
			Log.Info( $"Fetched {_leaderboardCache.Count} leaderboard entries" );
		}
		catch ( Exception e )
		{
			Log.Warning( $"Failed to fetch leaderboard: {e.Message}" );
		}

		return _leaderboardCache;
	}

	/// <summary>
	/// Get player's leaderboard position
	/// </summary>
	public async Task<int> GetPlayerRank()
	{
		try
		{
			var board = Leaderboards.Get( LEADERBOARD_NAME );
			await board.Refresh();
			var myEntry = board.Entries.Select( ( e, index ) => new { Entry = e, Index = index + 1 } )
				.FirstOrDefault( x => x.Entry.Me );
			return myEntry?.Index ?? -1;
		}
		catch
		{
			return -1;
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// ONLINE MATCHMAKING SYSTEM
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Check if we're connected to a multiplayer session
	/// </summary>
	public bool IsNetworkActive => GameNetworkSystem.IsActive;

	/// <summary>
	/// Get the local connection ID (unique per game instance)
	/// </summary>
	private Guid LocalConnectionId => Connection.Local?.Id ?? Guid.Empty;
	private string LocalPlayerName => Connection.Local?.DisplayName ?? TamerManager.Instance?.CurrentTamer?.Name ?? "Player";
	private long LocalSteamId => Connection.Local?.SteamId ?? 0;

	/// <summary>
	/// Join the online matchmaking queue
	/// </summary>
	public void JoinOnlineQueue()
	{
		if ( !GameNetworkSystem.IsActive )
		{
			OnMatchmakingError?.Invoke( "Not connected to a server. Join a lobby first!" );
			return;
		}

		if ( ArenaTeam.Count == 0 )
		{
			OnMatchmakingError?.Invoke( "Select a team first!" );
			return;
		}

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		IsOnlineMatch = true;
		CurrentMatchmakingState = MatchmakingState.SearchingForMatch;
		_queueStartTime = DateTime.UtcNow;

		// Create local queue entry
		_localQueueEntry = new QueuedPlayer
		{
			ConnectionId = LocalConnectionId.ToString(),
			SteamId = LocalSteamId,
			PlayerName = LocalPlayerName,
			ArenaPoints = tamer.ArenaPoints,
			TeamData = SerializeTeam( ArenaTeam ),
			QueueTime = DateTime.UtcNow
		};

		// Broadcast that we're looking for a match
		BroadcastJoinQueue(
			_localQueueEntry.ConnectionId,
			_localQueueEntry.SteamId,
			_localQueueEntry.PlayerName,
			_localQueueEntry.ArenaPoints,
			_localQueueEntry.TeamData
		);

		Log.Info( $"[Arena] Joined online queue with {ArenaTeam.Count} monsters, {tamer.ArenaPoints} points" );
	}

	/// <summary>
	/// Leave the online matchmaking queue
	/// </summary>
	public void LeaveOnlineQueue()
	{
		if ( CurrentMatchmakingState == MatchmakingState.None ) return;

		CurrentMatchmakingState = MatchmakingState.None;
		IsOnlineMatch = false;
		_localQueueEntry = null;
		_matchedOpponent = null;

		// Broadcast that we're leaving
		BroadcastLeaveQueue( LocalConnectionId.ToString() );

		Log.Info( "[Arena] Left online queue" );
	}

	/// <summary>
	/// Called every frame to check for matches
	/// </summary>
	protected override void OnUpdate()
	{
		if ( CurrentMatchmakingState != MatchmakingState.SearchingForMatch ) return;
		if ( _localQueueEntry == null ) return;

		// Check for timeout
		var timeInQueue = (DateTime.UtcNow - _queueStartTime).TotalSeconds;
		if ( timeInQueue > MATCH_TIMEOUT_SECONDS )
		{
			OnMatchmakingError?.Invoke( "No opponents found. Try again later!" );
			LeaveOnlineQueue();
			return;
		}

		// Calculate current rank range (expands over time)
		int rankRange = RANK_RANGE_INITIAL + (int)(timeInQueue / 10) * RANK_RANGE_EXPANSION_PER_10S;

		// Try to find a match
		var match = _playersInQueue
			.Where( p => p.ConnectionId != _localQueueEntry.ConnectionId )
			.Where( p => Math.Abs( p.ArenaPoints - _localQueueEntry.ArenaPoints ) <= rankRange )
			.OrderBy( p => Math.Abs( p.ArenaPoints - _localQueueEntry.ArenaPoints ) )
			.FirstOrDefault();

		if ( match != null )
		{
			// Found a match! The player with lower connection ID initiates
			bool weInitiate = string.Compare( _localQueueEntry.ConnectionId, match.ConnectionId ) < 0;

			if ( weInitiate )
			{
				// Generate battle seed
				BattleSeed = new Random().Next();
				_matchedOpponent = match;

				// Send match proposal to opponent
				SendMatchProposal(
					match.ConnectionId,
					_localQueueEntry.ConnectionId,
					_localQueueEntry.PlayerName,
					_localQueueEntry.ArenaPoints,
					_localQueueEntry.TeamData,
					BattleSeed
				);

				CurrentMatchmakingState = MatchmakingState.MatchFound;
				Log.Info( $"[Arena] Initiated match with {match.PlayerName}" );
			}
		}
	}

	/// <summary>
	/// Signal that local player is ready to start battle
	/// </summary>
	public void SignalReady()
	{
		Log.Info( $"[Arena] SignalReady called: _matchedOpponent={_matchedOpponent?.PlayerName ?? "null"}, _opponentReady={_opponentReady}" );

		if ( _matchedOpponent == null )
		{
			Log.Warning( "[Arena] SignalReady: No matched opponent!" );
			return;
		}

		_localPlayerReady = true;
		SendPlayerReady( _matchedOpponent.ConnectionId, LocalConnectionId.ToString() );

		Log.Info( $"[Arena] SignalReady: localReady={_localPlayerReady}, opponentReady={_opponentReady}" );

		if ( _localPlayerReady && _opponentReady )
		{
			StartOnlineBattle();
		}
	}

	/// <summary>
	/// Start the online battle
	/// </summary>
	private void StartOnlineBattle()
	{
		Log.Info( $"[Arena] StartOnlineBattle called: _matchedOpponent={_matchedOpponent?.PlayerName ?? "null"}" );

		if ( _matchedOpponent == null )
		{
			Log.Warning( "[Arena] StartOnlineBattle: No matched opponent!" );
			return;
		}

		CurrentMatchmakingState = MatchmakingState.InBattle;
		IsInArena = true;

		// Deserialize opponent team
		Log.Info( $"[Arena] Deserializing opponent team: {_matchedOpponent.TeamData}" );
		var opponentTeam = DeserializeTeam( _matchedOpponent.TeamData );
		Log.Info( $"[Arena] Deserialized {opponentTeam?.Count ?? 0} monsters" );

		if ( opponentTeam == null || opponentTeam.Count == 0 )
		{
			Log.Warning( "[Arena] StartOnlineBattle: Failed to deserialize opponent team!" );
			return;
		}

		// Create opponent data
		CurrentOpponent = new ArenaOpponent
		{
			Name = _matchedOpponent.PlayerName,
			ArenaPoints = _matchedOpponent.ArenaPoints,
			Rank = GetRankFromPoints( _matchedOpponent.ArenaPoints ),
			Team = opponentTeam,
			IsRealPlayer = true,
			ConnectionId = _matchedOpponent.ConnectionId
		};

		// Log team details
		Log.Info( $"[Arena] ArenaTeam: {ArenaTeam?.Count ?? 0} monsters" );
		foreach ( var m in ArenaTeam ?? new List<Monster>() )
		{
			Log.Info( $"  - {m?.Nickname ?? "null"}: HP={m?.MaxHP ?? 0}, ATK={m?.ATK ?? 0}" );
		}
		Log.Info( $"[Arena] OpponentTeam: {opponentTeam.Count} monsters" );
		foreach ( var m in opponentTeam )
		{
			Log.Info( $"  - {m?.Nickname ?? "null"}: HP={m?.MaxHP ?? 0}, ATK={m?.ATK ?? 0}" );
		}

		// Start manual battle with seeded random FIRST, before firing UI event
		// This ensures the battle is ready when BattleView tries to subscribe
		Log.Info( $"[Arena] Starting online manual battle vs {_matchedOpponent.PlayerName} with seed {BattleSeed}" );
		BattleManager.Instance?.StartManualBattleWithSeed( ArenaTeam, opponentTeam, BattleSeed, isArena: true );

		// Fire event so UI knows to switch to battle view (battle is now ready)
		OnBothPlayersReady?.Invoke();
	}

	/// <summary>
	/// Complete an online match
	/// </summary>
	public void OnOnlineBattleComplete( bool playerWon )
	{
		OnBattleComplete( playerWon );

		// Reset online state
		CurrentMatchmakingState = MatchmakingState.None;
		IsOnlineMatch = false;
		_matchedOpponent = null;
		_localPlayerReady = false;
		_opponentReady = false;
	}

	// ═══════════════════════════════════════════════════════════════
	// RPC METHODS - Network communication
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Broadcast joining the queue
	/// </summary>
	[Rpc.Broadcast]
	public void BroadcastJoinQueue( string connectionId, long steamId, string playerName, int arenaPoints, string teamData )
	{
		// Don't process our own broadcast
		if ( connectionId == LocalConnectionId.ToString() ) return;

		// Add to queue
		var existingPlayer = _playersInQueue.FirstOrDefault( p => p.ConnectionId == connectionId );
		if ( existingPlayer != null )
		{
			// Update existing entry
			existingPlayer.ArenaPoints = arenaPoints;
			existingPlayer.TeamData = teamData;
			existingPlayer.QueueTime = DateTime.UtcNow;
		}
		else
		{
			_playersInQueue.Add( new QueuedPlayer
			{
				ConnectionId = connectionId,
				SteamId = steamId,
				PlayerName = playerName,
				ArenaPoints = arenaPoints,
				TeamData = teamData,
				QueueTime = DateTime.UtcNow
			} );

			// Notify that a new player is searching for ranked
			OnPlayerSearchingRanked?.Invoke( playerName );
		}

		OnQueueUpdate?.Invoke( _playersInQueue.Count );
		Log.Info( $"[Arena] Player {playerName} joined queue ({_playersInQueue.Count} in queue)" );
	}

	/// <summary>
	/// Broadcast leaving the queue
	/// </summary>
	[Rpc.Broadcast]
	public void BroadcastLeaveQueue( string connectionId )
	{
		_playersInQueue.RemoveAll( p => p.ConnectionId == connectionId );
		OnQueueUpdate?.Invoke( _playersInQueue.Count );
		Log.Info( $"[Arena] Player left queue ({_playersInQueue.Count} in queue)" );
	}

	/// <summary>
	/// Send a match proposal to a specific player
	/// </summary>
	[Rpc.Broadcast]
	public void SendMatchProposal( string targetConnectionId, string senderConnectionId, string senderName, int senderPoints, string senderTeamData, int battleSeed )
	{
		// Only the target should process this
		if ( targetConnectionId != LocalConnectionId.ToString() ) return;

		Log.Info( $"[Arena] Received match proposal from {senderName}" );

		// Accept the match
		_matchedOpponent = new QueuedPlayer
		{
			ConnectionId = senderConnectionId,
			PlayerName = senderName,
			ArenaPoints = senderPoints,
			TeamData = senderTeamData
		};
		BattleSeed = battleSeed;

		// Remove both players from queue
		_playersInQueue.RemoveAll( p => p.ConnectionId == senderConnectionId || p.ConnectionId == LocalConnectionId.ToString() );

		// Create opponent for UI
		var opponentTeam = DeserializeTeam( senderTeamData );
		CurrentOpponent = new ArenaOpponent
		{
			Name = senderName,
			ArenaPoints = senderPoints,
			Rank = GetRankFromPoints( senderPoints ),
			Team = opponentTeam,
			IsRealPlayer = true,
			ConnectionId = senderConnectionId
		};

		CurrentMatchmakingState = MatchmakingState.MatchFound;
		OnOpponentFound?.Invoke( CurrentOpponent );

		// Send acceptance
		SendMatchAccepted( senderConnectionId, LocalConnectionId.ToString(), LocalPlayerName,
			TamerManager.Instance?.CurrentTamer?.ArenaPoints ?? 0, SerializeTeam( ArenaTeam ) );
	}

	/// <summary>
	/// Send match acceptance
	/// </summary>
	[Rpc.Broadcast]
	public void SendMatchAccepted( string targetConnectionId, string senderConnectionId, string senderName, int senderPoints, string senderTeamData )
	{
		if ( targetConnectionId != LocalConnectionId.ToString() ) return;

		Log.Info( $"[Arena] Match accepted by {senderName}" );

		// Update opponent data with their actual team
		var opponentTeam = DeserializeTeam( senderTeamData );
		CurrentOpponent = new ArenaOpponent
		{
			Name = senderName,
			ArenaPoints = senderPoints,
			Rank = GetRankFromPoints( senderPoints ),
			Team = opponentTeam,
			IsRealPlayer = true,
			ConnectionId = senderConnectionId
		};

		// Remove from queue
		_playersInQueue.RemoveAll( p => p.ConnectionId == senderConnectionId || p.ConnectionId == LocalConnectionId.ToString() );

		OnOpponentFound?.Invoke( CurrentOpponent );
	}

	/// <summary>
	/// Signal ready to start battle
	/// </summary>
	[Rpc.Broadcast]
	public void SendPlayerReady( string targetConnectionId, string senderConnectionId )
	{
		if ( targetConnectionId != LocalConnectionId.ToString() ) return;

		Log.Info( $"[Arena] Opponent is ready (received from {senderConnectionId})" );
		Log.Info( $"[Arena] Before: localReady={_localPlayerReady}, opponentReady={_opponentReady}" );
		_opponentReady = true;
		Log.Info( $"[Arena] After: localReady={_localPlayerReady}, opponentReady={_opponentReady}" );

		if ( _localPlayerReady && _opponentReady )
		{
			Log.Info( "[Arena] Both players ready, starting online battle!" );
			StartOnlineBattle();
		}
		else
		{
			Log.Info( "[Arena] Waiting for local player to click ready..." );
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// INetworkListener - Handle player disconnect
	// ═══════════════════════════════════════════════════════════════

	void INetworkListener.OnActive( Connection connection )
	{
		// New player connected - they'll broadcast their queue status if searching
	}

	void INetworkListener.OnDisconnected( Connection connection )
	{
		var connectionId = connection.Id.ToString();

		// Remove from queue
		_playersInQueue.RemoveAll( p => p.ConnectionId == connectionId );

		// If our opponent disconnected during match
		if ( _matchedOpponent?.ConnectionId == connectionId )
		{
			Log.Warning( "[Arena] Opponent disconnected!" );
			OnOpponentDisconnected?.Invoke();

			// Award win by default if in battle
			if ( CurrentMatchmakingState == MatchmakingState.InBattle )
			{
				OnOnlineBattleComplete( true );
			}
			else
			{
				// Reset to queue
				CurrentMatchmakingState = MatchmakingState.None;
				_matchedOpponent = null;
			}
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// SERIALIZATION HELPERS
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Serialize a team to a string for network transfer
	/// </summary>
	private string SerializeTeam( List<Monster> team )
	{
		// Simple format: speciesId:level:hp:atk:def:spd|speciesId:level:...
		var parts = team.Select( m => $"{m.SpeciesId}:{m.Level}:{m.MaxHP}:{m.ATK}:{m.DEF}:{m.SPD}" );
		return string.Join( "|", parts );
	}

	/// <summary>
	/// Deserialize a team from network data
	/// </summary>
	private List<Monster> DeserializeTeam( string teamData )
	{
		var team = new List<Monster>();
		if ( string.IsNullOrEmpty( teamData ) ) return team;

		var parts = teamData.Split( '|' );
		foreach ( var part in parts )
		{
			var data = part.Split( ':' );
			if ( data.Length < 6 ) continue;

			var monster = new Monster
			{
				SpeciesId = data[0],
				Level = int.Parse( data[1] ),
				Nickname = MonsterManager.Instance?.GetSpecies( data[0] )?.Name ?? "Monster"
			};

			// Set stats directly from serialized data (opponent's actual stats)
			monster.MaxHP = int.Parse( data[2] );
			monster.CurrentHP = monster.MaxHP;
			monster.ATK = int.Parse( data[3] );
			monster.DEF = int.Parse( data[4] );
			monster.SPD = int.Parse( data[5] );

			team.Add( monster );
		}

		return team;
	}
}

/// <summary>
/// Arena opponent data
/// </summary>
public class ArenaOpponent
{
	public string Name { get; set; }
	public int ArenaPoints { get; set; }
	public string Rank { get; set; }
	public List<Monster> Team { get; set; } = new();

	// Online match data
	public bool IsRealPlayer { get; set; } = false;
	public string ConnectionId { get; set; }
}

/// <summary>
/// Leaderboard entry
/// </summary>
public class LeaderboardEntry
{
	public int Rank { get; set; }
	public string Name { get; set; }
	public int Score { get; set; }
	public string RankTitle { get; set; }
}

/// <summary>
/// Player in the matchmaking queue
/// </summary>
public class QueuedPlayer
{
	public string ConnectionId { get; set; }
	public long SteamId { get; set; }
	public string PlayerName { get; set; }
	public int ArenaPoints { get; set; }
	public string TeamData { get; set; }
	public DateTime QueueTime { get; set; }
}
