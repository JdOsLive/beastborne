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

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Log.Info( "GameManager initialized" );
		}
		else
		{
			Log.Info( "GameManager already exists, removing duplicate" );
			Destroy();
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
