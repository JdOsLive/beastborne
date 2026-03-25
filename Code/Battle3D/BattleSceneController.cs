using Sandbox;
using Beastborne.Core;
using Beastborne.Data;
using Beastborne.Systems;
using System.Collections.Generic;

namespace Beastborne.Battle3D;

/// <summary>
/// Central controller that bridges BattleManager events to the 3D battle scene.
/// Manages monster billboards, camera, and arena.
/// The GameObject stays ENABLED but child objects are created/destroyed per battle.
/// </summary>
public sealed class BattleSceneController : Component
{
	public static BattleSceneController Instance { get; private set; }

	/// <summary>
	/// Whether the 3D battle scene is currently active
	/// </summary>
	public bool IsActive { get; private set; }

	private BattleCameraController cameraController;
	private BattleArena arena;
	private GameObject battleSceneRoot;

	private Dictionary<Guid, MonsterBillboard> billboards = new();
	private bool isSubscribed;

	// Arena layout positions
	// Camera at X=-300: +Y = left on screen, -Y = right on screen
	// -X = closer to camera (bigger), +X = further from camera (smaller)
	// Player: bottom-left (closer), Enemy: top-right (further)
	private Vector3[] GetPlayerPositions() => new[]
	{
		new Vector3( -120f, 50f, 25f ),   // Active (bottom-left, closer to camera)
		new Vector3( -140f, 70f, 25f ),   // Benched 1
		new Vector3( -140f, 30f, 25f ),   // Benched 2
	};

	private Vector3[] GetEnemyPositions() => new[]
	{
		new Vector3( 120f, -90f, 25f ),   // Active (top-right, further from camera)
		new Vector3( 140f, -110f, 25f ),  // Benched 1
		new Vector3( 140f, -70f, 25f ),   // Benched 2
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
		// Subscribe to BattleManager events once it exists
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

		if ( IsActive )
		{
			UpdateBillboardStates();
		}
	}

	private void HandleBattleStart()
	{
		Log.Info( "Battle3D: HandleBattleStart fired" );
		SetupBattle();
	}

	private void HandleTurnExecuted( BattleTurn turn )
	{
		if ( !IsActive ) return;

		// Camera shake on hits
		if ( turn.Damage > 0 && cameraController != null )
		{
			if ( turn.IsCritical || turn.IsSuperEffective )
			{
				cameraController.TriggerHitstop( turn.IsCritical );
			}
			else
			{
				cameraController.TriggerShake( 1f, 0.08f );
			}
		}

		// Update positions for swaps
		if ( turn.IsSwap )
		{
			RepositionBillboards();
		}
	}

	private void HandleBattleEnd( BattleResult result )
	{
		Log.Info( "Battle3D: HandleBattleEnd fired" );
		TeardownBattle();
	}

	private void HandleMonsterDamaged( Monster monster, int damage )
	{
		// Future: spawn damage number WorldPanel
	}

	private void HandleMonsterDefeated( Monster monster )
	{
		if ( billboards.TryGetValue( monster.Id, out var billboard ) )
		{
			billboard.IsKO = true;
		}
	}

	/// <summary>
	/// Set up the 3D battle scene
	/// </summary>
	private void SetupBattle()
	{
		var bm = BattleManager.Instance;
		if ( bm == null ) return;

		// Clean up any existing battle scene first
		if ( battleSceneRoot != null )
		{
			TeardownBattle();
		}

		// Create the root for all battle scene objects
		battleSceneRoot = new GameObject( true, "BattleSceneRoot" );
		battleSceneRoot.Parent = GameObject;

		IsActive = true;

		// Create and setup arena
		var arenaGo = new GameObject( true, "BattleArena" );
		arenaGo.Parent = battleSceneRoot;
		arena = arenaGo.Components.Create<BattleArena>();
		arena.Setup();

		// Create and setup camera
		var cameraGo = new GameObject( true, "BattleCamera" );
		cameraGo.Parent = battleSceneRoot;
		cameraController = cameraGo.Components.Create<BattleCameraController>();
		cameraController.Enable();

		// Spawn monster billboards
		SpawnBillboards( bm.PlayerTeam, true );
		SpawnBillboards( bm.EnemyTeam, false );

		RepositionBillboards();

		Log.Info( $"Battle3D: Scene setup with {billboards.Count} monsters" );
	}

	/// <summary>
	/// Tear down the 3D battle scene
	/// </summary>
	private void TeardownBattle()
	{
		billboards.Clear();

		// Destroy entire battle scene root (camera, arena, billboards)
		battleSceneRoot?.Destroy();
		battleSceneRoot = null;
		cameraController = null;
		arena = null;

		IsActive = false;

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

	/// <summary>
	/// Position billboards based on active/benched status
	/// </summary>
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
		var bm = BattleManager.Instance;
		if ( bm == null ) return;

		foreach ( var monster in bm.PlayerTeam )
		{
			if ( billboards.TryGetValue( monster.Id, out var bb ) )
				bb.IsKO = monster.CurrentHP <= 0;
		}

		foreach ( var monster in bm.EnemyTeam )
		{
			if ( billboards.TryGetValue( monster.Id, out var bb ) )
				bb.IsKO = monster.CurrentHP <= 0;
		}
	}

	/// <summary>
	/// Get the world position of a monster's billboard (for HP bar screen projection)
	/// </summary>
	public Vector3? GetMonsterWorldPosition( Guid monsterId )
	{
		if ( billboards.TryGetValue( monsterId, out var billboard ) )
			return billboard.GetTopPosition();
		return null;
	}

	/// <summary>
	/// Get the camera component for screen projection
	/// </summary>
	public CameraComponent GetCamera()
	{
		return cameraController?.GetCamera();
	}
}
