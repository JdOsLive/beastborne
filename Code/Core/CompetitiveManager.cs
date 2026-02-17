using Sandbox;
using Sandbox.Services;
using Sandbox.Network;
using Beastborne.Data;
using System.Text.Json;

namespace Beastborne.Core;

/// <summary>
/// Manages PvP arena matchmaking, ranked Best-of-3 battles, and rankings
/// </summary>
public sealed class CompetitiveManager : Component, Component.INetworkListener
{
	public static CompetitiveManager Instance { get; private set; }

	// ═══════════════════════════════════════════════════════════════
	// CONSTANTS
	// ═══════════════════════════════════════════════════════════════

	private const string LEADERBOARD_NAME = "arena-score-s0";
	private const int RANKED_LEVEL = 50;
	private const int BETWEEN_GAMES_SECONDS = 10;
	private const float MATCH_TIMEOUT_SECONDS = 90f;
	private const int RANK_RANGE_INITIAL = 100;
	private const int RANK_RANGE_EXPANSION_PER_10S = 50;
	private const int MAX_MATCH_HISTORY = 20;
	private const int GAMES_TO_WIN_SET = 2;

	// ═══════════════════════════════════════════════════════════════
	// STATE MACHINE
	// ═══════════════════════════════════════════════════════════════

	public enum ArenaState
	{
		Idle,
		Searching,
		MatchFound,
		ReadyCheck,
		InBattle,
		BetweenGames,
		Results
	}

	public enum ArenaMode
	{
		Ranked,
		QuickPlay
	}

	public ArenaState CurrentState { get; private set; } = ArenaState.Idle;
	public ArenaMode CurrentMode { get; private set; } = ArenaMode.Ranked;

	// ═══════════════════════════════════════════════════════════════
	// ARENA STATE
	// ═══════════════════════════════════════════════════════════════

	public bool IsInArena { get; private set; }
	public List<Monster> ArenaTeam { get; private set; } = new();
	public ArenaOpponent CurrentOpponent { get; private set; }
	public bool IsOnlineMatch { get; private set; } = false;

	// Normalized (Level 50) team copies used during ranked battles
	private List<Monster> _normalizedPlayerTeam = new();
	private List<Monster> _normalizedEnemyTeam = new();

	// ═══════════════════════════════════════════════════════════════
	// BEST OF 3 STATE
	// ═══════════════════════════════════════════════════════════════

	public int SetScorePlayer { get; private set; }
	public int SetScoreOpponent { get; private set; }
	public int CurrentGameNumber { get; private set; }
	public List<bool> GameResults { get; private set; } = new();
	public float BetweenGamesTimer { get; private set; }
	public bool LastSetWon { get; private set; }
	public int LastPointsChange { get; private set; }

	// Saved move PPs for restoration between games
	private Dictionary<Guid, List<int>> _savedPlayerMovePPs = new();
	private Dictionary<Guid, List<int>> _savedEnemyMovePPs = new();

	// ═══════════════════════════════════════════════════════════════
	// SEASON & BANS
	// ═══════════════════════════════════════════════════════════════

	public int Season { get; private set; } = 0;
	public List<string> BannedSpecies { get; private set; } = new();

	// Rank-up detection (set during CompleteSet)
	public string PreviousRank { get; private set; }
	public bool DidRankUp { get; private set; }
	public bool DidRankDown { get; private set; }

	// Season ban lists — add entries per season as needed
	private static readonly Dictionary<int, List<string>> SeasonBanLists = new()
	{
		{ 0, new List<string>() }, // Season 0: no bans
	};

	// ═══════════════════════════════════════════════════════════════
	// ONLINE MATCHMAKING
	// ═══════════════════════════════════════════════════════════════

	public int PlayersInQueue => _playersInQueue?.Count ?? 0;

	private List<QueuedPlayer> _playersInQueue = new();
	private QueuedPlayer _localQueueEntry;
	private QueuedPlayer _matchedOpponent;
	private DateTime _queueStartTime;

	public int BattleSeed { get; private set; }
	private bool _localPlayerReady = false;
	private bool _opponentReady = false;

	// ═══════════════════════════════════════════════════════════════
	// LEADERBOARD CACHE
	// ═══════════════════════════════════════════════════════════════

	private List<LeaderboardEntry> _leaderboardCache = new();
	private DateTime _lastLeaderboardFetch = DateTime.MinValue;
	private const float LEADERBOARD_CACHE_SECONDS = 60f;

	// ═══════════════════════════════════════════════════════════════
	// EVENTS
	// ═══════════════════════════════════════════════════════════════

	public Action<ArenaOpponent> OnOpponentFound;
	public Action<bool, int> OnSetComplete;        // won, points gained/lost
	public Action<int, int> OnGameEnd;             // playerScore, opponentScore (after each game)
	public Action OnBetweenGamesStart;
	public Action OnNextGameStart;
	public Action<int> OnQueueUpdate;
	public Action<string> OnMatchmakingError;
	public Action OnOpponentDisconnected;
	public Action OnBothPlayersReady;
	public Action<string> OnPlayerSearchingRanked;

	// ═══════════════════════════════════════════════════════════════
	// LIFECYCLE
	// ═══════════════════════════════════════════════════════════════

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			InitializeSeason();
			Log.Info( "CompetitiveManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	private void InitializeSeason()
	{
		BannedSpecies = SeasonBanLists.GetValueOrDefault( Season, new List<string>() );
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "CompetitiveManager";
		go.Components.Create<CompetitiveManager>();
	}

	protected override void OnUpdate()
	{
		switch ( CurrentState )
		{
			case ArenaState.Searching:
				TickMatchmaking();
				break;
			case ArenaState.BetweenGames:
				TickBetweenGames();
				break;
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// TEAM MANAGEMENT
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Set the team for arena battles. Filters out banned species.
	/// </summary>
	public void SetArenaTeam( List<Monster> team )
	{
		ArenaTeam = team.Where( m => !IsSpeciesBanned( m.SpeciesId ) ).Take( 3 ).ToList();
	}

	/// <summary>
	/// Check if a species is banned in the current season
	/// </summary>
	public bool IsSpeciesBanned( string speciesId )
	{
		return BannedSpecies.Contains( speciesId );
	}

	/// <summary>
	/// Normalize a team to Level 50 for ranked battles.
	/// Uses only base stats + growth curves + genetics + nature. No external bonuses.
	/// </summary>
	private List<Monster> NormalizeTeamToLevel50( List<Monster> team )
	{
		return team.Select( NormalizeMonsterToLevel50 ).ToList();
	}

	private Monster NormalizeMonsterToLevel50( Monster original )
	{
		var clone = original.Clone();
		clone.Level = RANKED_LEVEL;

		var species = MonsterManager.Instance?.GetSpecies( clone.SpeciesId );
		if ( species == null ) return clone;

		float levelFactor = MathF.Sqrt( RANKED_LEVEL );

		clone.MaxHP = species.BaseHP + (int)(levelFactor * species.HPGrowth * 4) + clone.Genetics.HPGene;
		clone.ATK = species.BaseATK + (int)(levelFactor * species.ATKGrowth * 4) + clone.Genetics.ATKGene;
		clone.DEF = species.BaseDEF + (int)(levelFactor * species.DEFGrowth * 4) + clone.Genetics.DEFGene;
		clone.SpA = species.BaseSpA + (int)(levelFactor * species.SpAGrowth * 4) + clone.Genetics.SpAGene;
		clone.SpD = species.BaseSpD + (int)(levelFactor * species.SpDGrowth * 4) + clone.Genetics.SpDGene;
		clone.SPD = species.BaseSPD + (int)(levelFactor * species.SPDGrowth * 4) + clone.Genetics.SPDGene;

		ApplyNatureModifiers( clone );

		clone.CurrentHP = clone.MaxHP;
		return clone;
	}

	private void ApplyNatureModifiers( Monster monster )
	{
		if ( monster.Genetics == null ) return;

		switch ( monster.Genetics.Nature )
		{
			case NatureType.Ferocious:
				monster.ATK = (int)(monster.ATK * 1.1f);
				monster.DEF = (int)(monster.DEF * 0.9f);
				break;
			case NatureType.Stalwart:
				monster.DEF = (int)(monster.DEF * 1.1f);
				monster.ATK = (int)(monster.ATK * 0.9f);
				break;
			case NatureType.Restless:
				monster.SPD = (int)(monster.SPD * 1.1f);
				monster.MaxHP = (int)(monster.MaxHP * 0.9f);
				break;
			case NatureType.Enduring:
				monster.MaxHP = (int)(monster.MaxHP * 1.1f);
				monster.SPD = (int)(monster.SPD * 0.9f);
				break;
			case NatureType.Reckless:
				monster.ATK = (int)(monster.ATK * 1.1f);
				monster.SPD = (int)(monster.SPD * 0.9f);
				break;
			case NatureType.Stoic:
				monster.DEF = (int)(monster.DEF * 1.1f);
				monster.SPD = (int)(monster.SPD * 0.9f);
				break;
			case NatureType.Skittish:
				monster.SPD = (int)(monster.SPD * 1.1f);
				monster.DEF = (int)(monster.DEF * 0.9f);
				break;
			case NatureType.Vigorous:
				monster.MaxHP = (int)(monster.MaxHP * 1.1f);
				monster.ATK = (int)(monster.ATK * 0.9f);
				break;
			case NatureType.Ruthless:
				monster.ATK = (int)(monster.ATK * 1.1f);
				monster.MaxHP = (int)(monster.MaxHP * 0.9f);
				break;
			case NatureType.Nimble:
				monster.SPD = (int)(monster.SPD * 1.1f);
				monster.ATK = (int)(monster.ATK * 0.9f);
				break;
			case NatureType.Mystical:
				monster.SpA = (int)(monster.SpA * 1.1f);
				monster.ATK = (int)(monster.ATK * 0.9f);
				break;
			case NatureType.Resolute:
				monster.SpD = (int)(monster.SpD * 1.1f);
				monster.SpA = (int)(monster.SpA * 0.9f);
				break;
			case NatureType.Arcane:
				monster.SpA = (int)(monster.SpA * 1.1f);
				monster.DEF = (int)(monster.DEF * 0.9f);
				break;
			case NatureType.Warded:
				monster.SpD = (int)(monster.SpD * 1.1f);
				monster.SPD = (int)(monster.SPD * 0.9f);
				break;
			case NatureType.Cunning:
				monster.SpA = (int)(monster.SpA * 1.1f);
				monster.MaxHP = (int)(monster.MaxHP * 0.9f);
				break;
			case NatureType.Serene:
				monster.SpD = (int)(monster.SpD * 1.1f);
				monster.ATK = (int)(monster.ATK * 0.9f);
				break;
		}
	}

	/// <summary>
	/// Save move PPs for a team (for restoration between games)
	/// </summary>
	private void SaveMovePPs( List<Monster> team, Dictionary<Guid, List<int>> storage )
	{
		storage.Clear();
		foreach ( var monster in team )
		{
			if ( monster?.Moves != null )
			{
				storage[monster.Id] = monster.Moves.Select( m => m.CurrentPP ).ToList();
			}
		}
	}

	/// <summary>
	/// Restore move PPs for a team from saved values
	/// </summary>
	private void RestoreMovePPs( List<Monster> team, Dictionary<Guid, List<int>> storage )
	{
		foreach ( var monster in team )
		{
			if ( monster?.Moves != null && storage.TryGetValue( monster.Id, out var pps ) )
			{
				for ( int i = 0; i < Math.Min( monster.Moves.Count, pps.Count ); i++ )
				{
					monster.Moves[i].CurrentPP = pps[i];
				}
			}
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// AI OPPONENT GENERATION
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Generate an AI opponent with type-diverse team scaled to player rank
	/// </summary>
	public ArenaOpponent GenerateOpponent( int playerPoints )
	{
		var random = new Random();

		int opponentPoints = playerPoints + random.Next( -200, 201 );
		opponentPoints = Math.Max( 0, opponentPoints );

		var team = GenerateAITeam( opponentPoints, random );

		var names = new[] { "Shadowkeeper", "Beastlord", "Mythwalker", "Spiritbinder",
						   "Ashtrainer", "Voidcaller", "Dawnseeker", "Stormtamer",
						   "Flamewhisper", "Deepwatcher", "Windrunner", "Earthshaper",
						   "Ironclaw", "Frostweaver", "Thundersoul", "Nightbane" };

		return new ArenaOpponent
		{
			Name = names[random.Next( names.Length )] + random.Next( 100, 999 ),
			ArenaPoints = opponentPoints,
			Team = team,
			Rank = GetRankFromPoints( opponentPoints ),
			IsRealPlayer = false
		};
	}

	/// <summary>
	/// Generate a type-diverse AI team with genetics scaled to rank
	/// </summary>
	private List<Monster> GenerateAITeam( int points, Random random )
	{
		var allSpecies = MonsterManager.Instance?.GetAllSpecies();
		if ( allSpecies == null || allSpecies.Count == 0 ) return new();

		// Filter: catchable, not banned
		var available = allSpecies
			.Where( s => s.IsCatchable && !IsSpeciesBanned( s.Id ) )
			.ToList();

		if ( available.Count == 0 ) return new();

		// Determine genetics quality based on rank
		int minGene = GetMinGeneForRank( points );

		var team = new List<Monster>();
		var usedElements = new List<ElementType>();

		int teamSize = 3;
		int attempts = 0;
		int maxAttempts = 100;

		while ( team.Count < teamSize && attempts < maxAttempts )
		{
			attempts++;
			var species = available[random.Next( available.Count )];

			// Type diversity: max 2 of the same element
			if ( usedElements.Count( e => e == species.Element ) >= 2 )
				continue;

			// Create the monster
			var monster = new Monster
			{
				SpeciesId = species.Id,
				Nickname = species.Name,
				Level = RANKED_LEVEL,
				Genetics = GenerateScaledGenetics( minGene, random )
			};

			// Calculate level 50 stats
			float levelFactor = MathF.Sqrt( RANKED_LEVEL );
			monster.MaxHP = species.BaseHP + (int)(levelFactor * species.HPGrowth * 4) + monster.Genetics.HPGene;
			monster.ATK = species.BaseATK + (int)(levelFactor * species.ATKGrowth * 4) + monster.Genetics.ATKGene;
			monster.DEF = species.BaseDEF + (int)(levelFactor * species.DEFGrowth * 4) + monster.Genetics.DEFGene;
			monster.SpA = species.BaseSpA + (int)(levelFactor * species.SpAGrowth * 4) + monster.Genetics.SpAGene;
			monster.SpD = species.BaseSpD + (int)(levelFactor * species.SpDGrowth * 4) + monster.Genetics.SpDGene;
			monster.SPD = species.BaseSPD + (int)(levelFactor * species.SPDGrowth * 4) + monster.Genetics.SPDGene;

			ApplyNatureModifiers( monster );
			monster.CurrentHP = monster.MaxHP;

			// Assign moves (top 4 learnable at level 50)
			AssignAIMoves( monster, species );

			// Assign a random trait
			if ( species.PossibleTraits?.Count > 0 )
			{
				monster.Traits = new List<string> { species.PossibleTraits[random.Next( species.PossibleTraits.Count )] };
			}

			team.Add( monster );
			usedElements.Add( species.Element );
		}

		return team;
	}

	/// <summary>
	/// Get minimum gene value based on arena points (rank scaling)
	/// </summary>
	private int GetMinGeneForRank( int points )
	{
		return points switch
		{
			>= 3000 => 23, // Master+: 23-30
			>= 2000 => 20, // Diamond: 20-30
			>= 1500 => 18, // Platinum: 18-30
			>= 1000 => 15, // Gold: 15-30
			>= 400 => 10,  // Silver: 10-30
			_ => 0          // Unranked/Bronze: 0-30
		};
	}

	/// <summary>
	/// Generate genetics with minimum gene floor based on rank
	/// </summary>
	private Genetics GenerateScaledGenetics( int minGene, Random random )
	{
		int maxGene = 30;
		return new Genetics
		{
			HPGene = random.Next( minGene, maxGene + 1 ),
			ATKGene = random.Next( minGene, maxGene + 1 ),
			DEFGene = random.Next( minGene, maxGene + 1 ),
			SpAGene = random.Next( minGene, maxGene + 1 ),
			SpDGene = random.Next( minGene, maxGene + 1 ),
			SPDGene = random.Next( minGene, maxGene + 1 ),
			Nature = (NatureType)random.Next( Enum.GetValues<NatureType>().Length )
		};
	}

	/// <summary>
	/// Assign the best available moves for an AI monster at level 50
	/// </summary>
	private void AssignAIMoves( Monster monster, MonsterSpecies species )
	{
		if ( species.LearnableMoves == null || species.LearnableMoves.Count == 0 )
		{
			monster.Moves = new List<MonsterMove>();
			return;
		}

		// Get all moves learnable at or below level 50, pick top 4 by level (strongest)
		var bestMoves = species.LearnableMoves
			.Where( lm => lm.LearnLevel <= RANKED_LEVEL )
			.OrderByDescending( lm => lm.LearnLevel )
			.Take( 4 )
			.Select( lm => new MonsterMove { MoveId = lm.MoveId, CurrentPP = 15 } )
			.ToList();

		monster.Moves = bestMoves;
	}

	// ═══════════════════════════════════════════════════════════════
	// ARENA SET FLOW (Best of 3)
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Start a Best-of-3 arena set (AI opponent)
	/// </summary>
	public void StartArenaSet()
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		if ( ArenaTeam == null || ArenaTeam.Count == 0 )
		{
			Log.Warning( "[Arena] Cannot start set: No arena team!" );
			return;
		}

		if ( CurrentOpponent == null )
		{
			Log.Warning( "[Arena] Cannot start set: No opponent!" );
			return;
		}

		// Exit any existing battle
		if ( BattleManager.Instance?.IsInBattle == true )
		{
			BattleManager.Instance.ExitBattle();
		}

		// Reset set state
		SetScorePlayer = 0;
		SetScoreOpponent = 0;
		CurrentGameNumber = 0;
		GameResults.Clear();
		IsInArena = true;

		// Normalize teams to Level 50
		_normalizedPlayerTeam = NormalizeTeamToLevel50( ArenaTeam );
		_normalizedEnemyTeam = CurrentOpponent.IsRealPlayer
			? CurrentOpponent.Team // Online: already normalized by sender
			: NormalizeTeamToLevel50( CurrentOpponent.Team );

		// Save initial move PPs
		SaveMovePPs( _normalizedPlayerTeam, _savedPlayerMovePPs );
		SaveMovePPs( _normalizedEnemyTeam, _savedEnemyMovePPs );

		Log.Info( $"[Arena] Starting Bo3 set vs {CurrentOpponent.Name}: {_normalizedPlayerTeam.Count} vs {_normalizedEnemyTeam.Count}" );

		// Start Game 1
		StartNextGame();
	}

	/// <summary>
	/// Start a Best-of-3 arena set for online match (called after both players ready)
	/// </summary>
	public void StartOnlineArenaSet()
	{
		if ( _matchedOpponent == null )
		{
			Log.Warning( "[Arena] StartOnlineArenaSet: No matched opponent!" );
			return;
		}

		// Deserialize opponent team (already normalized to Level 50 by sender)
		var opponentTeam = DeserializeTeam( _matchedOpponent.TeamData );
		if ( opponentTeam == null || opponentTeam.Count == 0 )
		{
			Log.Warning( "[Arena] Failed to deserialize opponent team!" );
			return;
		}

		CurrentOpponent = new ArenaOpponent
		{
			Name = _matchedOpponent.PlayerName,
			ArenaPoints = _matchedOpponent.ArenaPoints,
			Rank = GetRankFromPoints( _matchedOpponent.ArenaPoints ),
			Team = opponentTeam,
			IsRealPlayer = true,
			ConnectionId = _matchedOpponent.ConnectionId,
			SteamId = _matchedOpponent.SteamId
		};

		// Reset set state
		SetScorePlayer = 0;
		SetScoreOpponent = 0;
		CurrentGameNumber = 0;
		GameResults.Clear();
		IsInArena = true;

		// Normalize player team, opponent team is already normalized
		_normalizedPlayerTeam = NormalizeTeamToLevel50( ArenaTeam );
		_normalizedEnemyTeam = opponentTeam;

		SaveMovePPs( _normalizedPlayerTeam, _savedPlayerMovePPs );
		SaveMovePPs( _normalizedEnemyTeam, _savedEnemyMovePPs );

		Log.Info( $"[Arena] Starting online Bo3 set vs {CurrentOpponent.Name} with seed {BattleSeed}" );

		StartNextGame();
	}

	/// <summary>
	/// Start the next game in the set
	/// </summary>
	private void StartNextGame()
	{
		CurrentGameNumber++;
		CurrentState = ArenaState.InBattle;

		// Heal and restore PP
		HealTeams();

		Log.Info( $"[Arena] Starting Game {CurrentGameNumber} of set (Score: {SetScorePlayer}-{SetScoreOpponent})" );

		// Generate a new seed for each game (online uses shared seed)
		int gameSeed = IsOnlineMatch ? BattleSeed + CurrentGameNumber : new Random().Next();

		BattleManager.Instance?.StartManualBattleWithSeed(
			_normalizedPlayerTeam,
			_normalizedEnemyTeam,
			gameSeed,
			isArena: true
		);

		OnNextGameStart?.Invoke();
	}

	/// <summary>
	/// Record the result of a single game in the set
	/// </summary>
	public void RecordGameResult( bool playerWon )
	{
		GameResults.Add( playerWon );

		if ( playerWon )
			SetScorePlayer++;
		else
			SetScoreOpponent++;

		Log.Info( $"[Arena] Game {CurrentGameNumber} result: {(playerWon ? "WIN" : "LOSS")} — Set score: {SetScorePlayer}-{SetScoreOpponent}" );

		// Exit the current battle
		BattleManager.Instance?.ExitBattle();

		// Fire game end event
		OnGameEnd?.Invoke( SetScorePlayer, SetScoreOpponent );

		// Check if set is over
		if ( SetScorePlayer >= GAMES_TO_WIN_SET || SetScoreOpponent >= GAMES_TO_WIN_SET )
		{
			CompleteSet();
		}
		else
		{
			// Between games
			CurrentState = ArenaState.BetweenGames;
			BetweenGamesTimer = BETWEEN_GAMES_SECONDS;
			OnBetweenGamesStart?.Invoke();
		}
	}

	/// <summary>
	/// Complete the set and calculate rating changes
	/// </summary>
	private void CompleteSet()
	{
		bool playerWon = SetScorePlayer >= GAMES_TO_WIN_SET;
		CurrentState = ArenaState.Results;
		LastSetWon = playerWon;

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		// Rank-up detection: capture rank before changes
		PreviousRank = GetRankFromPoints( tamer.ArenaPoints );
		int previousTier = GetRankTier( tamer.ArenaPoints );

		int pointsChange = 0;

		if ( CurrentMode == ArenaMode.Ranked )
		{
			// Calculate rating change (Ranked only)
			int opponentPoints = CurrentOpponent?.ArenaPoints ?? 0;
			pointsChange = CalculateRatingChange(
				playerWon,
				tamer.ArenaPoints,
				opponentPoints,
				tamer.ArenaWinStreak,
				tamer.ArenaSetsCompleted
			);

			// Update arena points and rank
			tamer.ArenaPoints = Math.Max( 0, tamer.ArenaPoints + pointsChange );
			tamer.ArenaRank = GetRankFromPoints( tamer.ArenaPoints );
		}

		LastPointsChange = pointsChange;

		// Detect rank changes
		int newTier = GetRankTier( tamer.ArenaPoints );
		DidRankUp = newTier > previousTier;
		DidRankDown = newTier < previousTier;

		// Win/loss tracking applies to both modes
		tamer.ArenaSetsCompleted++;

		if ( playerWon )
		{
			tamer.ArenaWins++;
			tamer.ArenaWinStreak++;
			tamer.TotalBattlesWon++;
		}
		else
		{
			tamer.ArenaLosses++;
			tamer.ArenaWinStreak = 0;
			tamer.TotalBattlesLost++;
		}

		// Check for Reverse Sweep achievement (lose game 1, win games 2 and 3)
		if ( playerWon && GameResults.Count >= 3 && !GameResults[0] && GameResults[1] && GameResults[2] )
		{
			AchievementManager.Instance?.CheckProgress( AchievementRequirement.ArenaReverseSweep, 1 );
		}

		// Check arena achievements (Ranked only for rank-based ones)
		AchievementManager.Instance?.CheckProgress( AchievementRequirement.ArenaWins, tamer.ArenaWins );
		AchievementManager.Instance?.CheckProgress( AchievementRequirement.ArenaSetsCompleted, tamer.ArenaSetsCompleted );
		AchievementManager.Instance?.CheckProgress( AchievementRequirement.ArenaWinStreak, tamer.ArenaWinStreak );
		Stats.SetValue( "arena-sets", tamer.ArenaSetsCompleted );
		Stats.SetValue( "arena-streak", tamer.ArenaWinStreak );
		if ( CurrentMode == ArenaMode.Ranked )
		{
			AchievementManager.Instance?.CheckProgress( AchievementRequirement.ArenaRankReached, GetRankTier( tamer.ArenaPoints ) );
		}

		// Add match history
		AddMatchHistory( playerWon, pointsChange );

		// Collect opponent's tamer card (online matches only)
		if ( CurrentOpponent?.IsRealPlayer == true && CurrentOpponent.SteamId != 0 )
		{
			var profile = ChatManager.Instance?.GetProfileByConnectionId( CurrentOpponent.ConnectionId );
			TamerManager.Instance?.CollectTamerCard(
				CurrentOpponent.SteamId,
				CurrentOpponent.Name,
				profile?.Level ?? 0,
				CurrentOpponent.Rank,
				CurrentOpponent.ArenaPoints,
				profile?.FavoriteMonsterSpeciesId,
				profile?.AchievementCount ?? 0,
				0f,
				gender: profile?.Gender,
				favoriteExpeditionId: profile?.FavoriteExpeditionId,
				title: profile?.Title,
				titleColor: profile?.TitleColor,
				arenaWins: profile?.ArenaWins ?? 0,
				arenaLosses: profile?.ArenaLosses ?? 0,
				highestExpedition: profile?.HighestExpedition ?? 0,
				monstersCaught: profile?.MonstersCaught ?? 0,
				totalPlayTimeMinutes: profile?.TotalPlayTimeMinutes ?? 0,
				battlesWon: profile?.BattlesWon ?? 0,
				monstersBred: profile?.MonstersBred ?? 0,
				monstersEvolved: profile?.MonstersEvolved ?? 0,
				totalExpeditionsCompleted: profile?.TotalExpeditionsCompleted ?? 0,
				totalTradesCompleted: profile?.TotalTradesCompleted ?? 0
			);
		}

		// Submit to leaderboard (Ranked only)
		if ( CurrentMode == ArenaMode.Ranked )
		{
			SubmitScore( tamer.ArenaPoints );
		}
		Stats.SetValue( "arena-wins", tamer.ArenaWins );

		// Save
		TamerManager.Instance?.SaveToCloud();

		IsInArena = false;
		Log.Info( $"[Arena] Set complete: {(playerWon ? "WIN" : "LOSS")} ({SetScorePlayer}-{SetScoreOpponent}), {pointsChange:+#;-#;0} points" );

		OnSetComplete?.Invoke( playerWon, pointsChange );
	}

	/// <summary>
	/// Forfeit the entire set (retire from arena match)
	/// </summary>
	public void ForfeitSet()
	{
		Log.Info( $"[Arena] Player forfeited set at Game {CurrentGameNumber} (Score: {SetScorePlayer}-{SetScoreOpponent})" );
		BattleManager.Instance?.ExitBattle();
		SetScoreOpponent = GAMES_TO_WIN_SET;
		CompleteSet();
	}

	// ═══════════════════════════════════════════════════════════════
	// BETWEEN GAMES
	// ═══════════════════════════════════════════════════════════════

	private void TickBetweenGames()
	{
		BetweenGamesTimer -= Time.Delta;
		if ( BetweenGamesTimer <= 0 )
		{
			StartNextGame();
		}
	}

	/// <summary>
	/// Heal all monsters and restore PP between games
	/// </summary>
	private void HealTeams()
	{
		foreach ( var monster in _normalizedPlayerTeam )
		{
			monster?.FullHeal();
		}
		foreach ( var monster in _normalizedEnemyTeam )
		{
			monster?.FullHeal();
		}

		// Restore move PP to start-of-set values
		RestoreMovePPs( _normalizedPlayerTeam, _savedPlayerMovePPs );
		RestoreMovePPs( _normalizedEnemyTeam, _savedEnemyMovePPs );
	}

	/// <summary>
	/// Swap a held item on a monster between games
	/// </summary>
	public bool SwapHeldItem( int monsterIndex, string newItemId )
	{
		if ( CurrentState != ArenaState.BetweenGames ) return false;
		if ( monsterIndex < 0 || monsterIndex >= _normalizedPlayerTeam.Count ) return false;

		_normalizedPlayerTeam[monsterIndex].HeldItemId = newItemId;
		Log.Info( $"[Arena] Swapped held item on monster {monsterIndex} to {newItemId}" );
		return true;
	}

	/// <summary>
	/// Skip the between-games countdown (for testing or user action)
	/// </summary>
	public void SkipBetweenGamesTimer()
	{
		if ( CurrentState != ArenaState.BetweenGames ) return;
		BetweenGamesTimer = 0;
	}

	// ═══════════════════════════════════════════════════════════════
	// RATING SYSTEM
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Calculate rating change based on set result, rank differential, and modifiers
	/// </summary>
	private int CalculateRatingChange( bool won, int playerPoints, int opponentPoints, int winStreak, int setsPlayed )
	{
		int baseChange = won ? 25 : -15;

		// Rank differential modifier: more points for beating higher-ranked players
		int rankDiff = opponentPoints - playerPoints;
		float diffModifier = 1.0f + (rankDiff / 500f);
		diffModifier = Math.Clamp( diffModifier, 0.5f, 2.0f );

		// Win streak bonus (only for wins)
		int streakBonus = won ? Math.Min( winStreak * 5, 25 ) : 0;

		// Placement match amplifier: first 10 sets have 2x point swings
		float placementMultiplier = setsPlayed < 10 ? 2.0f : 1.0f;

		int change = (int)((baseChange * diffModifier + streakBonus) * placementMultiplier);

		return change;
	}

	// ═══════════════════════════════════════════════════════════════
	// MATCH HISTORY
	// ═══════════════════════════════════════════════════════════════

	private void AddMatchHistory( bool won, int pointsChange )
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		tamer.MatchHistory ??= new();

		tamer.MatchHistory.Insert( 0, new MatchHistoryEntry
		{
			OpponentName = CurrentOpponent?.Name ?? "Unknown",
			OpponentArenaPoints = CurrentOpponent?.ArenaPoints ?? 0,
			Won = won,
			GamesWon = SetScorePlayer,
			GamesLost = SetScoreOpponent,
			PointsChange = pointsChange,
			PlayedAt = DateTime.UtcNow,
			IsRanked = CurrentMode == ArenaMode.Ranked
		} );

		// Keep only the most recent entries
		while ( tamer.MatchHistory.Count > MAX_MATCH_HISTORY )
		{
			tamer.MatchHistory.RemoveAt( tamer.MatchHistory.Count - 1 );
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// FIND OPPONENT (AI)
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Set the arena mode (for AI matches where JoinOnlineQueue isn't called)
	/// </summary>
	public void SetMode( ArenaMode mode )
	{
		CurrentMode = mode;
	}

	/// <summary>
	/// Find a random AI opponent for arena battle
	/// </summary>
	public void FindOpponent()
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		CurrentOpponent = GenerateOpponent( tamer.ArenaPoints );
		OnOpponentFound?.Invoke( CurrentOpponent );
	}

	// ═══════════════════════════════════════════════════════════════
	// ONLINE MATCHMAKING
	// ═══════════════════════════════════════════════════════════════

	public bool IsNetworkActive => GameNetworkSystem.IsActive;

	private Guid LocalConnectionId => Connection.Local?.Id ?? Guid.Empty;
	private string LocalPlayerName => Connection.Local?.DisplayName ?? TamerManager.Instance?.CurrentTamer?.Name ?? "Player";
	private long LocalSteamId => Connection.Local?.SteamId ?? 0;

	/// <summary>
	/// Join the online matchmaking queue
	/// </summary>
	public void JoinOnlineQueue( ArenaMode mode = ArenaMode.Ranked )
	{
		CurrentMode = mode;
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
		CurrentState = ArenaState.Searching;
		_queueStartTime = DateTime.UtcNow;

		// Normalize team to level 50 and serialize
		var normalizedTeam = NormalizeTeamToLevel50( ArenaTeam );

		_localQueueEntry = new QueuedPlayer
		{
			ConnectionId = LocalConnectionId.ToString(),
			SteamId = LocalSteamId,
			PlayerName = LocalPlayerName,
			ArenaPoints = tamer.ArenaPoints,
			TeamData = SerializeTeam( normalizedTeam ),
			QueueTime = DateTime.UtcNow
		};

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
		if ( CurrentState == ArenaState.Idle ) return;

		CurrentState = ArenaState.Idle;
		IsOnlineMatch = false;
		_localQueueEntry = null;
		_matchedOpponent = null;

		BroadcastLeaveQueue( LocalConnectionId.ToString() );
		Log.Info( "[Arena] Left online queue" );
	}

	/// <summary>
	/// Signal that local player is ready to start the online set
	/// </summary>
	public void SignalReady()
	{
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
	/// Start the online battle (called when both players are ready)
	/// </summary>
	private void StartOnlineBattle()
	{
		if ( _matchedOpponent == null )
		{
			Log.Warning( "[Arena] StartOnlineBattle: No matched opponent!" );
			return;
		}

		Log.Info( $"[Arena] Both players ready, starting online Bo3 set vs {_matchedOpponent.PlayerName}" );

		CurrentState = ArenaState.InBattle;

		// Start the Bo3 set
		StartOnlineArenaSet();

		// Fire event so UI knows to switch to battle view
		OnBothPlayersReady?.Invoke();
	}

	/// <summary>
	/// Complete an online match (called after set ends)
	/// </summary>
	public void OnOnlineBattleComplete( bool playerWon )
	{
		// Reset online state
		CurrentState = ArenaState.Idle;
		IsOnlineMatch = false;
		_matchedOpponent = null;
		_localPlayerReady = false;
		_opponentReady = false;
	}

	/// <summary>
	/// Tick matchmaking search
	/// </summary>
	private void TickMatchmaking()
	{
		if ( _localQueueEntry == null ) return;

		// Check for timeout
		var timeInQueue = (DateTime.UtcNow - _queueStartTime).TotalSeconds;
		if ( timeInQueue > MATCH_TIMEOUT_SECONDS )
		{
			OnMatchmakingError?.Invoke( "No opponents found. Try again later!" );
			LeaveOnlineQueue();
			return;
		}

		// Calculate expanding rank range
		int rankRange = RANK_RANGE_INITIAL + (int)(timeInQueue / 10) * RANK_RANGE_EXPANSION_PER_10S;

		// Try to find a match
		var match = _playersInQueue
			.Where( p => p.ConnectionId != _localQueueEntry.ConnectionId )
			.Where( p => Math.Abs( p.ArenaPoints - _localQueueEntry.ArenaPoints ) <= rankRange )
			.OrderBy( p => Math.Abs( p.ArenaPoints - _localQueueEntry.ArenaPoints ) )
			.FirstOrDefault();

		if ( match != null )
		{
			// Player with lower connection ID initiates
			bool weInitiate = string.Compare( _localQueueEntry.ConnectionId, match.ConnectionId ) < 0;

			if ( weInitiate )
			{
				BattleSeed = new Random().Next();
				_matchedOpponent = match;

				SendMatchProposal(
					match.ConnectionId,
					_localQueueEntry.ConnectionId,
					_localQueueEntry.PlayerName,
					_localQueueEntry.ArenaPoints,
					_localQueueEntry.TeamData,
					BattleSeed
				);

				CurrentState = ArenaState.MatchFound;
				Log.Info( $"[Arena] Initiated match with {match.PlayerName}" );
			}
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// RPC METHODS
	// ═══════════════════════════════════════════════════════════════

	[Rpc.Broadcast]
	public void BroadcastJoinQueue( string connectionId, long steamId, string playerName, int arenaPoints, string teamData )
	{
		if ( connectionId == LocalConnectionId.ToString() ) return;

		var existing = _playersInQueue.FirstOrDefault( p => p.ConnectionId == connectionId );
		if ( existing != null )
		{
			existing.ArenaPoints = arenaPoints;
			existing.TeamData = teamData;
			existing.QueueTime = DateTime.UtcNow;
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

			OnPlayerSearchingRanked?.Invoke( playerName );
		}

		OnQueueUpdate?.Invoke( _playersInQueue.Count );
		Log.Info( $"[Arena] Player {playerName} joined queue ({_playersInQueue.Count} in queue)" );
	}

	[Rpc.Broadcast]
	public void BroadcastLeaveQueue( string connectionId )
	{
		_playersInQueue.RemoveAll( p => p.ConnectionId == connectionId );
		OnQueueUpdate?.Invoke( _playersInQueue.Count );
		Log.Info( $"[Arena] Player left queue ({_playersInQueue.Count} in queue)" );
	}

	[Rpc.Broadcast]
	public void SendMatchProposal( string targetConnectionId, string senderConnectionId, string senderName, int senderPoints, string senderTeamData, int battleSeed )
	{
		if ( targetConnectionId != LocalConnectionId.ToString() ) return;

		Log.Info( $"[Arena] Received match proposal from {senderName}" );

		_matchedOpponent = new QueuedPlayer
		{
			ConnectionId = senderConnectionId,
			PlayerName = senderName,
			ArenaPoints = senderPoints,
			TeamData = senderTeamData
		};
		BattleSeed = battleSeed;

		_playersInQueue.RemoveAll( p => p.ConnectionId == senderConnectionId || p.ConnectionId == LocalConnectionId.ToString() );

		var opponentTeam = DeserializeTeam( senderTeamData );
		CurrentOpponent = new ArenaOpponent
		{
			Name = senderName,
			ArenaPoints = senderPoints,
			Rank = GetRankFromPoints( senderPoints ),
			Team = opponentTeam,
			IsRealPlayer = true,
			ConnectionId = senderConnectionId,
			SteamId = Connection.All.FirstOrDefault( c => c.Id.ToString() == senderConnectionId )?.SteamId ?? 0
		};

		CurrentState = ArenaState.MatchFound;
		OnOpponentFound?.Invoke( CurrentOpponent );

		// Send acceptance with our normalized team
		var normalizedTeam = NormalizeTeamToLevel50( ArenaTeam );
		SendMatchAccepted( senderConnectionId, LocalConnectionId.ToString(), LocalPlayerName,
			TamerManager.Instance?.CurrentTamer?.ArenaPoints ?? 0, SerializeTeam( normalizedTeam ) );
	}

	[Rpc.Broadcast]
	public void SendMatchAccepted( string targetConnectionId, string senderConnectionId, string senderName, int senderPoints, string senderTeamData )
	{
		if ( targetConnectionId != LocalConnectionId.ToString() ) return;

		Log.Info( $"[Arena] Match accepted by {senderName}" );

		var opponentTeam = DeserializeTeam( senderTeamData );
		CurrentOpponent = new ArenaOpponent
		{
			Name = senderName,
			ArenaPoints = senderPoints,
			Rank = GetRankFromPoints( senderPoints ),
			Team = opponentTeam,
			IsRealPlayer = true,
			ConnectionId = senderConnectionId,
			SteamId = Connection.All.FirstOrDefault( c => c.Id.ToString() == senderConnectionId )?.SteamId ?? 0
		};

		_playersInQueue.RemoveAll( p => p.ConnectionId == senderConnectionId || p.ConnectionId == LocalConnectionId.ToString() );

		OnOpponentFound?.Invoke( CurrentOpponent );
	}

	[Rpc.Broadcast]
	public void SendPlayerReady( string targetConnectionId, string senderConnectionId )
	{
		if ( targetConnectionId != LocalConnectionId.ToString() ) return;

		Log.Info( $"[Arena] Opponent is ready (received from {senderConnectionId})" );
		_opponentReady = true;

		if ( _localPlayerReady && _opponentReady )
		{
			Log.Info( "[Arena] Both players ready, starting online Bo3 set!" );
			StartOnlineBattle();
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// NETWORK LISTENER
	// ═══════════════════════════════════════════════════════════════

	void INetworkListener.OnActive( Connection connection ) { }

	void INetworkListener.OnDisconnected( Connection connection )
	{
		var connectionId = connection.Id.ToString();

		_playersInQueue.RemoveAll( p => p.ConnectionId == connectionId );

		if ( _matchedOpponent?.ConnectionId == connectionId )
		{
			Log.Warning( "[Arena] Opponent disconnected!" );
			OnOpponentDisconnected?.Invoke();

			if ( CurrentState == ArenaState.InBattle || CurrentState == ArenaState.BetweenGames )
			{
				// Award win by default
				RecordGameResult( true );
				if ( CurrentState != ArenaState.Results )
				{
					// Force set completion
					SetScorePlayer = GAMES_TO_WIN_SET;
					CompleteSet();
				}
			}
			else
			{
				CurrentState = ArenaState.Idle;
				_matchedOpponent = null;
			}
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// SERIALIZATION (JSON - Full Stats)
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Serialize a team to JSON for network transfer (includes all stats, moves, traits)
	/// </summary>
	private string SerializeTeam( List<Monster> team )
	{
		var data = team.Select( m => new RankedMonsterData
		{
			S = m.SpeciesId,
			N = m.Nickname,
			OL = m.Level,
			HP = m.MaxHP,
			Atk = m.ATK,
			Def = m.DEF,
			SpA = m.SpA,
			SpD = m.SpD,
			Spe = m.SPD,
			M = m.Moves?.Select( mv => mv.MoveId ).ToList() ?? new(),
			T = m.Traits?.FirstOrDefault(),
			I = m.HeldItemId
		} ).ToList();

		return JsonSerializer.Serialize( data );
	}

	/// <summary>
	/// Deserialize a team from JSON network data
	/// </summary>
	private List<Monster> DeserializeTeam( string teamData )
	{
		if ( string.IsNullOrEmpty( teamData ) ) return new();

		try
		{
			var data = JsonSerializer.Deserialize<List<RankedMonsterData>>( teamData );
			if ( data == null ) return new();

			return data.Select( d =>
			{
				var monster = new Monster
				{
					SpeciesId = d.S,
					Nickname = d.N ?? MonsterManager.Instance?.GetSpecies( d.S )?.Name ?? "Monster",
					Level = RANKED_LEVEL,
					MaxHP = d.HP,
					ATK = d.Atk,
					DEF = d.Def,
					SpA = d.SpA,
					SpD = d.SpD,
					SPD = d.Spe,
					HeldItemId = d.I,
					Traits = !string.IsNullOrEmpty( d.T ) ? new List<string> { d.T } : new(),
					Moves = d.M?.Select( moveId => new MonsterMove { MoveId = moveId, CurrentPP = 15 } ).ToList() ?? new()
				};

				monster.CurrentHP = monster.MaxHP;
				return monster;
			} ).ToList();
		}
		catch ( Exception e )
		{
			Log.Warning( $"[Arena] Failed to deserialize team: {e.Message}" );
			return new();
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// LEADERBOARD
	// ═══════════════════════════════════════════════════════════════

	private async void SubmitScore( int score )
	{
		try
		{
			Stats.SetValue( LEADERBOARD_NAME, score );
			Log.Info( $"Submitted arena score: {score}" );
		}
		catch ( Exception e )
		{
			Log.Warning( $"Failed to submit leaderboard score: {e.Message}" );
		}
	}

	public async Task<List<LeaderboardEntry>> GetLeaderboard( int count = 100 )
	{
		if ( (DateTime.UtcNow - _lastLeaderboardFetch).TotalSeconds < LEADERBOARD_CACHE_SECONDS )
		{
			return _leaderboardCache;
		}

		try
		{
			var board = Leaderboards.GetFromStat( "publicsquare.beastborne", LEADERBOARD_NAME );
			board.MaxEntries = count;

			await board.Refresh();

			_leaderboardCache = board.Entries.Select( e => new LeaderboardEntry
			{
				Rank = (int)e.Rank,
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

	public async Task<int> GetPlayerRank()
	{
		try
		{
			var board = Leaderboards.GetFromStat( "publicsquare.beastborne", LEADERBOARD_NAME );
			await board.Refresh();
			var myName = Connection.Local?.DisplayName;
			var myEntry = board.Entries.FirstOrDefault( x => x.DisplayName == myName );
			return myEntry.DisplayName == myName ? (int)myEntry.Rank : -1;
		}
		catch
		{
			return -1;
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// RESET / CLEANUP
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Reset all arena state back to idle
	/// </summary>
	public void ResetToIdle()
	{
		CurrentState = ArenaState.Idle;
		IsInArena = false;
		IsOnlineMatch = false;
		CurrentOpponent = null;
		_matchedOpponent = null;
		_localPlayerReady = false;
		_opponentReady = false;
		SetScorePlayer = 0;
		SetScoreOpponent = 0;
		CurrentGameNumber = 0;
		GameResults.Clear();
	}

	// ═══════════════════════════════════════════════════════════════
	// STATIC UTILITIES
	// ═══════════════════════════════════════════════════════════════

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

	public static string GetRankIcon( string rank )
	{
		return rank switch
		{
			"Mythic" => "★",
			"Legendary" => "◆",
			"Master" => "♦",
			"Diamond" => "◇",
			"Platinum" => "●",
			"Gold" => "○",
			"Silver" => "▪",
			"Bronze" => "▫",
			_ => "○"
		};
	}

	/// <summary>
	/// Get a numeric tier for rank comparison (used by achievements)
	/// </summary>
	public static int GetRankTier( int points )
	{
		return points switch
		{
			>= 5000 => 8, // Mythic
			>= 3000 => 7, // Legendary
			>= 2000 => 6, // Master
			>= 1500 => 5, // Diamond
			>= 1000 => 4, // Platinum
			>= 700 => 3,  // Gold
			>= 400 => 2,  // Silver
			>= 200 => 1,  // Bronze
			_ => 0         // Unranked
		};
	}
}

// ═══════════════════════════════════════════════════════════════
// DATA CLASSES
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Arena opponent data
/// </summary>
public class ArenaOpponent
{
	public string Name { get; set; }
	public int ArenaPoints { get; set; }
	public string Rank { get; set; }
	public List<Monster> Team { get; set; } = new();

	public bool IsRealPlayer { get; set; } = false;
	public string ConnectionId { get; set; }
	public long SteamId { get; set; }
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

/// <summary>
/// Compact monster data for ranked team JSON serialization
/// </summary>
public class RankedMonsterData
{
	public string S { get; set; }        // SpeciesId
	public string N { get; set; }        // Nickname
	public int OL { get; set; }          // OriginalLevel (display only)
	public int HP { get; set; }          // MaxHP
	public int Atk { get; set; }         // ATK
	public int Def { get; set; }         // DEF
	public int SpA { get; set; }         // Special Attack
	public int SpD { get; set; }         // Special Defense
	public int Spe { get; set; }         // Speed
	public List<string> M { get; set; }  // MoveIds
	public string T { get; set; }        // Trait
	public string I { get; set; }        // HeldItemId
}
