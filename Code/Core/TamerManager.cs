using Sandbox;
using Sandbox.Services;
using Beastborne.Data;
using System.Text.Json;
using System.Linq;

namespace Beastborne.Core;

/// <summary>
/// Manages the player's tamer data, skills, and resources.
///
/// Phase 2: state is owned by <see cref="SaveService"/>. All reads are
/// hydrated from <c>SaveService.Instance.CurrentBlob.Tamer</c> on load;
/// every mutation writes back into the blob and marks it dirty. The
/// service handles batched/throttled cloud pushes + local-cache fallback.
/// </summary>
public sealed class TamerManager : Component
{
	public static TamerManager Instance { get; private set; }

	private const float AUTOSAVE_INTERVAL_SECONDS = 30f;

	public Tamer CurrentTamer { get; private set; }
	public SkillTree SkillTree { get; private set; }

	private float _lastAutosaveTime = 0f;
	private float _playtimeAccumulator = 0f; // seconds since last blob push
	private bool _hasHydrated;

	// Events
	public Action<int> OnGoldChanged;
	public Action<int> OnGemsChanged;
	public Action<int> OnBossTokensChanged;
	public Action<int> OnLevelUp;
	public Action<string> OnSkillUnlocked;
	public Action<string> OnTitleChanged;

	// Anything before this UTC moment counts as "alpha" — a player who
	// loads any session before this gets PlayedDuringAlpha set to true,
	// which sticks for life and unlocks the Alpha title.
	// Bump if launch slips. Currently 2026-05-01 00:00 UTC as a placeholder.
	public static readonly DateTime LaunchDate = new( 2026, 5, 1, 0, 0, 0, DateTimeKind.Utc );

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Log.Info( "TamerManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	protected override void OnStart()
	{
		SkillTree = SkillTree.CreateDefault();

		// Hydrate once SaveService.LoadAsync resolves. If it already has, run now;
		// otherwise subscribe so we pick up the event when it fires.
		if ( SaveService.Instance != null && SaveService.Instance.IsLoaded )
		{
			Hydrate();
		}
		else if ( SaveService.Instance != null )
		{
			SaveService.Instance.OnSaveLoaded += Hydrate;
		}
		else
		{
			// No SaveService at all — shouldn't happen in practice, but don't crash.
			Log.Warning( "[TamerManager] SaveService.Instance is null; starting with defaults" );
			HydrateFromBlank();
		}

		// Reset hook: when the player hits "Reset Game Data", wipe in-memory state
		// and re-hydrate from the fresh empty blob.
		if ( SaveService.Instance != null )
		{
			SaveService.Instance.OnSaveReset += HandleSaveReset;
		}
	}

	private void HandleSaveReset()
	{
		_hasHydrated = false;
		CurrentTamer = null;
		HydrateFromBlank();
		Log.Info( "[TamerManager] reset to defaults" );
	}

	protected override void OnUpdate()
	{
		if ( CurrentTamer == null ) return;

		// Track playtime in-memory every frame, but cap each tick at 1 second.
		// Time.Delta can spike to huge values after a hot reload, alt-tab return,
		// load-screen completion, or pause exit — without this cap, a single
		// glitched frame could add hours/days to TotalPlayTime, which is how the
		// dev save ended up at "1y 89d" after only a handful of test sessions.
		var dt = MathF.Max( 0f, MathF.Min( Time.Delta, 1f ) );
		CurrentTamer.TotalPlayTime += TimeSpan.FromSeconds( dt );
		_playtimeAccumulator += dt;

		// Periodic autosave: push the current tamer snapshot back into the blob
		// and mark it dirty. SaveService throttles actual cloud writes.
		if ( Time.Now - _lastAutosaveTime > AUTOSAVE_INTERVAL_SECONDS )
		{
			_lastAutosaveTime = Time.Now;
			_playtimeAccumulator = 0f;
			SaveToCloud();
		}
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "TamerManager";
		go.Components.Create<TamerManager>();
	}

	// ═══════════════════════════════════════════════════════════════
	// HYDRATION — pulls Tamer state out of SaveBlob on boot
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Populate <see cref="CurrentTamer"/> from <c>SaveService.CurrentBlob.Tamer</c>.
	/// Called once after the SaveService finishes loading.
	/// </summary>
	private void Hydrate()
	{
		if ( _hasHydrated ) return;
		_hasHydrated = true;

		var blob = SaveService.Instance?.CurrentBlob;
		var section = blob?.Tamer;

		if ( section?.Tamer != null )
		{
			CurrentTamer = section.Tamer;
			// Rebind display name + last login from the connection (not saved).
			CurrentTamer.Name = Connection.Local?.DisplayName ?? CurrentTamer.Name ?? "Tamer";
			CurrentTamer.LastLogin = DateTime.UtcNow;

			// Defensive: older snapshots may have null collections after a
			// schema bump; re-initialise to empty so downstream nulls don't
			// crash anything.
			CurrentTamer.SkillRanks ??= new();
			CurrentTamer.ClearedBosses ??= new();
			CurrentTamer.UnlockedTitles ??= new();
			CurrentTamer.Inventory ??= new();
			CurrentTamer.EquippedRelics ??= new();
			CurrentTamer.ActiveBoosts ??= new();
			CurrentTamer.Achievements ??= new();
			CurrentTamer.SpeciesMastery ??= new();
			CurrentTamer.MatchHistory ??= new();
			CurrentTamer.CollectedCards ??= new();
			CurrentTamer.CardBadges ??= new();

			// Purge expired boosts on load.
			CurrentTamer.ActiveBoosts = CurrentTamer.ActiveBoosts.Where( b => !b.IsExpired ).ToList();

			// Strip any inventory entries whose itemId no longer resolves via
			// ItemManager.GetItem. The bag header (.inv-pill .all-items badge)
			// sums every entry in CurrentTamer.Inventory, but the grid silently
			// skips items where GetItem returns null — without this purge the
			// "ALL ITEMS" total drifts above the visible card count.
			//
			// Sources of ghost entries we've seen in the wild:
			// - Old typo'd reward IDs ("gene_booster" vs the registered
			//   "boss_gene_booster"; fixed forward in DailyRewardManager).
			// - mat_<species> for species that were renamed or fully removed
			//   from the DB across pre-launch culls (RegisterBeastMaterials
			//   now iterates every species in the current DB, but anything
			//   removed entirely is still orphaned in old saves).
			// - Stale IDs from removed items / dev cookie data.
			//
			// This guarantees, by construction, header total == sum of
			// renderable cards going forward — even if a future
			// rename/removal misses a registration.
			NormalizeInventory();

			// Apply title grants (Alpha + Johnson) and prune any orphans
			// from the post-cull title cleanup.
			ApplySpecialTitleGrants();
			PruneOrphanTitles();

			// Overflow recovery (ported from old LoadFromCloud).
			if ( CurrentTamer.Gold < 0 )
			{
				Log.Warning( $"Overflow recovery: Gold was {CurrentTamer.Gold}, restoring to {int.MaxValue}" );
				CurrentTamer.Gold = int.MaxValue;
			}
			if ( CurrentTamer.TotalGoldEarned < 0 )
			{
				Log.Warning( $"Overflow recovery: TotalGoldEarned was {CurrentTamer.TotalGoldEarned}, restoring to {int.MaxValue}" );
				CurrentTamer.TotalGoldEarned = int.MaxValue;
			}
			if ( CurrentTamer.TotalDamageDealt < 0 )
			{
				Log.Warning( $"Overflow recovery: TotalDamageDealt was {CurrentTamer.TotalDamageDealt}, restoring to {int.MaxValue}" );
				CurrentTamer.TotalDamageDealt = int.MaxValue;
			}

			// (V2 SP migration moved below the V1 leaderboard reset and re-keyed
			// onto Tamer.MigrationVersion — see the second migration block.)

			// MigrationVersion 1 — beta-launch leaderboard reset. Pre-launch
			// playtest data (raid runs counted as battle wins, dev bot-testing,
			// missing aggregation config, etc) inflated lifetime counters AND
			// the public leaderboards. Zero out the local counters AND push 0
			// to every affected leaderboard slug so the public boards visibly
			// reset — without the explicit upload, players' boards stay frozen
			// at the inflated value until they next trigger an event that
			// uploads a stat. The local achievements/missions that key off
			// these counters will re-progress, which is intentional — fresh
			// boards mean fresh progression too.
			if ( CurrentTamer.MigrationVersion < 1 )
			{
				int prevBattles = CurrentTamer.TotalBattlesWon;
				int prevDamage = CurrentTamer.TotalDamageDealt;
				int prevKOs = CurrentTamer.TotalKnockouts;
				int prevCaught = CurrentTamer.TotalMonstersCaught;
				int prevBred = CurrentTamer.TotalMonstersBred;
				int prevEvolved = CurrentTamer.TotalMonstersEvolved;
				int prevExpeditions = CurrentTamer.TotalExpeditionsCompleted;

				CurrentTamer.TotalBattlesWon = 0;
				CurrentTamer.TotalBattlesLost = 0;
				CurrentTamer.TotalDamageDealt = 0;
				CurrentTamer.TotalKnockouts = 0;
				CurrentTamer.TotalMonstersCaught = 0;
				CurrentTamer.TotalMonstersBred = 0;
				CurrentTamer.TotalMonstersEvolved = 0;
				CurrentTamer.TotalExpeditionsCompleted = 0;

				// Push 0 to every public-facing leaderboard slug so other clients
				// see the reset immediately. Stats.SetValue is fire-and-forget;
				// failures are logged but non-fatal.
				try
				{
					Stats.SetValue( "battles-won-launch", 0 );
					Stats.SetValue( "total-damage-launch", 0 );
					Stats.SetValue( "total-knockouts-launch", 0 );
					Stats.SetValue( "monsters-caught-launch", 0 );
					Stats.SetValue( "monsters-bred-launch", 0 );
					Stats.SetValue( "monsters-evolved-launch", 0 );
					Stats.SetValue( "expeditions-completed-launch", 0 );
				}
				catch ( Exception e )
				{
					Log.Warning( $"[TamerManager] Migration v1 leaderboard push failed: {e.Message}" );
				}

				CurrentTamer.MigrationVersion = 1;
				Log.Info( $"[TamerManager] Beta-launch migration v1: cleared inflated stats (was BattlesWon={prevBattles}, Damage={prevDamage}, KOs={prevKOs}, Caught={prevCaught}, Bred={prevBred}, Evolved={prevEvolved}, Expeditions={prevExpeditions}) and pushed 0 to public leaderboards" );
			}

			// MigrationVersion 2 — kept as a no-op marker so older saves that
			// stamped it don't re-trip anything. The old destructive top-up
			// behavior was replaced by NormalizeSkillState below, which runs
			// on EVERY hydrate (idempotent, only logs when it actually repairs).
			if ( CurrentTamer.MigrationVersion < 2 )
			{
				CurrentTamer.MigrationVersion = 2;
			}

			// ── DEFINITIVE SKILL-STATE FIX ─────────────────────────────────
			// Single self-healing audit on hydrate. Enforces the invariant
			// TotalEarnedSP(Level) == SkillPoints + Σ(rank × CostPerRank).
			// Idempotent — players whose tree is already valid see no change,
			// no toast, no log spam. Players whose tree drifted (orphan keys
			// from cut skills, ranks past MaxRank, over-budget spent, missing
			// SP after a corrupted save) get a surgical repair + one toast.
			//
			// REPLACES the v1.2.1 aggressive blanket respec — which wiped
			// valid trees and caused its own desync problems when the next
			// session loaded an older cloud copy before autosave flushed.
			var repair = CurrentTamer.NormalizeSkillState( SkillTree );
			if ( repair.ChangedAnything )
			{
				Log.Info( $"[NormalizeSkillState/hydrate] Lv{CurrentTamer.Level} repaired: {repair}" );

				// Only notify the player if the change is something they'll
				// actually NOTICE in the tree UI. Pure SP top-up (e.g. they
				// had 5 SP and we restored to 6) needs no scary "tree
				// refunded" toast — silent fix.
				bool playerVisibleRepair =
					repair.OrphanRanksStripped > 0
					|| repair.OverRankRefunds > 0
					|| repair.OverBudgetRanksStripped > 0;

				if ( playerVisibleRepair )
				{
					NotificationManager.Instance?.AddNotification(
						NotificationType.Success,
						"Skill Tree Repaired",
						$"Your skill tree was out of sync — repaired and {repair.SkillPointsAfter} SP are available.",
						8f );
				}
			}

			// Clamp on LOAD as well as on save. Without this, a corrupted save
			// (e.g. pre-launch dev build with inflated TotalPlayTime) would be
			// displayed and submitted to the leaderboard before the next
			// autosave clamps it on disk.
			ClampStats();

			Log.Info( $"[TamerManager] Hydrated: {CurrentTamer.Name} Lv{CurrentTamer.Level} XP={CurrentTamer.TotalXP}" );
		}
		else
		{
			HydrateFromBlank();
		}
	}

	/// <summary>
	/// Fresh-player init. Only runs when the blob has no tamer section at all.
	/// </summary>
	private void HydrateFromBlank()
	{
		CurrentTamer = new Tamer
		{
			Name = Connection.Local?.DisplayName ?? "Tamer",
			Gender = TamerGender.Male,
			Level = 1,
			TotalXP = 0,
			Gold = 100,
			Gems = 0,
			ContractInk = 10,
			SkillPoints = 6,
			HighestExpeditionCleared = 0,
			ArenaRank = "Unranked",
			ArenaPoints = 0,
			LastLogin = DateTime.UtcNow,
			CreatedAt = DateTime.UtcNow,
		};

		// Mirror the defensive ??= block from the existing-save Hydrate path.
		// Tamer.cs declares field initializers (= new()) on all collection
		// fields, but this extra belt-and-suspenders guard protects against
		// any future refactor that removes a default and guarantees downstream
		// code (e.g. tamer.CollectedCards.Add(...) in OnSaveLoaded subscribers)
		// never trips a NullReferenceException on a fresh save.
		CurrentTamer.SkillRanks ??= new();
		CurrentTamer.ClearedBosses ??= new();
		CurrentTamer.UnlockedTitles ??= new();
		CurrentTamer.Inventory ??= new();
		CurrentTamer.EquippedRelics ??= new();
		CurrentTamer.ActiveBoosts ??= new();
		CurrentTamer.Achievements ??= new();
		CurrentTamer.SpeciesMastery ??= new();
		CurrentTamer.MatchHistory ??= new();
		CurrentTamer.CollectedCards ??= new();
		CurrentTamer.CardBadges ??= new();

		// Fresh saves are pre-launch by definition (we cleared the launch gate
		// in code) — apply special grants so Alpha + Johnson land immediately.
		ApplySpecialTitleGrants();

		Log.Info( "[TamerManager] Hydrated: fresh tamer (no existing save)" );
	}

	/// <summary>
	/// Apply title grants that aren't tied to achievements:
	///  • Johnson — granted to everyone, always (flavor title).
	///  • Alpha — granted to anyone whose first session loaded before
	///    <see cref="LaunchDate"/>. Sticks for life via PlayedDuringAlpha.
	/// Idempotent — safe to call on every hydrate.
	/// </summary>
	private void ApplySpecialTitleGrants()
	{
		if ( CurrentTamer == null ) return;
		CurrentTamer.UnlockedTitles ??= new();

		// Johnson — everyone gets it.
		if ( !CurrentTamer.UnlockedTitles.Contains( CosmeticDatabase.JohnsonTitleId ) )
		{
			CurrentTamer.UnlockedTitles.Add( CosmeticDatabase.JohnsonTitleId );
		}

		// Alpha — flag any pre-launch session, then grant if flagged.
		if ( DateTime.UtcNow < LaunchDate )
		{
			CurrentTamer.PlayedDuringAlpha = true;
		}

		if ( CurrentTamer.PlayedDuringAlpha
			&& !CurrentTamer.UnlockedTitles.Contains( CosmeticDatabase.AlphaTitleId ) )
		{
			CurrentTamer.UnlockedTitles.Add( CosmeticDatabase.AlphaTitleId );
		}
	}

	/// <summary>
	/// Drop any UnlockedTitles entries that no longer exist in CosmeticDatabase
	/// (e.g. titles that were cut in the launch cleanup pass). Also clears
	/// ActiveTitleId if it pointed at a now-cut title.
	/// </summary>
	private void PruneOrphanTitles()
	{
		if ( CurrentTamer?.UnlockedTitles == null ) return;
		int before = CurrentTamer.UnlockedTitles.Count;
		CurrentTamer.UnlockedTitles = CurrentTamer.UnlockedTitles
			.Where( id => CosmeticDatabase.GetTitle( id ) != null )
			.ToList();

		if ( !string.IsNullOrEmpty( CurrentTamer.ActiveTitleId )
			&& CosmeticDatabase.GetTitle( CurrentTamer.ActiveTitleId ) == null )
		{
			CurrentTamer.ActiveTitleId = null;
		}

		int dropped = before - CurrentTamer.UnlockedTitles.Count;
		if ( dropped > 0 )
		{
			Log.Info( $"[TamerManager] Pruned {dropped} orphan title(s) cut in cleanup pass" );
		}
	}

	// (RunSkillPointMigrationV2 deleted 2026-05-21 — replaced by the
	// idempotent NormalizeSkillState pass invoked above. Same job, but it
	// runs on EVERY hydrate / level-up / shop reset, not just once per save.)

	// ═══════════════════════════════════════════════════════════════
	// PERSISTENCE — push the in-memory Tamer back into the blob
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Legacy name kept for call-site compatibility across the codebase.
	/// Writes the current tamer snapshot into the save blob and marks it
	/// dirty so <see cref="SaveService"/> flushes it on the next tick.
	/// </summary>
	public void SaveToCloud()
	{
		if ( CurrentTamer == null ) return;
		WriteSnapshot();
	}

	private void WriteSnapshot()
	{
		var service = SaveService.Instance;
		if ( service == null ) return;

		ClampStats();

		var blob = service.CurrentBlob;
		if ( blob == null ) return;

		blob.Tamer ??= new TamerSaveData();

		// Clean expired boosts before persisting so the blob doesn't grow stale.
		if ( CurrentTamer.ActiveBoosts != null )
			CurrentTamer.ActiveBoosts = CurrentTamer.ActiveBoosts.Where( b => !b.IsExpired ).ToList();

		// Embed the entire Tamer object — it's already JSON-friendly.
		blob.Tamer.Tamer = CurrentTamer;

		// Leaderboard fire-and-forget (keep existing behaviour).
		Stats.SetValue( "total-playtime-launch", (int)CurrentTamer.TotalPlayTime.TotalMinutes );

		service.MarkDirty( "tamer" );
	}

	private double GetCloudStat( string statName )
	{
		try
		{
			var stat = Stats.LocalPlayer.Get( statName );
			return stat.Value;
		}
		catch
		{
			return 0;
		}
	}

	/// <summary>
	/// Clamp stats to reasonable maximums to prevent inflated/corrupted values from reaching leaderboards.
	/// </summary>
	/// <summary>
	/// Strip every inventory entry whose itemId doesn't resolve via
	/// <c>ItemManager.GetItem</c>. Hard guarantee that the bag header's
	/// total (which sums every entry blind) stays equal to the sum of
	/// the items the grid actually renders (which skips null-GetItem
	/// entries).
	///
	/// Equipped relics are NEVER stripped — if a save references an
	/// equipped relic whose definition was removed, dropping it would
	/// also break the equip slot. Equipped relic IDs are excluded from
	/// the purge.
	///
	/// Quantities are also clamped: any non-positive count is removed
	/// (older code paths could push zeroes into the dict without
	/// removing the key).
	/// </summary>
	private void NormalizeInventory()
	{
		if ( CurrentTamer?.Inventory == null ) return;
		if ( ItemManager.Instance == null )
		{
			// Defensive: if ItemManager hasn't booted yet, do nothing rather
			// than wipe every entry. This shouldn't happen because Hydrate
			// runs after SaveService.OnSaveLoaded fires (which is after every
			// manager's OnStart), but be safe.
			Log.Warning( "[TamerManager] NormalizeInventory skipped: ItemManager.Instance is null" );
			return;
		}

		var equipped = CurrentTamer.EquippedRelics ?? new();
		var toRemove = new List<string>();

		foreach ( var kvp in CurrentTamer.Inventory )
		{
			if ( kvp.Value <= 0 )
			{
				toRemove.Add( kvp.Key );
				continue;
			}

			// Never strip an equipped relic, even if the definition is missing
			// — keep the slot intact so the equip UI doesn't crater.
			if ( equipped.Contains( kvp.Key ) ) continue;

			if ( ItemManager.Instance.GetItem( kvp.Key ) == null )
			{
				toRemove.Add( kvp.Key );
			}
		}

		if ( toRemove.Count > 0 )
		{
			int totalGhostQty = 0;
			foreach ( var id in toRemove )
			{
				totalGhostQty += CurrentTamer.Inventory.GetValueOrDefault( id, 0 );
				CurrentTamer.Inventory.Remove( id );
			}
			Log.Warning( $"[TamerManager] NormalizeInventory: stripped {toRemove.Count} ghost itemId(s) totaling {totalGhostQty} qty — bag header now matches visible cards. Removed: {string.Join( ", ", toRemove )}" );
		}
	}

	private void ClampStats()
	{
		if ( CurrentTamer == null ) return;

		int maxExpeditions = ExpeditionManager.Instance?.Expeditions?.Count ?? 16;
		CurrentTamer.HighestExpeditionCleared = Math.Clamp( CurrentTamer.HighestExpeditionCleared, 0, maxExpeditions );

		CurrentTamer.Level = Math.Clamp( CurrentTamer.Level, 1, Tamer.MaxLevel );

		// Playtime: anything over 1 year is treated as a corrupted save (most
		// often from pre-launch dev builds where Time.Delta could spike during
		// hot-reload, alt-tab, or pause exit and inject hours/days into the
		// total in a single frame). Hard-reset to 0 in that case rather than
		// clamping — the displayed "365d 0h" was misleading.
		// Otherwise clamp to a sane max so even very heavy legit grinders
		// can't push the leaderboard into garbage values.
		const double absurdPlaytimeMin = 365.0 * 24.0 * 60.0;   // 1 year
		const double maxPlaytimeMin = 90.0 * 24.0 * 60.0;        // 90 days
		var totalMin = CurrentTamer.TotalPlayTime.TotalMinutes;
		if ( totalMin > absurdPlaytimeMin )
		{
			Log.Warning( $"Overflow recovery: TotalPlayTime was {totalMin:F0} min — resetting to 0 (treated as corrupt)." );
			CurrentTamer.TotalPlayTime = TimeSpan.Zero;
		}
		else if ( totalMin > maxPlaytimeMin )
		{
			CurrentTamer.TotalPlayTime = TimeSpan.FromMinutes( maxPlaytimeMin );
		}

		if ( CurrentTamer.Gold < 0 ) CurrentTamer.Gold = 0;
		if ( CurrentTamer.TotalGoldEarned < 0 ) CurrentTamer.TotalGoldEarned = 0;

		if ( CurrentTamer.TotalExpeditionsCompleted < 0 ) CurrentTamer.TotalExpeditionsCompleted = 0;
		if ( CurrentTamer.TotalMonstersCaught < 0 ) CurrentTamer.TotalMonstersCaught = 0;
		if ( CurrentTamer.TotalMonstersBred < 0 ) CurrentTamer.TotalMonstersBred = 0;
		if ( CurrentTamer.TotalMonstersEvolved < 0 ) CurrentTamer.TotalMonstersEvolved = 0;
		if ( CurrentTamer.TotalBattlesWon < 0 ) CurrentTamer.TotalBattlesWon = 0;
		if ( CurrentTamer.TotalDamageDealt < 0 ) CurrentTamer.TotalDamageDealt = 0;
		if ( CurrentTamer.TotalKnockouts < 0 ) CurrentTamer.TotalKnockouts = 0;
	}

	// Resource management
	public void AddGold( int amount )
	{
		// Apply Golden Touch (gold from all sources bonus)
		float goldBonus = GetSkillBonus( SkillEffectType.GoldFromAllSources );
		if ( goldBonus > 0 )
		{
			amount = (int)(amount * (1 + goldBonus / 100f));
		}

		// Apply guild gold perk (Lv8: +10%, Lv15: +25%)
		float guildGoldMultiplier = GuildManager.Instance?.GetGoldMultiplier() ?? 1.0f;
		amount = (int)(amount * guildGoldMultiplier);

		// Prevent int32 overflow (cap at int.MaxValue)
		long newGold = (long)CurrentTamer.Gold + amount;
		CurrentTamer.Gold = (int)Math.Min( newGold, int.MaxValue );

		long newTotal = (long)CurrentTamer.TotalGoldEarned + amount;
		CurrentTamer.TotalGoldEarned = (int)Math.Min( newTotal, int.MaxValue );

		OnGoldChanged?.Invoke( CurrentTamer.Gold );
		AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.TotalGoldEarned, CurrentTamer.TotalGoldEarned );
		Stats.SetValue( "total-gold-launch", (int)(CurrentTamer.TotalGoldEarned / 1000) );
	}

	public bool SpendGold( int amount )
	{
		if ( !CurrentTamer.SpendGold( amount ) ) return false;
		OnGoldChanged?.Invoke( CurrentTamer.Gold );
		return true;
	}

	public void AddGems( int amount )
	{
		CurrentTamer.Gems += amount;
		OnGemsChanged?.Invoke( CurrentTamer.Gems );
	}

	public bool SpendGems( int amount )
	{
		if ( !CurrentTamer.SpendGems( amount ) ) return false;
		OnGemsChanged?.Invoke( CurrentTamer.Gems );
		return true;
	}

	public bool SpendContractInk( int amount = 1 )
	{
		return CurrentTamer.SpendContractInk( amount );
	}

	public void AddContractInk( int amount )
	{
		CurrentTamer.ContractInk += amount;
	}

	// Boss Tokens
	public void AddBossTokens( int amount )
	{
		CurrentTamer.BossTokens += amount;
		OnBossTokensChanged?.Invoke( CurrentTamer.BossTokens );
	}

	public bool SpendBossTokens( int amount )
	{
		if ( CurrentTamer.BossTokens < amount ) return false;
		CurrentTamer.BossTokens -= amount;
		CurrentTamer.BossTokensSpent += amount;
		OnBossTokensChanged?.Invoke( CurrentTamer.BossTokens );
		AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.BossTokensSpent, CurrentTamer.BossTokensSpent );
		Stats.SetValue( "boss-tokens-spent", CurrentTamer.BossTokensSpent );
		return true;
	}

	// Boss tracking
	public bool HasClearedBoss( string expeditionId )
	{
		return CurrentTamer.ClearedBosses.Contains( expeditionId );
	}

	public void MarkBossCleared( string expeditionId )
	{
		if ( !CurrentTamer.ClearedBosses.Contains( expeditionId ) )
		{
			CurrentTamer.ClearedBosses.Add( expeditionId );
			Log.Info( $"[MarkBossCleared] Added '{expeditionId}' to ClearedBosses. Total: {CurrentTamer.ClearedBosses.Count}, List: [{string.Join( ", ", CurrentTamer.ClearedBosses )}]" );
			AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.BossesCleared, CurrentTamer.ClearedBosses.Count );
			SaveToCloud();
		}
		else
		{
			Log.Info( $"[MarkBossCleared] '{expeditionId}' already in ClearedBosses, skipping" );
		}
	}

	// Cosmetics
	public bool HasTitle( string titleId )
	{
		return CurrentTamer.UnlockedTitles.Contains( titleId );
	}

	public bool UnlockTitle( string titleId )
	{
		if ( CurrentTamer.UnlockedTitles.Contains( titleId ) ) return false;
		CurrentTamer.UnlockedTitles.Add( titleId );
		SaveToCloud();
		return true;
	}

	public void SetActiveTitle( string titleId )
	{
		if ( titleId != null && !CurrentTamer.UnlockedTitles.Contains( titleId ) ) return;
		CurrentTamer.ActiveTitleId = titleId;
		OnTitleChanged?.Invoke( titleId );
		SaveToCloud();
	}

	public void SetGender( TamerGender gender )
	{
		CurrentTamer.Gender = gender;
		SaveToCloud();
	}

	// 2x Tamer XP Event — ends Mar 16, 2026 at 10:00 UTC
	public static readonly DateTime EventXPEnd = new DateTime( 2026, 3, 16, 10, 0, 0, DateTimeKind.Utc );
	public static bool IsDoubleXPActive => DateTime.UtcNow < EventXPEnd;

	// XP and leveling
	public void AddXP( int amount )
	{
		// Apply tamer XP boost from shop
		float tamerXPBoost = ShopManager.Instance?.GetBoostMultiplier( ShopItemType.TamerXPBoost ) ?? 1.0f;

		// Apply relic tamer XP bonus
		float relicTamerXP = ItemManager.Instance?.GetRelicBonus( ItemEffectType.PassiveTamerXP ) ?? 0;

		// Apply 2x event bonus
		float eventMultiplier = IsDoubleXPActive ? 2.0f : 1.0f;

		// Apply guild XP perk (Lv4: +5%, Lv10: +15%)
		float guildXPMultiplier = GuildManager.Instance?.GetTamerXPMultiplier() ?? 1.0f;

		// Apply server-driven live-event XP boost (player-wide week-long boosts, etc.)
		float liveEventXPBoost = (float)(LiveEventManager.Instance?.GetBoostMultiplier( "xp" ) ?? 1.0);

		int boostedAmount = (int)(amount * tamerXPBoost * (1 + relicTamerXP / 100f) * eventMultiplier * guildXPMultiplier * liveEventXPBoost);

		if ( CurrentTamer.AddXP( boostedAmount ) )
		{
			// Defensive: AddXP just incremented SkillPoints by 1-per-level.
			// Normalize so the invariant still holds (cheap on a clean save:
			// idempotent, returns no-op). Catches any latent corruption that
			// got worse with the level-up SP grant.
			var rep = CurrentTamer.NormalizeSkillState( SkillTree );
			if ( rep.ChangedAnything )
				Log.Info( $"[NormalizeSkillState/level-up] Lv{CurrentTamer.Level} repaired: {rep}" );

			OnLevelUp?.Invoke( CurrentTamer.Level );
			AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.TamerLevel, CurrentTamer.Level );
			Stats.SetValue( "tamer-level", CurrentTamer.Level );
		}
	}

	// ─── DEV CONSOLE COMMANDS ─────────────────────────────────────────
	// Quick testing helpers. Bypass boost multipliers so the value you
	// ask for is what you get. Strip these before shipping.

	[ConCmd( "dev_addxp" )]
	public static void DevAddXp( int amount )
	{
		var tamer = Instance?.CurrentTamer;
		if ( tamer == null ) { Log.Warning( "[dev_addxp] No current tamer." ); return; }
		bool leveledUp = tamer.AddXP( amount );
		if ( leveledUp )
		{
			Instance.OnLevelUp?.Invoke( tamer.Level );
			Stats.SetValue( "tamer-level", tamer.Level );
		}
		Instance.SaveToCloud();
		Log.Info( $"[dev_addxp] +{amount} XP → Lv {tamer.Level} ({tamer.TotalXP} total)" );
	}

	[ConCmd( "dev_setlevel" )]
	public static void DevSetLevel( int level )
	{
		var tamer = Instance?.CurrentTamer;
		if ( tamer == null ) { Log.Warning( "[dev_setlevel] No current tamer." ); return; }
		level = Math.Clamp( level, 1, Tamer.MaxLevel );
		// Walk forward via AddXP so level-up side effects fire correctly.
		while ( tamer.Level < level )
		{
			int needed = tamer.XPForNextLevel - tamer.CurrentLevelXP;
			if ( needed <= 0 ) break;
			tamer.AddXP( needed );
		}
		Instance.OnLevelUp?.Invoke( tamer.Level );
		Stats.SetValue( "tamer-level", tamer.Level );
		Instance.SaveToCloud();
		Log.Info( $"[dev_setlevel] Now Lv {tamer.Level}" );
	}

	[ConCmd( "dev_addgold" )]
	public static void DevAddGold( int amount )
	{
		Instance?.AddGold( amount );
		Log.Info( $"[dev_addgold] +{amount}g → {Instance?.CurrentTamer?.Gold}g total" );
	}

	// Skill management (ranked system)

	/// <summary>
	/// Check if tamer has at least 1 rank in a skill
	/// </summary>
	public bool HasSkill( string skillId )
	{
		return GetSkillRank( skillId ) > 0;
	}

	/// <summary>
	/// Get the current rank of a skill (0 if not learned)
	/// </summary>
	public int GetSkillRank( string skillId )
	{
		return CurrentTamer.SkillRanks.GetValueOrDefault( skillId, 0 );
	}

	/// <summary>
	/// Check if a skill is at max rank
	/// </summary>
	public bool IsSkillMaxed( string skillId )
	{
		var node = SkillTree.GetNode( skillId );
		if ( node == null ) return false;
		return GetSkillRank( skillId ) >= node.MaxRank;
	}

	/// <summary>
	/// Check if tamer can upgrade this skill (has points and meets requirements)
	/// </summary>
	public bool CanUnlockSkill( string skillId )
	{
		var node = SkillTree.GetNode( skillId );
		if ( node == null ) return false;
		if ( CurrentTamer.SkillPoints < node.CostPerRank ) return false;

		int currentRank = GetSkillRank( skillId );
		if ( currentRank >= node.MaxRank ) return false; // Already maxed

		// Check branch point investment requirement (tier-based)
		int branchPointsSpent = SkillTree.GetPointsSpentInBranch( node.Branch, CurrentTamer.SkillRanks );
		if ( branchPointsSpent < node.RequiredBranchPoints ) return false;

		// Check specific skill prerequisite (for special chains like Crit Eye -> Devastating Blows)
		if ( !string.IsNullOrEmpty( node.RequiredSkillId ) )
		{
			if ( GetSkillRank( node.RequiredSkillId ) < node.RequiredSkillRank ) return false;
		}

		return true;
	}

	/// <summary>
	/// Upgrade a skill by 1 rank
	/// </summary>
	public bool UnlockSkill( string skillId )
	{
		if ( !CanUnlockSkill( skillId ) ) return false;

		var node = SkillTree.GetNode( skillId );
		CurrentTamer.SkillPoints -= node.CostPerRank;

		int currentRank = GetSkillRank( skillId );
		CurrentTamer.SkillRanks[skillId] = currentRank + 1;

		OnSkillUnlocked?.Invoke( skillId );

		// Achievement hooks for skills
		AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.SkillsUnlocked, CurrentTamer.SkillRanks.Count );
		AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.SkillPointsInvested, GetTotalSkillPointsSpent() );
		Stats.SetValue( "skills-unlocked", CurrentTamer.SkillRanks.Count );
		Stats.SetValue( "skill-points", GetTotalSkillPointsSpent() );

		SaveToCloud();

		return true;
	}

	/// <summary>
	/// Upgrade a skill to max rank (spending all required points)
	/// </summary>
	public bool MaxOutSkill( string skillId )
	{
		var node = SkillTree.GetNode( skillId );
		if ( node == null ) return false;

		int currentRank = GetSkillRank( skillId );
		int ranksNeeded = node.MaxRank - currentRank;
		int costNeeded = ranksNeeded * node.CostPerRank;

		if ( CurrentTamer.SkillPoints < costNeeded ) return false;

		// Check branch point investment requirement (tier-based)
		int branchPointsSpent = SkillTree.GetPointsSpentInBranch( node.Branch, CurrentTamer.SkillRanks );
		if ( branchPointsSpent < node.RequiredBranchPoints ) return false;

		// Check specific skill prerequisite
		if ( !string.IsNullOrEmpty( node.RequiredSkillId ) )
		{
			if ( GetSkillRank( node.RequiredSkillId ) < node.RequiredSkillRank ) return false;
		}

		CurrentTamer.SkillPoints -= costNeeded;
		CurrentTamer.SkillRanks[skillId] = node.MaxRank;

		OnSkillUnlocked?.Invoke( skillId );
		SaveToCloud();

		return true;
	}

	/// <summary>
	/// Resets all tamer data to defaults
	/// </summary>
	public void ResetTamer()
	{
		CurrentTamer = new Tamer
		{
			Name = Connection.Local?.DisplayName ?? "Tamer",
			Gender = TamerGender.Male,
			Level = 1,
			TotalXP = 0,
			Gold = 100,
			Gems = 0,
			ContractInk = 10,
			BossTokens = 0,
			SkillPoints = 6,
			HighestExpeditionCleared = 0,
			ArenaRank = "Unranked",
			ArenaPoints = 0,
			TotalBattlesWon = 0,
			TotalBattlesLost = 0,
			TotalMonstersCaught = 0,
			TotalMonstersBred = 0,
			TotalMonstersEvolved = 0,
			SkillRanks = new(),
			ClearedBosses = new(),
			UnlockedTitles = new(),
			Inventory = new(),
			EquippedRelics = new(),
			ActiveBoosts = new(),
			ActiveTitleId = null,
			ActiveLevelTitle = null,
			LastLogin = DateTime.UtcNow
		};

		ApplySpecialTitleGrants();

		SaveToCloud();
		Log.Info( "Tamer data reset to defaults" );
	}

	// Get total number of skills available in the skill tree
	public int GetTotalSkillCount() => SkillTree?.AllNodes?.Count ?? 0;

	// Get total bonus from all unlocked skills for a specific effect type (multiplied by rank)
	public float GetSkillBonus( SkillEffectType effectType, ElementType? element = null )
	{
		float total = 0;

		foreach ( var kvp in CurrentTamer.SkillRanks )
		{
			string skillId = kvp.Key;
			int rank = kvp.Value;
			if ( rank <= 0 ) continue;

			var node = SkillTree.GetNode( skillId );
			if ( node?.Effects == null ) continue;

			foreach ( var effect in node.Effects )
			{
				if ( effect.Type == effectType )
				{
					// For element-specific effects, check if element matches
					if ( effect.AffectedElement.HasValue && element.HasValue )
					{
						if ( effect.AffectedElement.Value == element.Value )
							total += effect.Value * rank;
					}
					else if ( !effect.AffectedElement.HasValue )
					{
						total += effect.Value * rank;
					}
				}
			}
		}

		return total;
	}

	/// <summary>
	/// Get total skill points spent on all skills
	/// </summary>
	public int GetTotalSkillPointsSpent()
	{
		int total = 0;
		foreach ( var kvp in CurrentTamer.SkillRanks )
		{
			var node = SkillTree.GetNode( kvp.Key );
			if ( node != null )
			{
				total += kvp.Value * node.CostPerRank;
			}
		}
		return total;
	}

	/// <summary>
	/// Get total ranks unlocked across all skills
	/// </summary>
	public int GetTotalRanksUnlocked()
	{
		return CurrentTamer.SkillRanks.Values.Sum();
	}

	/// <summary>
	/// Get max possible ranks across all skills
	/// </summary>
	public int GetMaxPossibleRanks()
	{
		return SkillTree?.AllNodes?.Sum( n => n.MaxRank ) ?? 0;
	}

	/// <summary>
	/// Wipe every skill rank and refund the spent SP back to the tamer's pool.
	/// Returns the number of SP refunded. UI/skill-tree panel calls this from
	/// the inline two-stage confirm flow; gem/currency costs (if any) are
	/// handled by callers that wrap this (currently none — reset is free).
	/// </summary>
	public int ResetSkillTree()
	{
		if ( CurrentTamer == null || SkillTree == null ) return 0;

		int refundedPoints = GetTotalSkillPointsSpent();
		CurrentTamer.SkillRanks.Clear();
		// Re-derive SP from the invariant rather than adding the refund to
		// a possibly-stale free pool — this is the OFFICIAL reset path, so
		// "all earned SP, all loose" is the only correct result.
		CurrentTamer.SkillPoints = Tamer.GetTotalSkillPointsForLevel( CurrentTamer.Level );

		// Belt-and-suspenders normalize so the invariant is guaranteed
		// regardless of what state we entered from.
		var rep = CurrentTamer.NormalizeSkillState( SkillTree );
		if ( rep.ChangedAnything )
			Log.Info( $"[NormalizeSkillState/reset] Lv{CurrentTamer.Level} repaired: {rep}" );

		Stats.SetValue( "skills-unlocked", 0 );
		Stats.SetValue( "skill-points", 0 );

		SaveToCloud();
		Log.Info( $"[TamerManager] Skill tree reset — refunded {refundedPoints} SP, pool now {CurrentTamer.SkillPoints}" );
		return refundedPoints;
	}

	/// <summary>
	/// Collect or update another player's tamer card
	/// </summary>
	public void CollectTamerCard( long steamId, string name, int level, string arenaRank, int arenaPoints, string favoriteSpeciesId, int achievementCount, float winRate,
		string gender = null, string favoriteExpeditionId = null, string title = null, string titleColor = null, int arenaWins = 0, int arenaLosses = 0, int highestExpedition = 0, int monstersCaught = 0, int totalPlayTimeMinutes = 0,
		int battlesWon = 0, int monstersBred = 0, int monstersEvolved = 0, int totalExpeditionsCompleted = 0, int totalTradesCompleted = 0 )
	{
		if ( CurrentTamer == null || steamId == 0 ) return;

		var existing = CurrentTamer.CollectedCards.FirstOrDefault( c => c.SteamId == steamId );
		if ( existing != null )
		{
			// Update existing card (only overwrite with non-default values)
			existing.Name = name;
			if ( level > 0 ) existing.Level = level;
			existing.ArenaRank = arenaRank ?? existing.ArenaRank ?? "Unranked";
			if ( arenaPoints > 0 ) existing.ArenaPoints = arenaPoints;
			if ( !string.IsNullOrEmpty( favoriteSpeciesId ) ) existing.FavoriteMonsterSpeciesId = favoriteSpeciesId;
			if ( achievementCount > 0 ) existing.AchievementCount = achievementCount;
			if ( winRate > 0 ) existing.WinRate = winRate;
			if ( !string.IsNullOrEmpty( gender ) ) existing.Gender = gender;
			if ( !string.IsNullOrEmpty( favoriteExpeditionId ) ) existing.FavoriteExpeditionId = favoriteExpeditionId;
			if ( !string.IsNullOrEmpty( title ) ) existing.Title = title;
			if ( !string.IsNullOrEmpty( titleColor ) ) existing.TitleColor = titleColor;
			if ( arenaWins > 0 ) existing.ArenaWins = arenaWins;
			if ( arenaLosses > 0 ) existing.ArenaLosses = arenaLosses;
			if ( highestExpedition > 0 ) existing.HighestExpedition = highestExpedition;
			if ( monstersCaught > 0 ) existing.MonstersCaught = monstersCaught;
			if ( totalPlayTimeMinutes > 0 ) existing.TotalPlayTimeMinutes = totalPlayTimeMinutes;
			if ( battlesWon > 0 ) existing.BattlesWon = battlesWon;
			if ( monstersBred > 0 ) existing.MonstersBred = monstersBred;
			if ( monstersEvolved > 0 ) existing.MonstersEvolved = monstersEvolved;
			if ( totalExpeditionsCompleted > 0 ) existing.TotalExpeditionsCompleted = totalExpeditionsCompleted;
			if ( totalTradesCompleted > 0 ) existing.TotalTradesCompleted = totalTradesCompleted;
			existing.LastUpdated = DateTime.UtcNow;
		}
		else
		{
			// Add new card
			CurrentTamer.CollectedCards.Add( new CollectedTamerCard
			{
				SteamId = steamId,
				Name = name,
				Level = level,
				ArenaRank = arenaRank ?? "Unranked",
				ArenaPoints = arenaPoints,
				FavoriteMonsterSpeciesId = favoriteSpeciesId,
				AchievementCount = achievementCount,
				WinRate = winRate,
				Gender = gender ?? "Male",
				FavoriteExpeditionId = favoriteExpeditionId,
				Title = title,
				TitleColor = titleColor ?? "#a78bfa",
				ArenaWins = arenaWins,
				ArenaLosses = arenaLosses,
				HighestExpedition = highestExpedition,
				MonstersCaught = monstersCaught,
				TotalPlayTimeMinutes = totalPlayTimeMinutes,
				BattlesWon = battlesWon,
				MonstersBred = monstersBred,
				MonstersEvolved = monstersEvolved,
				TotalExpeditionsCompleted = totalExpeditionsCompleted,
				TotalTradesCompleted = totalTradesCompleted,
				CollectedAt = DateTime.UtcNow,
				LastUpdated = DateTime.UtcNow
			} );

			// Achievement check for collecting cards
			AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.TamerCardsCollected, CurrentTamer.CollectedCards.Count );
			Stats.SetValue( "cards-collected", CurrentTamer.CollectedCards.Count );
		}
	}
}
