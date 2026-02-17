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
		// SaveSlotManager must be initialized FIRST since other managers use it for key prefixes
		SaveSlotManager.EnsureInstance( Scene );
		TamerManager.EnsureInstance( Scene );
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

		// Check if we should show the tutorial for new players
		if ( TutorialManager.Instance?.ShouldShowTutorial() == true )
		{
			TutorialManager.Instance.StartTutorial();
		}

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
