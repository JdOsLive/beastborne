using Sandbox;

namespace Beastborne.Core;

public enum GameState
{
	MainMenu,
	Hub,
	InExpedition,
	InArena,
	Breeding,
	SkillTree
}

/// <summary>
/// Central game manager - handles game state and coordinates other managers
/// Single-scene setup: UI panels show/hide based on game state
/// </summary>
public sealed class GameManager : Component
{
	public static GameManager Instance { get; private set; }

	public GameState CurrentState { get; private set; } = GameState.MainMenu;

	// Events - UI components subscribe to these to show/hide themselves
	public Action<GameState, GameState> OnStateChanged;

	/// <summary>
	/// Shared tag stamped on every DontDestroyOnLoad manager GameObject (this one
	/// plus all the singletons created in <see cref="InitializeManagers"/>). Used
	/// by the reboot path below to find and tear down a previous session's stale
	/// persistent objects on an editor hotload / scene reload.
	/// </summary>
	public const string PersistentTag = "bb-persistent";

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			ClaimInstance();
			Log.Info( "GameManager initialized" );
		}
		else if ( Instance != this )
		{
			// A GameManager from a previous editor session is still lingering (its
			// GameObject survived as DontDestroyOnLoad, but on hotload the manager
			// statics get cleared — so SaveService.Instance etc. read null and the
			// game "resumes" half-dead: save never reloads, panels mount invisible).
			// Instead of self-destructing and inheriting that broken state, the
			// freshly-loaded GameManager WINS: nuke every stale persistent object
			// from the old session (each manager's OnDestroy nulls its own static),
			// then claim Instance and let OnStart rebuild everything from a clean
			// boot — same path a real player gets. Shipped builds never hit this
			// (no scene reload during play), so it's an editor-only safety net.
			Log.Info( "GameManager: stale session detected — forcing clean reboot" );
			ClearStalePersistents();
			ClaimInstance();
		}
	}

	private void ClaimInstance()
	{
		Instance = this;
		GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
		GameObject.Tags.Add( PersistentTag );
	}

	/// <summary>
	/// Destroy every DontDestroyOnLoad manager object left over from a prior
	/// session (everything tagged <see cref="PersistentTag"/> except this fresh
	/// GameManager). Uses DestroyImmediate so each manager's OnDestroy runs
	/// synchronously and nulls its static Instance BEFORE OnStart re-runs
	/// InitializeManagers — otherwise the lingering statics would make every
	/// EnsureInstance a no-op and we'd resume just as broken.
	/// </summary>
	private void ClearStalePersistents()
	{
		// Query both enabled states into a dedup set — s&box's GetAllObjects(bool)
		// filters by enabled-state and we want every persistent object regardless.
		var stale = new System.Collections.Generic.HashSet<GameObject>();
		foreach ( var enabled in new[] { true, false } )
		{
			foreach ( var go in Scene.GetAllObjects( enabled ) )
			{
				if ( go == GameObject || !go.IsValid() ) continue;
				if ( go.Tags.Has( PersistentTag ) )
					stale.Add( go );
			}
		}

		foreach ( var go in stale )
		{
			if ( go.IsValid() )
				go.DestroyImmediate();
		}
	}

	protected override void OnStart()
	{
		InitializeManagers();
	}

	private void InitializeManagers()
	{
		// SaveService owns the single Steam-id-scoped save blob and drives hydration
		// for every save-owning manager. Must be first so other managers can
		// subscribe to OnSaveLoaded (or race-check IsLoaded) in their OnStart.
		SaveService.EnsureInstance( Scene );

		TamerManager.EnsureInstance( Scene );
		// GiftManager depends on BOTH SaveService (save-load hook) and
		// TamerManager (Currency writes on claim). Keep it right after the tamer
		// wire-up so its OnStart-time fetch can bind OnSaveLoaded cleanly.
		GiftManager.EnsureInstance( Scene );
		MonsterManager.EnsureInstance( Scene );
		BeastiaryManager.EnsureInstance( Scene );
		ExpeditionManager.EnsureInstance( Scene );
		BattleManager.EnsureInstance( Scene );
		CompetitiveManager.EnsureInstance( Scene );
		ShopManager.EnsureInstance( Scene );
		ItemManager.EnsureInstance( Scene );
		ChatManager.EnsureInstance( Scene );
		NotificationManager.EnsureInstance( Scene );
		TutorialManager.EnsureInstance( Scene );
		SettingsManager.EnsureInstance( Scene );
		AchievementManager.EnsureInstance( Scene );
		TradingManager.EnsureInstance( Scene );
		VoiceChatManager.EnsureInstance( Scene );
		GuildManager.EnsureInstance( Scene );
		DailyRewardManager.EnsureInstance( Scene );
		MissionManager.EnsureInstance( Scene );
		SideQuestManager.EnsureInstance( Scene );
		TradeNodeManager.EnsureInstance( Scene );
		LiveEventManager.EnsureInstance( Scene );
		Battle3D.BattleSceneController.EnsureInstance( Scene );
	}

	public void ChangeState( GameState newState )
	{
		if ( CurrentState == newState ) return;

		var oldState = CurrentState;
		CurrentState = newState;

		Log.Info( $"Game state: {oldState} -> {newState}" );
		OnStateChanged?.Invoke( oldState, newState );
	}

	public void StartGame()
	{
		ChangeState( GameState.Hub );

		// Tutorial auto-start intentionally disabled — the tutorial system is being
		// redesigned. TutorialManager still exists and holds state for forward
		// compatibility; a future rework will re-wire the trigger.

		// Run retroactive achievement check for existing players
		AchievementManager.Instance?.RetroactiveCheck();

		// Broadcast player profile to other online players (gender, favorite expedition)
		ChatManager.Instance?.SendPlayerProfile();
	}

	public void ReturnToMainMenu()
	{
		ChangeState( GameState.MainMenu );
	}

	public void EnterExpedition()
	{
		ChangeState( GameState.InExpedition );
	}

	public void EnterArena()
	{
		ChangeState( GameState.InArena );
	}

	public void EnterBreeding()
	{
		ChangeState( GameState.Breeding );
	}

	public void EnterSkillTree()
	{
		ChangeState( GameState.SkillTree );
	}

	public void ExitToHub()
	{
		ChangeState( GameState.Hub );
	}

	public bool IsInGame => CurrentState != GameState.MainMenu;
	public bool IsInHub => CurrentState == GameState.Hub;
}
