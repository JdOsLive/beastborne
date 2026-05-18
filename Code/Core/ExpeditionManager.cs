using Sandbox;
using Sandbox.Services;
using Beastborne.Data;
using Beastborne.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beastborne.Core;

/// <summary>
/// Manages PvE expedition content
/// </summary>
public sealed class ExpeditionManager : Component
{
	public static ExpeditionManager Instance { get; private set; }

	// Expedition data
	private List<Expedition> _expeditions = new();
	public IReadOnlyList<Expedition> Expeditions => _expeditions;

	// Current expedition state
	public Expedition CurrentExpedition { get; private set; }
	public int CurrentWave { get; private set; }
	public List<Monster> SelectedTeam { get; private set; } = new();

	// Auto-contract settings
	public bool AutoNegotiate { get; set; } = false;
	public int AutoNegotiateStrategy { get; set; } = 0; // Index into negotiation options (0-3)
	public string AutoContractTargetSpecies { get; set; } = null;
	public bool IsRunningInBackground { get; private set; } = false;

	// Hard Mode — per-expedition opt-in after Normal clear (see project_hard_mode_live.md).
	// Multipliers locked by user 2026-05-13. Enemy multipliers apply at spawn after RecalculateStats.
	public const float HARD_MODE_LEVEL_MULT = 1.25f;
	public const float HARD_MODE_XP_MULT = 2.0f;
	public const float HARD_MODE_GOLD_MULT = 2.0f;
	public const float HARD_MODE_DROP_RATE_MULT = 1.5f;
	public const float HARD_MODE_ATK_MULT = 1.15f;
	public const float HARD_MODE_HPDEFSPD_MULT = 1.10f;
	// Per-zone Hard Token award range, granted on EVERY Hard clear of an
	// expedition (repeatable by design — Hard zones are a farmable token
	// source). This is intentionally not gated to first-clear, unlike the
	// HardModeCleared flag which only tracks the first clear.
	public const int HARD_MODE_TOKEN_AWARD_MIN = 3;
	public const int HARD_MODE_TOKEN_AWARD_MAX = 5;

	/// <summary>
	/// Check if Hard Mode is unlocked for a specific expedition.
	/// Hard Mode unlocks per-expedition after the player clears that expedition on Normal.
	/// </summary>
	public bool IsHardModeUnlocked( string expeditionId = null )
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return false;
		var id = expeditionId ?? CurrentExpedition?.Id;
		if ( string.IsNullOrEmpty( id ) ) return false;
		return tamer.HardModeUnlocked.GetValueOrDefault( id );
	}

	/// <summary>
	/// Get the effective enemy level for the current expedition.
	/// Hard Mode scales by 25% (floored) instead of the old flat +10.
	/// </summary>
	public int GetEffectiveEnemyLevel( int baseLevel )
	{
		if ( CurrentExpedition?.IsHardMode == true )
			return (int)Math.Floor( baseLevel * HARD_MODE_LEVEL_MULT );
		return baseLevel;
	}

	/// <summary>XP multiplier — 2x in Hard Mode, 1x otherwise.</summary>
	public float GetXPMultiplier()
	{
		return CurrentExpedition?.IsHardMode == true ? HARD_MODE_XP_MULT : 1f;
	}

	/// <summary>Gold multiplier — 2x in Hard Mode, 1x otherwise.</summary>
	public float GetGoldMultiplier()
	{
		return CurrentExpedition?.IsHardMode == true ? HARD_MODE_GOLD_MULT : 1f;
	}

	/// <summary>Drop rate multiplier — 1.5x in Hard Mode, 1x otherwise.</summary>
	public float GetDropRateMultiplier()
	{
		return CurrentExpedition?.IsHardMode == true ? HARD_MODE_DROP_RATE_MULT : 1f;
	}

	/// <summary>
	/// Apply Hard Mode enemy stat multipliers to a freshly-spawned wild enemy.
	/// Called from CreateEnemyMonster/CreateBossMonster AFTER RecalculateStats so
	/// the multiplier rides on top of the v1.0.3 linear stat formula without
	/// touching the damage formula's inputs.
	/// </summary>
	private void ApplyHardModeStatMultipliers( Monster enemy )
	{
		if ( enemy == null || CurrentExpedition?.IsHardMode != true ) return;
		enemy.MaxHP = (int)( enemy.MaxHP * HARD_MODE_HPDEFSPD_MULT );
		enemy.ATK = (int)( enemy.ATK * HARD_MODE_ATK_MULT );
		enemy.DEF = (int)( enemy.DEF * HARD_MODE_HPDEFSPD_MULT );
		enemy.SpA = (int)( enemy.SpA * HARD_MODE_ATK_MULT );
		enemy.SpD = (int)( enemy.SpD * HARD_MODE_HPDEFSPD_MULT );
		// SPD intentionally unchanged — don't make Hard Mode a speed-tier nightmare.
	}

	/// <summary>
	/// Zone-id → Tamer Hard Token field name. Used when awarding tokens on
	/// Hard clear. Four launch zones; mini-expedition uses "threaded".
	/// </summary>
	private static readonly Dictionary<string, string> ZoneTokenBucket = new()
	{
		{ "weaverton_pasture", "tide" },     // Weaverton zone (the pasture is the Lv1 entry)
		{ "weaverton_approach", "tide" },    // Tutorial alias maps to same bucket
		{ "saltmoor_forest", "loom" },       // Weaverwood (internal id is saltmoor_forest)
		{ "old_saltmoor", "dawn" },          // Weavermere (internal id is old_saltmoor)
		{ "mini_loomweaver_burrow", "threaded" } // Whispering Hollow mini-expedition
	};

	/// <summary>
	/// Award Hard Mode tokens to the appropriate per-zone bucket on Tamer.
	/// Awards a random count in [HARD_MODE_TOKEN_AWARD_MIN..HARD_MODE_TOKEN_AWARD_MAX].
	/// Granted on every Hard clear (repeatable — Hard zones are a farmable
	/// token source); deliberately NOT first-clear-gated.
	/// No-op if the expedition is not in the zone map or the player is on a Normal run.
	/// </summary>
	private void AwardHardModeTokens()
	{
		if ( CurrentExpedition?.IsHardMode != true ) return;
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;
		if ( !ZoneTokenBucket.TryGetValue( CurrentExpedition.Id, out var bucket ) )
		{
			Log.Info( $"[Hard Mode] No token bucket mapped for expedition '{CurrentExpedition.Id}' — skipping token award." );
			return;
		}
		int count = _sharedRandom.Next( HARD_MODE_TOKEN_AWARD_MIN, HARD_MODE_TOKEN_AWARD_MAX + 1 );
		switch ( bucket )
		{
			case "tide": tamer.TideTokens += count; break;
			case "loom": tamer.LoomTokens += count; break;
			case "dawn": tamer.DawnTokens += count; break;
			case "threaded": tamer.ThreadedTokens += count; break;
		}
		Log.Info( $"[Hard Mode] Awarded {count} {bucket} tokens for clearing {CurrentExpedition.Id} on Hard." );
	}

	// Species filter for auto-contract
	public bool UseSpeciesFilter { get; set; } = false;
	public HashSet<string> EnabledSpeciesFilter { get; set; } = new();

	/// <summary>
	/// Check if a species is enabled for auto-contract
	/// Returns true if filter is disabled OR if species is in the enabled list
	/// </summary>
	public bool IsSpeciesEnabledForAutoContract( string speciesId )
	{
		if ( !UseSpeciesFilter || EnabledSpeciesFilter.Count == 0 )
			return true; // Filter disabled or empty = all enabled
		return EnabledSpeciesFilter.Contains( speciesId );
	}

	// Accumulated rewards during expedition
	public int AccumulatedGold { get; private set; } = 0;
	public int AccumulatedXP { get; private set; } = 0;
	public int AccumulatedTokens { get; private set; } = 0;
	public List<(string ItemId, int Quantity)> AccumulatedItems { get; private set; } = new();

	/// <summary>
	/// Add rewards to accumulated totals (called from GameHUD during background battles)
	/// </summary>
	public void AddAccumulatedRewards( int gold, int xp )
	{
		AccumulatedGold += gold;
		AccumulatedXP += xp;
		Log.Info( $"[ExpeditionManager] AddAccumulatedRewards: +{gold} gold, +{xp} XP. Total: {AccumulatedGold} gold, {AccumulatedXP} XP" );
	}

	/// <summary>
	/// Add item drops to accumulated items
	/// </summary>
	public void AddAccumulatedItems( List<(string ItemId, int Quantity)> items )
	{
		if ( items == null || items.Count == 0 ) return;

		foreach ( var (itemId, qty) in items )
		{
			// Check if we already have this item accumulated, merge quantities
			var existing = AccumulatedItems.FindIndex( i => i.ItemId == itemId );
			if ( existing >= 0 )
			{
				var current = AccumulatedItems[existing];
				AccumulatedItems[existing] = (itemId, current.Quantity + qty);
			}
			else
			{
				AccumulatedItems.Add( (itemId, qty) );
			}
		}

		Log.Info( $"[ExpeditionManager] Added {items.Count} item drops. Total accumulated: {AccumulatedItems.Count} unique items" );
	}

	/// <summary>
	/// Award all accumulated items to player's inventory
	/// </summary>
	private void AwardAccumulatedItems()
	{
		if ( AccumulatedItems == null || AccumulatedItems.Count == 0 ) return;

		foreach ( var (itemId, qty) in AccumulatedItems )
		{
			ItemManager.Instance?.AddItem( itemId, qty );
		}

		Log.Info( $"[ExpeditionManager] Awarded {AccumulatedItems.Count} unique item types to inventory" );
		AccumulatedItems = new();
	}

	// Negotiation state - tracks catchable enemies from the last battle
	public List<Monster> CatchableEnemies { get; private set; } = new();
	public bool HasCatchableEnemy => CatchableEnemies.Count > 0;

	// Boss state
	public ActiveBossState CurrentBossState { get; private set; }
	public BossData SelectedBoss { get; private set; }
	public bool IsBossWave => CurrentExpedition != null && CurrentExpedition.HasBoss && (CurrentExpedition.IsBossGauntlet || CurrentWave == CurrentExpedition.Waves);
	public bool IsRareBossEncounter { get; private set; } = false;
	private List<BossData> GauntletBossOrder { get; set; } // Shuffled boss order for gauntlets

	// Shared random instance to avoid seeding issues on quick retries
	private static readonly Random _sharedRandom = new Random();

	// Events
	public Action<Expedition> OnExpeditionStarted;
	public Action<bool> OnExpeditionComplete;
	public Action<Monster> OnMonsterCaught;
	public Action<int, int> OnWaveCompleted; // wave number, total waves
	public Action<string> OnHardModeUnlocked; // expeditionId — fires when Hard Mode first unlocks for a zone

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Enabled = true; // Ensure component is enabled for OnUpdate to be called
			Log.Info( "ExpeditionManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	protected override void OnStart()
	{
		GenerateExpeditions();

		// Subscribe to battle end events for background processing
		SubscribeToBattleManager();
	}

	/// <summary>
	/// Subscribe to BattleManager events. Called from OnStart and OnUpdate to handle initialization order.
	/// </summary>
	private bool _subscribedToBattleManager = false;
	private void SubscribeToBattleManager()
	{
		if (_subscribedToBattleManager)
		{
			return;
		}

		if (BattleManager.Instance == null)
		{
			Log.Info("SubscribeToBattleManager: BattleManager.Instance is null, will retry later");
			return;
		}

		BattleManager.Instance.OnBattleEnd += OnBattleEndBackground;
		BattleManager.Instance.OnMonsterDefeated += OnEnemyDefeated;
		_subscribedToBattleManager = true;

		int handlerCount = BattleManager.Instance.OnBattleEnd?.GetInvocationList()?.Length ?? 0;
		Log.Info($"ExpeditionManager subscribed to BattleManager.OnBattleEnd! Handler count: {handlerCount}");
	}

	protected override void OnDestroy()
	{
		if (BattleManager.Instance != null)
		{
			BattleManager.Instance.OnBattleEnd -= OnBattleEndBackground;
			BattleManager.Instance.OnMonsterDefeated -= OnEnemyDefeated;
		}

		// Clear the static so a stale reference past play mode doesn't make the
		// next session's OnAwake self-destruct the new manager.
		if ( Instance == this )
			Instance = null;
	}

	/// <summary>
	/// Called when any monster is defeated in battle.
	/// Awards Tamer XP + per-KO monster XP to the active player beast that scored the KO.
	/// Uses Pokemon-style level-scaled formula with 0.5-2.0 caps to prevent grinding
	/// and reward fighting over-leveled enemies. See balance-knowledge/reference-values.md.
	/// </summary>
	private void OnEnemyDefeated( Monster defeatedMonster )
	{
		// Only award XP during an active expedition
		if ( CurrentExpedition == null )
			return;

		// Check if the defeated monster is an enemy (not a player monster)
		if ( BattleManager.Instance?.PlayerTeam?.Any( m => m.Id == defeatedMonster.Id ) == true )
			return; // This is a player monster, not an enemy

		// ═══ TAMER XP (unchanged behavior) ═══
		int tamerBaseXP = 1 + (defeatedMonster.Level / 3);
		float xpBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ExpeditionXPBonus ) ?? 0;
		int tamerXP = (int)(tamerBaseXP * (1 + xpBonus / 100f));
		TamerManager.Instance?.AddXP( tamerXP );

		// ═══ PER-KO MONSTER XP ═══
		// Award to the active player beast (the one on the field when KO landed).
		// Future polish: could track the actual turn's attacker for precision, but
		// active-at-defeat matches player intuition for most cases.
		var activeBeast = BattleManager.Instance?.GetActivePlayerMonster();
		if ( activeBeast != null && activeBeast.CurrentHP > 0 )
		{
			var defeatedSpecies = MonsterManager.Instance?.GetSpecies( defeatedMonster.SpeciesId );
			int baseYield = defeatedSpecies?.BaseExpYield ?? 60;

			// Level ratio: defeatedLevel / participantLevel, clamped 0.5..2.0
			int participantLevel = Math.Max( 1, activeBeast.Level );
			float ratio = defeatedMonster.Level / (float)participantLevel;
			ratio = Math.Clamp( ratio, 0.5f, 2.0f );

			// Pokemon-style formula: baseYield × defeatedLevel / 7 × ratio
			int monsterXP = (int)(baseYield * defeatedMonster.Level / 7f * ratio);
			monsterXP = (int)(monsterXP * (1 + xpBonus / 100f));
			float liveEventXPBoost = (float)(LiveEventManager.Instance?.GetBoostMultiplier( "xp" ) ?? 1.0);
			monsterXP = (int)(monsterXP * liveEventXPBoost);
			if ( monsterXP < 1 ) monsterXP = 1;

			bool leveledUp = activeBeast.AddXP( monsterXP );
			Log.Info( $"[XP] {activeBeast.Nickname ?? activeBeast.SpeciesId} gained {monsterXP} XP (defeatedLv={defeatedMonster.Level}, yourLv={participantLevel}, ratio={ratio:F2}, baseYield={baseYield}){(leveledUp ? " → LEVEL UP!" : "")}" );

			if ( leveledUp )
			{
				NotificationManager.Instance?.AddNotification(
					NotificationType.Success,
					"Level Up!",
					$"{activeBeast.Nickname ?? activeBeast.SpeciesId} reached Lv {activeBeast.Level}!" );
				SoundManager.PlaySuccess();
			}
		}

		Log.Info( $"Tamer gained {tamerXP} XP from defeating {defeatedMonster.Nickname ?? "enemy"} (Level {defeatedMonster.Level})" );

		// ═══ SIGNATURE MATERIAL DROP ═══
		// Every KO of a launch-roster species drops its themed material (MH-style).
		// Guaranteed 1 per KO — predictable reward, feeds trade nodes + side quests.
		var defeatedSpeciesForMat = MonsterManager.Instance?.GetSpecies( defeatedMonster.SpeciesId );
		if ( defeatedSpeciesForMat != null && defeatedSpeciesForMat.HasSignatureDrop )
		{
			var materialId = defeatedSpeciesForMat.SignatureDropItemId;
			var tamer = TamerManager.Instance?.CurrentTamer;
			if ( tamer != null && !string.IsNullOrEmpty( materialId ) )
			{
				if ( tamer.Inventory.ContainsKey( materialId ) )
					tamer.Inventory[materialId]++;
				else
					tamer.Inventory[materialId] = 1;
				Log.Info( $"[Material] +1 {defeatedSpeciesForMat.SignatureDropName ?? materialId} from {defeatedSpeciesForMat.Name}" );
				SideQuestManager.Instance?.TrackMaterialCollected( materialId, 1 );
			}
		}
	}

	/// <summary>
	/// Distribute completion-bonus XP to every team member (active + bench) on
	/// expedition victory. Uses the same 0.5-2.0 level-ratio clamp as per-KO
	/// XP so overleveled beasts get less and underleveled beasts get more.
	/// Per balance-knowledge/reference-values.md, this is ~30% of a run's total
	/// monster XP (the other ~70% comes from per-KO during battle).
	/// </summary>
	private void AwardTeamCompletionXP( int baseXP )
	{
		if ( SelectedTeam == null || CurrentExpedition == null ) return;
		int enemyLevel = Math.Max( 1, CurrentExpedition.BaseEnemyLevel );

		foreach ( var monster in SelectedTeam )
		{
			if ( monster == null || monster.Level >= Monster.MaxLevel ) continue;

			float ratio = enemyLevel / (float)Math.Max( 1, monster.Level );
			ratio = Math.Clamp( ratio, 0.5f, 2.0f );
			float liveEventXPBoost = (float)(LiveEventManager.Instance?.GetBoostMultiplier( "xp" ) ?? 1.0);
			int xp = Math.Max( 1, (int)(baseXP * ratio * liveEventXPBoost) );

			bool leveledUp = monster.AddXP( xp );
			Log.Info( $"[XP Completion] {monster.Nickname ?? monster.SpeciesId} gained {xp} (ratio={ratio:F2}){(leveledUp ? " → LEVEL UP!" : "")}" );

			if ( leveledUp )
			{
				NotificationManager.Instance?.AddNotification(
					NotificationType.Success,
					"Level Up!",
					$"{monster.Nickname ?? monster.SpeciesId} reached Lv {monster.Level}!" );
			}
		}
	}

	protected override void OnUpdate()
	{
		// Ensure subscription to BattleManager (handles initialization order)
		if (!_subscribedToBattleManager)
		{
			SubscribeToBattleManager();
		}

		// Note: Background battle ticking is handled by GameHUD.OnUpdate via TickBackgroundExpedition()
		// This ensures reliable ticking since Component.OnUpdate may not run in all scenarios.
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null )
		{
			Log.Info( $"ExpeditionManager.EnsureInstance: Instance already exists, Enabled={Instance.Enabled}" );
			return;
		}

		var go = scene.CreateObject();
		go.Name = "ExpeditionManager";
		go.Flags = GameObjectFlags.DontDestroyOnLoad;
		var em = go.Components.Create<ExpeditionManager>();
		em.Enabled = true;
		Log.Info( $"ExpeditionManager.EnsureInstance: Created new instance, Enabled={em.Enabled}" );
	}

	private void GenerateExpeditions()
	{
		// Create expedition stages.
		_expeditions.Clear();

		// ═════════════════════════════════════════════════════════════════
		// LAUNCH EXPEDITIONS — 3 zones, unlock-gated via HighestExpeditionCleared.
		//
		// Level jumps (1 → 15 → 30) force team investment instead of a
		// 1→5→10 treadmill. Pre-launch scope: 13 post-launch expeditions
		// were cut from this list 2026-04-21. Add new zones as a SECOND
		// TOWN post-launch — do not re-inflate this list with element
		// themeparks.
		//
		// Player-facing names are "Weaverton / Weaverwood / Weavermere"
		// — internal IDs (saltmoor_cove / saltmoor_forest / old_saltmoor)
		// stay as-is so save data, drop tables, and side-quest keys
		// don't break. Legacy art paths kept (file rename is a
		// post-launch cleanup).
		// ═════════════════════════════════════════════════════════════════

		// ─── Weaverton Approach (tutorial-only, 3 waves, no boss) ───────────
		// One-shot tutorial expedition. Hand-tuned to be a clean ~5-minute
		// first-run experience with a guaranteed contractable Beast at wave 2.
		// Filtered from the World Map after tutorial completes/skips.
		_expeditions.Add( new Expedition
		{
			Id = "saltmoor_approach",
			Name = "Weaverton Approach",
			Description = "A friendly stretch of pasture lane just outside Weaverton's gate. Perfect for a first run.",
			RequiredLevel = 1,
			Waves = 3,
			BaseEnemyLevel = 1,
			// Tutorial pool drawn from Weaverton handmade wilds. The first
			// fight is hand-rigged (see TutorialManager); the rest pull
			// from this list.
			PossibleSpecies = new() { "wishlift", "heartwell", "sheepot" },
			Element = ElementType.Neutral,
			GoldReward = 30,
			XPReward = 20,
			HasBoss = false,
			BackgroundImage = "ui/locations/whispering_woods_background.png",
			TutorialOnly = true,
		} );

		// ─── Weaverton (Lv 1) — starter, 5 waves, no boss ──────────────
		// A wool-and-loom village built around the Sheepot trade. Pots
		// stacked along every fence, dye-vats in every yard, smoke from
		// the dye-houses curling up over the rooftops. The training pen
		// sits just outside the wool-pasture so new tamers can practice
		// without spooking the herd.
		_expeditions.Add( new Expedition
		{
			Id = "saltmoor_cove",
			Name = "Weaverton Pasture",
			Description = "The fenced loop the wool-village of Weaverton set aside for young tamers to practice on. Sheepots graze just over the hedge, and the beasts that wander in past the pasture gate are gentle and curious — perfect for trying out your first contracts without anyone getting sheared.",
			RequiredLevel = 1,
			Waves = 5,
			BaseEnemyLevel = 1,
			// Pasture Round (level 1) — three Spookior wilds plus the
			// Vegetable-Lamb-of-Tartary line opener (sheepot, by Jet).
			// The rest of the original starter pool (twigsnap/dewdrop/etc.)
			// was pulled to keep the very first zone tight and themed.
			PossibleSpecies = new() { "wishlift", "heartwell", "twincoil", "sheepot" },
			Element = ElementType.Neutral,
			GoldReward = 60,
			XPReward = 45,
			HasBoss = false,
			BackgroundImage = "ui/locations/whispering_woods_background.png"
		} );

		// ─── Weaverwood (Lv 10) — mid, 7 waves, has boss ──────────────
		// The forest east of Weaverton — old growth, dye-leaves, mossy
		// paths the herders use to take Sheepots out to summer pasture.
		// Jackacabra hunt the herders' flocks at the wood's edge; the gnome
		// line keeps to the deeper groves. Recommended Lv softened from
		// 15 → 10 so the jump from the village pasture isn't as steep.
		_expeditions.Add( new Expedition
		{
			Id = "saltmoor_forest",
			Name = "Weaverwood",
			Description = "The pinewood east of Weaverton. The herders take Sheepots up these paths each summer for the long pasture, and the village's dye-leaves come from the old groves further in. The beasts here are wilder than the pasture pens — and lately the herders are coming back short a sheep more often than they'd like.",
			RequiredLevel = 10,
			Waves = 7,
			BaseEnemyLevel = 10,
			// Forest pool — handmade beasts only. Gnoll (base) and
			// Jackacabra (predator) live in the deeper grove; Twincoil
			// + Wishlift roam in from the Weaverton pasture. Gnollium
			// is evo-only (level up Gnoll to 28); the herders say it
			// never shows itself to anyone who doesn't earn it. The old
			// AI-gen forest pool (sproutkin/thornveil/etc.) has been
			// retired pre-launch in favor of fewer, handmade beasts.
			PossibleSpecies = new() { "gnoll", "jackacabra", "wishlift", "twincoil", "stomplet" },
			Element = ElementType.Nature,
			GoldReward = 220,
			XPReward = 320,
			HasBoss = true,
			BackgroundImage = "ui/locations/ember_cavern_background.png"
		} );

		// ─── Weavermere (Lv 20) — final launch zone, 10 waves, has boss ──
		// The mirror-still pond above Weaverton. The village's painters,
		// dyers, and pattern-weavers walk up here at dawn to find the
		// colors no loom can match. Padlip live in the lily-pads; older
		// beasts drift up from the village pasture and settle in.
		// Recommended Lv softened from 30 → 20 so the jump from
		// Weaverwood (Lv 10) isn't as steep.
		_expeditions.Add( new Expedition
		{
			Id = "old_saltmoor",
			Name = "Weavermere",
			Description = "The mirror-still pond above Weaverton, ringed in lily pads and dragonfly-flicker. The village's painters, dyers, and pattern-weavers walk up here at dawn to find the colors no loom can match — and a few have come back with stranger stories than they went up with.",
			RequiredLevel = 20,
			Waves = 10,
			BaseEnemyLevel = 20,
			// Lake/pond pool — handmade-only after the AI-gen water beasts
			// (puddlejaw/mirrorpond/weepfin/streamling/rivercrest/bubblite/
			// coralheim) were retired pre-launch. Padlip spawns in the
			// regular waves; Twincoil + Heartwell roam in from the
			// Weaverton side as overgrown adults that drifted up the
			// drowned streets. Liliprince only appears as a boss-wave
			// encounter (see BossPoolDatabase). The player normally meets
			// Liliprince by evolving a Padlip at 32.
			PossibleSpecies = new() { "padlip", "twincoil", "heartwell", "dewdrop" },
			Element = ElementType.Water,
			GoldReward = 480,
			XPReward = 720,
			HasBoss = true,
			BackgroundImage = "ui/locations/lake_of_tears_background.png"
		} );

		// ─── Whispering Hollow (Mini, Lv 25) — cave on the forest road ──────
		// An old cave halfway between Saltmoor Cove and Old Saltmoor, where
		// travelers used to camp on the forest road to the mere. They stopped.
		// Geographically in the forest, but gated on clearing Old Saltmoor —
		// lore framing: the player has made the journey now and knows what they
		// skipped on the path. Threadlet is the marquee encounter; cerametz/
		// twincoil/wishlift fill out the cave's dark-hollow atmosphere.
		// Loomweaver (stage 2) is the named Elite boss on wave 4 — not in the contract pool.
		_expeditions.Add( new Expedition
		{
			Id = "mini_loomweaver_burrow",
			Name = "Whispering Hollow",
			Description = "An old cave on the forest road, where travelers used to camp on their way to the mere. They don't anymore. Something in the dark was always listening, and it grew tired of being polite.",
			RequiredLevel = 25,
			Waves = 4,
			BaseEnemyLevel = 25,
			PossibleSpecies = new() { "threadlet", "cerametz", "twincoil", "wishlift" },
			SpeciesWeights = new() { ["threadlet"] = 1.0f, ["cerametz"] = 2.5f, ["twincoil"] = 2.5f, ["wishlift"] = 2.5f },
			Element = ElementType.Earth,
			GoldReward = 380,
			XPReward = 560,
			HasBoss = true,
			BackgroundImage = "ui/locations/lake_of_tears_background.png",
			IsMiniExpedition = true,
		} );

		Log.Info( $"Generated {_expeditions.Count} expeditions (launch)" );
	}

	public Expedition GetExpedition( string id )
	{
		return _expeditions.FirstOrDefault( e => e.Id == id );
	}

	/// <summary>
	/// Gate for starting an expedition. Progression is SEQUENTIAL — an
	/// expedition is available only if the one before it in the list has
	/// been cleared at least once. Tamer level is advisory (shown as
	/// "Recommended Level" in UI), never a hard cap: an underleveled
	/// player can still try if they want to push through a rough run.
	/// </summary>
	public bool CanStartExpedition( string expeditionId )
	{
		var expedition = GetExpedition( expeditionId );
		if ( expedition == null ) return false;
		if ( TamerManager.Instance?.CurrentTamer == null ) return false;

		// Tutorial-only expeditions: always startable while the tutorial is active,
		// never startable after. Don't gate on the sequential-clear chain.
		if ( expedition.TutorialOnly )
		{
			return TutorialManager.Instance?.IsTutorialActive == true;
		}

		// Mini-expeditions: skip the sequential chain. Gate on clearing old_saltmoor
		// (the final main-path zone) at least once.
		if ( expedition.IsMiniExpedition )
		{
			return HasClearedExpedition( "old_saltmoor" );
		}

		// First non-tutorial, non-mini expedition in the ordered list is always open.
		// Skip tutorial-only and mini expeditions when computing prev/idx so the chain
		// isn't broken for players who skipped the tutorial or haven't found mini-content.
		var realChain = _expeditions.Where( e => !e.TutorialOnly && !e.IsMiniExpedition ).ToList();
		int idx = realChain.FindIndex( e => e.Id == expeditionId );
		if ( idx <= 0 ) return true;

		// Subsequent expeditions require the immediately-preceding
		// expedition to have been cleared.
		var prev = realChain[idx - 1];
		return HasClearedExpedition( prev.Id );
	}

	/// <summary>
	/// True iff the expedition has been successfully cleared at least once.
	/// Uses <see cref="Tamer.HighestExpeditionCleared"/> (a 1-based count of
	/// cleared expeditions) compared against the zone's 0-based index.
	/// Side quests and trade nodes for a zone gate on this so the main loop
	/// reads first, then side content opens up on the way back through.
	///
	/// Index MUST be computed against the non-tutorial chain — UpdateExpeditionStats
	/// writes HighestExpeditionCleared against that same chain. Indexing against
	/// the full _expeditions list (which includes Weaverton Approach at 0) puts
	/// every real zone one slot too high, so a freshly-cleared first zone reads
	/// as "not cleared" and the next zone stays locked.
	/// </summary>
	public bool HasClearedExpedition( string expeditionId )
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return false;
		// Mini-expeditions are not in the real chain — they never affect HighestExpeditionCleared.
		var realChain = _expeditions.Where( e => !e.TutorialOnly && !e.IsMiniExpedition ).ToList();
		int idx = realChain.FindIndex( e => e.Id == expeditionId );
		if ( idx < 0 ) return false;
		return idx < tamer.HighestExpeditionCleared;
	}

	public void SelectTeam( List<Monster> team )
	{
		SelectedTeam = team.Take( 3 ).ToList();
	}

	public void StartExpedition( string expeditionId, bool hardMode = false )
	{
		Log.Info( $"StartExpedition called: expeditionId={expeditionId}, hardMode={hardMode}" );

		if ( SelectedTeam.Count == 0 )
		{
			Log.Warning( "No team selected for expedition!" );
			return;
		}

		// Clean up any stale battle state
		if ( BattleManager.Instance?.IsInBattle == true )
		{
			Log.Info( "Cleaning up stale battle state" );
			BattleManager.Instance.ExitBattle();
		}

		CurrentExpedition = GetExpedition( expeditionId );
		if ( CurrentExpedition != null )
			CurrentExpedition.IsHardMode = hardMode;
		if ( CurrentExpedition == null )
		{
			Log.Warning( $"Unknown expedition: {expeditionId}" );
			return;
		}

		CurrentWave = 0;
		AccumulatedGold = 0;
		AccumulatedXP = 0;
		AccumulatedTokens = 0;
		AccumulatedItems = new();

		// Select boss for this expedition run
		SelectBossForExpedition();

		// Apply default settings from SettingsManager
		if ( SettingsManager.Instance != null )
		{
			var settings = SettingsManager.Instance.Settings;
			AutoNegotiate = settings.DefaultAutoContract;
			AutoNegotiateStrategy = settings.DefaultNegotiationStrategy;
			UseSpeciesFilter = settings.UseAutoContractSpeciesFilter;
			EnabledSpeciesFilter = SettingsManager.Instance.GetSpeciesFilterList();
		}

		// Reset the player's active monster index for a fresh expedition
		BattleManager.Instance?.ResetPlayerActiveIndex();

		OnExpeditionStarted?.Invoke( CurrentExpedition );

		// Tutorial: notify that the player embarked on an expedition.
		TutorialManager.Instance?.NotifyEvent( "expedition.embarked" );

		// Start first wave
		StartNextWave();
	}

	/// <summary>
	/// Enable background expedition mode - battles continue without UI
	/// </summary>
	public void EnableBackgroundMode()
	{
		IsRunningInBackground = true;

		// Ensure we're subscribed to battle end events
		SubscribeToBattleManager();

		Log.Info( $"Expedition running in background mode. IsInBattle={BattleManager.Instance?.IsInBattle}, IsPlaying={BattleManager.Instance?.IsPlaying}, Subscribed={_subscribedToBattleManager}" );

		// Ensure playback is started when entering background mode
		if ( BattleManager.Instance?.IsInBattle == true && !BattleManager.Instance.IsPlaying )
		{
			BattleManager.Instance.StartPlayback();
			Log.Info( $"Background mode: Started playback immediately" );
		}
	}

	/// <summary>
	/// Disable background mode - UI is now visible
	/// </summary>
	public void DisableBackgroundMode()
	{
		IsRunningInBackground = false;
		Log.Info( "Expedition background mode disabled" );
	}

	/// <summary>
	/// Reset to wave 1 for auto-retry (keeps expedition and team, just resets wave)
	/// </summary>
	public void ResetToWaveOne()
	{
		Log.Info( $"ResetToWaveOne: Resetting from wave {CurrentWave} to 0" );
		CurrentWave = 0;
		AccumulatedGold = 0;
		AccumulatedXP = 0;
		AccumulatedTokens = 0;
		AccumulatedItems = new();
	}

	/// <summary>
	/// Update expedition tracking stats (highest cleared + total completed).
	/// Shared by RetryExpedition, OnBattleEndBackground (auto-retry), and CompleteExpedition.
	/// </summary>
	private void UpdateExpeditionStats()
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null || CurrentExpedition == null ) return;

		// Don't bump HighestExpeditionCleared / TotalExpeditionsCompleted on
		// tutorial-only or mini-expedition runs — they don't count as canonical progression.
		if ( CurrentExpedition.TutorialOnly || CurrentExpedition.IsMiniExpedition ) return;

		if ( CurrentExpedition.IsHardMode )
		{
			// Hard Mode clear — record per-expedition and update the aggregate count for achievements.
			if ( !tamer.HardModeCleared.GetValueOrDefault( CurrentExpedition.Id ) )
			{
				tamer.HardModeCleared[CurrentExpedition.Id] = true;
				int hardClearCount = tamer.HardModeCleared.Count( kvp => kvp.Value );
				tamer.HighestHardModeCleared = Math.Max( tamer.HighestHardModeCleared, hardClearCount );
				AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.HighestHardModeCleared, tamer.HighestHardModeCleared );
				Log.Info( $"[Hard Mode] First hard clear: {CurrentExpedition.Id} — total hard clears: {hardClearCount}" );
			}
		}
		else
		{
			// Normal clear — update canonical highest-cleared counter.
			var realChain = _expeditions.Where( e => !e.TutorialOnly && !e.IsMiniExpedition ).ToList();
			int expeditionIndex = realChain.IndexOf( CurrentExpedition );
			if ( expeditionIndex >= 0 && expeditionIndex >= tamer.HighestExpeditionCleared )
			{
				tamer.HighestExpeditionCleared = Math.Min( expeditionIndex + 1, realChain.Count );
				AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.HighestExpeditionCleared, tamer.HighestExpeditionCleared );
				Stats.SetValue( "expedition-highest", tamer.HighestExpeditionCleared );
			}

			// First Normal clear of this expedition → unlock Hard Mode for it.
			if ( !tamer.HardModeUnlocked.GetValueOrDefault( CurrentExpedition.Id ) )
			{
				tamer.HardModeUnlocked[CurrentExpedition.Id] = true;
				Log.Info( $"[Hard Mode] Unlocked for {CurrentExpedition.Id}" );
				NotificationManager.Instance?.AddNotification(
					NotificationType.Success,
					"Hard Mode Unlocked!",
					$"{CurrentExpedition.Name} — Hard Mode is now available." );
				OnHardModeUnlocked?.Invoke( CurrentExpedition.Id );
			}
		}

		tamer.TotalExpeditionsCompleted++;
		AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.ExpeditionsCompleted, tamer.TotalExpeditionsCompleted );
		Stats.SetValue( "expeditions-completed-launch", tamer.TotalExpeditionsCompleted );
	}

	/// <summary>
	/// Retry the current expedition from wave 1, optionally awarding completion rewards first.
	/// Does NOT reset the player's active monster index.
	/// </summary>
	public void RetryExpedition( bool awardCompletionRewards )
	{
		if ( CurrentExpedition == null )
		{
			Log.Warning( "RetryExpedition: No current expedition to retry!" );
			return;
		}

		Log.Info( $"RetryExpedition: awardRewards={awardCompletionRewards}, CurrentExpedition={CurrentExpedition.Id}" );

		if ( awardCompletionRewards )
		{
			UpdateExpeditionStats();

			// Award expedition completion rewards (accumulated battle rewards already given per-wave)
			// Apply skill bonuses and hard mode multipliers to rewards
			float goldBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ExpeditionGoldBonus ) ?? 0;
			float xpBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ExpeditionXPBonus ) ?? 0;
			float hardGoldMult = GetGoldMultiplier();
			float hardXPMult = GetXPMultiplier();
			float guildExpedBonus = (GuildManager.Instance?.IsInGuild == true && (GuildManager.Instance?.Guild?.Level ?? 0) >= 2) ? 0.05f : 0f;
			int finalGold = (int)(CurrentExpedition.GoldReward * (1 + goldBonus / 100f) * (1 + guildExpedBonus) * hardGoldMult);
			int finalXP = (int)(CurrentExpedition.XPReward * (1 + xpBonus / 100f) * hardXPMult);
			TamerManager.Instance?.AddGold( finalGold );
			TamerManager.Instance?.AddXP( finalXP );
			AwardTeamCompletionXP( finalXP ); // completion bonus to every team member
			Log.Info( $"RetryExpedition: Awarded expedition completion rewards: {finalGold} gold (+{goldBonus}%, x{hardGoldMult}), {finalXP} XP (+{xpBonus}%, x{hardXPMult})" );
			AwardHardModeTokens();

			// Guild XP for expedition completion
			GuildManager.Instance?.AddGuildXP( 20 );
			GuildManager.Instance?.IncrementAchievement( "expedition" );

			// Award Boss Tokens if a boss was defeated
			if ( SelectedBoss != null )
			{
				AwardBossTokens();
			}

			// Track mission progress for expedition completion (retry path)
			MissionManager.Instance?.TrackExpeditionComplete( hardMode: CurrentExpedition?.IsHardMode == true, goldEarned: finalGold );
			SideQuestManager.Instance?.TrackExpeditionCleared( CurrentExpedition?.Id );

			// Guild weekly goals — Expedition Blitz (count) + Treasury Push (gold).
			GuildManager.Instance?.TrackGuildGoal( Data.GuildGoalType.ExpeditionsCleared, 1 );
			if ( finalGold > 0 )
				GuildManager.Instance?.TrackGuildGoal( Data.GuildGoalType.GoldEarned, finalGold );
		}

		// Award accumulated item drops to inventory before resetting
		AwardAccumulatedItems();

		// Restore PP for all team monsters before starting fresh
		RestoreTeamPP();

		// Reset wave counter but keep the expedition active
		CurrentWave = 0;
		AccumulatedGold = 0;
		AccumulatedXP = 0;
		AccumulatedTokens = 0;
		AccumulatedItems = new();

		// Clear boss gauntlet order so it gets re-shuffled on retry
		GauntletBossOrder = null;

		// Re-select boss for the new run (repopulates GauntletBossOrder for gauntlets)
		SelectBossForExpedition();

		// Start wave 1
		StartNextWave();
	}

	/// <summary>
	/// Cancel the current expedition without rewards
	/// </summary>
	public void CancelExpedition()
	{
		Log.Info( "Cancelling expedition" );

		// Clean up battle state
		if ( BattleManager.Instance?.IsInBattle == true )
		{
			BattleManager.Instance.ExitBattle();
		}

		// Restore PP for the team BEFORE clearing it. The complete/retry paths
		// already do this, but the early-end path (player hits "End Expedition"
		// from the leave dialog) was missing it — moves stayed depleted into
		// the next run and players had to pop ethers or wait for a successful
		// expedition completion to refill. Match the complete-path behavior.
		RestoreTeamPP();

		// Heal the whole roster on the way out — no stuck 0-HP beasts on the map.
		RestoreAllOwnedHP();

		// Reset state
		CurrentExpedition = null;
		CurrentWave = 0;
		AccumulatedGold = 0;
		AccumulatedXP = 0;
		AccumulatedTokens = 0;
		AccumulatedItems = new();
		IsRunningInBackground = false;
		SelectedTeam.Clear();
	}

	/// <summary>
	/// Handle battle end - works both in background mode and when UI is handling it
	/// </summary>
	private void OnBattleEndBackground( BattleResult result )
	{
		Log.Info( $"[BG] OnBattleEndBackground CALLED! result={result != null}, PlayerWon={result?.PlayerWon}" );

		// Must have an active expedition to handle
		if ( CurrentExpedition == null )
		{
			Log.Info( $"OnBattleEndBackground skipped: No active expedition" );
			return;
		}

		// Only handle automatically if we're in background mode
		// When NOT in background mode, the ExpeditionPanel.OnBattleComplete handles progression
		if ( !IsRunningInBackground )
		{
			Log.Info( $"OnBattleEndBackground: Not in background mode, UI will handle. Wave={CurrentWave}/{CurrentExpedition.Waves}" );
			return;
		}

		Log.Info( $"[BG] Battle ended: PlayerWon={result?.PlayerWon}, Wave={CurrentWave}/{CurrentExpedition.Waves}" );

		if ( result != null )
		{
			// Track accumulated rewards for UI display (actual awarding is done by BattleManager.DistributeRewards)
			AccumulatedGold += result.TotalGold;
			AccumulatedXP += result.TotalXP;
			Log.Info( $"[BG] Accumulated rewards: {result.TotalGold} gold, {result.TotalXP} XP (total: {AccumulatedGold} gold, {AccumulatedXP} XP)" );

			// Track accumulated item drops
			if ( result.ItemDrops?.Count > 0 )
			{
				AddAccumulatedItems( result.ItemDrops );
			}
		}

		bool playerWon = result?.PlayerWon ?? false;

		// Determine if we'll continue to another battle (only if won and more waves remain)
		bool willContinue = playerWon && CurrentWave < CurrentExpedition.Waves;

		// Use transition-friendly exit for continuous battles, otherwise full exit
		if ( BattleManager.Instance?.IsInBattle == true )
		{
			if ( willContinue )
			{
				BattleManager.Instance.PrepareForNextWave();
				BattleManager.Instance.ExitBattleForTransition();
			}
			else
			{
				BattleManager.Instance.ExitBattle();
			}
		}

		if ( playerWon && CurrentWave < CurrentExpedition.Waves )
		{
			Log.Info( $"[BG] Player won wave {CurrentWave}, continuing to next wave" );
			MissionManager.Instance?.TrackWaveCleared();
			OnWaveCompleted?.Invoke( CurrentWave, CurrentExpedition.Waves );
			_ = StartNextWaveDelayed();
		}
		else if ( playerWon && CurrentWave >= CurrentExpedition.Waves )
		{
			Log.Info( $"[BG] Player completed all waves!" );
			MissionManager.Instance?.TrackWaveCleared();
			CompleteExpedition( true );
		}
		else
		{
			Log.Info( $"[BG] Player lost. Completing expedition as failed." );
			CompleteExpedition( false );
		}
	}

	/// <summary>
	/// Start next wave with a small delay to ensure clean battle state
	/// </summary>
	private async Task StartNextWaveDelayed()
	{
		Log.Info( $"[BG] StartNextWaveDelayed: Waiting 50ms before starting next wave. IsBackground={IsRunningInBackground}, CurrentExpedition={CurrentExpedition?.Id}" );

		// Wait for attack VFX (0.8s) + faint animation (0.8s) + breathing room
		await Task.Delay( 2200 );

		Log.Info( $"[BG] StartNextWaveDelayed: After delay. IsBackground={IsRunningInBackground}, CurrentExpedition={CurrentExpedition?.Id}" );

		if ( !IsRunningInBackground || CurrentExpedition == null )
		{
			Log.Info( "[BG] StartNextWaveDelayed: No longer in background mode, aborting" );
			return;
		}

		StartNextWave();
		Log.Info( $"[BG] StartNextWaveDelayed: Started wave {CurrentWave}, IsInBattle={BattleManager.Instance?.IsInBattle}" );
	}

	/// <summary>
	/// Select a boss for the current expedition run
	/// </summary>
	private void SelectBossForExpedition()
	{
		SelectedBoss = null;
		CurrentBossState = null;
		IsRareBossEncounter = false;
		GauntletBossOrder = null;

		if ( CurrentExpedition == null ) return;

		if ( !CurrentExpedition.HasBoss )
		{
			Log.Info( $"Expedition {CurrentExpedition.Id} is boss-free — skipping boss selection" );
			return;
		}

		var pool = BossPoolDatabase.GetPool( CurrentExpedition.Id );
		if ( pool == null || pool.Bosses.Count == 0 )
		{
			Log.Info( $"No boss pool found for expedition {CurrentExpedition.Id}" );
			return;
		}

		var random = _sharedRandom;

		// For boss gauntlets, use weighted selection to make mythic/legendary bosses rarer
		if ( CurrentExpedition.IsBossGauntlet )
		{
			GauntletBossOrder = SelectWeightedBosses( pool.Bosses, CurrentExpedition.Waves, random );
			Log.Info( $"[Gauntlet] Selected {GauntletBossOrder.Count} bosses: {string.Join( ", ", GauntletBossOrder.Select( b => $"{b.SpeciesId}({b.Tier})" ) )}" );
			return;
		}

		// Check for rare boss spawn (level 50+ only)
		if ( pool.RareBossChance > 0 && pool.RareBosses?.Count > 0 )
		{
			// Apply Rare Radar bonus to rare boss chance (skill tree + shop boost)
			float skillBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.RareEncounterChance ) ?? 0;
			float rareRadarMultiplier = ShopManager.Instance?.GetBoostMultiplier( Data.ShopItemType.RareEncounter ) ?? 1.0f;
			float rareBossChance = pool.RareBossChance * (1 + skillBonus / 100f) * rareRadarMultiplier;

			if ( random.NextDouble() < rareBossChance )
			{
				// Rare boss spawns!
				SelectedBoss = pool.RareBosses[random.Next( pool.RareBosses.Count )];
				IsRareBossEncounter = true;
				Log.Info( $"RARE BOSS ENCOUNTER! Selected {SelectedBoss.SpeciesId} (Tier: {SelectedBoss.Tier})" );
			}
		}

		// If no rare boss, select from normal pool using weighted selection
		// Higher tier bosses are much rarer to encounter
		if ( SelectedBoss == null )
		{
			SelectedBoss = SelectWeightedBoss( pool.Bosses, random );
			Log.Info( $"Selected boss {SelectedBoss.SpeciesId} (Tier: {SelectedBoss.Tier}) for expedition {CurrentExpedition.Id}" );
		}

		// Initialize boss state for phase tracking
		CurrentBossState = new ActiveBossState
		{
			BossData = SelectedBoss,
			IsRareBoss = IsRareBossEncounter
		};
	}

	/// <summary>
	/// Boss tier weights for weighted selection (Normal/Elite tiers only).
	/// Legendary and Mythic bosses are handled separately with flat chances.
	/// </summary>
	private static int GetBossTierWeight( BossTier tier ) => tier switch
	{
		BossTier.Normal => 10,
		BossTier.Elite => 6,
		_ => 1
	};

	private const float MYTHIC_BOSS_CHANCE = 0.01f; // 1% chance for a Mythic boss
	private const float LEGENDARY_BOSS_CHANCE = 0.05f; // 5% chance for a Legendary boss

	/// <summary>
	/// Select a single boss from the pool.
	/// Mythic bosses have a flat 1% chance, Legendary bosses have a flat 5% chance.
	/// If neither procs, picks from Normal/Elite bosses using weighted selection.
	/// </summary>
	private BossData SelectWeightedBoss( List<BossData> pool, Random random )
	{
		var mythicBosses = pool.Where( b => b.Tier == BossTier.Mythic ).ToList();
		var legendaryBosses = pool.Where( b => b.Tier == BossTier.Legendary ).ToList();
		var baseBosses = pool.Where( b => b.Tier != BossTier.Mythic && b.Tier != BossTier.Legendary ).ToList();

		// 1% chance: roll for a Mythic boss
		if ( mythicBosses.Count > 0 && random.NextDouble() < MYTHIC_BOSS_CHANCE )
		{
			return mythicBosses[random.Next( mythicBosses.Count )];
		}

		// 5% chance: roll for a Legendary boss
		if ( legendaryBosses.Count > 0 && random.NextDouble() < LEGENDARY_BOSS_CHANCE )
		{
			return legendaryBosses[random.Next( legendaryBosses.Count )];
		}

		// Remaining: pick from Normal/Elite bosses using weighted selection
		var pickPool = baseBosses.Count > 0 ? baseBosses : pool;
		int totalWeight = pickPool.Sum( b => GetBossTierWeight( b.Tier ) );
		int roll = random.Next( totalWeight );

		int cumulative = 0;
		foreach ( var boss in pickPool )
		{
			cumulative += GetBossTierWeight( boss.Tier );
			if ( roll < cumulative )
				return boss;
		}

		return pickPool[random.Next( pickPool.Count )];
	}

	/// <summary>
	/// Select bosses for gauntlet. Mythic bosses have a flat 1% chance per slot.
	/// Non-Mythic bosses use weighted random selection.
	/// </summary>
	private List<BossData> SelectWeightedBosses( List<BossData> pool, int count, Random random )
	{
		var result = new List<BossData>();
		var available = pool.ToList();

		while ( result.Count < count )
		{
			if ( available.Count == 0 )
			{
				available = pool.ToList();
			}

			var mythicBosses = available.Where( b => b.Tier == BossTier.Mythic ).ToList();
			var nonMythicBosses = available.Where( b => b.Tier != BossTier.Mythic ).ToList();

			BossData selected = null;

			// 1% chance for a Mythic boss per slot
			if ( mythicBosses.Count > 0 && random.NextDouble() < MYTHIC_BOSS_CHANCE )
			{
				selected = mythicBosses[random.Next( mythicBosses.Count )];
			}
			else
			{
				// Pick from non-Mythic using weighted selection
				var pickPool = nonMythicBosses.Count > 0 ? nonMythicBosses : available;
				int totalWeight = pickPool.Sum( b => GetBossTierWeight( b.Tier ) );
				int roll = random.Next( totalWeight );

				int cumulative = 0;
				foreach ( var boss in pickPool )
				{
					cumulative += GetBossTierWeight( boss.Tier );
					if ( roll < cumulative )
					{
						selected = boss;
						break;
					}
				}
			}

			if ( selected != null )
			{
				result.Add( selected );
				available.Remove( selected );
			}
		}

		return result;
	}

	public void StartNextWave()
	{
		if ( CurrentExpedition == null )
		{
			Log.Warning( "StartNextWave: CurrentExpedition is null!" );
			return;
		}

		CurrentWave++;
		bool isBossWave = CurrentWave == CurrentExpedition.Waves;
		Log.Info( $"StartNextWave: Wave {CurrentWave}/{CurrentExpedition.Waves}, IsBoss={isBossWave}, SelectedTeam count: {SelectedTeam?.Count ?? 0}" );

		// Generate enemies for this wave
		var enemies = GenerateWaveEnemies();
		Log.Info( $"StartNextWave: Generated {enemies.Count} enemies" );

		if ( enemies.Count == 0 )
		{
			Log.Warning( "StartNextWave: No enemies generated!" );
		}

		// Start battle
		if ( BattleManager.Instance == null )
		{
			Log.Warning( "StartNextWave: BattleManager.Instance is null!" );
			return;
		}

		// Pass boss state to battle manager for phase tracking
		// For gauntlets, every wave is a boss wave; otherwise just the final wave
		bool shouldShowBoss = (CurrentExpedition.IsBossGauntlet || isBossWave) && CurrentBossState != null;
		if ( shouldShowBoss )
		{
			BattleManager.Instance.SetBossState( CurrentBossState );
		}
		else
		{
			BattleManager.Instance.SetBossState( null );
		}

		// Use manual battle mode (horde system with target selection)
		BattleManager.Instance.StartManualBattle( SelectedTeam, enemies );
	}

	private List<Monster> GenerateWaveEnemies()
	{
		var enemies = new List<Monster>();
		var random = _sharedRandom;

		// Determine number of enemies (1-3 based on wave)
		int enemyCount = Math.Min( 3, 1 + (CurrentWave / 2) );

		// Check if this is a boss gauntlet (every wave is a boss)
		if ( CurrentExpedition.IsBossGauntlet && GauntletBossOrder != null )
		{
			int bossIndex = CurrentWave - 1;
			if ( bossIndex < GauntletBossOrder.Count )
			{
				var waveBoss = GauntletBossOrder[bossIndex];

				// Boss level increases with each wave (hard mode adds bonus levels)
				int bossLevel = GetEffectiveEnemyLevel( CurrentExpedition.BaseEnemyLevel ) + (CurrentWave * 2);
				var boss = CreateBossMonster( waveBoss, bossLevel );
				if ( boss != null )
				{
					enemies.Add( boss );
					Log.Info( $"[Gauntlet] Wave {CurrentWave}: Spawned boss {boss.Nickname} Lv.{boss.Level}" );
				}

				// Update boss state for this wave's boss
				CurrentBossState = new ActiveBossState
				{
					BossData = waveBoss,
					IsRareBoss = false
				};
				SelectedBoss = waveBoss;
			}
			return enemies;
		}

		// Check if this is the boss wave (final wave)
		bool isBossWave = CurrentWave == CurrentExpedition.Waves;

		if ( isBossWave && SelectedBoss != null )
		{
			// Boss wave - spawn ONLY the boss (no minions for epic 3v1 feel)
			int bossLevel = GetEffectiveEnemyLevel( CurrentExpedition.BaseEnemyLevel ) + 5;
			var boss = CreateBossMonster( SelectedBoss, bossLevel );
			if ( boss != null )
			{
				enemies.Add( boss );
				Log.Info( $"Spawned boss {boss.Nickname} Lv.{boss.Level} (HP={boss.MaxHP}, ATK={boss.ATK}, DEF={boss.DEF})" );
			}
		}
		else
		{
			// Normal wave - spawn regular enemies (excluding ALL boss species from the pool)
			// Get all boss species IDs from the boss pool for this expedition
			var bossPool = BossPoolDatabase.GetPool( CurrentExpedition.Id );
			var allBossSpeciesIds = new HashSet<string>();

			if ( bossPool != null )
			{
				// Add all regular boss species
				foreach ( var boss in bossPool.Bosses )
					allBossSpeciesIds.Add( boss.SpeciesId );

				// Add all rare boss species too
				if ( bossPool.RareBosses != null )
				{
					foreach ( var rareBoss in bossPool.RareBosses )
						allBossSpeciesIds.Add( rareBoss.SpeciesId );
				}
			}

			// Filter out ALL boss species so they only appear on boss waves
			var availableSpecies = CurrentExpedition.PossibleSpecies
				.Where( s => !allBossSpeciesIds.Contains( s ) )
				.ToList();

			// Fallback if all species were bosses (shouldn't happen, but safety check)
			if ( availableSpecies.Count == 0 )
				availableSpecies = CurrentExpedition.PossibleSpecies;

			for ( int i = 0; i < enemyCount; i++ )
			{
				// Pick species — weighted if SpeciesWeights populated, uniform otherwise
				string speciesId;
				var weights = CurrentExpedition.SpeciesWeights;
				if ( weights != null && weights.Count > 0 )
				{
					float total = availableSpecies.Sum( s => weights.TryGetValue( s, out var w ) ? w : 1f );
					float roll = (float)(random.NextDouble() * total);
					float cumulative = 0f;
					speciesId = availableSpecies[^1]; // fallback to last if rounding eats the roll
					foreach ( var s in availableSpecies )
					{
						cumulative += weights.TryGetValue( s, out var w ) ? w : 1f;
						if ( roll < cumulative ) { speciesId = s; break; }
					}
				}
				else
				{
					speciesId = availableSpecies[random.Next( availableSpecies.Count )];
				}

				// Level scales with wave (hard mode adds bonus levels).
				// Slope is +1 per wave (was +2) — under linear stat scaling
				// the +2 slope made later waves jump ~14 stat per wave, which
				// felt like running into a wall. +1 keeps the in-expedition
				// difficulty curve readable.
				int level = GetEffectiveEnemyLevel( CurrentExpedition.BaseEnemyLevel ) + (CurrentWave - 1) + random.Next( -1, 2 );
				level = Math.Max( 1, level );

				var enemy = CreateEnemyMonster( speciesId, level );
				if ( enemy != null ) enemies.Add( enemy );
			}
		}

		return enemies;
	}

	/// <summary>
	/// Create a boss monster with tier-based stat multipliers
	/// </summary>
	private Monster CreateBossMonster( BossData bossData, int level )
	{
		var species = MonsterManager.Instance?.GetSpecies( bossData.SpeciesId );
		if ( species == null )
		{
			Log.Warning( $"CreateBossMonster: Species '{bossData.SpeciesId}' not found!" );
			return null;
		}

		// Mark species as "seen" in the beastiary
		BeastiaryManager.Instance?.SeeSpecies( bossData.SpeciesId );

		// Generate stronger genetics for bosses (15-25 range)
		var random = _sharedRandom;
		var genetics = new Genetics
		{
			HPGene = random.Next( 15, 26 ),
			ATKGene = random.Next( 15, 26 ),
			DEFGene = random.Next( 15, 26 ),
			SPDGene = random.Next( 15, 26 ),
			Nature = (NatureType)random.Next( 0, 5 )
		};

		var boss = new Monster
		{
			SpeciesId = bossData.SpeciesId,
			Nickname = species.Name,
			Level = level,
			Genetics = genetics,
			IsBoss = true
		};

		MonsterManager.Instance?.RecalculateStats( boss );

		// Apply tier multipliers
		var (hpMult, atkMult, defMult) = bossData.GetTierMultipliers();
		boss.MaxHP = (int)(boss.MaxHP * hpMult);
		boss.ATK = (int)(boss.ATK * atkMult);
		boss.DEF = (int)(boss.DEF * defMult);

		// Hard Mode enemy stat multipliers (applied on TOP of boss tier multipliers,
		// then FullHeal so the displayed HP matches the new MaxHP).
		ApplyHardModeStatMultipliers( boss );

		boss.FullHeal();

		Log.Info( $"CreateBossMonster: Created {boss.Nickname} Lv.{level} with multipliers HP={hpMult}x ATK={atkMult}x DEF={defMult}x" );

		return boss;
	}

	private Monster CreateEnemyMonster( string speciesId, int level )
	{
		var species = MonsterManager.Instance?.GetSpecies( speciesId );
		if ( species == null )
		{
			Log.Warning( $"CreateEnemyMonster: Species '{speciesId}' not found!" );
			return null;
		}

		// Mark species as "seen" in the beastiary (shows image + name only)
		BeastiaryManager.Instance?.SeeSpecies( speciesId );

		// Generate weak genetics for enemies (0-10 range)
		// This makes early game more forgiving
		var random = _sharedRandom;
		var genetics = new Genetics
		{
			HPGene = random.Next( 0, 11 ),
			ATKGene = random.Next( 0, 11 ),
			DEFGene = random.Next( 0, 11 ),
			SPDGene = random.Next( 0, 11 ),
			Nature = (NatureType)random.Next( 0, 5 )
		};

		var enemy = new Monster
		{
			SpeciesId = speciesId,
			Nickname = species.Name,
			Level = level,
			Genetics = genetics
		};

		MonsterManager.Instance?.RecalculateStats( enemy );

		// Hard Mode enemy stat multipliers (+15% ATK/SpA, +10% HP/DEF/SpD; SPD unchanged).
		// Applied AFTER RecalculateStats so the v1.0.3 linear stat formula stays intact —
		// this rides on top of the formula output, never modifying the formula inputs.
		ApplyHardModeStatMultipliers( enemy );

		enemy.FullHeal();

		Log.Info( $"CreateEnemyMonster: Created {enemy.Nickname} Lv.{level} with HP={enemy.MaxHP}, ATK={enemy.ATK}" );

		return enemy;
	}

	public void OnWaveComplete( bool playerWon )
	{
		if ( !playerWon )
		{
			// Expedition failed
			CompleteExpedition( false );
			return;
		}

		if ( CurrentWave >= CurrentExpedition.Waves )
		{
			// All waves cleared!
			CompleteExpedition( true );
		}
		else
		{
			// Offer to catch or continue
			// (This would be handled by UI)
		}
	}

	/// <summary>
	/// Foreground result path — call instead of <see cref="OnWaveComplete"/>
	/// when a UI popup is going to sit over the battle and the player is
	/// the one who decides when the expedition "ends." Awards rewards and
	/// fires <see cref="OnExpeditionComplete"/> but keeps
	/// <see cref="CurrentExpedition"/> + <see cref="SelectedTeam"/> set so
	/// ExpeditionPanel stays rendered behind the popup. Caller must invoke
	/// <see cref="FinalizeExpedition"/> once the popup is dismissed.
	/// </summary>
	public void ResolveExpedition( bool success )
	{
		CompleteExpedition( success, tearDown: false );
	}

	/// <summary>
	/// Finish the teardown started by <see cref="ResolveExpedition"/>.
	/// Nulls the current expedition + clears the team so the HUD can
	/// swap back to the world map. Idempotent — safe to call twice.
	/// </summary>
	public void FinalizeExpedition()
	{
		if ( CurrentExpedition == null ) return;

		Log.Info( $"FinalizeExpedition: tearing down {CurrentExpedition.Id}" );
		CurrentExpedition = null;
		CurrentWave = 0;
		IsRunningInBackground = false;
		SelectedTeam?.Clear();
		SelectedBoss = null;
	}

	private void CompleteExpedition( bool success, bool tearDown = true )
	{
		Log.Info( $"CompleteExpedition: success={success}, tearDown={tearDown}, Wave={CurrentWave}, IsBackground={IsRunningInBackground}" );

		if ( success )
		{
			UpdateExpeditionStats();

			// Award expedition completion rewards (accumulated battle rewards already given per-wave)
			// Apply skill bonuses and hard mode multipliers to rewards
			float goldBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ExpeditionGoldBonus ) ?? 0;
			float xpBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ExpeditionXPBonus ) ?? 0;
			float hardGoldMult = GetGoldMultiplier();
			float hardXPMult = GetXPMultiplier();
			float guildExpedBonus = (GuildManager.Instance?.IsInGuild == true && (GuildManager.Instance?.Guild?.Level ?? 0) >= 2) ? 0.05f : 0f;
			int finalGold = (int)(CurrentExpedition.GoldReward * (1 + goldBonus / 100f) * (1 + guildExpedBonus) * hardGoldMult);
			int finalXP = (int)(CurrentExpedition.XPReward * (1 + xpBonus / 100f) * hardXPMult);
			TamerManager.Instance?.AddGold( finalGold );
			TamerManager.Instance?.AddXP( finalXP );
			AwardTeamCompletionXP( finalXP ); // completion bonus to every team member
			Log.Info( $"CompleteExpedition: Awarded expedition completion rewards: {finalGold} gold (+{goldBonus}%, x{hardGoldMult}), {finalXP} XP (+{xpBonus}%, x{hardXPMult})" );
			AwardHardModeTokens();

			// Track expedition completions for veteran stats
			foreach ( var monster in SelectedTeam )
			{
				if ( monster == null ) continue;
				var ownedMonster = MonsterManager.Instance?.GetMonster( monster.Id );
				if ( ownedMonster != null )
				{
					ownedMonster.ExpeditionsCompleted++;
					ownedMonster.AddJournalEntry(
						$"Completed {CurrentExpedition.Name} expedition!",
						Data.JournalEntryType.Expedition,
						zoneId: CurrentExpedition.Id
					);
				}
			}

			// Award Boss Tokens if a boss was defeated
			if ( SelectedBoss != null )
			{
				AwardBossTokens();
			}

			// Track mission progress for expedition completion
			MissionManager.Instance?.TrackExpeditionComplete( hardMode: CurrentExpedition?.IsHardMode == true, goldEarned: finalGold );
			SideQuestManager.Instance?.TrackExpeditionCleared( CurrentExpedition?.Id );

			// Guild weekly goals — Expedition Blitz (count) + Treasury Push (gold).
			GuildManager.Instance?.TrackGuildGoal( Data.GuildGoalType.ExpeditionsCleared, 1 );
			if ( finalGold > 0 )
				GuildManager.Instance?.TrackGuildGoal( Data.GuildGoalType.GoldEarned, finalGold );

			// Fire "next zone unlocked" toast if the clear bumped HighestExpeditionCleared.
			NotificationManager.Instance?.CheckForNewExpeditionUnlocks();
		}

		// Award accumulated item drops to inventory
		AwardAccumulatedItems();

		// Restore PP for all team monsters after expedition
		RestoreTeamPP();

		// Heal all owned beasts to full HP — fainted beasts shouldn't linger
		// at 0 HP on the home/map screen between runs.
		RestoreAllOwnedHP();

		// Update Lazy demand progress for monsters that rested (weren't in expedition)
		UpdateRestingMonstersContracts();

		OnExpeditionComplete?.Invoke( success );

		// Tutorial: notify that the expedition completed (any outcome).
		TutorialManager.Instance?.NotifyEvent( "expedition.completed" );

		if ( tearDown )
		{
			CurrentExpedition = null;
			CurrentWave = 0;
			IsRunningInBackground = false;
		}

		Log.Info( $"CompleteExpedition done: CurrentExpedition={CurrentExpedition == null}, CurrentWave={CurrentWave}, IsBackground={IsRunningInBackground}" );
	}

	/// <summary>
	/// Last amount of boss tokens awarded (for UI display)
	/// </summary>
	public int LastAwardedBossTokens { get; private set; } = 0;

	/// <summary>
	/// Award Boss Tokens for defeating the expedition boss
	/// </summary>
	private void AwardBossTokens()
	{
		if ( SelectedBoss == null || CurrentExpedition == null )
			return;

		int baseTokens = SelectedBoss.BaseTokenReward;
		int totalTokens = baseTokens;

		// First-time clear bonus
		var clearedBosses = TamerManager.Instance?.CurrentTamer?.ClearedBosses ?? new List<string>();
		bool isFirstClear = !TamerManager.Instance.HasClearedBoss( CurrentExpedition.Id );
		Log.Info( $"[BossTokens] Checking first clear for '{CurrentExpedition.Id}': isFirstClear={isFirstClear}, ClearedBosses=[{string.Join( ", ", clearedBosses )}]" );

		if ( isFirstClear )
		{
			totalTokens += SelectedBoss.FirstClearBonus;
			TamerManager.Instance.MarkBossCleared( CurrentExpedition.Id );
			Log.Info( $"First-time boss clear bonus: +{SelectedBoss.FirstClearBonus} tokens for expedition '{CurrentExpedition.Id}'" );
		}

		// Rare boss multiplier (3x tokens)
		if ( IsRareBossEncounter )
		{
			totalTokens *= 3;
			Log.Info( $"Rare boss multiplier: 3x tokens" );
		}

		// Token Collector skill bonus
		float tokenBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.BossTokenBonus ) ?? 0;
		if ( tokenBonus > 0 )
		{
			int bonusTokens = (int)(totalTokens * tokenBonus / 100f);
			totalTokens += bonusTokens;
			Log.Info( $"Token Collector bonus: +{bonusTokens} tokens (+{tokenBonus}%)" );
		}

		// Rare Radar bonus (applies to tokens too)
		float rareRadarBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.RareEncounterChance ) ?? 0;
		if ( rareRadarBonus > 0 )
		{
			int bonusTokens = (int)(totalTokens * rareRadarBonus / 100f);
			totalTokens += bonusTokens;
			Log.Info( $"Rare Radar skill bonus: +{bonusTokens} tokens" );
		}

		TamerManager.Instance?.AddBossTokens( totalTokens );
		LastAwardedBossTokens = totalTokens;
		AccumulatedTokens += totalTokens;
		Log.Info( $"Awarded {totalTokens} Boss Tokens (base={baseTokens}, firstClear={isFirstClear}, rare={IsRareBossEncounter})" );
	}

	/// <summary>
	/// Updates contract satisfaction for monsters with Lazy demands that weren't in the expedition
	/// </summary>
	private void UpdateRestingMonstersContracts()
	{
		var allMonsters = MonsterManager.Instance?.OwnedMonsters;
		if ( allMonsters == null ) return;

		var expeditionTeamIds = SelectedTeam?.Select( m => m.Id ).ToHashSet() ?? new HashSet<Guid>();

		foreach ( var monster in allMonsters )
		{
			// Skip monsters that were in the expedition
			if ( expeditionTeamIds.Contains( monster.Id ) ) continue;

			// Skip monsters without contracts
			if ( monster.Contract == null ) continue;

			// Check for Lazy demands and update progress
			foreach ( var demand in monster.Contract.SecondaryDemands.Prepend( monster.Contract.PrimaryDemand ) )
			{
				if ( demand.Type == ContractDemandType.Lazy )
				{
					demand.CurrentProgress++;

					// Check if demand was satisfied
					if ( demand.CurrentProgress >= demand.RequiredAmount )
					{
						int satisfactionGain = demand == monster.Contract.PrimaryDemand ? 10 : 5;
						monster.Contract.UpdateSatisfaction( satisfactionGain );
						demand.CurrentProgress = 0; // Reset for next cycle
					}
				}
			}
		}

		// Save monsters after updating contracts
		MonsterManager.Instance?.SaveMonsters();
	}

	/// <summary>
	/// Restore PP for all monsters in the selected team
	/// Called when completing or retrying an expedition
	/// </summary>
	private void RestoreTeamPP()
	{
		if ( SelectedTeam == null || SelectedTeam.Count == 0 )
			return;

		foreach ( var monster in SelectedTeam )
		{
			if ( monster == null )
				continue;

			monster.RestoreAllPP( MoveDatabase.GetMove );
		}

		Log.Info( $"Restored PP for {SelectedTeam.Count} team monsters" );
	}

	/// <summary>
	/// Restore HP to full for every owned beast. Called whenever an expedition
	/// resolves (success OR failure) so fainted beasts don't linger at 0 HP on
	/// the world map / home screen. Pokemon-style "free Pokemon Center" — keeps
	/// the between-run loop frictionless while the balance team figures out a
	/// more punitive model.
	/// </summary>
	private void RestoreAllOwnedHP()
	{
		var mm = MonsterManager.Instance;
		if ( mm?.OwnedMonsters == null ) return;

		int healed = 0;
		foreach ( var monster in mm.OwnedMonsters )
		{
			if ( monster == null ) continue;
			if ( monster.CurrentHP >= monster.MaxHP ) continue;
			monster.CurrentHP = monster.MaxHP;
			healed++;
		}

		if ( healed > 0 )
		{
			Log.Info( $"[Expedition] Healed {healed} owned beasts to full HP on expedition end" );
		}
	}

	/// <summary>
	/// Store catchable enemies from the last completed battle
	/// Called after a wave victory to enable negotiation
	/// </summary>
	public void StoreCatchableEnemies( List<Monster> enemies )
	{
		CatchableEnemies.Clear();
		if ( enemies == null ) return;

		foreach ( var enemy in enemies )
		{
			if ( enemy == null ) continue;

			var species = MonsterManager.Instance?.GetSpecies( enemy.SpeciesId );
			if ( species != null && species.IsCatchable )
			{
				CatchableEnemies.Add( enemy );
			}
		}

		Log.Info( $"Stored {CatchableEnemies.Count} catchable enemies for negotiation" );
	}

	/// <summary>
	/// Clear catchable enemies (after negotiation or skipping)
	/// </summary>
	public void ClearCatchableEnemies()
	{
		CatchableEnemies.Clear();
	}

	/// <summary>
	/// Get a random catchable enemy for negotiation
	/// </summary>
	public Monster GetRandomCatchableEnemy()
	{
		if ( CatchableEnemies.Count == 0 ) return null;
		var random = _sharedRandom;
		return CatchableEnemies[random.Next( CatchableEnemies.Count )];
	}

	/// <summary>
	/// Attempt to catch a wild monster during expedition
	/// </summary>
	public bool TryCatchMonster( Monster target )
	{
		var species = MonsterManager.Instance?.GetSpecies( target.SpeciesId );
		if ( species == null || !species.IsCatchable ) return false;

		// Check for ink save chance from relics
		bool inkSaved = false;
		float inkSaveChance = ItemManager.Instance?.GetRelicBonus( ItemEffectType.PassiveInkSaver ) ?? 0;
		if ( inkSaveChance > 0 && _sharedRandom.NextDouble() < (inkSaveChance / 100f) )
		{
			inkSaved = true;
			Log.Info( "Relic saved contract ink!" );
		}

		// Check if we have contract ink (skip spend if ink was saved)
		if ( !inkSaved && !TamerManager.Instance.SpendContractInk() )
		{
			Log.Warning( "No contract ink!" );
			return false;
		}

		// Calculate catch chance
		float baseCatchRate = species.BaseCatchRate;

		// HP modifier (lower HP = higher catch rate)
		float hpPercent = (float)target.CurrentHP / target.MaxHP;
		float hpModifier = 1.0f + (1.0f - hpPercent) * 0.5f;

		// Skill bonus
		float catchBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.CatchRateBonus ) ?? 0;

		// Relic catch rate bonus
		float relicCatchBonus = ItemManager.Instance?.GetRelicBonus( ItemEffectType.PassiveCatchRate ) ?? 0;

		// Held item catch rate bonus (e.g. Contract Seal) - check all team monsters
		float heldCatchBonus = 0;
		if ( ItemManager.Instance != null )
		{
			foreach ( var monster in SelectedTeam )
			{
				heldCatchBonus += ItemManager.Instance.GetHeldItemBonus( monster, ItemEffectType.CatchRateBoost );
			}
		}

		// Previously caught bonus - easier to re-catch species you've already contracted
		bool previouslyCaught = BeastiaryManager.Instance?.IsDiscovered( target.SpeciesId ) ?? false;
		float previousCatchBonus = previouslyCaught ? 15f : 0f;

		// Guild catch rate bonus (Lv6: +5%)
		float guildCatchBonus = GuildManager.Instance?.GetCatchRateBonus() ?? 0f;

		float finalCatchRate = baseCatchRate * hpModifier * (1 + catchBonus / 100f) * (1 + relicCatchBonus / 100f) * (1 + heldCatchBonus / 100f) * (1 + previousCatchBonus / 100f) * (1 + guildCatchBonus / 100f);
		finalCatchRate = Math.Min( 0.95f, finalCatchRate ); // Max 95% catch rate

		// Master Ink guarantees capture
		bool hasMasterInk = TamerManager.Instance?.CurrentTamer?.HasMasterInk == true;
		if ( hasMasterInk )
		{
			finalCatchRate = 1.0f;
			TamerManager.Instance.CurrentTamer.HasMasterInk = false;
			TamerManager.Instance.SaveToCloud();
			Log.Info( "Master Ink used! Guaranteed capture." );
		}

		var random = _sharedRandom;
		bool caught = random.NextDouble() < finalCatchRate;

		if ( caught )
		{
			// Create caught monster
			var caughtMonster = MonsterManager.Instance?.CreateMonster( target.SpeciesId, isBred: false, target.Genetics );
			if ( caughtMonster != null )
			{
				// Set level to match the wild monster's level (capped at MaxLevel)
				caughtMonster.Level = Math.Min( target.Level, Monster.MaxLevel );
				MonsterManager.Instance?.RecalculateStats( caughtMonster );

				// Refresh moves to match the correct level (CreateMonster uses level 1)
				MonsterManager.Instance?.RefreshMovesForLevel( caughtMonster );

				// Full heal after stat recalculation
				caughtMonster.FullHeal();

				RecordMonsterCatch( caughtMonster );

				Log.Info( $"Caught {caughtMonster.Nickname} at level {caughtMonster.Level}!" );
			}
		}

		return caught;
	}

	/// <summary>
	/// Records a monster catch: increments stats, updates leaderboard, checks achievements, and fires the event.
	/// All catch paths (TryCatchMonster, negotiation, auto-catch) should call this.
	/// </summary>
	public void RecordMonsterCatch( Monster caughtMonster )
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		tamer.TotalMonstersCaught++;
		Stats.SetValue( "monsters-caught-launch", tamer.TotalMonstersCaught );

		// Guild XP for monster catch
		GuildManager.Instance?.AddGuildXP( 10 );
		GuildManager.Instance?.IncrementAchievement( "catch" );

		bool isNewDiscovery = !(BeastiaryManager.Instance?.IsDiscovered( caughtMonster.SpeciesId ) ?? true);
		BeastiaryManager.Instance?.DiscoverSpecies( caughtMonster.SpeciesId );
		AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.TotalMonstersCaught, tamer.TotalMonstersCaught );

		// Track mission progress for monster catch
		var caughtSpecies = MonsterManager.Instance?.GetSpecies( caughtMonster.SpeciesId );
		bool isRareOrHigher = caughtSpecies != null && caughtSpecies.BaseRarity >= Rarity.Rare;
		MissionManager.Instance?.TrackMonsterCaught( isRareOrHigher: isRareOrHigher, isNewDiscovery: isNewDiscovery );
		SideQuestManager.Instance?.TrackContract();

		// Guild "Contract Drive" weekly goal.
		GuildManager.Instance?.TrackGuildGoal( Data.GuildGoalType.BeastsContracted, 1 );

		OnMonsterCaught?.Invoke( caughtMonster );

		TamerManager.Instance?.SaveToCloud();
	}
}

/// <summary>
/// Expedition data structure
/// </summary>
public class Expedition
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public int RequiredLevel { get; set; }
	public int Waves { get; set; }
	public int BaseEnemyLevel { get; set; }
	public List<string> PossibleSpecies { get; set; } = new();
	public ElementType Element { get; set; }
	public int GoldReward { get; set; }
	public int XPReward { get; set; }
	public bool HasBoss { get; set; } = true;
	public string BossSpeciesId { get; set; }
	public string BackgroundImage { get; set; }
	public bool IsBossGauntlet { get; set; } // Every wave is a boss fight

	/// <summary>
	/// True if this expedition is only shown when the tutorial is active.
	/// Filtered out of the World Map list once the player completes/skips the tutorial.
	/// </summary>
	public bool TutorialOnly { get; set; }

	/// <summary>
	/// True for optional side-content expeditions that branch off the main path.
	/// Mini-expeditions skip the sequential-chain gate (using their own unlock predicate
	/// instead) and do NOT count toward HighestExpeditionCleared.
	/// </summary>
	public bool IsMiniExpedition { get; set; }

	/// <summary>
	/// Optional per-species spawn weights for weighted-random selection in GenerateWaveEnemies.
	/// Keys must match entries in PossibleSpecies. Species not listed default to 1.0.
	/// When null or empty, falls back to uniform random selection.
	/// </summary>
	public Dictionary<string, float> SpeciesWeights { get; set; }

	/// <summary>
	/// Runtime flag — set to true when the player starts this expedition in Hard Mode.
	/// NOT a persisted property on the definition; set by StartExpedition and cleared on reset.
	/// BattleSimulator reads this to apply hard-mode enemy modifiers.
	/// </summary>
	public bool IsHardMode { get; set; } = false;
}
