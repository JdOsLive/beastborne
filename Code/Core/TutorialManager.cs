using Sandbox;
using Beastborne.Data;
using System.Collections.Generic;

namespace Beastborne.Core;

/// <summary>
/// Manages the tutorial system - tracks progress, handles step transitions,
/// and persists completion state per save slot.
/// </summary>
public sealed class TutorialManager : Component
{
	public static TutorialManager Instance { get; private set; }

	private const string TUTORIAL_COMPLETED_KEY = "tutorial-completed";
	private const string TUTORIAL_STEP_KEY = "tutorial-step";
	private const string TUTORIAL_SKIPPED_KEY = "tutorial-skipped";

	/// <summary>
	/// Get the full key with slot prefix
	/// </summary>
	private static string GetKey( string key ) => $"{SaveSlotManager.GetSlotPrefix()}{key}";

	/// <summary>
	/// Whether the tutorial is currently being shown
	/// </summary>
	public bool IsTutorialActive { get; private set; }

	/// <summary>
	/// The current tutorial step being displayed
	/// </summary>
	public TutorialStep CurrentStep => CurrentStepIndex >= 0 && CurrentStepIndex < Steps.Count ? Steps[CurrentStepIndex] : null;

	/// <summary>
	/// Current step index (0-based)
	/// </summary>
	public int CurrentStepIndex { get; private set; } = -1;

	/// <summary>
	/// Whether the player has completed (or skipped) the tutorial for this save
	/// </summary>
	public bool HasCompletedTutorial { get; private set; }

	/// <summary>
	/// Whether the tutorial was skipped rather than completed
	/// </summary>
	public bool WasSkipped { get; private set; }

	/// <summary>
	/// All tutorial steps in order
	/// </summary>
	public List<TutorialStep> Steps { get; private set; } = new();

	// Events
	public Action<TutorialStep> OnStepChanged;
	public Action OnTutorialCompleted;
	public Action OnTutorialStarted;

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			InitializeSteps();
			Log.Info( "TutorialManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	protected override void OnStart()
	{
		LoadState();
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "TutorialManager";
		go.Components.Create<TutorialManager>();
	}

	/// <summary>
	/// Initialize all tutorial steps
	/// </summary>
	private void InitializeSteps()
	{
		Steps = new List<TutorialStep>
		{
			new TutorialStep
			{
				Id = "welcome",
				Title = "Welcome, Tamer!",
				Message = "Welcome to Beastborne! I'll guide you through your journey as a Beast Tamer. Together, we'll explore, battle, and catch powerful beasts!",
				BackgroundImage = "ui/main menu/mainmenu_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "beasts_intro",
				Title = "Your Beasts",
				Message = "This is your beast collection. You start with one loyal companion who will fight by your side. Take good care of them!",
				TargetTab = "monsters",
				BackgroundImage = "ui/menus/monsters_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "beast_stats",
				Title = "Beast Stats",
				Message = "Each beast has 6 stats: HP, ATK, DEF, SpA (Special Attack), SpD (Special Defense), and SPD. Genetics affect their growth - higher genes mean stronger stats!",
				TargetTab = "monsters",
				BackgroundImage = "ui/menus/monsters_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "fusion_intro",
				Title = "Fusion Basics",
				Message = "Fusion lets you combine TWO beasts of the SAME SPECIES to create a new offspring. Both parents are consumed in the process, but the child inherits their best genetics!",
				TargetTab = "monsters",
				BackgroundImage = "ui/menus/fusion_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "fusion_benefits",
				Title = "Why Fuse?",
				Message = "Fused beasts inherit genes from BOTH parents, so you can breed for perfect stats over generations. Best of all - fused beasts have NO CONTRACT DEMANDS! They're loyal forever.",
				TargetTab = "monsters",
				BackgroundImage = "ui/menus/fusion_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "expedition_intro",
				Title = "Expeditions",
				Message = "Battle wild beasts in expeditions to earn Gold and XP. Each area has multiple waves of enemies and unique beasts to discover!",
				TargetTab = "expedition",
				BackgroundImage = "ui/menus/expedition_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "team_select",
				Title = "Select Your Team",
				Message = "Choose up to 3 beasts for your expedition team. Pick wisely - element matchups matter! Fire beats Nature, Water beats Fire, and so on.",
				TargetTab = "expedition",
				BackgroundImage = "ui/menus/expedition_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "battle_basics",
				Title = "Battle System",
				Message = "Each beast has 4 moves to use in battle! You can toggle AUTO mode to let them fight automatically, or MANUAL mode to choose moves yourself. Faster beasts attack first!",
				TargetTab = "expedition",
				BackgroundImage = "ui/menus/expedition_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "catching_intro",
				Title = "Catching Beasts",
				Message = "After winning a battle, you can negotiate contracts to catch wild beasts! Each negotiation style has different success rates and contract terms.",
				TargetTab = "expedition",
				BackgroundImage = "ui/menus/expedition_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "contract_ink",
				Title = "Contract Ink",
				Message = "You need Contract Ink to catch beasts. You can buy more in the Shop, or earn it from expeditions. Caught beasts have contract demands you must satisfy!",
				BackgroundImage = "ui/menus/shop_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "skills_intro",
				Title = "Tamer Skills",
				Message = "As you level up, you earn Skill Points. Spend them on the skill tree to unlock powerful abilities that buff ALL your beasts!",
				TargetTab = "skills",
				BackgroundImage = "ui/menus/skilltree_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "arena_intro",
				Title = "The Arena",
				Message = "Ready for a challenge? Battle other tamers in ranked PvP matches! Climb the leaderboard and prove you're the best!",
				TargetTab = "arena",
				BackgroundImage = "ui/menus/arena_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "beastiary_intro",
				Title = "Beastiary",
				Message = "Track all the beasts you've seen and caught in the Beastiary. Can you discover them all? Some are rarer than others!",
				TargetTab = "beastiary",
				BackgroundImage = "ui/menus/beastiary_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "shop_intro",
				Title = "The Shop",
				Message = "Buy XP boosts, Gold boosts, Contract Ink, and storage expansions with Gold. Server boosts help everyone playing!",
				TargetTab = "shop",
				BackgroundImage = "ui/menus/shop_background.png",
				Position = TutorialPosition.Center
			},
			new TutorialStep
			{
				Id = "complete",
				Title = "You're Ready!",
				Message = "That's the basics! Now go explore, battle, catch beasts, and become the ultimate Beast Tamer. Good luck on your journey!",
				BackgroundImage = "ui/main menu/mainmenu_background.png",
				Position = TutorialPosition.Center
			}
		};
	}

	/// <summary>
	/// Load tutorial state from cookies
	/// </summary>
	private void LoadState()
	{
		HasCompletedTutorial = Game.Cookies.Get<bool>( GetKey( TUTORIAL_COMPLETED_KEY ), false );
		WasSkipped = Game.Cookies.Get<bool>( GetKey( TUTORIAL_SKIPPED_KEY ), false );
		CurrentStepIndex = Game.Cookies.Get<int>( GetKey( TUTORIAL_STEP_KEY ), 0 );

		Log.Info( $"TutorialManager loaded: Completed={HasCompletedTutorial}, Skipped={WasSkipped}, Step={CurrentStepIndex}" );
	}

	/// <summary>
	/// Save tutorial state to cookies
	/// </summary>
	private void SaveState()
	{
		Game.Cookies.Set( GetKey( TUTORIAL_COMPLETED_KEY ), HasCompletedTutorial );
		Game.Cookies.Set( GetKey( TUTORIAL_SKIPPED_KEY ), WasSkipped );
		Game.Cookies.Set( GetKey( TUTORIAL_STEP_KEY ), CurrentStepIndex );
	}

	/// <summary>
	/// Reload state from the current save slot
	/// </summary>
	public void ReloadFromSlot()
	{
		LoadState();
		Log.Info( $"TutorialManager reloaded from slot {SaveSlotManager.Instance?.ActiveSlot}" );
	}

	/// <summary>
	/// Check if the tutorial should auto-start for this player
	/// </summary>
	public bool ShouldShowTutorial()
	{
		Log.Info( $"[Tutorial] ShouldShowTutorial check: HasCompleted={HasCompletedTutorial}, WasSkipped={WasSkipped}, IsTutorialActive={IsTutorialActive}" );

		// Don't show if already active
		if ( IsTutorialActive )
		{
			Log.Info( "[Tutorial] Already active, not starting again" );
			return false;
		}

		// Don't show if already completed or skipped
		if ( HasCompletedTutorial || WasSkipped )
		{
			Log.Info( "[Tutorial] Already completed or skipped" );
			return false;
		}

		// Show for new players (check if they have any meaningful progress)
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null )
		{
			Log.Info( "[Tutorial] Tamer is null" );
			return false;
		}

		Log.Info( $"[Tutorial] Tamer: Level={tamer.Level}, ExpCleared={tamer.HighestExpeditionCleared}" );

		// If they're level 1 with no expeditions cleared, they're new
		bool isNew = tamer.Level <= 1 && tamer.HighestExpeditionCleared == 0;
		Log.Info( $"[Tutorial] IsNewPlayer={isNew}" );
		return isNew;
	}

	/// <summary>
	/// Start the tutorial from the beginning
	/// </summary>
	public void StartTutorial()
	{
		if ( Steps.Count == 0 ) return;

		CurrentStepIndex = 0;
		IsTutorialActive = true;
		HasCompletedTutorial = false;
		WasSkipped = false;
		SaveState();

		Log.Info( $"Tutorial started at step 0: {CurrentStep?.Title}" );
		OnTutorialStarted?.Invoke();
		OnStepChanged?.Invoke( CurrentStep );
	}

	/// <summary>
	/// Restart the tutorial (for replay from menu)
	/// </summary>
	public void RestartTutorial()
	{
		// Reset completion state
		HasCompletedTutorial = false;
		WasSkipped = false;
		CurrentStepIndex = 0;
		SaveState();

		// Start fresh
		StartTutorial();
	}

	/// <summary>
	/// Advance to the next tutorial step
	/// </summary>
	public void NextStep()
	{
		if ( !IsTutorialActive ) return;

		CurrentStepIndex++;

		if ( CurrentStepIndex >= Steps.Count )
		{
			// Tutorial complete
			CompleteTutorial();
		}
		else
		{
			SaveState();
			Log.Info( $"Tutorial step {CurrentStepIndex}: {CurrentStep?.Title}" );
			OnStepChanged?.Invoke( CurrentStep );
		}
	}

	/// <summary>
	/// Go back to the previous step
	/// </summary>
	public void PreviousStep()
	{
		if ( !IsTutorialActive ) return;
		if ( CurrentStepIndex <= 0 ) return;

		CurrentStepIndex--;
		SaveState();
		Log.Info( $"Tutorial back to step {CurrentStepIndex}: {CurrentStep?.Title}" );
		OnStepChanged?.Invoke( CurrentStep );
	}

	/// <summary>
	/// Skip the tutorial entirely
	/// </summary>
	public void SkipTutorial()
	{
		IsTutorialActive = false;
		HasCompletedTutorial = true;
		WasSkipped = true;
		CurrentStepIndex = Steps.Count;
		SaveState();

		Log.Info( "Tutorial skipped" );
		OnTutorialCompleted?.Invoke();
	}

	/// <summary>
	/// Complete the tutorial normally
	/// </summary>
	private void CompleteTutorial()
	{
		IsTutorialActive = false;
		HasCompletedTutorial = true;
		WasSkipped = false;
		SaveState();

		Log.Info( "Tutorial completed!" );
		OnTutorialCompleted?.Invoke();
	}

	/// <summary>
	/// Get total number of steps
	/// </summary>
	public int TotalSteps => Steps.Count;

	/// <summary>
	/// Get completion percentage (0-1)
	/// </summary>
	public float Progress => Steps.Count > 0 ? (float)CurrentStepIndex / Steps.Count : 0;
}
