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

	private bool _hasHydrated;

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

	protected override void OnDestroy()
	{
		// Clear the static so a stale reference past play mode doesn't make the
		// next session's OnAwake self-destruct the new manager.
		if ( Instance == this )
			Instance = null;
	}

	protected override void OnStart()
	{
		if ( SaveService.Instance != null && SaveService.Instance.IsLoaded )
		{
			Hydrate();
		}
		else if ( SaveService.Instance != null )
		{
			SaveService.Instance.OnSaveLoaded += Hydrate;
		}

		if ( SaveService.Instance != null )
		{
			SaveService.Instance.OnSaveReset += HandleSaveReset;
		}
	}

	private void HandleSaveReset()
	{
		_hasHydrated = false;
		HasCompletedTutorial = false;
		WasSkipped = false;
		IsTutorialActive = false;
		CurrentStepIndex = 0;
		Hydrate();
		Log.Info( "[TutorialManager] reset" );
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "TutorialManager";
		go.Components.Create<TutorialManager>();
	}

	/// <summary>
	/// Initialize all tutorial steps. Walks the player through one full expedition +
	/// one rigged contract success on a tutorial-only zone. Real rewards stick.
	///
	/// Steps fall into three groups:
	///     1. INFO STEPS — no AdvanceEvent. Show a centered bubble; player clicks Next.
	///     2. ACTION STEPS — have AdvanceEvent. Show a hint near a TargetSelector;
	///        the system advances when the player does the thing.
	///     3. RIGGED STEPS — RiggedSuccess flips on. Other systems read this to
	///        force friendly outcomes (easy enemy spawn / contract success).
	/// </summary>
	private void InitializeSteps()
	{
		Steps = new List<TutorialStep>
		{
			// 1 — Welcome (info)
			new TutorialStep
			{
				Id = "welcome",
				Title = "Welcome, Tamer.",
				Message = "I'll walk you through your first expedition. We'll explore a zone, fight a few wild Beasts, and tame one to add to your roster. Take it at your own pace.",
				Position = TutorialPosition.Center,
			},

			// 2 — Open the World Map (action: player clicks World Map)
			new TutorialStep
			{
				Id = "open-map",
				Title = "Open the World Map",
				Message = "Every expedition starts here. Click the Expedition tab on the bottom command bar.",
				ActionHint = "Click the Expedition tab.",
				TargetSelector = ".cb-tab-expedition",
				AdvanceEvent = "worldmap.opened",
			},

			// 3 — Pick the tutorial zone
			new TutorialStep
			{
				Id = "pick-zone",
				Title = "Weaverton Approach",
				Message = "Click your first zone — Weaverton Approach, a friendly stretch of pasture lane just outside the village gate. Perfect for a first run.",
				ActionHint = "Pick Weaverton Approach.",
				TargetSelector = ".zone-card.tutorial-zone",
				AdvanceEvent = "worldmap.zone-selected",
			},

			// 4 — Build a team
			new TutorialStep
			{
				Id = "build-team",
				Title = "Build Your Team",
				Message = "Add your starter to the team slot. You only need one Beast for now — once you tame more, fill all three slots before tougher zones.",
				ActionHint = "Drag your starter into Slot 1.",
				TargetSelector = ".picker-monster.starter",
				AdvanceEvent = "team.beast-added",
			},

			// 5 — Embark
			new TutorialStep
			{
				Id = "embark",
				Title = "Embark",
				Message = "When you're ready, embark. The run starts on the next click.",
				ActionHint = "Click Embark.",
				TargetSelector = ".embark-btn",
				AdvanceEvent = "expedition.embarked",
			},

			// 6 — First move (action: player picks a move)
			new TutorialStep
			{
				Id = "wave-move",
				Title = "Pick a Move",
				Message = "Each Beast has up to four moves. Match a move's element to your Beast's element for a 1.5× STAB damage bonus. Tap any move to commit.",
				ActionHint = "Pick a move to attack.",
				TargetSelector = ".move-pill",
				AdvanceEvent = "battle.move-selected",
				RiggedSuccess = true, // tells ExpeditionManager to spawn a weak enemy
			},

			// 7 — Wave clear (info)
			new TutorialStep
			{
				Id = "wave-result",
				Title = "First Win",
				Message = "Nice. Wild Beasts hit for chip damage; bosses hit much harder. Your HP carries between waves but PP refills, so don't be afraid to use your strongest moves.",
				Position = TutorialPosition.Center,
			},

			// 8 — Weaken (action: player drops enemy HP below 50%)
			new TutorialStep
			{
				Id = "weaken",
				Title = "A Tameable Beast",
				Message = "This wild Beast can be added to your roster — but only if you weaken it first. The lower its HP, the higher your contract odds. Keep attacking until it's around half HP.",
				ActionHint = "Lower its HP to ~50%.",
				TargetSelector = ".enemy-hp",
				AdvanceEvent = "battle.enemy-hp-low",
			},

			// 9 — Open contract (action: player opens contract panel)
			new TutorialStep
			{
				Id = "open-contract",
				Title = "Open Negotiations",
				Message = "Now's the moment. Open contract negotiations to start the deal.",
				ActionHint = "Click Contract.",
				TargetSelector = ".contract-btn",
				AdvanceEvent = "contract.opened",
			},

			// 10 — Pick approach (action: player selects any approach)
			new TutorialStep
			{
				Id = "pick-approach",
				Title = "Choose an Approach",
				Message = "Four approaches, each with different ink costs and odds. Fair Terms costs 2 Contract Ink and lands half the time on most Beasts — a reliable opener. Pick whichever feels right.",
				ActionHint = "Select an approach.",
				TargetSelector = ".approach-pill.kindness",
				AdvanceEvent = "contract.approach-selected",
			},

			// 11 — Confirm (action: rigged success)
			new TutorialStep
			{
				Id = "confirm-contract",
				Title = "Confirm",
				Message = "Lock it in. The Beast will join your roster.",
				ActionHint = "Click Confirm.",
				TargetSelector = ".contract-confirm-btn",
				AdvanceEvent = "contract.confirmed",
				RiggedSuccess = true, // ContractGenerator forces success during this step
			},

			// 12 — Contract success (info)
			new TutorialStep
			{
				Id = "contract-success",
				Title = "It Joined Your Roster",
				Message = "That's how taming works. Every species you contract fills out your Beastbook and unlocks species-wide mastery bonuses for every future Beast of that type.",
				Position = TutorialPosition.Center,
			},

			// 13 — Finish the run (action: ExpeditionManager fires complete event)
			new TutorialStep
			{
				Id = "finish-run",
				Title = "Finish the Run",
				Message = "Beat the rest of the waves to bank your rewards. Dying or fleeing forfeits everything — so push only when you can win.",
				AdvanceEvent = "expedition.completed",
				Position = TutorialPosition.Center,
			},

			// 14 — Complete (info, ends tutorial)
			new TutorialStep
			{
				Id = "complete",
				Title = "That's the Loop",
				Message = "Tame · Fuse · Expedition · Ascend. Every system in the game feeds back into those four pillars. Once you've cleared a zone on Normal, you can replay it on Hard for doubled XP and gold. Open the Game Guide any time you want a deeper dive — and welcome to Beastborne.",
				Position = TutorialPosition.Center,
			},
		};
	}

	/// <summary>
	/// Pull tutorial flags out of the save blob on boot.
	/// </summary>
	private void Hydrate()
	{
		if ( _hasHydrated ) return;
		_hasHydrated = true;

		var section = SaveService.Instance?.CurrentBlob?.Tutorial;
		if ( section != null )
		{
			HasCompletedTutorial = section.Completed;
			WasSkipped = section.Skipped;
			CurrentStepIndex = section.StepIndex;
		}
		else
		{
			HasCompletedTutorial = false;
			WasSkipped = false;
			CurrentStepIndex = 0;
		}

		Log.Info( $"[TutorialManager] Hydrated: Completed={HasCompletedTutorial}, Skipped={WasSkipped}, Step={CurrentStepIndex}" );
	}

	/// <summary>
	/// Push tutorial state back into the save blob.
	/// </summary>
	private void SaveState()
	{
		var service = SaveService.Instance;
		if ( service == null ) return;
		var blob = service.CurrentBlob;
		if ( blob == null ) return;

		blob.Tutorial ??= new TutorialSaveData();
		blob.Tutorial.Completed = HasCompletedTutorial;
		blob.Tutorial.Skipped = WasSkipped;
		blob.Tutorial.StepIndex = CurrentStepIndex;
		service.MarkDirty( "tutorial" );
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

		// Newness signal: the player has cleared zero expeditions. Level alone
		// is unreliable because a fresh starter spawns at Lv 3, so a strict
		// "Level <= 1" check would block every new player from seeing the tutorial.
		bool isNew = tamer.HighestExpeditionCleared == 0;
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
			ApplyStepEntryEffects();
			OnStepChanged?.Invoke( CurrentStep );
		}
	}

	/// <summary>
	/// One-shot effects that fire when a specific tutorial step becomes active.
	/// Top-up failsafes go here so the player can't brick the walkthrough by
	/// being short on resources (e.g. Contract Ink) when they reach the contract
	/// flow on a replayed tutorial.
	/// </summary>
	private void ApplyStepEntryEffects()
	{
		var step = CurrentStep;
		if ( step == null ) return;

		if ( step.Id == "open-contract" )
		{
			var tamer = TamerManager.Instance?.CurrentTamer;
			if ( tamer != null && tamer.ContractInk < 5 )
			{
				int topUp = 5 - tamer.ContractInk;
				tamer.ContractInk = 5;
				TamerManager.Instance?.SaveToCloud();
				Log.Info( $"[Tutorial] Topped up Contract Ink (+{topUp} → 5) so the player can finish the contract walkthrough." );
			}
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
	/// Complete the tutorial normally. Pays a small Contract Ink reward as a
	/// thank-you for finishing — the contracted Beast and the run rewards are
	/// the centerpiece, this is the cherry on top.
	/// </summary>
	private void CompleteTutorial()
	{
		IsTutorialActive = false;
		HasCompletedTutorial = true;
		WasSkipped = false;
		SaveState();

		// Completion bonus: 50 Contract Ink. Lets the player run a few more taming
		// attempts immediately without grinding for ink first.
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer != null )
		{
			tamer.ContractInk += 50;
			TamerManager.Instance?.SaveToCloud();
			Log.Info( "[Tutorial] Awarded 50 Contract Ink completion bonus." );
		}

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

	// ============================================================
	// PHASE-2 EXTENSIONS — event-driven advancement
	// ============================================================

	/// <summary>
	/// Returns true if the tutorial is active and the current step's Id matches.
	/// Used by other systems to gate tutorial-only behavior:
	///     if ( TutorialManager.Instance?.IsActiveStep( "rig-the-contract" ) == true ) ...
	/// </summary>
	public bool IsActiveStep( string id )
	{
		return IsTutorialActive && CurrentStep?.Id == id;
	}

	/// <summary>
	/// Returns true if the tutorial is active and the current step has RiggedSuccess set.
	/// Used by ContractGenerator + ExpeditionManager to know when to force a friendly outcome.
	/// </summary>
	public bool IsRiggedStep()
	{
		return IsTutorialActive && CurrentStep?.RiggedSuccess == true;
	}

	/// <summary>
	/// Notify the tutorial system that a player action happened. If the current
	/// step's AdvanceEvent matches the given id, the tutorial advances.
	/// No-op if tutorial is inactive or the event doesn't match.
	///
	/// Other systems call this from event handlers, e.g.:
	///     TutorialManager.Instance?.NotifyEvent( "battle.move-selected" );
	/// </summary>
	public void NotifyEvent( string eventId )
	{
		if ( !IsTutorialActive ) return;
		if ( string.IsNullOrEmpty( eventId ) ) return;

		var step = CurrentStep;
		if ( step == null || string.IsNullOrEmpty( step.AdvanceEvent ) ) return;

		if ( step.AdvanceEvent == eventId )
		{
			Log.Info( $"[Tutorial] Auto-advance: step '{step.Id}' got matching event '{eventId}'" );
			NextStep();
		}
	}
}
