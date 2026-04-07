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
			Log.Info( "Battle3D: Controller initialized" );
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
		go.Name = "BattleSceneController";
		go.Components.Create<BattleSceneController>();
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
		// Wait for faint animations to finish before playing next turn
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
			if ( !anyFainting )
			{
				waitingForFaint = false;
				isAnimatingTurn = false;
			}
			return;
		}

		if ( isAnimatingTurn )
		{
			turnAnimTimer -= Time.Delta;
			if ( turnAnimTimer <= 0f )
			{
				// Apply any pending KOs now that the VFX delay has passed
				bool anyKO = false;
				while ( pendingKOs.Count > 0 )
				{
					var koId = pendingKOs.Dequeue();
					if ( billboards.TryGetValue( koId, out var bb ) )
					{
						Log.Info( $"Battle3D: Applying KO to {bb.Monster?.Nickname}" );
						bb.IsKO = true;
						anyKO = true;
					}
				}

				if ( anyKO )
				{
					// Switch to waiting for faint — don't process next turn yet
					waitingForFaint = true;
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
			isAnimatingTurn = true;
			turnAnimTimer = TurnAnimDelay;
		}
	}

	// ── Event Handlers ──

	private void HandleBattleStart()
	{
		Log.Info( $"Battle3D: HandleBattleStart fired. SceneState={sceneState}, hasRoot={battleSceneRoot != null}" );

		if ( sceneState != SceneState.Inactive && battleSceneRoot != null )
		{
			// Scene already exists — flag for wave transition
			// Don't transition yet — let the turn queue + faint play first
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
		}
		else if ( turn.IsMiss )
		{
			AttackVFXManager.PlayMissVFX( defenderBB, battleSceneRoot );
			cameraController?.TriggerShake( 0.5f, 0.05f );
			Systems.SoundManager.PlayAttackMiss();
			SpawnMissText( defenderBB );
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
		pendingEndCheck = false;

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

		foreach ( var monster in bm.PlayerTeam )
		{
			if ( billboards.TryGetValue( monster.Id, out var bb ) )
			{
				bb.ResetFromFaint();
			}
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

		Log.Info( $"Battle3D: Scene setup with {billboards.Count} monsters" );
	}

	private void TeardownBattle()
	{
		billboards.Clear();
		turnQueue.Clear();
		pendingKOs.Clear();
		isAnimatingTurn = false;
		waitingForFaint = false;
		incomingEnemies.Clear();
		pendingEndCheck = false;
		pendingWaveTransition = false;

		battleSceneRoot?.Destroy();
		battleSceneRoot = null;
		cameraController = null;
		arena = null;

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
				billboard.WorldPosition = positions[0];
				billboard.IsActive = true;
			}
			else if ( benchIdx < positions.Length )
			{
				billboard.WorldPosition = positions[benchIdx];
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
		var cam = Scene.Camera;
		if ( cam == null ) return null;

		// Get the center of the sprite (offset up by half sprite height)
		var worldPos = bb.WorldPosition + Vector3.Up * (bb.IsPlayerSide ? 14f : 22f);
		var screenNormal = cam.PointToScreenNormal( worldPos );

		return new Vector2( screenNormal.x, screenNormal.y );
	}

	// ── Utility ──

	private void SpawnDamageNumber( BattleTurn turn )
	{
		if ( !billboards.TryGetValue( turn.DefenderId, out var defenderBB ) ) return;
		var spawnPos = defenderBB.GetTopPosition() + Vector3.Up * 5f;
		spawnPos += new Vector3( 0f, Random.Shared.Float( -5f, 5f ), 0f );

		var go = new GameObject( true, $"DamageNum_{turn.Damage}" );
		go.Parent = battleSceneRoot;
		go.WorldPosition = spawnPos;

		var dmgNum = go.Components.Create<FloatingDamageNumber>();
		dmgNum.Setup( turn.Damage, turn.IsCritical, turn.IsSuperEffective );
	}

	private void SpawnMissText( MonsterBillboard defenderBB )
	{
		if ( defenderBB == null ) return;
		var spawnPos = defenderBB.GetTopPosition() + Vector3.Up * 5f;

		var go = new GameObject( true, "MissText" );
		go.Parent = battleSceneRoot;
		go.WorldPosition = spawnPos;

		var dmgNum = go.Components.Create<FloatingDamageNumber>();
		dmgNum.SetupMiss();
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
