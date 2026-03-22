using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sandbox;
using Sandbox.Services;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// Manages the daily login streak system with escalating rewards,
/// streak shields, milestones, and weekly multipliers.
/// </summary>
public sealed class DailyRewardManager : Component
{
	public static DailyRewardManager Instance { get; private set; }

	private const string STAT_PREFIX = "tamer-";

	/// <summary>
	/// Get the full key with slot prefix
	/// </summary>
	private static string GetKey( string key ) => $"{SaveSlotManager.GetSlotPrefix()}{key}";

	// ═══════════════════════════════════════════════════════════════
	// STREAK STATE (loaded from / saved to cookies)
	// ═══════════════════════════════════════════════════════════════

	/// <summary>Current consecutive login days</summary>
	public int DailyStreak { get; private set; }

	/// <summary>Last date the player logged in (UTC)</summary>
	public DateTime LastLoginDate { get; private set; }

	/// <summary>Cumulative total days ever logged in</summary>
	public int TotalLoginDays { get; private set; }

	/// <summary>Whether the one-per-week streak shield has been used</summary>
	public bool StreakShieldUsed { get; private set; }

	/// <summary>When the shield resets (next Monday midnight UTC)</summary>
	public DateTime StreakShieldResetDate { get; private set; }

	/// <summary>Whether today's daily reward has already been claimed</summary>
	public bool TodayRewardClaimed { get; private set; }

	/// <summary>Which milestone day-counts have been claimed</summary>
	public List<int> MilestonesClaimed { get; private set; } = new();

	/// <summary>True if the shield was consumed this session (for UI messaging)</summary>
	public bool ShieldSavedStreak { get; private set; }

	/// <summary>True if a Day 7 beast reward is pending (box was full)</summary>
	public bool Day7BeastPending { get; private set; }

	// Events
	public Action OnStreakUpdated;
	public Action<int, int, int, int> OnRewardClaimed; // gold, gems, ink, xp

	// ═══════════════════════════════════════════════════════════════
	// LIFECYCLE
	// ═══════════════════════════════════════════════════════════════

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Log.Info( "DailyRewardManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	protected override void OnStart()
	{
		LoadFromCookies();
		CheckLoginStreak();
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "DailyRewardManager";
		go.Components.Create<DailyRewardManager>();
	}

	// ═══════════════════════════════════════════════════════════════
	// STREAK REWARDS TABLE
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Returns (gold, gems, ink) base rewards for a given day in the 7-day cycle.
	/// </summary>
	public (int Gold, int Gems, int Ink) GetDayReward( int day )
	{
		return day switch
		{
			1 => (2000, 0, 5),
			2 => (3000, 0, 5),
			3 => (5000, 0, 10),
			4 => (7500, 0, 10),
			5 => (10000, 2, 15),
			6 => (15000, 3, 0),
			7 => (25000, 5, 20),
			_ => (2000, 0, 5)
		};
	}

	/// <summary>
	/// Returns the item reward ID for a given day (empty for most days).
	/// Day 4 gives an XP Feast, Day 6 gives a Catch Lure.
	/// </summary>
	public string GetDayItemReward( int day )
	{
		return day switch
		{
			4 => "xp_feast",
			6 => "catch_lure",
			_ => null
		};
	}

	/// <summary>
	/// Current day in the 7-day cycle (1 through 7).
	/// </summary>
	public int GetStreakDay()
	{
		if ( DailyStreak <= 0 ) return 1;
		int mod = DailyStreak % 7;
		return mod == 0 ? 7 : mod;
	}

	/// <summary>
	/// Calculates the week multiplier bonus on gold.
	/// 2+ weeks: 50% more gold. 4+ weeks: 100% more gold.
	/// </summary>
	public float GetWeekMultiplier()
	{
		int completedWeeks = DailyStreak / 7;
		if ( completedWeeks >= 4 ) return 2.0f;
		if ( completedWeeks >= 2 ) return 1.5f;
		return 1.0f;
	}

	/// <summary>
	/// Returns the final reward amounts for today, including the week multiplier on gold.
	/// </summary>
	public (int Gold, int Gems, int Ink) GetTodayReward()
	{
		int day = GetStreakDay();
		var (gold, gems, ink) = GetDayReward( day );
		gold = (int)(gold * GetWeekMultiplier());
		return (gold, gems, ink);
	}

	// ═══════════════════════════════════════════════════════════════
	// STREAK CHECK (called on game load)
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Check if today is a new day and update the streak accordingly.
	/// Handles shield activation, streak reset, and new-day detection.
	/// </summary>
	public void CheckLoginStreak()
	{
		ShieldSavedStreak = false;
		var now = DateTime.UtcNow;
		var today = now.Date;
		var lastDate = LastLoginDate.Date;

		// Reset shield if we've passed the reset date (Monday midnight UTC)
		if ( now >= StreakShieldResetDate )
		{
			StreakShieldUsed = false;
			StreakShieldResetDate = GetNextMondayMidnight( now );
		}

		// Same day — nothing to do
		if ( lastDate == today )
		{
			return;
		}

		int daysMissed = (today - lastDate).Days;

		if ( daysMissed == 1 )
		{
			// Consecutive day — advance streak
			DailyStreak++;
			TotalLoginDays++;
			TodayRewardClaimed = false;
		}
		else if ( daysMissed == 2 && !StreakShieldUsed )
		{
			// Missed exactly 1 day — shield activates
			StreakShieldUsed = true;
			ShieldSavedStreak = true;
			DailyStreak++;
			TotalLoginDays++;
			TodayRewardClaimed = false;
			Log.Info( $"[DailyReward] Streak shield activated! Streak preserved at {DailyStreak}" );
		}
		else if ( daysMissed > 1 )
		{
			// Missed too many days (or missed 2 and shield already used) — reset
			DailyStreak = 1;
			TotalLoginDays++;
			TodayRewardClaimed = false;
			Log.Info( "[DailyReward] Streak reset — too many days missed" );
		}
		else if ( lastDate == DateTime.MinValue.Date )
		{
			// First time ever logging in
			DailyStreak = 1;
			TotalLoginDays = 1;
			TodayRewardClaimed = false;
		}

		LastLoginDate = now;

		// Update leaderboard
		Stats.SetValue( "daily-streak", DailyStreak );

		// Notify UI
		OnStreakUpdated?.Invoke();

		SaveToCookies();

		Log.Info( $"[DailyReward] Login check complete — Day {GetStreakDay()}, Streak {DailyStreak}, Total {TotalLoginDays}" );
	}

	// ═══════════════════════════════════════════════════════════════
	// CLAIMING DAILY REWARD
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Claim today's daily login reward. Returns true if successful.
	/// </summary>
	public bool ClaimDailyReward()
	{
		if ( TodayRewardClaimed ) return false;

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return false;

		var (gold, gems, ink) = GetTodayReward();
		int day = GetStreakDay();
		string itemId = GetDayItemReward( day );

		// Grant gold (use TamerManager to apply Golden Touch + guild bonuses)
		TamerManager.Instance.AddGold( gold );

		// Grant gems
		if ( gems > 0 )
			TamerManager.Instance.AddGems( gems );

		// Grant ink
		if ( ink > 0 )
			TamerManager.Instance.AddContractInk( ink );

		// Grant item reward
		if ( !string.IsNullOrEmpty( itemId ) )
		{
			if ( tamer.Inventory.ContainsKey( itemId ) )
				tamer.Inventory[itemId]++;
			else
				tamer.Inventory[itemId] = 1;
		}

		// Day 7: grant legendary beast
		if ( day == 7 )
		{
			GrantDay7Beast();
		}

		TodayRewardClaimed = true;

		// Notification
		NotificationManager.Instance?.AddNotification(
			NotificationType.Success,
			"Daily Reward Claimed!",
			$"Day {day}: {gold:N0} Gold" +
			(gems > 0 ? $" + {gems} Gems" : "") +
			(ink > 0 ? $" + {ink} Ink" : "")
		);

		OnRewardClaimed?.Invoke( gold, gems, ink, 0 );

		SaveToCookies();
		TamerManager.Instance?.SaveToCloud();

		Log.Info( $"[DailyReward] Claimed Day {day} reward: {gold}G, {gems} Gems, {ink} Ink" );
		return true;
	}

	/// <summary>
	/// Grant a random Legendary beast for Day 7 completion.
	/// </summary>
	private void GrantDay7Beast()
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		// Check box space
		int currentCount = MonsterManager.Instance?.OwnedMonsters?.Count ?? 0;
		int maxCount = MonsterManager.Instance?.MaxMonsters ?? 0;

		if ( currentCount >= maxCount )
		{
			Day7BeastPending = true;
			NotificationManager.Instance?.AddNotification(
				NotificationType.Warning,
				"Box Full!",
				"Free your box space to claim your Day 7 beast!"
			);
			SaveToCookies();
			return;
		}

		SpawnLegendaryBeast();
	}

	/// <summary>
	/// Try to claim the pending Day 7 beast (called when player frees box space).
	/// </summary>
	public bool ClaimPendingBeast()
	{
		if ( !Day7BeastPending ) return false;

		int currentCount = MonsterManager.Instance?.OwnedMonsters?.Count ?? 0;
		int maxCount = MonsterManager.Instance?.MaxMonsters ?? 0;

		if ( currentCount >= maxCount ) return false;

		SpawnLegendaryBeast();
		Day7BeastPending = false;
		SaveToCookies();
		return true;
	}

	/// <summary>
	/// Create and add a random Legendary beast to the player's box.
	/// </summary>
	private void SpawnLegendaryBeast()
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;

		// Pick a random Legendary species
		var legendaryIds = new[] { "mythweaver", "worldserpent", "voiddragon", "primordius" };
		var random = new Random();
		var speciesId = legendaryIds[random.Next( legendaryIds.Length )];

		var species = MonsterManager.Instance?.GetSpecies( speciesId );
		if ( species == null )
		{
			Log.Warning( $"[DailyReward] Could not find legendary species '{speciesId}'" );
			return;
		}

		// Generate decent-quality genetics (re-roll until total gene value >= 90, i.e., 50%+ quality)
		Genetics genetics;
		do
		{
			genetics = Genetics.GenerateRandom();
		}
		while ( genetics.TotalValue < 90 );

		var monster = new Monster
		{
			SpeciesId = speciesId,
			Nickname = species.Name,
			Level = 25,
			Genetics = genetics,
			OriginalTrainerName = tamer.Name ?? "Unknown",
			OriginalTrainerId = Connection.Local?.SteamId ?? 0
		};

		MonsterManager.Instance?.RecalculateStats( monster );
		MonsterManager.Instance?.AddMonster( monster );

		// Add journal entry
		monster.AddJournalEntry(
			"Arrived as a Daily Streak reward — a gift for loyalty.",
			JournalEntryType.General
		);

		NotificationManager.Instance?.AddNotification(
			NotificationType.Success,
			"Legendary Beast Received!",
			$"A wild {species.Name} has joined your team!"
		);

		// Announce in chat
		var playerName = tamer.Name ?? "Player";
		ChatManager.Instance?.AnnounceMilestone( playerName,
			$"received a Legendary {species.Name} from their Day 7 streak reward!"
		);

		Log.Info( $"[DailyReward] Granted Day 7 Legendary: {species.Name} (Lv25)" );
	}

	// ═══════════════════════════════════════════════════════════════
	// MILESTONES
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// All milestone thresholds (in total cumulative days).
	/// </summary>
	public static readonly int[] MilestoneThresholds = { 7, 14, 30, 60, 100, 365 };

	/// <summary>
	/// Returns milestone reward details for a given day threshold.
	/// </summary>
	public (int Gold, int Gems, string Title, string ItemId) GetMilestoneReward( int totalDays )
	{
		return totalDays switch
		{
			7   => (25000, 5, "Dedicated", null),
			14  => (50000, 10, null, "gene_booster"),
			30  => (100000, 25, "Devoted", "master_ink"),
			60  => (250000, 50, "Faithful", "trait_reroll"),
			100 => (500000, 100, "Eternal", "loyalty_charm"),
			365 => (1000000, 250, "Beastborne Veteran", null),
			_ => (0, 0, null, null)
		};
	}

	/// <summary>
	/// Get the next unclaimed milestone the player is working toward, or -1 if all claimed.
	/// </summary>
	public int GetNextMilestone()
	{
		foreach ( var threshold in MilestoneThresholds )
		{
			if ( !MilestonesClaimed.Contains( threshold ) )
				return threshold;
		}
		return -1;
	}

	/// <summary>
	/// Check if a milestone is claimable (reached but not yet claimed).
	/// </summary>
	public bool IsMilestoneClaimable( int milestone )
	{
		return DailyStreak >= milestone && !MilestonesClaimed.Contains( milestone );
	}

	/// <summary>
	/// Claim a milestone reward. Returns true if successful.
	/// </summary>
	public bool ClaimMilestone( int milestone )
	{
		if ( !IsMilestoneClaimable( milestone ) ) return false;

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return false;

		var (gold, gems, title, itemId) = GetMilestoneReward( milestone );
		if ( gold == 0 && gems == 0 ) return false;

		TamerManager.Instance.AddGold( gold );

		if ( gems > 0 )
			TamerManager.Instance.AddGems( gems );

		if ( !string.IsNullOrEmpty( title ) && !tamer.UnlockedTitles.Contains( title ) )
			tamer.UnlockedTitles.Add( title );

		if ( !string.IsNullOrEmpty( itemId ) )
		{
			if ( tamer.Inventory.ContainsKey( itemId ) )
				tamer.Inventory[itemId]++;
			else
				tamer.Inventory[itemId] = 1;
		}

		MilestonesClaimed.Add( milestone );

		NotificationManager.Instance?.AddNotification(
			NotificationType.Success,
			$"Milestone: {milestone} Days!",
			$"{gold:N0} Gold + {gems} Gems" +
			(!string.IsNullOrEmpty( title ) ? $" + \"{title}\" Title" : "")
		);

		// Announce in chat
		var playerName = tamer.Name ?? "Player";
		ChatManager.Instance?.AnnounceMilestone( playerName,
			$"reached a {milestone}-day login streak milestone!"
		);

		SaveToCookies();
		TamerManager.Instance?.SaveToCloud();

		Log.Info( $"[DailyReward] Claimed milestone {milestone}: {gold}G, {gems} Gems" );
		return true;
	}

	// ═══════════════════════════════════════════════════════════════
	// STREAK SHIELD
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Whether the streak shield is available (not yet used this week).
	/// </summary>
	public bool IsShieldActive() => !StreakShieldUsed;

	/// <summary>
	/// Get the time remaining until shield regenerates.
	/// </summary>
	public TimeSpan GetShieldRegenTime()
	{
		if ( !StreakShieldUsed ) return TimeSpan.Zero;
		var remaining = StreakShieldResetDate - DateTime.UtcNow;
		return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
	}

	// ═══════════════════════════════════════════════════════════════
	// DISPLAY HELPERS
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Format streak as "X Days" or "Y Weeks, Z Days" for display.
	/// </summary>
	public string GetStreakDisplayText()
	{
		if ( DailyStreak < 7 )
			return $"{DailyStreak} Day{(DailyStreak != 1 ? "s" : "")}";

		int weeks = DailyStreak / 7;
		int days = DailyStreak % 7;

		if ( days == 0 )
			return $"{weeks} Week{(weeks != 1 ? "s" : "")}";

		return $"{weeks} Week{(weeks != 1 ? "s" : "")}, {days} Day{(days != 1 ? "s" : "")}";
	}

	/// <summary>
	/// Returns the number of unclaimed rewards (daily + milestones) for badge display.
	/// </summary>
	public int GetUnclaimedCount()
	{
		int count = 0;
		if ( !TodayRewardClaimed ) count++;
		foreach ( var m in MilestoneThresholds )
		{
			if ( IsMilestoneClaimable( m ) ) count++;
		}
		if ( Day7BeastPending ) count++;
		return count;
	}

	// ═══════════════════════════════════════════════════════════════
	// PERSISTENCE (Cookie-based)
	// ═══════════════════════════════════════════════════════════════

	private void LoadFromCookies()
	{
		DailyStreak = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}daily-streak" ), 0 );
		TotalLoginDays = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}total-login-days" ), 0 );
		StreakShieldUsed = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}streak-shield-used" ), 0 ) == 1;
		TodayRewardClaimed = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}today-reward-claimed" ), 0 ) == 1;
		Day7BeastPending = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}day7-beast-pending" ), 0 ) == 1;

		// Load last login date
		var lastLoginTicks = Game.Cookies.Get<long>( GetKey( $"{STAT_PREFIX}last-login-ticks" ), 0 );
		LastLoginDate = lastLoginTicks > 0 ? new DateTime( lastLoginTicks, DateTimeKind.Utc ) : DateTime.MinValue;

		// Load shield reset date
		var shieldResetTicks = Game.Cookies.Get<long>( GetKey( $"{STAT_PREFIX}shield-reset-ticks" ), 0 );
		StreakShieldResetDate = shieldResetTicks > 0
			? new DateTime( shieldResetTicks, DateTimeKind.Utc )
			: GetNextMondayMidnight( DateTime.UtcNow );

		// Load milestones claimed
		var milestonesJson = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}milestones-claimed" ), "[]" );
		try
		{
			MilestonesClaimed = JsonSerializer.Deserialize<List<int>>( milestonesJson ) ?? new();
		}
		catch
		{
			MilestonesClaimed = new();
		}

		Log.Info( $"[DailyReward] Loaded: Streak={DailyStreak}, Total={TotalLoginDays}, Shield={!StreakShieldUsed}" );
	}

	private void SaveToCookies()
	{
		Game.Cookies.Set( GetKey( $"{STAT_PREFIX}daily-streak" ), DailyStreak );
		Game.Cookies.Set( GetKey( $"{STAT_PREFIX}total-login-days" ), TotalLoginDays );
		Game.Cookies.Set( GetKey( $"{STAT_PREFIX}streak-shield-used" ), StreakShieldUsed ? 1 : 0 );
		Game.Cookies.Set( GetKey( $"{STAT_PREFIX}today-reward-claimed" ), TodayRewardClaimed ? 1 : 0 );
		Game.Cookies.Set( GetKey( $"{STAT_PREFIX}day7-beast-pending" ), Day7BeastPending ? 1 : 0 );
		Game.Cookies.Set( GetKey( $"{STAT_PREFIX}last-login-ticks" ), LastLoginDate.Ticks );
		Game.Cookies.Set( GetKey( $"{STAT_PREFIX}shield-reset-ticks" ), StreakShieldResetDate.Ticks );

		var milestonesJson = JsonSerializer.Serialize( MilestonesClaimed ?? new() );
		Game.Cookies.Set( GetKey( $"{STAT_PREFIX}milestones-claimed" ), milestonesJson );
	}

	// ═══════════════════════════════════════════════════════════════
	// UTILITIES
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Returns next Monday at midnight UTC from the given date.
	/// </summary>
	private static DateTime GetNextMondayMidnight( DateTime from )
	{
		int daysUntilMonday = ((int)DayOfWeek.Monday - (int)from.DayOfWeek + 7) % 7;
		if ( daysUntilMonday == 0 ) daysUntilMonday = 7; // If today is Monday, next Monday
		return from.Date.AddDays( daysUntilMonday );
	}
}
