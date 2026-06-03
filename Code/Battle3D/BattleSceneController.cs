using Sandbox;
using Beastborne.Core;
using Beastborne.Data;
using Beastborne.Systems;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beastborne.Battle3D;

/// <summary>
/// Central controller that bridges BattleManager events to the 3D battle scene.
/// The scene PERSISTS across waves — only defeated enemies are removed and new ones animate in.
/// Camera, arena, and player billboards stay for the entire expedition.
/// </summary>
public sealed class BattleSceneController : Component
{
	public static BattleSceneController Instance { get; private set; }

	private enum SceneState { Inactive, Active, WaveTransition }
	private SceneState sceneState = SceneState.Inactive;

	public bool IsActive => sceneState != SceneState.Inactive;

	/// <summary>
	/// True while enemies are fading out / sliding in during a wave transition.
	/// UI should hide new enemy health tabs until this is false.
	/// </summary>
	public bool IsWaveTransitioning => sceneState == SceneState.WaveTransition || pendingWaveTransition;

	/// <summary>
	/// True while the scene still has pending turns to animate — queued
	/// turns waiting to play, the current turn's VFX still running, or
	/// a faint animation settling. BattleView uses this to keep the
	/// player's action bar disabled until the visual story catches up to
	/// the simulation, so the player can't input a move before the prior
	/// turn's animation finishes.
	/// </summary>
	public bool IsPlayingTurns => turnQueue.Count > 0 || isAnimatingTurn || waitingForFaint || (cameraController?.IsInKOZoom ?? false);

	/// <summary>
	/// Fires when a queued turn's visuals START playing (one per turn).
	/// BattleView subscribes so its log feed reveals entries in sync with
	/// the 3D scene instead of all at once when BattleManager finishes
	/// simulating the round.
	/// </summary>
	public Action<BattleTurn> OnTurnVisualsStarted;

	/// <summary>
	/// 0-1 progress of new enemies sliding into position. UI can use this to fade in health tabs.
	/// </summary>
	public float WaveEnterProgress { get; private set; }

	// 3D mouse picking
	public MonsterBillboard HoveredBillboard { get; private set; }
	public Action<Monster> OnBillboardClicked { get; set; }
	private MonsterBillboard hoveredBillboard;

	private BattleCameraController cameraController;
	private BattleArena arena;
	private GameObject battleSceneRoot;

	private Dictionary<Guid, MonsterBillboard> billboards = new();
	private bool isSubscribed;

	// Turn animation queue — plays turns with delay between them
	private Queue<BattleTurn> turnQueue = new();
	private float turnAnimTimer;
	[Property] public float TurnAnimDelay { get; set; } = 0.8f;
	private bool isAnimatingTurn;

	// Track which monsters need KO after their turn VFX plays
	private Queue<Guid> pendingKOs = new();
	private bool waitingForFaint;
	// After the KO zoom + faint both finish, hold a small extra beat before
	// the next turn fires so the player can register what happened. Without
	// this, the camera snaps out of the money-shot and the next attack
	// starts on the same frame — feels rushed. 0.25s is the smallest beat
	// that reads as "moment of silence" without pacing the battle into
	// molasses.
	private const float PostKOSettleDuration = 0.25f;
	private float postKOSettleTimer;

	// Wave transition state
	private bool pendingWaveTransition;
	private int waveTransitionPhase; // 0=wait-for-faint, 1=spawn-and-slide-in
	private float waveTransitionTimer;
	[Property] public float FaintWaitDuration { get; set; } = 1.2f;
	[Property] public float EnemyEnterDuration { get; set; } = 0.6f;
	private List<MonsterBillboard> incomingEnemies = new();

	// Expedition-end teardown check
	private bool pendingEndCheck;
	private float endCheckTimer;

	// Arena layout positions
	private Vector3[] GetPlayerPositions() => new[]
	{
		new Vector3( -200f, 30f, 25f ),
		new Vector3( -220f, 40f, 25f ),
		new Vector3( -220f, 0f, 25f ),
	};

	private Vector3[] GetEnemyPositions() => new[]
	{
		new Vector3( 40f, -80f, 25f ),
		new Vector3( 20f, -40f, 25f ),
		new Vector3( 0f, -10f, 25f ),
	};

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			GameObject.Tags.Add( "bb-persistent" );
			Log.Info( "Battle3D: Controller initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	protected override void OnDestroy()
	{
		// Clear the static so a stale reference to this destroyed controller
		// doesn't linger past play mode — otherwise consumers see Instance as
		// non-null but Instance.Scene is null, and the next session's OnAwake
		// singleton check would wrongly self-destruct the new controller.
		if ( Instance == this )
			Instance = null;
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;
		var go = scene.CreateObject();
		go.Name = "BattleSceneController";
		go.Components.Create<BattleSceneController>();
		// The damage-number overlay reads from DamageNumberManager.Instance;
		// keep its lifecycle tied to the battle scene controller's so the
		// singleton always exists once battles can start.
		DamageNumberManager.EnsureInstance( scene );
		// Same pattern for the localized impact-ring overlay.
		ImpactRingManager.EnsureInstance( scene );
	}

	protected override void OnUpdate()
	{
		if ( !isSubscribed && BattleManager.Instance != null )
		{
			BattleManager.Instance.OnBattleStart += HandleBattleStart;
			BattleManager.Instance.OnTurnExecuted += HandleTurnExecuted;
			BattleManager.Instance.OnBattleEnd += HandleBattleEnd;
			BattleManager.Instance.OnMonsterDamaged += HandleMonsterDamaged;
			BattleManager.Instance.OnMonsterDefeated += HandleMonsterDefeated;
			isSubscribed = true;
			Log.Info( "Battle3D: Subscribed to BattleManager events" );
		}

		switch ( sceneState )
		{
			case SceneState.Active:
				ProcessTurnQueue();
				UpdateBillboardStates();
				UpdateMousePicking();
				// Check if we should start wave transition after VFX + faint
				if ( pendingWaveTransition && !isAnimatingTurn && turnQueue.Count == 0 )
				{
					CheckPendingWaveTransition();
				}
				break;

			case SceneState.WaveTransition:
				ProcessWaveTransition();
				break;
		}

		// Check if expedition truly ended (teardown the scene)
		if ( pendingEndCheck )
		{
			endCheckTimer -= Time.Delta;
			if ( endCheckTimer <= 0f )
			{
				pendingEndCheck = false;
				var bm = BattleManager.Instance;
				if ( bm == null || (!bm.IsInBattle && !bm.IsTransitioning) )
				{
					Log.Info( "Battle3D: Expedition ended, tearing down scene" );
					TeardownBattle();
				}
			}
		}
	}

	// ── Turn Queue ──

	private void ProcessTurnQueue()
	{
		// Wait for faint animations to finish before playing next turn.
		// Also wait for the KO money-shot zoom — we don't want the next
		// turn to fire mid-cinematic.
		if ( waitingForFaint )
		{
			bool anyFainting = false;
			foreach ( var kvp in billboards )
			{
				if ( kvp.Value.IsFainting || (kvp.Value.IsKO && !kvp.Value.FaintComplete) )
				{
					anyFainting = true;
					break;
				}
			}
			bool koZoomActive = cameraController?.IsInKOZoom ?? false;
			if ( !anyFainting && !koZoomActive )
			{
				// Both faint and KO zoom finished — start the post-KO settle
				// hold. Tick it down on subsequent frames before clearing the
				// wait state. This is what gives the "moment of silence"
				// after the camera snaps back to default.
				if ( postKOSettleTimer < PostKOSettleDuration )
				{
					postKOSettleTimer += Time.Delta;
					return;
				}
				postKOSettleTimer = 0f;
				waitingForFaint = false;
				isAnimatingTurn = false;
				// Re-read PlayerActiveIndex / EnemyActiveIndex and move the
				// newly-active beast into the front slot. BattleSimulator's
				// auto-swap updates the active indices on KO, but the scene
				// didn't refresh — the swapped-in beast's billboard was
				// parked at its bench position and never moved, so players
				// saw an empty front slot after a faint.
				RepositionBillboards();
			}
			return;
		}

		if ( isAnimatingTurn )
		{
			turnAnimTimer -= Time.Delta;
			if ( turnAnimTimer <= 0f )
			{
				// Apply any pending KOs now that the VFX delay has passed.
				// Capture the LAST KO'd defender's world pos for the money-shot
				// (most-recent KO == most narratively relevant target).
				bool anyKO = false;
				Vector3? koTargetPos = null;
				while ( pendingKOs.Count > 0 )
				{
					var koId = pendingKOs.Dequeue();
					if ( billboards.TryGetValue( koId, out var bb ) )
					{
						Log.Info( $"Battle3D: Applying KO to {bb.Monster?.Nickname}" );
						koTargetPos = bb.WorldPosition;
						bb.IsKO = true;
						anyKO = true;
					}
				}

				if ( anyKO )
				{
					// Switch to waiting for faint — don't process next turn yet
					waitingForFaint = true;

					// Trigger the KO money-shot. Skip during wave transition
					// (too chaotic) — though by construction we shouldn't reach
					// here in WaveTransition state since ProcessTurnQueue is
					// only called in the Active state, this is defensive.
					if ( cameraController != null && koTargetPos.HasValue && sceneState == SceneState.Active )
					{
						cameraController.TriggerKOZoom( koTargetPos.Value );
					}
				}
				else
				{
					isAnimatingTurn = false;
				}
			}
			return;
		}

		if ( turnQueue.Count > 0 )
		{
			var turn = turnQueue.Dequeue();
			PlayTurnVisuals( turn );
			// Announce the turn so the battle log reveals its entry in sync
			// with the on-screen animation instead of jumping ahead.
			try { OnTurnVisualsStarted?.Invoke( turn ); }
			catch ( Exception ex ) { Log.Warning( $"Battle3D: OnTurnVisualsStarted handler threw: {ex.Message}" ); }
			isAnimatingTurn = true;
			turnAnimTimer = TurnAnimDelay;
		}
	}

	// ── Event Handlers ──

	// Track the expedition id the current scene is showing so we can
	// distinguish "same expedition, next wave" (wave-transition reuse is
	// correct) from "different expedition, start of run" (need full rebuild
	// — new team, fresh camera, fresh scene). Set in SetupBattle.
	private string currentSceneExpeditionId;

	private void HandleBattleStart()
	{
		var em = ExpeditionManager.Instance;
		var newExpId = em?.CurrentExpedition?.Id;
		var newWave = em?.CurrentWave ?? 0;
		Log.Info( $"Battle3D: HandleBattleStart fired. SceneState={sceneState}, hasRoot={battleSceneRoot != null}, newExp={newExpId} (wave {newWave}), sceneExp={currentSceneExpeditionId}" );

		if ( sceneState != SceneState.Inactive && battleSceneRoot != null )
		{
			// A fresh run starts at Wave 1. If the new expedition starts at
			// wave 1 while we still have a scene from a previous run, that's
			// ALWAYS a new-expedition entry (even if the expedition id is the
			// same — e.g. Retry of the same zone) and needs a clean rebuild.
			// Wave-transition reuse should ONLY happen for wave 2+ of the
			// same continuous run. Reusing the old scene across runs has been
			// producing blank / half-visible scenes (stale camera, stale
			// arena state, stale billboard visual state from the prior KO).
			bool sameExpedition = !string.IsNullOrEmpty( newExpId )
				&& newExpId == currentSceneExpeditionId;
			bool isFreshRun = newWave <= 1 || !sameExpedition;
			if ( isFreshRun )
			{
				Log.Info( $"Battle3D: Fresh expedition start (wave={newWave}, sameExp={sameExpedition}) — full rebuild" );
				SetupBattle();
				return;
			}

			// Additional safety: even within the same expedition, if any
			// current player monster has no billboard (team composition
			// changed between waves — unusual but possible), rebuild.
			var bm = BattleManager.Instance;
			bool teamMatches = true;
			if ( bm?.PlayerTeam != null )
			{
				foreach ( var monster in bm.PlayerTeam )
				{
					if ( monster == null ) continue;
					if ( !billboards.ContainsKey( monster.Id ) )
					{
						teamMatches = false;
						break;
					}
				}
			}

			if ( !teamMatches )
			{
				Log.Info( "Battle3D: Player team composition differs from existing scene — full rebuild" );
				SetupBattle();
				return;
			}

			Log.Info( "Battle3D: Detected wave transition, pending after VFX/faint" );
			pendingWaveTransition = true;
			WaveEnterProgress = 0f; // Reset so UI doesn't flash with old progress
			pendingEndCheck = false; // Cancel end check since new wave is starting
		}
		else
		{
			// First battle of expedition — full setup
			SetupBattle();
		}
	}

	private void HandleTurnExecuted( BattleTurn turn )
	{
		if ( sceneState == SceneState.Inactive ) return;
		turnQueue.Enqueue( turn );
	}

	private void HandleBattleEnd( BattleResult result )
	{
		Log.Info( $"Battle3D: HandleBattleEnd fired, turns in queue: {turnQueue.Count}" );
		cameraController?.FocusDefault();

		// Schedule a check: if no new battle starts (expedition over), teardown
		pendingEndCheck = true;
		endCheckTimer = 30.0f; // Wait long enough for negotiation + wave transition
	}

	private void HandleMonsterDamaged( Monster monster, int damage ) { }

	private void HandleMonsterDefeated( Monster monster )
	{
		// Don't set IsKO here — let UpdateBillboardStates handle it after VFX queue drains
		Log.Info( $"Battle3D: Monster defeated: {monster.Nickname}" );
	}

	private void PlayTurnVisuals( BattleTurn turn )
	{
		Log.Info( $"Battle3D PlayTurnVisuals: {turn.AttackerName} -> {turn.DefenderName}, Damage={turn.Damage}, IsMiss={turn.IsMiss}" );

		MonsterBillboard attackerBB = null;
		MonsterBillboard defenderBB = null;

		if ( billboards.TryGetValue( turn.AttackerId, out var abb ) ) attackerBB = abb;
		if ( billboards.TryGetValue( turn.DefenderId, out var dbb ) ) defenderBB = dbb;

		if ( turn.Damage > 0 )
		{
			AttackVFXManager.PlayAttackVFX( turn, attackerBB, defenderBB, battleSceneRoot );
			defenderBB?.PlayHitFlash();

			if ( cameraController != null )
			{
				if ( turn.IsCritical )
					cameraController.TriggerHitstop( true );
				else if ( turn.IsSuperEffective )
					cameraController.TriggerHitstop( false );
				else
					cameraController.TriggerShake( 1.5f, 0.1f );
			}

			// Play hit sound
			if ( turn.IsCritical )
				Systems.SoundManager.PlayCriticalHit();
			else
				Systems.SoundManager.PlayAttackHit();

			SpawnDamageNumber( turn );

			// ── Billboard physical commit (lunge + knockback) ─────────
			TriggerLungeAndKnockback( attackerBB, defenderBB, turn.IsCritical, turn.IsSuperEffective, isMiss: false );

			// ── Localized impact rings (Item 3) ───────────────────────
			// Fire AFTER damage number so the ring sits at the same captured
			// screen pos. Yellow ring on crit, orange ring on super. Both
			// can fire on the same hit; orange staggers 60ms behind yellow.
			if ( turn.IsCritical )
			{
				ImpactRingManager.Instance?.SpawnRing( turn.DefenderId, "#fde047", isCrit: true );
			}
			if ( turn.IsSuperEffective )
			{
				_ = SpawnSuperRingDelayedAsync( turn.DefenderId, turn.IsCritical );
			}
		}
		else if ( turn.IsMiss )
		{
			AttackVFXManager.PlayMissVFX( defenderBB, battleSceneRoot );
			cameraController?.TriggerShake( 0.5f, 0.05f );
			Systems.SoundManager.PlayAttackMiss();
			SpawnMissText( defenderBB );

			// Miss: attacker still commits to the swing, defender gets no knockback.
			TriggerLungeAndKnockback( attackerBB, defenderBB, false, false, isMiss: true );
		}

		if ( turn.IsSwap )
		{
			RepositionBillboards();
		}

		// If defender was killed by this turn, queue their KO for after VFX
		if ( turn.DefenderHPAfter <= 0 && turn.DefenderId != Guid.Empty && turn.Damage > 0 )
		{
			pendingKOs.Enqueue( turn.DefenderId );
		}
	}

	// ── Wave Transition ──

	/// <summary>
	/// Check if all faint animations are done, then begin the wave transition
	/// </summary>
	private void CheckPendingWaveTransition()
	{
		foreach ( var kvp in billboards )
		{
			var bb = kvp.Value;
			if ( bb.IsFainting ) return; // Still animating
			if ( bb.IsKO && !bb.FaintComplete ) return; // KO set but faint hasn't started yet
		}

		pendingWaveTransition = false;
		BeginWaveTransition();
	}

	private void BeginWaveTransition()
	{
		sceneState = SceneState.WaveTransition;
		waveTransitionPhase = 0;
		waveTransitionTimer = 0f;
		WaveEnterProgress = 0f;
		incomingEnemies.Clear();
		turnQueue.Clear();
		isAnimatingTurn = false;
		waitingForFaint = false;
		postKOSettleTimer = 0f;
		pendingEndCheck = false;

		// Clear any in-flight billboard motion so a half-completed knockback
		// from the last turn doesn't bleed into the wave-transition slide-in.
		foreach ( var kvp in billboards )
		{
			kvp.Value?.ClearMotion();
		}

		// Faint is already done — remove dead enemies immediately
		RemoveDefeatedEnemyBillboards();
		RefreshPlayerBillboards();

		Log.Info( "Battle3D: Beginning wave transition — spawning new enemies" );

		// Spawn new enemies off-screen
		SpawnNewEnemyBillboards();
		Log.Info( $"Battle3D: Spawned {incomingEnemies.Count} new enemy billboards" );
	}

	private void ProcessWaveTransition()
	{
		waveTransitionTimer += Time.Delta;

		// Brief pause before enemies slide in
		if ( waveTransitionPhase == 0 )
		{
			if ( waveTransitionTimer >= 0.3f )
			{
				waveTransitionPhase = 1;
				waveTransitionTimer = 0f;
			}
			return;
		}

		// Animate new enemies sliding in
		var progress = MathF.Min( waveTransitionTimer / EnemyEnterDuration, 1f );
		WaveEnterProgress = progress;
		foreach ( var bb in incomingEnemies )
		{
			if ( bb == null ) continue;
			bb.AnimateEntrance( progress );
		}
		if ( progress >= 1f )
		{
			FinishWaveTransition();
		}
	}

	private void RemoveDefeatedEnemyBillboards()
	{
		var toRemove = new List<Guid>();
		foreach ( var kvp in billboards )
		{
			if ( !kvp.Value.IsPlayerSide )
			{
				kvp.Value.GameObject.Destroy();
				toRemove.Add( kvp.Key );
			}
		}
		foreach ( var id in toRemove )
		{
			billboards.Remove( id );
		}
		Log.Info( $"Battle3D: Removed {toRemove.Count} old enemy billboards" );
	}

	private void RefreshPlayerBillboards()
	{
		var bm = BattleManager.Instance;
		if ( bm == null ) return;

		// Case 1: reconcile billboards with the current player team.
		//  a) existing billboard for a monster still on the team → refresh it
		//  b) no billboard for a monster on the team → create one (e.g. team
		//     composition changed between expeditions — new beast in slot)
		//  c) billboard for a monster no longer on the team → destroy it
		//     (otherwise old beasts linger at bench positions forever)
		//
		// The wave-transition path used to skip (b) and (c), which left the
		// whole player side invisible whenever you started a new expedition
		// with any team change — the scene only had orphaned billboards from
		// the previous run.
		var currentIds = new HashSet<System.Guid>();
		foreach ( var monster in bm.PlayerTeam )
		{
			if ( monster == null ) continue;
			currentIds.Add( monster.Id );

			if ( billboards.TryGetValue( monster.Id, out var bb ) )
			{
				bb.ResetFromFaint();
				// If the player evolved this beast between expeditions, the
				// cached Species on the billboard is stale and would keep
				// showing the pre-evolution sprite. Refresh it.
				bb.RefreshSpeciesIfChanged();
			}
			else
			{
				// New beast — spawn a fresh billboard.
				var species = MonsterManager.Instance?.GetSpecies( monster.SpeciesId );
				if ( species == null ) continue;

				var go = new GameObject( true, $"Billboard_{monster.Nickname ?? species.Name}" );
				go.Parent = battleSceneRoot;

				var billboard = go.Components.Create<MonsterBillboard>();
				billboard.Setup( monster, species, true );
				billboards[monster.Id] = billboard;
				Log.Info( $"Battle3D: Spawned new player billboard for {monster.Nickname ?? species.Name} (team changed between expeditions)" );
			}
		}

		// Prune billboards for monsters that were on the prior team but not
		// this one. Walk keys into a temp list so we can mutate the dict.
		var stale = new List<System.Guid>();
		foreach ( var kvp in billboards )
		{
			var bb = kvp.Value;
			if ( bb == null || !bb.IsPlayerSide ) continue;
			if ( !currentIds.Contains( kvp.Key ) ) stale.Add( kvp.Key );
		}
		foreach ( var id in stale )
		{
			billboards[id]?.GameObject?.Destroy();
			billboards.Remove( id );
		}
		if ( stale.Count > 0 )
		{
			Log.Info( $"Battle3D: Pruned {stale.Count} stale player billboards from previous team" );
		}

		// Reposition in case active index changed
		PositionTeam( bm.PlayerTeam, GetPlayerPositions(), true );
	}

	private void SpawnNewEnemyBillboards()
	{
		var bm = BattleManager.Instance;
		if ( bm == null ) return;

		var enemyPositions = GetEnemyPositions();
		int posIdx = 0;

		foreach ( var monster in bm.EnemyTeam )
		{
			if ( monster == null || monster.CurrentHP <= 0 ) continue;

			var species = MonsterManager.Instance?.GetSpecies( monster.SpeciesId );
			if ( species == null ) continue;

			var targetPos = posIdx < enemyPositions.Length ? enemyPositions[posIdx] : enemyPositions[0];
			// Start off-screen to the right
			var startPos = targetPos + new Vector3( 200f, 0f, 0f );

			var go = new GameObject( true, $"Billboard_{monster.Nickname ?? species.Name}" );
			go.Parent = battleSceneRoot;

			var billboard = go.Components.Create<MonsterBillboard>();
			billboard.Setup( monster, species, false );
			billboard.EntranceStartPos = startPos;
			billboard.EntranceTargetPos = targetPos;
			billboard.WorldPosition = startPos;
			billboard.SetEntranceAlpha( 0f );
			billboard.IsActive = (posIdx == 0); // First enemy is active

			billboards[monster.Id] = billboard;
			incomingEnemies.Add( billboard );
			posIdx++;
		}
	}

	private void FinishWaveTransition()
	{
		incomingEnemies.Clear();
		WaveEnterProgress = 1f;
		sceneState = SceneState.Active;
		RepositionBillboards();
		Log.Info( "Battle3D: Wave transition complete, scene is Active" );
	}

	// ── Scene Setup / Teardown ──

	private void SetupBattle()
	{
		var bm = BattleManager.Instance;
		if ( bm == null ) return;

		if ( battleSceneRoot != null )
		{
			TeardownBattle();
		}

		battleSceneRoot = new GameObject( true, "BattleSceneRoot" );
		battleSceneRoot.Parent = GameObject;

		sceneState = SceneState.Active;

		var arenaGo = new GameObject( true, "BattleArena" );
		arenaGo.Parent = battleSceneRoot;
		arena = arenaGo.Components.Create<BattleArena>();
		arena.Setup();

		var cameraGo = new GameObject( true, "BattleCamera" );
		cameraGo.Parent = battleSceneRoot;
		cameraController = cameraGo.Components.Create<BattleCameraController>();
		cameraController.Enable();

		SpawnBillboards( bm.PlayerTeam, true );
		SpawnBillboards( bm.EnemyTeam, false );
		RepositionBillboards();

		// Record which expedition this scene belongs to. HandleBattleStart
		// uses this to detect cross-expedition battle starts and force a
		// full rebuild rather than a wave-transition reuse.
		currentSceneExpeditionId = ExpeditionManager.Instance?.CurrentExpedition?.Id;

		// Diagnostic: log the final positions so we can tell at a glance
		// whether billboards are being placed where the camera is looking.
		// Previously a "blank scene" bug (arena + sprites invisible) turned
		// out to be a camera-control race after expedition retries; leaving
		// this in so the next such bug surfaces quickly.
		foreach ( var kvp in billboards )
		{
			var bb = kvp.Value;
			Log.Info( $"Battle3D: Billboard '{bb.Monster?.Nickname ?? bb.Species?.Name}' placed at {bb.WorldPosition}, IsActive={bb.IsActive}, IsPlayerSide={bb.IsPlayerSide}, IsKO={bb.IsKO}" );
		}
		var cam = cameraController?.GetCamera();
		Log.Info( $"Battle3D: Scene setup with {billboards.Count} monsters for expedition '{currentSceneExpeditionId}'. Camera pos={cam?.WorldPosition}, FOV={cam?.FieldOfView}, Enabled={cam?.Enabled}, IsMain={cam?.IsMainCamera}" );
	}

	private void TeardownBattle()
	{
		billboards.Clear();
		turnQueue.Clear();
		pendingKOs.Clear();
		isAnimatingTurn = false;
		postKOSettleTimer = 0f;
		waitingForFaint = false;
		incomingEnemies.Clear();
		pendingEndCheck = false;
		pendingWaveTransition = false;

		// Purge any in-flight damage numbers so they don't drift into the
		// post-battle view.
		DamageNumberManager.Instance?.Clear();
		ImpactRingManager.Instance?.Clear();

		// Restore the main camera to its pre-battle state BEFORE destroying
		// the scene root. The BattleCameraController component lives on a
		// child of battleSceneRoot, so destroying the root would take its
		// OnDestroy offline; explicit Disable() here guarantees the camera
		// gets its saved position/rotation/FOV back.
		cameraController?.Disable();

		battleSceneRoot?.Destroy();
		battleSceneRoot = null;
		cameraController = null;
		arena = null;
		currentSceneExpeditionId = null;

		sceneState = SceneState.Inactive;
		Log.Info( "Battle3D: Scene torn down" );
	}

	private void SpawnBillboards( List<Monster> team, bool isPlayerSide )
	{
		if ( team == null ) return;

		foreach ( var monster in team )
		{
			var species = MonsterManager.Instance?.GetSpecies( monster.SpeciesId );
			if ( species == null ) continue;

			var go = new GameObject( true, $"Billboard_{monster.Nickname ?? species.Name}" );
			go.Parent = battleSceneRoot;

			var billboard = go.Components.Create<MonsterBillboard>();
			billboard.Setup( monster, species, isPlayerSide );
			billboards[monster.Id] = billboard;
		}
	}

	private void RepositionBillboards()
	{
		var bm = BattleManager.Instance;
		if ( bm == null ) return;

		PositionTeam( bm.PlayerTeam, GetPlayerPositions(), true );
		PositionTeam( bm.EnemyTeam, GetEnemyPositions(), false );
	}

	private void PositionTeam( List<Monster> team, Vector3[] positions, bool isPlayerSide )
	{
		if ( team == null ) return;

		int activeIdx = isPlayerSide
			? (BattleManager.Instance?.CurrentBattleState?.PlayerActiveIndex ?? 0)
			: (BattleManager.Instance?.CurrentBattleState?.EnemyActiveIndex ?? 0);

		int benchIdx = 1;
		for ( int i = 0; i < team.Count; i++ )
		{
			var monster = team[i];
			if ( !billboards.TryGetValue( monster.Id, out var billboard ) ) continue;

			if ( i == activeIdx )
			{
				billboard.SetRestPosition( positions[0] );
				billboard.IsActive = true;
			}
			else if ( benchIdx < positions.Length )
			{
				billboard.SetRestPosition( positions[benchIdx] );
				billboard.IsActive = false;
				benchIdx++;
			}
		}
	}

	private void UpdateBillboardStates()
	{
		// KO states are now driven by the turn queue (pendingKOs).
		// This method only handles edge cases like status effects.
		// Don't update while turns are playing.
		if ( turnQueue.Count > 0 || isAnimatingTurn ) return;
	}

	// ── 3D Mouse Picking ──

	// 3D mouse picking disabled — s&box Scene.Trace doesn't detect SpriteRenderer BoxColliders
	// Target selection uses diamond-style UI cards instead
	private void UpdateMousePicking() { }

	/// <summary>
	/// Get the screen-space position (0-1 normalized) of a monster's billboard center
	/// Returns null if billboard not found or camera unavailable
	/// </summary>
	public Vector2? GetMonsterScreenPosition( Guid monsterId )
	{
		if ( !billboards.TryGetValue( monsterId, out var bb ) ) return null;
		// Scene is null when Instance is a stale reference to a controller whose
		// GameObject was destroyed (e.g. play mode stopped) — guard before .Camera.
		var cam = Scene?.Camera;
		if ( cam == null ) return null;

		// Get the center of the sprite (offset up by half sprite height)
		var worldPos = bb.WorldPosition + Vector3.Up * (bb.IsPlayerSide ? 14f : 22f);
		var screenNormal = cam.PointToScreenNormal( worldPos );

		return new Vector2( screenNormal.x, screenNormal.y );
	}

	/// <summary>
	/// Enumerate every visible, non-fainted billboard for screen-space FX overlays
	/// (status condition halos, etc). Returns the monster id + screen-normalized
	/// position. Skips KO'd/fainted/inactive-bench billboards so we don't render
	/// status halos over invisible sprites.
	/// </summary>
	public IEnumerable<(Guid Id, Vector2 ScreenPos)> EnumerateVisibleBillboards()
	{
		// Scene is null when Instance is a stale reference to a controller whose
		// GameObject was destroyed (e.g. play mode stopped) — guard before .Camera.
		var cam = Scene?.Camera;
		if ( cam == null ) yield break;

		foreach ( var kvp in billboards )
		{
			var bb = kvp.Value;
			if ( bb == null ) continue;
			if ( bb.IsKO || bb.IsFainting || bb.FaintComplete ) continue;
			if ( !bb.IsActive ) continue;

			var worldPos = bb.WorldPosition + Vector3.Up * (bb.IsPlayerSide ? 14f : 22f);
			var sn = cam.PointToScreenNormal( worldPos );
			yield return (kvp.Key, new Vector2( sn.x, sn.y ));
		}
	}

	// ── Utility ──

	// Damage numbers were 3D TextRenderer GameObjects; rendering moved to a
	// screen-space Razor overlay (DamageNumberOverlay) because TextRenderer
	// has no outline / stroke / shadow / gradient API and could never match
	// the "Manga Impact" mockup. These methods now just delegate spawn
	// requests to DamageNumberManager which holds the live list.
	private void SpawnDamageNumber( BattleTurn turn )
	{
		var mgr = DamageNumberManager.Instance;
		if ( mgr == null ) return;
		mgr.SpawnHit( turn.DefenderId, turn.Damage, turn.IsCritical, turn.IsSuperEffective );
	}

	private void SpawnMissText( MonsterBillboard defenderBB )
	{
		if ( defenderBB == null ) return;
		var mgr = DamageNumberManager.Instance;
		if ( mgr == null ) return;
		mgr.SpawnMiss( defenderBB.Monster.Id );
	}

	/// <summary>
	/// Drive the attacker's lunge and (when applicable) the defender's knockback.
	/// Tuning lives in MonsterBillboard's constants — this method just dispatches
	/// the (crit/super/miss) bucket and computes the direction vector.
	/// </summary>
	private void TriggerLungeAndKnockback( MonsterBillboard attacker, MonsterBillboard defender, bool isCrit, bool isSuper, bool isMiss )
	{
		if ( attacker == null ) return;

		// Direction from attacker toward defender. If we don't have a defender
		// (rare — e.g. self-targeted move on miss), fall back to side-aware
		// forward so the attacker still commits visually.
		Vector3 dirToTarget;
		if ( defender != null )
		{
			var delta = defender.RestPosition - attacker.RestPosition;
			if ( delta.LengthSquared > 0.01f )
				dirToTarget = delta.Normal;
			else
				dirToTarget = attacker.IsPlayerSide ? Vector3.Forward : Vector3.Backward;
		}
		else
		{
			dirToTarget = attacker.IsPlayerSide ? Vector3.Forward : Vector3.Backward;
		}

		float lungeDist, lungeOut, lungeHold, lungeBack;
		float kbDist, kbOut, kbBack, kbDelay;

		if ( isMiss )
		{
			lungeDist = MonsterBillboard.LungeDistanceMiss;
			lungeOut = MonsterBillboard.LungeOutMiss;
			lungeHold = MonsterBillboard.LungeHoldMiss;
			lungeBack = MonsterBillboard.LungeBackMiss;
			kbDist = 0f; kbOut = 0f; kbBack = 0f; kbDelay = 0f;
		}
		else if ( isCrit )
		{
			lungeDist = MonsterBillboard.LungeDistanceCrit;
			lungeOut = MonsterBillboard.LungeOutCrit;
			lungeHold = MonsterBillboard.LungeHoldCrit;
			lungeBack = MonsterBillboard.LungeBackCrit;
			kbDist = MonsterBillboard.KnockbackDistanceCrit;
			kbOut = MonsterBillboard.KbOutCrit;
			kbBack = MonsterBillboard.KbBackCrit;
			kbDelay = MonsterBillboard.KbDelayCrit;
		}
		else if ( isSuper )
		{
			lungeDist = MonsterBillboard.LungeDistanceSuper;
			lungeOut = MonsterBillboard.LungeOutSuper;
			lungeHold = MonsterBillboard.LungeHoldSuper;
			lungeBack = MonsterBillboard.LungeBackSuper;
			kbDist = MonsterBillboard.KnockbackDistanceSuper;
			kbOut = MonsterBillboard.KbOutSuper;
			kbBack = MonsterBillboard.KbBackSuper;
			kbDelay = MonsterBillboard.KbDelaySuper;
		}
		else
		{
			lungeDist = MonsterBillboard.LungeDistanceNormal;
			lungeOut = MonsterBillboard.LungeOutNormal;
			lungeHold = MonsterBillboard.LungeHoldNormal;
			lungeBack = MonsterBillboard.LungeBackNormal;
			kbDist = MonsterBillboard.KnockbackDistanceNormal;
			kbOut = MonsterBillboard.KbOutNormal;
			kbBack = MonsterBillboard.KbBackNormal;
			kbDelay = MonsterBillboard.KbDelayNormal;
		}

		Log.Info( $"Battle3D: Lunge → {attacker.Monster?.Nickname} dist={lungeDist} (crit={isCrit} super={isSuper} miss={isMiss})" );
		attacker.LungeForward( dirToTarget, lungeDist, lungeOut, lungeHold, lungeBack );

		if ( !isMiss && defender != null && kbDist > 0f )
		{
			defender.Knockback( dirToTarget, kbDist, kbOut, kbBack, kbDelay );
		}
	}

	/// <summary>
	/// Spawn the orange super-effective ring 60ms after the call site so on
	/// super-crits the yellow ring fires first and the orange one chases.
	/// </summary>
	private async Task SpawnSuperRingDelayedAsync( Guid defenderId, bool isCritOnSameHit )
	{
		try
		{
			if ( isCritOnSameHit )
				await Task.DelayRealtimeSeconds( 0.06f );
			ImpactRingManager.Instance?.SpawnRing( defenderId, "#fb923c", isCrit: false );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"Battle3D: SpawnSuperRingDelayed threw: {ex.Message}" );
		}
	}

	public Vector3? GetMonsterWorldPosition( Guid monsterId )
	{
		if ( billboards.TryGetValue( monsterId, out var billboard ) )
			return billboard.GetTopPosition();
		return null;
	}

	public CameraComponent GetCamera() => cameraController?.GetCamera();
	public BattleCameraController GetCameraController() => cameraController;

	// ── Swap Preview ──

	// ── Swap Carousel (3D circle layout) ──

	// Circle carousel positions — all player beasts rotate through these
	// Arranged in a circle: center-front, back-left, back-right
	private static readonly Vector3 SwapCenterPos = new Vector3( -200f, 30f, 25f );      // Normal player battle position (selected)
	private static readonly Vector3 SwapLeftPos = new Vector3( -180f, 60f, 25f );        // Behind left (+X = further from camera)
	private static readonly Vector3 SwapRightPos = new Vector3( -180f, 0f, 25f );        // Behind right
	private static readonly Vector3 SwapFarLeftPos = new Vector3( -165f, 100f, 25f );    // Far back left (4+ teams)
	private static readonly Vector3 SwapFarRightPos = new Vector3( -165f, -35f, 25f );   // Far back right

	private static Vector3 GetSwapSlotPosition( int slot, int total )
	{
		return slot switch
		{
			0 => SwapCenterPos,
			1 => SwapLeftPos,
			2 => SwapRightPos,
			3 => SwapFarLeftPos,
			4 => SwapFarRightPos,
			_ => SwapCenterPos + new Vector3( -40f * slot, 0f, 0f )
		};
	}

	private Dictionary<Guid, Vector3> savedSwapPositions = new();
	private List<Monster> swapAllBeasts = new(); // All player beasts ordered for carousel
	private int swapRotation = 0; // Current rotation offset

	public void EnterSwapMode( int selectedIndex )
	{
		var bm = BattleManager.Instance;
		if ( bm == null ) return;

		// Build list of ALL alive player beasts for the circle
		// Put swap targets first, active beast last
		swapAllBeasts.Clear();
		var active = bm.GetActivePlayerMonster();
		foreach ( var monster in bm.PlayerTeam )
		{
			if ( monster == null || monster.CurrentHP <= 0 ) continue;
			if ( monster == active ) continue; // Add active last
			swapAllBeasts.Add( monster );
		}
		if ( active != null && active.CurrentHP > 0 )
			swapAllBeasts.Add( active );

		swapRotation = selectedIndex;

		// Save all positions and make all beasts visible
		foreach ( var monster in bm.PlayerTeam )
		{
			if ( !billboards.TryGetValue( monster.Id, out var bb ) ) continue;
			if ( !bb.IsPlayerSide ) continue;

			savedSwapPositions[monster.Id] = bb.WorldPosition;
			bb.IsSwapVisible = true;
		}

		ArrangeSwapCarousel( mode: SwapArrangeMode.FadeIn );
	}

	public void RotateSwapCarousel( int newIndex )
	{
		swapRotation = newIndex;
		ArrangeSwapCarousel( mode: SwapArrangeMode.Lerp );
	}

	private enum SwapArrangeMode { Snap, Lerp, FadeIn }

	private void ArrangeSwapCarousel( SwapArrangeMode mode = SwapArrangeMode.Snap )
	{
		if ( swapAllBeasts.Count == 0 ) return;

		// Clear all highlights
		foreach ( var kvp in billboards )
		{
			kvp.Value.IsSwapHighlighted = false;
		}

		// Position each swap target based on its rotated slot
		for ( int i = 0; i < swapAllBeasts.Count; i++ )
		{
			var monster = swapAllBeasts[i];
			if ( !billboards.TryGetValue( monster.Id, out var bb ) ) continue;

			// Calculate slot: distance from the selected index (wrapped)
			int slot = (i - swapRotation + swapAllBeasts.Count) % swapAllBeasts.Count;
			var targetPos = GetSwapSlotPosition( slot, swapAllBeasts.Count );
			var targetFade = (slot == 0) ? 1f : 0.35f;

			bb.IsSwapHighlighted = (slot == 0);

			if ( mode == SwapArrangeMode.Lerp )
			{
				bb.LerpToSwapPosition( targetPos, targetFade );
			}
			else if ( mode == SwapArrangeMode.FadeIn )
			{
				// Active beast stays put, benched beasts fade in
				if ( slot == 0 )
				{
					bb.WorldPosition = targetPos;
					bb.SwapFade = targetFade;
				}
				else
				{
					bb.FadeInToSwapPosition( targetPos, targetFade );
				}
			}
			else
			{
				bb.WorldPosition = targetPos;
				bb.SwapFade = targetFade;
			}
		}
	}

	public void ExitSwapMode()
	{
		// Fade out benched beasts, snap active beast back
		var active = BattleManager.Instance?.GetActivePlayerMonster();

		foreach ( var kvp in billboards )
		{
			var bb = kvp.Value;
			if ( !bb.IsPlayerSide ) continue;
			if ( !bb.IsSwapVisible ) continue;

			// Active beast snaps back immediately
			if ( bb.Monster == active )
			{
				bb.ClearSwapState();
				if ( savedSwapPositions.TryGetValue( kvp.Key, out var pos ) )
					bb.WorldPosition = pos;
			}
			else
			{
				// Benched beasts fade out, then clear
				var id = kvp.Key;
				bb.FadeOutSwap( () =>
				{
					bb.ClearSwapState();
					if ( savedSwapPositions.TryGetValue( id, out var savedPos ) )
						bb.WorldPosition = savedPos;
				} );
			}
		}

		swapAllBeasts.Clear();
		swapRotation = 0;
		// Note: savedSwapPositions cleared by callbacks, but clear any stragglers
		_ = CleanupSavedPositionsAsync();
	}

	private async Task CleanupSavedPositionsAsync()
	{
		await Task.DelayRealtimeSeconds( 0.3f );
		savedSwapPositions.Clear();
	}

	// Keep old API for compatibility but redirect
	public void ShowBenchedPlayerBeasts( bool show ) { }
	public void HighlightSwapTarget( Guid monsterId ) { }
	public void ClearSwapHighlights() => ExitSwapMode();

	// ── Contract Capture ──

	/// <summary>
	/// Remove a contracted enemy billboard with fade-out animation
	/// </summary>
	public void RemoveContractedEnemy( Guid monsterId )
	{
		if ( !billboards.TryGetValue( monsterId, out var billboard ) ) return;

		Log.Info( $"Battle3D: Contract capture effect on: {monsterId}" );

		// Spawn the purple ink capture effect
		var effectGo = new GameObject( true, $"ContractCapture_{monsterId}" );
		effectGo.Parent = battleSceneRoot;
		effectGo.WorldPosition = billboard.WorldPosition;

		var effect = effectGo.Components.Create<ContractCaptureEffect>();
		effect.Setup( billboard );

		// Remove from tracking (the effect handles billboard destruction)
		billboards.Remove( monsterId );
	}
}
