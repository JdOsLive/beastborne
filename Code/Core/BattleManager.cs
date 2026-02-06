using Sandbox;
using Beastborne.Data;
using Beastborne.Systems;
using System.Threading.Tasks;

namespace Beastborne.Core;

/// <summary>
/// Current mode of battle input
/// </summary>
public enum BattleInputMode
{
	Auto,       // AI handles all decisions
	Manual      // Player selects moves (future feature)
}

/// <summary>
/// Manages active battles and battle state
/// </summary>
public sealed class BattleManager : Component
{
	public static BattleManager Instance { get; private set; }

	// Current battle state
	public bool IsInBattle { get; private set; }
	public bool IsTransitioning { get; private set; } // True during wave transitions to prevent UI flicker
	public List<Monster> PlayerTeam { get; private set; } = new();
	public List<Monster> EnemyTeam { get; private set; } = new();
	public BattleResult CurrentResult { get; private set; }

	// Battle playback
	public int CurrentTurnIndex { get; private set; }
	public float TurnTimer { get; private set; }
	public bool IsPlaying { get; private set; }
	public float PlaybackSpeed { get; set; } = 1.0f;

	private const float TURN_DELAY = 1.5f; // Seconds between turns during playback

	// Move-based battle system
	public bool UseMoveBased { get; set; } = true; // Toggle for new vs old system
	public BattleInputMode InputMode { get; set; } = BattleInputMode.Auto;
	public BattleState CurrentBattleState { get; private set; }

	// Preserve player's active monster between waves
	private int _lastPlayerActiveIndex = 0;

	// Manual move selection
	public string QueuedPlayerMove { get; private set; }
	public bool IsWaitingForPlayerInput { get; private set; }

	/// <summary>
	/// True when PlaybackManualTurns is actively playing back turns (prevents ManualTick interference)
	/// </summary>
	private bool isPlayingBackManualTurns = false;

	// Delayed battle end for manual mode (so player can see HP bar animation)
	private bool pendingManualBattleEnd = false;
	private float manualBattleEndTimer = 0f;
	private const float MANUAL_BATTLE_END_DELAY = 0.7f; // Seconds to wait before showing result

	// Skip animations state - execute all turns instantly, then delay before result
	private bool skipAnimationsPending = false;
	private float skipAnimationsTimer = 0f;
	private const float SKIP_ANIMATIONS_DELAY = 1.0f; // Pause so player sees final state before next wave

	// Boss state for phase tracking
	public ActiveBossState CurrentBossState { get; private set; }

	// Events
	public Action OnBattleStart;
	public Action<BattleTurn> OnTurnExecuted;
	public Action<BattleResult> OnBattleEnd;
	public Action<Monster, int> OnMonsterDamaged;
	public Action<Monster> OnMonsterDefeated;
	public Action<string> OnMoveUsed; // New event for move announcements
	public Action<PhaseTransitionResult> OnBossPhaseTransition; // Boss phase change event

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Enabled = true; // Ensure component is enabled
			Log.Info( "BattleManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	// Note: Background battle ticking is handled by GameHUD.OnUpdate via TickBackgroundExpedition()
	// BattleView UI calls ManualTick() directly when visible, so no OnUpdate needed here.

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null )
		{
			Log.Info( $"BattleManager.EnsureInstance: Instance already exists, Enabled={Instance.Enabled}" );
			return;
		}

		var go = scene.CreateObject();
		go.Name = "BattleManager";
		go.Flags = GameObjectFlags.DontDestroyOnLoad;
		var bm = go.Components.Create<BattleManager>();
		bm.Enabled = true;
		Log.Info( $"BattleManager.EnsureInstance: Created new instance, Enabled={bm.Enabled}" );
	}

	/// <summary>
	/// Set the boss state for phase tracking during boss fights
	/// </summary>
	public void SetBossState( ActiveBossState bossState )
	{
		CurrentBossState = bossState;
		if ( bossState != null )
		{
			Log.Info( $"BattleManager: Boss state set for {bossState.BossData?.SpeciesId}, IsRare={bossState.IsRareBoss}" );
		}
	}

	/// <summary>
	/// Start a new battle
	/// </summary>
	public void StartBattle( List<Monster> playerTeam, List<Monster> enemyTeam )
	{
		StartBattleWithSeed( playerTeam, enemyTeam, null );
	}

	/// <summary>
	/// Start a new battle with a specific random seed (for online sync)
	/// </summary>
	public void StartBattleWithSeed( List<Monster> playerTeam, List<Monster> enemyTeam, int? seed )
	{
		Log.Info( $"StartBattle called: playerTeam={playerTeam?.Count ?? 0}, enemyTeam={enemyTeam?.Count ?? 0}, IsInBattle={IsInBattle}, IsTransitioning={IsTransitioning}, seed={seed}" );

		// If we're in a battle and not transitioning, reject
		if ( IsInBattle && !IsTransitioning )
		{
			Log.Warning( $"Already in battle! CurrentResult turns={CurrentResult?.Turns?.Count ?? 0}" );
			return;
		}

		// Clear transitioning flag since we're starting fresh
		IsTransitioning = false;

		// Apply default battle speed from settings
		if ( SettingsManager.Instance != null )
		{
			PlaybackSpeed = SettingsManager.Instance.Settings.DefaultBattleSpeed;
		}

		if ( playerTeam == null || playerTeam.Count == 0 )
		{
			Log.Warning( "Cannot start battle: No player team!" );
			return;
		}

		if ( enemyTeam == null || enemyTeam.Count == 0 )
		{
			Log.Warning( "Cannot start battle: No enemy team!" );
			return;
		}

		// Create copies of the teams so we don't modify the original lists
		PlayerTeam = new List<Monster>( playerTeam );
		EnemyTeam = new List<Monster>( enemyTeam );

		// Heal player team to full for the battle
		foreach ( var monster in PlayerTeam )
		{
			monster?.FullHeal();
		}

		// Generate enemy stats (only if not already set - online opponents have pre-set stats)
		foreach ( var enemy in EnemyTeam )
		{
			if ( enemy != null )
			{
				// Only recalculate if stats aren't already set (deserialized online monsters have stats)
				if ( enemy.MaxHP <= 0 )
				{
					MonsterManager.Instance?.RecalculateStats( enemy );
				}
				enemy.FullHeal();
			}
		}

		// Check if we should use manual mode (auto-battle is OFF)
		// Get auto-battle setting from ExpeditionManager if available
		bool useManualMode = !IsAutoMode || (ExpeditionManager.Instance != null && !ExpeditionManager.Instance.AutoBattle);
		Log.Info( $"StartBattle: IsAutoMode={IsAutoMode}, ExpeditionAutoBattle={ExpeditionManager.Instance?.AutoBattle}, useManualMode={useManualMode}" );

		if ( useManualMode )
		{
			// Manual mode: Don't pre-simulate, set up for turn-by-turn play
			CurrentBattleState = new BattleState();
			if ( seed.HasValue )
			{
				CurrentBattleState.RandomSeed = seed.Value;
			}
			// Restore the player's active monster from previous wave (if valid)
			if ( _lastPlayerActiveIndex > 0 && _lastPlayerActiveIndex < PlayerTeam.Count && PlayerTeam[_lastPlayerActiveIndex]?.CurrentHP > 0 )
				CurrentBattleState.PlayerActiveIndex = _lastPlayerActiveIndex;

			foreach ( var m in PlayerTeam.Concat( EnemyTeam ) )
			{
				if ( m != null )
					CurrentBattleState.InitializeMonster( m.Id );
			}

			// Create empty result container for manual mode
			CurrentResult = new BattleResult();
			CurrentResult.Turns = new List<BattleTurn>();
			manualModeTurns.Clear();

			CurrentTurnIndex = 0;
			IsInBattle = true;
			IsPlaying = false;
			IsWaitingForPlayerInput = true; // Wait for player input in manual mode
			TurnTimer = 0;

			OnBattleStart?.Invoke();
			Log.Info( $"Manual battle started: {PlayerTeam.Count} vs {EnemyTeam.Count}" );
			return;
		}

		// Auto mode: Simulate the battle (with optional seed for deterministic online battles)
		try
		{
			if ( UseMoveBased )
			{
				// Use new move-based battle system
				CurrentResult = BattleSimulator.SimulateBattleWithMoves( PlayerTeam, EnemyTeam, seed, InputMode == BattleInputMode.Auto );
				CurrentBattleState = new BattleState();
				// Restore the player's active monster from previous wave (if valid)
				if ( _lastPlayerActiveIndex > 0 && _lastPlayerActiveIndex < PlayerTeam.Count && PlayerTeam[_lastPlayerActiveIndex]?.CurrentHP > 0 )
					CurrentBattleState.PlayerActiveIndex = _lastPlayerActiveIndex;
				Log.Info( $"Move-based battle simulated: {CurrentResult?.Turns?.Count ?? 0} turns, PlayerWon={CurrentResult?.PlayerWon}" );
			}
			else
			{
				// Use legacy battle system
				CurrentResult = BattleSimulator.SimulateBattle( PlayerTeam, EnemyTeam, seed );
				Log.Info( $"Legacy battle simulated: {CurrentResult?.Turns?.Count ?? 0} turns, PlayerWon={CurrentResult?.PlayerWon}" );
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"Failed to simulate battle: {ex.Message}\n{ex.StackTrace}" );
			return;
		}

		CurrentTurnIndex = 0;
		IsInBattle = true;
		IsPlaying = false;
		IsWaitingForPlayerInput = false; // Auto-battle doesn't wait for input
		TurnTimer = 0;
		skipAnimationsPending = false;

		OnBattleStart?.Invoke();
		Log.Info( $"Auto battle started: {PlayerTeam.Count} vs {EnemyTeam.Count}" );
	}

	/// <summary>
	/// Start playback of the battle
	/// </summary>
	/// <param name="userInitiated">True if user explicitly clicked play (allows override of waiting state)</param>
	public void StartPlayback( bool userInitiated = false )
	{
		Log.Info( $"StartPlayback called: IsInBattle={IsInBattle}, CurrentResult={CurrentResult != null}, Turns={CurrentResult?.Turns?.Count ?? 0}, IsWaitingForPlayerInput={IsWaitingForPlayerInput}, userInitiated={userInitiated}" );
		if ( !IsInBattle )
		{
			Log.Warning( "StartPlayback: Not in battle!" );
			return;
		}
		if ( CurrentResult == null )
		{
			Log.Warning( "StartPlayback: No CurrentResult!" );
			return;
		}

		// Don't auto-play in manual mode when waiting for input
		// But allow if user explicitly clicked the play button
		if ( IsWaitingForPlayerInput && !userInitiated )
		{
			Log.Info( "StartPlayback: In manual mode, waiting for player input - not auto-playing" );
			return;
		}

		IsPlaying = true;
		TurnTimer = 0;
		Log.Info( $"StartPlayback: IsPlaying set to {IsPlaying}" );
	}

	/// <summary>
	/// Pause playback
	/// </summary>
	public void PausePlayback()
	{
		IsPlaying = false;
	}

	/// <summary>
	/// Skip to next turn
	/// </summary>
	public void SkipToNextTurn()
	{
		if ( !IsInBattle || CurrentResult == null ) return;
		ExecuteNextTurn();
	}

	/// <summary>
	/// Skip to end of battle
	/// </summary>
	public void SkipToEnd()
	{
		if ( !IsInBattle || CurrentResult == null ) return;

		while ( CurrentTurnIndex < CurrentResult.Turns.Count )
		{
			ExecuteNextTurn();
		}

		EndBattle();
	}

	/// <summary>
	/// Manual tick for UI-driven updates (backup when OnUpdate doesn't run)
	/// </summary>
	public void ManualTick( float delta )
	{
		// Handle delayed battle end for manual mode (lets player see HP bar animation)
		if ( pendingManualBattleEnd )
		{
			manualBattleEndTimer -= delta;
			if ( manualBattleEndTimer <= 0 )
			{
				pendingManualBattleEnd = false;
				EndBattleManual();
			}
			return;
		}

		if ( !IsInBattle || !IsPlaying || CurrentResult == null )
			return;

		// In manual mode, don't auto-advance if waiting for player input
		if ( IsWaitingForPlayerInput )
			return;

		// Don't process if PlaybackManualTurns is handling playback (prevents race condition)
		if ( isPlayingBackManualTurns )
			return;

		// Skip animations: execute all turns instantly, then wait briefly before ending
		if ( SettingsManager.Instance?.Settings?.SkipBattleAnimations == true && CurrentResult?.Turns != null )
		{
			// Already executed all turns, waiting for delay
			if ( skipAnimationsPending )
			{
				skipAnimationsTimer -= delta * PlaybackSpeed;
				if ( skipAnimationsTimer <= 0 )
				{
					skipAnimationsPending = false;
					Log.Info( $"[Battle] Skip animations: Ending battle. PlayerWon={CurrentResult.PlayerWon}" );
					EndBattle();
				}
				return;
			}

			// Execute all remaining turns at once
			if ( CurrentTurnIndex < CurrentResult.Turns.Count )
			{
				while ( CurrentTurnIndex < CurrentResult.Turns.Count )
				{
					ExecuteNextTurn();
				}
				// Brief pause so player sees the final HP state
				skipAnimationsPending = true;
				skipAnimationsTimer = SKIP_ANIMATIONS_DELAY;
			}
			return;
		}

		TurnTimer += delta * PlaybackSpeed;

		if ( TurnTimer >= TURN_DELAY )
		{
			TurnTimer = 0;
			ExecuteNextTurn();

			// Check if battle is over (but not if we're in manual mode with no turns yet)
			if ( CurrentTurnIndex >= CurrentResult.Turns.Count && CurrentResult.Turns.Count > 0 )
			{
				Log.Info( $"[Battle] All turns executed ({CurrentTurnIndex}/{CurrentResult.Turns.Count}), ending battle. PlayerWon={CurrentResult.PlayerWon}" );
				EndBattle();
			}
		}
	}

	private void UpdateBattlePlayback()
	{
		if ( CurrentResult == null || CurrentResult.Turns == null )
		{
			IsPlaying = false;
			return;
		}

		// Skip animations: execute all turns instantly, then wait briefly before ending
		if ( SettingsManager.Instance?.Settings?.SkipBattleAnimations == true )
		{
			if ( skipAnimationsPending )
			{
				skipAnimationsTimer -= Time.Delta;
				if ( skipAnimationsTimer <= 0 )
				{
					skipAnimationsPending = false;
					EndBattle();
				}
				return;
			}

			if ( CurrentTurnIndex < CurrentResult.Turns.Count )
			{
				while ( CurrentTurnIndex < CurrentResult.Turns.Count )
				{
					ExecuteNextTurn();
				}
				skipAnimationsPending = true;
				skipAnimationsTimer = SKIP_ANIMATIONS_DELAY;
			}
			return;
		}

		TurnTimer += Time.Delta * PlaybackSpeed;

		if ( TurnTimer >= TURN_DELAY )
		{
			TurnTimer = 0;
			ExecuteNextTurn();

			// Check if battle is over
			if ( CurrentTurnIndex >= CurrentResult.Turns.Count )
			{
				EndBattle();
			}
		}
	}

	private void ExecuteNextTurn()
	{
		// Safety checks
		if ( CurrentResult == null || CurrentResult.Turns == null )
			return;

		if ( CurrentTurnIndex >= CurrentResult.Turns.Count ) return;

		var turn = CurrentResult.Turns[CurrentTurnIndex];
		if ( turn == null )
		{
			CurrentTurnIndex++;
			return;
		}

		// Handle swap turns
		if ( turn.IsSwap )
		{
			OnMoveUsed?.Invoke( $"{turn.AttackerName} switched to {turn.SwapToName}!" );
			OnTurnExecuted?.Invoke( turn );
			CurrentTurnIndex++;
			return;
		}

		// Handle status message turns (paralyzed, asleep, confused, etc.)
		if ( !string.IsNullOrEmpty( turn.StatusMessage ) && turn.DefenderId == Guid.Empty )
		{
			OnMoveUsed?.Invoke( turn.StatusMessage );
			OnTurnExecuted?.Invoke( turn );
			CurrentTurnIndex++;
			return;
		}

		// Handle move announcements
		if ( !string.IsNullOrEmpty( turn.MoveName ) )
		{
			string announcement = $"{turn.AttackerName} used {turn.MoveName}!";
			OnMoveUsed?.Invoke( announcement );
		}

		// Handle miss
		if ( turn.IsMiss )
		{
			OnMoveUsed?.Invoke( $"{turn.AttackerName}'s attack missed!" );
			OnTurnExecuted?.Invoke( turn );
			CurrentTurnIndex++;
			return;
		}

		// Find the actual monsters
		var defender = FindMonster( turn.DefenderId );
		if ( defender != null && turn.Damage > 0 )
		{
			defender.CurrentHP = turn.DefenderHPAfter;
			OnMonsterDamaged?.Invoke( defender, turn.Damage );

			// Check for boss phase transition
			if ( defender.IsBoss && defender.CurrentHP > 0 )
			{
				CheckBossPhaseTransition( defender );
			}

			// Announce effectiveness
			if ( turn.IsSuperEffective )
			{
				OnMoveUsed?.Invoke( "It's super effective!" );
			}
			else if ( turn.IsResisted )
			{
				OnMoveUsed?.Invoke( "It's not very effective..." );
			}

			// Announce critical hit
			if ( turn.IsCritical )
			{
				OnMoveUsed?.Invoke( "A critical hit!" );
			}

			if ( defender.CurrentHP <= 0 )
			{
				OnMonsterDefeated?.Invoke( defender );
			}
		}

		// Announce effect messages
		if ( turn.EffectMessages != null )
		{
			foreach ( var msg in turn.EffectMessages )
			{
				OnMoveUsed?.Invoke( msg );
			}
		}

		OnTurnExecuted?.Invoke( turn );
		CurrentTurnIndex++;
	}

	private Monster FindMonster( Guid id )
	{
		if ( PlayerTeam == null && EnemyTeam == null )
			return null;

		return PlayerTeam?.FirstOrDefault( m => m?.Id == id )
			?? EnemyTeam?.FirstOrDefault( m => m?.Id == id );
	}

	private void EndBattle()
	{
		int handlerCount = OnBattleEnd?.GetInvocationList()?.Length ?? 0;
		int turnsPlayed = CurrentResult?.Turns?.Count ?? 0;
		Log.Info( $"[Battle] EndBattle called. CurrentResult={CurrentResult != null}, PlayerWon={CurrentResult?.PlayerWon}, TurnsPlayed={turnsPlayed}, HandlerCount={handlerCount}" );

		// SAFEGUARD: Don't end battle as a defeat if no turns have been played
		// This prevents false defeats when starting in manual mode
		if ( turnsPlayed == 0 && CurrentResult?.PlayerWon != true )
		{
			Log.Warning( "EndBattle called with 0 turns played and not a win - ignoring to prevent false defeat" );
			return;
		}

		IsPlaying = false;

		// Preserve the active monster index for the next wave
		if ( CurrentBattleState != null )
			_lastPlayerActiveIndex = CurrentBattleState.PlayerActiveIndex;

		if ( CurrentResult == null )
		{
			Log.Warning( "EndBattle called but CurrentResult is null" );
			OnBattleEnd?.Invoke( null );
			return;
		}

		if ( CurrentResult.PlayerWon )
		{
			// Award XP and gold
			DistributeRewards();
		}

		var result = CurrentResult;
		Log.Info( $"Battle ended. Player won: {result.PlayerWon}, invoking {handlerCount} handlers" );
		OnBattleEnd?.Invoke( result );
	}

	private void DistributeRewards()
	{
		// Give gold and tamer XP
		TamerManager.Instance?.AddGold( CurrentResult.TotalGold );
		TamerManager.Instance?.AddXP( CurrentResult.TotalXP );
		Log.Info( $"[Battle] DistributeRewards: Awarded {CurrentResult.TotalGold} gold, {CurrentResult.TotalXP} XP to tamer" );

		// Track veteran stats from battle turns
		TrackVeteranStats();

		// Distribute XP to all party monsters, split evenly
		int xpPerMonster = PlayerTeam.Count > 0 ? CurrentResult.TotalXP / PlayerTeam.Count : 0;
		int goldPerMonster = PlayerTeam.Count > 0 ? CurrentResult.TotalGold / PlayerTeam.Count : 0;

		foreach ( var monster in PlayerTeam )
		{
			bool leveledUp = monster.GainXP( xpPerMonster );
			if ( leveledUp )
			{
				// Recalculate stats when leveling up
				MonsterManager.Instance?.RecalculateStats( monster );

				// Check for new moves to learn
				var learnedMoves = MonsterManager.Instance?.CheckAndLearnNewMoves( monster );
				if ( learnedMoves != null && learnedMoves.Count > 0 )
				{
					foreach ( var moveName in learnedMoves )
					{
						NotificationManager.Instance?.AddNotification(
							NotificationType.Success,
							"New Move Learned!",
							$"{monster.Nickname} learned {moveName}!"
						);
					}
				}

				// Evolution is now manual - done via the monster detail panel
				// Notify if monster can now evolve (only once per monster)
				var species = MonsterManager.Instance?.GetSpecies( monster.SpeciesId );
				if ( species != null && monster.CanEvolve( species ) && !monster.HasBeenNotifiedForEvolution )
				{
					monster.HasBeenNotifiedForEvolution = true;
					var evolvedSpecies = MonsterManager.Instance?.GetSpecies( species.EvolvesTo );
					NotificationManager.Instance?.NotifyEvolutionReady(
						monster.Nickname,
						evolvedSpecies?.Name ?? "???"
					);
				}
			}
		}

		// Update tamer battle stats
		if ( TamerManager.Instance?.CurrentTamer != null )
		{
			TamerManager.Instance.CurrentTamer.TotalBattlesWon++;
		}

		// Update contract satisfaction for participating monsters
		bool hasCompanions = PlayerTeam.Count( m => m.CurrentHP > 0 ) > 1;

		foreach ( var monster in PlayerTeam )
		{
			if ( monster.Contract == null ) continue;

			// Update battle-related demands
			foreach ( var demand in monster.Contract.SecondaryDemands.Prepend( monster.Contract.PrimaryDemand ) )
			{
				bool progressMade = false;

				switch ( demand.Type )
				{
					case ContractDemandType.Bloodthirsty:
						// Wants battles - any participation counts
						demand.CurrentProgress++;
						progressMade = true;
						break;

					case ContractDemandType.Greedy:
						// Wants gold - progress based on gold earned
						if ( monster.CurrentHP > 0 && goldPerMonster > 0 )
						{
							demand.CurrentProgress += goldPerMonster;
							progressMade = true;
						}
						break;

					case ContractDemandType.Ambitious:
						// Wants to level up - progress based on XP gained
						if ( monster.CurrentHP > 0 && xpPerMonster > 0 )
						{
							demand.CurrentProgress += xpPerMonster;
							progressMade = true;
						}
						break;

					case ContractDemandType.Social:
						// Wants companions - satisfied when fighting with allies
						if ( hasCompanions )
						{
							demand.CurrentProgress++;
							progressMade = true;
						}
						break;

					case ContractDemandType.Competitive:
						// Wants to win - only counts victories
						if ( CurrentResult.PlayerWon && monster.CurrentHP > 0 )
						{
							demand.CurrentProgress++;
							progressMade = true;
						}
						break;

					case ContractDemandType.Lazy:
						// Lazy monsters don't like being in too many battles
						// This is handled negatively - frequent battles decrease satisfaction
						// We track battles and penalize if too many in a short time
						// For now, we don't give positive progress in battles
						break;
				}

				// Check if demand was satisfied
				if ( progressMade && demand.CurrentProgress >= demand.RequiredAmount )
				{
					int satisfactionGain = demand == monster.Contract.PrimaryDemand ? 10 : 5;
					monster.Contract.UpdateSatisfaction( satisfactionGain );
					demand.CurrentProgress = 0; // Reset for next cycle
				}
			}
		}

		// Save progress
		MonsterManager.Instance?.SaveMonsters();
		TamerManager.Instance?.SaveToCloud();
	}

	/// <summary>
	/// Track veteran stats from battle turns (damage dealt, KOs, battles fought)
	/// </summary>
	private void TrackVeteranStats()
	{
		if ( CurrentResult?.Turns == null || PlayerTeam == null )
			return;

		// Track damage dealt and knockouts per monster
		var damageByMonster = new Dictionary<Guid, int>();
		var kosByMonster = new Dictionary<Guid, int>();

		foreach ( var turn in CurrentResult.Turns )
		{
			// Skip non-attack turns
			if ( turn.Damage <= 0 || !turn.IsPlayerAttacker )
				continue;

			var attackerId = turn.AttackerId;

			// Track damage dealt
			if ( !damageByMonster.ContainsKey( attackerId ) )
				damageByMonster[attackerId] = 0;
			damageByMonster[attackerId] += turn.Damage;

			// Track knockouts (defender HP after is 0)
			if ( turn.DefenderHPAfter <= 0 )
			{
				if ( !kosByMonster.ContainsKey( attackerId ) )
					kosByMonster[attackerId] = 0;
				kosByMonster[attackerId]++;
			}
		}

		// Apply stats to actual monsters
		foreach ( var monster in PlayerTeam )
		{
			if ( monster == null ) continue;

			// Find the original monster in MonsterManager
			var ownedMonster = MonsterManager.Instance?.GetMonster( monster.Id );
			if ( ownedMonster == null ) continue;

			// Increment battles fought
			var previousRank = ownedMonster.GetVeteranRank();
			ownedMonster.BattlesFought++;

			// Add damage dealt
			if ( damageByMonster.TryGetValue( monster.Id, out var damage ) )
			{
				ownedMonster.TotalDamageDealt += damage;
			}

			// Add knockouts
			if ( kosByMonster.TryGetValue( monster.Id, out var kos ) )
			{
				ownedMonster.TotalKnockouts += kos;
			}

			// Check for boss defeat
			if ( CurrentBossState != null && CurrentResult.PlayerWon )
			{
				ownedMonster.BossesDefeated++;

				// Add journal entry for boss defeat with species ID and zone ID
				var bossSpeciesId = CurrentBossState.BossData?.SpeciesId;
				var bossSpecies = MonsterManager.Instance?.GetSpecies( bossSpeciesId );
				var bossName = bossSpecies?.Name ?? "a powerful boss";
				var expeditionZoneId = ExpeditionManager.Instance?.CurrentExpedition?.Id;
				ownedMonster.AddJournalEntry(
					$"Helped defeat {bossName}!",
					JournalEntryType.BossDefeat,
					speciesId: bossSpeciesId,
					zoneId: expeditionZoneId
				);
			}

			// Check for battle mastery rank milestone
			var newRank = ownedMonster.GetVeteranRank();
			if ( newRank != previousRank && newRank != VeteranRank.Rookie )
			{
				ownedMonster.AddJournalEntry(
					$"Achieved {newRank} mastery after {ownedMonster.BattlesFought} battles!",
					JournalEntryType.Milestone
				);

				// Notify player of rank up
				NotificationManager.Instance?.AddNotification(
					NotificationType.Success,
					"Battle Mastery Rank Up!",
					$"{ownedMonster.Nickname} is now a {newRank}! (+{(int)(ownedMonster.GetVeteranBonusPercent() * 100)}% stats)"
				);
			}
		}
	}

	/// <summary>
	/// End the current battle and cleanup
	/// </summary>
	public void ExitBattle()
	{
		Log.Info( "ExitBattle called" );
		IsInBattle = false;
		IsPlaying = false;
		IsTransitioning = false;
		isPlayingBackManualTurns = false;
		pendingManualBattleEnd = false;
		PlayerTeam.Clear();
		EnemyTeam.Clear();
		CurrentResult = null;
		CurrentTurnIndex = 0;
		CurrentBattleState = null;
	}

	/// <summary>
	/// Prepare for transitioning to the next wave without clearing teams (prevents UI flicker)
	/// Call this before starting the next battle
	/// </summary>
	public void PrepareForNextWave()
	{
		Log.Info( "PrepareForNextWave: Setting transition mode" );
		IsTransitioning = true;
		IsPlaying = false;
		// Don't clear teams - keep them for UI stability
		// The next StartBattle call will replace them
	}

	/// <summary>
	/// Exit battle state for wave transition (clears state but leaves transitioning flag)
	/// </summary>
	public void ExitBattleForTransition()
	{
		Log.Info( "ExitBattleForTransition called" );
		IsInBattle = false;
		IsPlaying = false;
		// Keep IsTransitioning = true
		// Keep teams for UI stability during transition
		CurrentResult = null;
		CurrentTurnIndex = 0;
	}

	/// <summary>
	/// Get the current HP of a monster in battle
	/// </summary>
	public int GetCurrentHP( Guid monsterId )
	{
		var monster = FindMonster( monsterId );
		return monster?.CurrentHP ?? 0;
	}

	/// <summary>
	/// Check if a monster is still alive in the current battle
	/// </summary>
	public bool IsMonsterAlive( Guid monsterId )
	{
		return GetCurrentHP( monsterId ) > 0;
	}

	/// <summary>
	/// Queue a player's move selection for the next turn
	/// </summary>
	public void QueuePlayerMove( string moveId )
	{
		Log.Info( $"BattleManager: Queuing player move - {moveId}" );
		QueuedPlayerMove = moveId;
		IsWaitingForPlayerInput = false;
	}

	/// <summary>
	/// Queue a swap action for the player
	/// </summary>
	public void QueuePlayerSwap( int swapToIndex )
	{
		Log.Info( $"BattleManager: Queuing player swap to index {swapToIndex}" );
		// For now, swaps are handled similarly - we could store the swap index
		// and process it during the next turn execution
		IsWaitingForPlayerInput = false;
	}

	/// <summary>
	/// Set whether we're waiting for player input (for manual mode)
	/// </summary>
	public void SetWaitingForInput( bool waiting )
	{
		IsWaitingForPlayerInput = waiting;
		if ( waiting )
		{
			// Pause playback when waiting for input
			IsPlaying = false;
		}
	}

	/// <summary>
	/// Clear the queued player move
	/// </summary>
	public void ClearQueuedMove()
	{
		QueuedPlayerMove = null;
	}

	/// <summary>
	/// Get the currently active player monster (first alive monster)
	/// </summary>
	public Monster GetActivePlayerMonster()
	{
		if ( PlayerTeam == null ) return null;

		// Use the battle state's active index if in battle
		if ( CurrentBattleState != null && CurrentBattleState.PlayerActiveIndex >= 0 && CurrentBattleState.PlayerActiveIndex < PlayerTeam.Count )
		{
			var active = PlayerTeam[CurrentBattleState.PlayerActiveIndex];
			if ( active?.CurrentHP > 0 )
				return active;
		}

		// Fallback: return first alive monster
		foreach ( var monster in PlayerTeam )
		{
			if ( monster?.CurrentHP > 0 )
				return monster;
		}
		return null;
	}

	/// <summary>
	/// Get the currently active enemy monster (first alive monster)
	/// </summary>
	public Monster GetActiveEnemyMonster()
	{
		if ( EnemyTeam == null ) return null;

		// Use the battle state's active index if in battle
		if ( CurrentBattleState != null && CurrentBattleState.EnemyActiveIndex >= 0 && CurrentBattleState.EnemyActiveIndex < EnemyTeam.Count )
		{
			var active = EnemyTeam[CurrentBattleState.EnemyActiveIndex];
			if ( active?.CurrentHP > 0 )
				return active;
		}

		// Fallback: return first alive monster
		foreach ( var monster in EnemyTeam )
		{
			if ( monster?.CurrentHP > 0 )
				return monster;
		}
		return null;
	}

	// ============================================
	// MANUAL MODE - TURN BY TURN EXECUTION
	// ============================================

	/// <summary>
	/// Whether we're in manual battle mode (turn-by-turn)
	/// </summary>
	public bool IsManualMode => InputMode == BattleInputMode.Manual && !IsAutoMode;

	/// <summary>
	/// Whether auto-battle is enabled (default to OFF for better new player experience)
	/// </summary>
	public bool IsAutoMode { get; set; } = false;

	/// <summary>
	/// List of turns accumulated during manual mode
	/// </summary>
	private List<BattleTurn> manualModeTurns = new();

	/// <summary>
	/// Horde system: Player's selected target (first alive enemy if not set)
	/// </summary>
	public Guid PlayerTargetId { get; private set; } = Guid.Empty;

	/// <summary>
	/// Pending swap index for manual mode (-1 = no swap pending)
	/// </summary>
	private int pendingSwapIndex = -1;

	/// <summary>
	/// Start a battle in manual mode (doesn't pre-simulate)
	/// </summary>
	public void StartManualBattle( List<Monster> playerTeam, List<Monster> enemyTeam, bool isArena = false )
	{
		Log.Info( $"StartManualBattle called: playerTeam={playerTeam?.Count ?? 0}, enemyTeam={enemyTeam?.Count ?? 0}, isArena={isArena}" );

		if ( IsInBattle && !IsTransitioning )
		{
			Log.Warning( "Already in battle!" );
			return;
		}

		IsTransitioning = false;

		// Apply default battle speed from settings
		if ( SettingsManager.Instance != null )
		{
			PlaybackSpeed = SettingsManager.Instance.Settings.DefaultBattleSpeed;
		}

		if ( playerTeam == null || playerTeam.Count == 0 )
		{
			Log.Warning( "Cannot start battle: No player team!" );
			return;
		}

		if ( enemyTeam == null || enemyTeam.Count == 0 )
		{
			Log.Warning( "Cannot start battle: No enemy team!" );
			return;
		}

		// Create copies of the teams
		PlayerTeam = new List<Monster>( playerTeam );
		EnemyTeam = new List<Monster>( enemyTeam );

		// Heal player team to full
		foreach ( var monster in PlayerTeam )
		{
			monster?.FullHeal();
		}

		// Setup enemy stats
		foreach ( var enemy in EnemyTeam )
		{
			if ( enemy != null )
			{
				if ( enemy.MaxHP <= 0 )
				{
					MonsterManager.Instance?.RecalculateStats( enemy );
				}
				enemy.FullHeal();
			}
		}

		// Initialize battle state for turn-by-turn
		CurrentBattleState = new BattleState();
		CurrentBattleState.IsArenaMode = isArena;
		foreach ( var m in PlayerTeam.Concat( EnemyTeam ) )
		{
			if ( m != null )
				CurrentBattleState.InitializeMonster( m.Id );
		}

		// Restore the player's active monster from previous wave (if valid)
		if ( _lastPlayerActiveIndex > 0 && _lastPlayerActiveIndex < PlayerTeam.Count && PlayerTeam[_lastPlayerActiveIndex]?.CurrentHP > 0 )
			CurrentBattleState.PlayerActiveIndex = _lastPlayerActiveIndex;

		// Create result container for manual mode
		CurrentResult = new BattleResult();
		CurrentResult.Turns = new List<BattleTurn>();
		manualModeTurns.Clear();

		CurrentTurnIndex = 0;
		IsInBattle = true;
		IsPlaying = false;
		IsWaitingForPlayerInput = true;
		isPlayingBackManualTurns = false;
		pendingManualBattleEnd = false;
		skipAnimationsPending = false;
		TurnTimer = 0;

		OnBattleStart?.Invoke();
		Log.Info( $"Manual battle started: {PlayerTeam.Count} vs {EnemyTeam.Count}, isArena={isArena}" );
	}

	/// <summary>
	/// Start a manual battle with a specific random seed (for online sync)
	/// </summary>
	public void StartManualBattleWithSeed( List<Monster> playerTeam, List<Monster> enemyTeam, int? seed, bool isArena = false )
	{
		Log.Info( $"StartManualBattleWithSeed called: playerTeam={playerTeam?.Count ?? 0}, enemyTeam={enemyTeam?.Count ?? 0}, seed={seed}, isArena={isArena}" );

		if ( IsInBattle && !IsTransitioning )
		{
			Log.Warning( "Already in battle!" );
			return;
		}

		IsTransitioning = false;

		if ( playerTeam == null || playerTeam.Count == 0 )
		{
			Log.Warning( "Cannot start battle: No player team!" );
			return;
		}

		if ( enemyTeam == null || enemyTeam.Count == 0 )
		{
			Log.Warning( "Cannot start battle: No enemy team!" );
			return;
		}

		// Create copies of the teams
		PlayerTeam = new List<Monster>( playerTeam );
		EnemyTeam = new List<Monster>( enemyTeam );

		// Heal player team to full
		foreach ( var monster in PlayerTeam )
		{
			monster?.FullHeal();
		}

		// Setup enemy stats
		foreach ( var enemy in EnemyTeam )
		{
			if ( enemy != null )
			{
				if ( enemy.MaxHP <= 0 )
				{
					MonsterManager.Instance?.RecalculateStats( enemy );
				}
				enemy.FullHeal();
			}
		}

		// Initialize battle state for turn-by-turn with optional seed
		CurrentBattleState = new BattleState();
		if ( seed.HasValue )
		{
			CurrentBattleState.RandomSeed = seed.Value;
		}
		CurrentBattleState.IsArenaMode = isArena;
		foreach ( var m in PlayerTeam.Concat( EnemyTeam ) )
		{
			if ( m != null )
				CurrentBattleState.InitializeMonster( m.Id );
		}

		// Restore the player's active monster from previous wave (if valid)
		if ( _lastPlayerActiveIndex > 0 && _lastPlayerActiveIndex < PlayerTeam.Count && PlayerTeam[_lastPlayerActiveIndex]?.CurrentHP > 0 )
			CurrentBattleState.PlayerActiveIndex = _lastPlayerActiveIndex;

		// Create result container for manual mode
		CurrentResult = new BattleResult();
		CurrentResult.Turns = new List<BattleTurn>();
		manualModeTurns.Clear();

		CurrentTurnIndex = 0;
		IsInBattle = true;
		IsPlaying = false;
		IsWaitingForPlayerInput = true;
		isPlayingBackManualTurns = false;
		pendingManualBattleEnd = false;
		skipAnimationsPending = false;
		TurnTimer = 0;

		OnBattleStart?.Invoke();
		Log.Info( $"Manual battle with seed started: {PlayerTeam.Count} vs {EnemyTeam.Count}, seed={seed}, isArena={isArena}" );
	}

	/// <summary>
	/// Execute the player's chosen move and continue the battle
	/// </summary>
	public void ExecutePlayerMove( string moveId )
	{
		if ( !IsInBattle || !IsWaitingForPlayerInput )
		{
			Log.Warning( $"ExecutePlayerMove: Invalid state - IsInBattle={IsInBattle}, IsWaitingForPlayerInput={IsWaitingForPlayerInput}" );
			return;
		}

		Log.Info( $"ExecutePlayerMove: Executing move {moveId}, target={PlayerTargetId}, swapIdx={pendingSwapIndex}" );

		// Execute a single turn with player's selected target
		var turnResults = BattleSimulator.ExecuteSingleTurn(
			PlayerTeam,
			EnemyTeam,
			CurrentBattleState,
			moveId,
			PlayerTargetId != Guid.Empty ? PlayerTargetId : null,
			pendingSwapIndex
		);

		Log.Info( $"ExecutePlayerMove: Generated {turnResults.Count} turns" );
		foreach ( var turn in turnResults )
		{
			Log.Info( $"  Turn: {turn.AttackerName} -> {turn.DefenderName}, Move={turn.MoveName}, Damage={turn.Damage}, IsPlayerAttacker={turn.IsPlayerAttacker}" );
		}

		// Add turns to result
		CurrentResult.Turns.AddRange( turnResults );
		manualModeTurns.AddRange( turnResults );

		// Process turns and check if battle is over
		IsWaitingForPlayerInput = false;
		PlaybackManualTurns( turnResults );
	}

	/// <summary>
	/// Execute a swap action for the player
	/// </summary>
	public void ExecutePlayerSwap( int swapToIndex )
	{
		if ( !IsInBattle || !IsWaitingForPlayerInput )
		{
			Log.Warning( "ExecutePlayerSwap: Invalid state" );
			return;
		}

		// Validate swap target
		if ( swapToIndex < 0 || swapToIndex >= PlayerTeam.Count )
		{
			Log.Warning( $"ExecutePlayerSwap: Invalid swap index {swapToIndex}" );
			return;
		}

		if ( PlayerTeam[swapToIndex].CurrentHP <= 0 )
		{
			Log.Warning( $"ExecutePlayerSwap: Target monster is KO'd" );
			return;
		}

		// Can't swap to the same monster
		if ( swapToIndex == CurrentBattleState.PlayerActiveIndex )
		{
			Log.Warning( $"ExecutePlayerSwap: Already the active monster" );
			return;
		}

		Log.Info( $"ExecutePlayerSwap: Swapping to index {swapToIndex}" );

		// Store the swap index and execute
		pendingSwapIndex = swapToIndex;
		ExecutePlayerMove( "swap" );
		pendingSwapIndex = -1;
	}

	/// <summary>
	/// Process turns from manual mode synchronously (no async delays - UI handles animations)
	/// </summary>
	private void PlaybackManualTurns( List<BattleTurn> turns )
	{
		Log.Info( $"PlaybackManualTurns: Processing {turns.Count} turns" );
		isPlayingBackManualTurns = true;

		foreach ( var turn in turns )
		{
			// Announce move
			if ( !string.IsNullOrEmpty( turn.MoveName ) )
			{
				OnMoveUsed?.Invoke( $"{turn.AttackerName} used {turn.MoveName}!" );
			}
			else if ( !string.IsNullOrEmpty( turn.StatusMessage ) )
			{
				OnMoveUsed?.Invoke( turn.StatusMessage );
			}
			else if ( turn.IsSwap )
			{
				OnMoveUsed?.Invoke( $"{turn.SwapToName} was sent out!" );
			}

			// Apply damage to monster objects (needed for IsBattleOver to work)
			if ( turn.DefenderId != Guid.Empty && turn.Damage > 0 )
			{
				var defender = FindMonster( turn.DefenderId );
				if ( defender != null )
				{
					defender.CurrentHP = turn.DefenderHPAfter;
					OnMonsterDamaged?.Invoke( defender, turn.Damage );

					// Check for boss phase transition
					if ( defender.IsBoss && defender.CurrentHP > 0 )
					{
						CheckBossPhaseTransition( defender );
					}
				}
			}

			OnTurnExecuted?.Invoke( turn );

			// Effectiveness/crit announcements
			if ( turn.IsSuperEffective )
				OnMoveUsed?.Invoke( "It's super effective!" );
			else if ( turn.IsResisted )
				OnMoveUsed?.Invoke( "It's not very effective..." );

			if ( turn.IsCritical )
				OnMoveUsed?.Invoke( "A critical hit!" );

			// KO check
			if ( turn.DefenderHPAfter <= 0 && turn.DefenderId != Guid.Empty )
			{
				var defender = FindMonster( turn.DefenderId );
				if ( defender != null )
				{
					OnMoveUsed?.Invoke( $"{turn.DefenderName} fainted!" );
					OnMonsterDefeated?.Invoke( defender );
				}
			}

			// Effect messages
			if ( turn.EffectMessages != null )
			{
				foreach ( var msg in turn.EffectMessages )
				{
					OnMoveUsed?.Invoke( msg );
				}
			}
		}

		isPlayingBackManualTurns = false;

		// Check if battle is over (all enemies defeated)
		bool battleOver = BattleSimulator.IsBattleOver( PlayerTeam, EnemyTeam );
		Log.Info( $"PlaybackManualTurns: Done. BattleOver={battleOver}" );

		if ( battleOver )
		{
			// Determine winner
			CurrentResult.PlayerWon = !BattleSimulator.IsTeamDefeated( PlayerTeam );
			CurrentResult.TotalTurns = CurrentBattleState?.TurnNumber ?? 0;

			// Calculate rewards
			if ( CurrentResult.PlayerWon && PlayerTeam.Count > 0 )
			{
				int avgLevel = (int)PlayerTeam.Average( p => p.Level );
				foreach ( var enemy in EnemyTeam )
				{
					CurrentResult.TotalXP += BattleSimulator.CalculateXPGain( enemy, avgLevel );
					CurrentResult.TotalGold += BattleSimulator.CalculateGoldDrop( enemy );
				}
			}

			// Skip animations: end battle immediately without delay
			if ( SettingsManager.Instance?.Settings?.SkipBattleAnimations == true )
			{
				Log.Info( $"PlaybackManualTurns: Battle over (skip animations), ending immediately. PlayerWon={CurrentResult.PlayerWon}" );
				IsWaitingForPlayerInput = false;
				EndBattleManual();
				return;
			}

			// Delay battle end so player can see HP bar animation
			Log.Info( $"PlaybackManualTurns: Battle over, waiting {MANUAL_BATTLE_END_DELAY}s for animations. PlayerWon={CurrentResult.PlayerWon}" );
			IsWaitingForPlayerInput = false;
			IsPlaying = true; // Keep ticking so ManualTick processes the delayed end
			pendingManualBattleEnd = true;
			manualBattleEndTimer = MANUAL_BATTLE_END_DELAY;
		}
		else
		{
			// Battle continues - wait for next player input
			IsPlaying = false;
			IsWaitingForPlayerInput = true;
		}
	}

	/// <summary>
	/// End a manual mode battle
	/// </summary>
	private void EndBattleManual()
	{
		int turnsPlayed = CurrentResult?.Turns?.Count ?? 0;
		int handlerCount = OnBattleEnd?.GetInvocationList()?.Length ?? 0;
		Log.Info( $"EndBattleManual: PlayerWon={CurrentResult?.PlayerWon}, TurnsPlayed={turnsPlayed}, HandlerCount={handlerCount}" );

		// SAFEGUARD: Don't end battle as a defeat if no turns have been played
		// This prevents false defeats when starting in manual mode
		if ( turnsPlayed == 0 && CurrentResult?.PlayerWon != true )
		{
			Log.Warning( "EndBattleManual called with 0 turns played and not a win - ignoring to prevent false defeat" );
			return;
		}

		IsPlaying = false;

		// Preserve the active monster index for the next wave
		if ( CurrentBattleState != null )
			_lastPlayerActiveIndex = CurrentBattleState.PlayerActiveIndex;

		if ( CurrentResult == null )
		{
			Log.Warning( "EndBattleManual: CurrentResult is null" );
			OnBattleEnd?.Invoke( null );
			return;
		}

		if ( CurrentResult.PlayerWon )
		{
			DistributeRewards();
		}

		Log.Info( $"EndBattleManual: Invoking OnBattleEnd with PlayerWon={CurrentResult.PlayerWon}" );
		OnBattleEnd?.Invoke( CurrentResult );
	}

	/// <summary>
	/// Toggle between auto and manual mode mid-battle
	/// </summary>
	public void SetAutoMode( bool auto )
	{
		Log.Info( $"SetAutoMode: auto={auto}, IsInBattle={IsInBattle}, IsWaitingForPlayerInput={IsWaitingForPlayerInput}, IsPlaying={IsPlaying}" );
		IsAutoMode = auto;

		if ( !IsInBattle )
			return;

		if ( auto )
		{
			// Switching to auto mode
			if ( IsWaitingForPlayerInput )
			{
				// Simulate the rest of the battle
				Log.Info( "Switching to auto mode - simulating remainder" );
				SimulateRemainderOfBattle();
			}
			// If already playing, just let it continue
		}
		else
		{
			// Switching to manual mode - ALWAYS pause and wait for input
			Log.Info( $"Switching to manual mode - pausing and waiting for player input. CurrentTurnIndex={CurrentTurnIndex}" );
			IsPlaying = false;
			IsWaitingForPlayerInput = true;

			// ALWAYS restore monster HP when switching to manual mode
			// The pre-simulation already applied all the damage, so we need to reset
			// This gives the player a fresh start to play manually
			Log.Info( "Restoring all monster HP for manual mode" );
			foreach ( var monster in PlayerTeam )
			{
				monster?.FullHeal();
			}
			foreach ( var monster in EnemyTeam )
			{
				monster?.FullHeal();
			}

			// Reset battle state for fresh manual mode
			CurrentBattleState = new BattleState();
			CurrentBattleState.TurnNumber = 0; // Start fresh
			foreach ( var m in PlayerTeam.Concat( EnemyTeam ) )
			{
				if ( m != null )
					CurrentBattleState.InitializeMonster( m.Id );
			}
			Log.Info( "Created fresh BattleState for manual mode" );

			// Clear ALL pre-simulated turns - we're starting fresh
			if ( CurrentResult?.Turns != null )
			{
				Log.Info( $"Clearing all {CurrentResult.Turns.Count} pre-simulated turns" );
				CurrentResult.Turns.Clear();
			}

			// Reset playback index
			CurrentTurnIndex = 0;
			manualModeTurns.Clear();
		}
	}

	/// <summary>
	/// When switching from manual to auto, simulate the rest of the battle
	/// </summary>
	private void SimulateRemainderOfBattle()
	{
		IsWaitingForPlayerInput = false;

		// Continue simulating until battle is over
		while ( !BattleSimulator.IsBattleOver( PlayerTeam, EnemyTeam ) && CurrentBattleState.TurnNumber < 100 )
		{
			var playerActive = GetActivePlayerMonster();
			var enemyActive = GetActiveEnemyMonster();

			if ( playerActive == null || enemyActive == null )
				break;

			// AI selects move for player
			var playerChoice = BattleAI.SelectAction( playerActive, enemyActive, CurrentBattleState, PlayerTeam, true );

			var turnResults = BattleSimulator.ExecuteSingleTurn(
				PlayerTeam,
				EnemyTeam,
				CurrentBattleState,
				playerChoice.MoveId ?? "struggle"
			);

			CurrentResult.Turns.AddRange( turnResults );
		}

		// Determine winner
		CurrentResult.PlayerWon = !BattleSimulator.IsTeamDefeated( PlayerTeam );
		CurrentResult.TotalTurns = CurrentBattleState.TurnNumber;

		// Calculate rewards
		if ( CurrentResult.PlayerWon && PlayerTeam.Count > 0 )
		{
			int avgLevel = (int)PlayerTeam.Average( p => p.Level );
			foreach ( var enemy in EnemyTeam )
			{
				CurrentResult.TotalXP += BattleSimulator.CalculateXPGain( enemy, avgLevel );
				CurrentResult.TotalGold += BattleSimulator.CalculateGoldDrop( enemy );
			}
		}

		// Start playback from current index
		CurrentTurnIndex = manualModeTurns.Count; // Skip turns we already played
		IsPlaying = true;
		TurnTimer = 0;
	}

	/// <summary>
	/// Reset the preserved player active index (call when starting a fresh expedition)
	/// </summary>
	public void ResetPlayerActiveIndex()
	{
		_lastPlayerActiveIndex = 0;
	}

	/// <summary>
	/// Get list of available swap targets (alive monsters not currently active)
	/// </summary>
	public List<Monster> GetAvailableSwapTargets()
	{
		if ( PlayerTeam == null ) return new List<Monster>();

		var active = GetActivePlayerMonster();
		return PlayerTeam.Where( m => m != null && m != active && m.CurrentHP > 0 ).ToList();
	}

	/// <summary>
	/// Get the index of a monster in the player team
	/// </summary>
	public int GetMonsterIndex( Monster monster )
	{
		if ( PlayerTeam == null || monster == null ) return -1;
		return PlayerTeam.IndexOf( monster );
	}

	/// <summary>
	/// Horde system: Set the player's target enemy
	/// </summary>
	public void SetPlayerTarget( Monster target )
	{
		if ( target == null || target.CurrentHP <= 0 )
		{
			// Clear invalid target
			PlayerTargetId = Guid.Empty;
			return;
		}

		// Verify target is in enemy team
		if ( EnemyTeam?.Contains( target ) != true )
		{
			Log.Warning( "SetPlayerTarget: Target is not in enemy team" );
			return;
		}

		PlayerTargetId = target.Id;
		Log.Info( $"Player targeting: {target.Nickname}" );
	}

	/// <summary>
	/// Get the player's current target (or first alive enemy if none selected)
	/// </summary>
	public Monster GetPlayerTarget()
	{
		if ( EnemyTeam == null ) return null;

		// If a specific target is selected and alive, use it
		if ( PlayerTargetId != Guid.Empty )
		{
			var target = EnemyTeam.FirstOrDefault( m => m?.Id == PlayerTargetId && m.CurrentHP > 0 );
			if ( target != null )
				return target;
		}

		// Default to first alive enemy
		return EnemyTeam.FirstOrDefault( m => m?.CurrentHP > 0 );
	}

	/// <summary>
	/// Clear the player's target selection
	/// </summary>
	public void ClearPlayerTarget()
	{
		PlayerTargetId = Guid.Empty;
	}

	/// <summary>
	/// Check if a boss should transition to a new phase and handle the transition
	/// </summary>
	private void CheckBossPhaseTransition( Monster boss )
	{
		if ( boss == null || !boss.IsBoss || CurrentBossState == null )
			return;

		var result = BattleSimulator.CheckBossPhaseTransition( boss, CurrentBossState );
		if ( result == null )
			return;

		// Announce the phase transition
		if ( !string.IsNullOrEmpty( result.Message ) )
		{
			OnMoveUsed?.Invoke( result.Message );
			Log.Info( $"Boss phase transition: {result.Message}" );
		}

		// Execute the phase ability
		if ( result.Ability != BossAbilityType.None )
		{
			var abilityMessages = BattleSimulator.ExecuteBossAbility( boss, result.Phase, PlayerTeam, CurrentBattleState );
			foreach ( var msg in abilityMessages )
			{
				OnMoveUsed?.Invoke( msg );
			}
		}

		// Fire the event for UI updates
		OnBossPhaseTransition?.Invoke( result );
	}
}
