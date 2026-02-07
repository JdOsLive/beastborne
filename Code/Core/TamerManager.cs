using Sandbox;
using Sandbox.Services;
using Beastborne.Data;
using System.Text.Json;
using System.Linq;

namespace Beastborne.Core;

/// <summary>
/// Manages the player's tamer data, skills, and resources
/// </summary>
public sealed class TamerManager : Component
{
	public static TamerManager Instance { get; private set; }

	private const string STAT_PREFIX = "tamer-";
	private const float SAVE_INTERVAL = 30f;

	/// <summary>
	/// Get the full key with slot prefix
	/// </summary>
	private static string GetKey( string key ) => $"{SaveSlotManager.GetSlotPrefix()}{key}";

	public Tamer CurrentTamer { get; private set; }
	public SkillTree SkillTree { get; private set; }

	private float lastSaveTime = 0f;

	// Events
	public Action<int> OnGoldChanged;
	public Action<int> OnGemsChanged;
	public Action<int> OnBossTokensChanged;
	public Action<int> OnLevelUp;
	public Action<string> OnSkillUnlocked;
	public Action<string> OnThemeChanged;
	public Action<string> OnTitleChanged;

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
		LoadFromCloud();
	}

	protected override void OnUpdate()
	{
		// Track playtime
		if ( CurrentTamer != null )
		{
			CurrentTamer.TotalPlayTime += TimeSpan.FromSeconds( Time.Delta );
		}

		// Periodic auto-save
		if ( Time.Now - lastSaveTime > SAVE_INTERVAL )
		{
			SaveToCloud();
			lastSaveTime = Time.Now;
		}
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "TamerManager";
		go.Components.Create<TamerManager>();
	}

	private void LoadFromCloud()
	{
		// Load gender from cookie
		var genderStr = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}gender" ), "0" );
		var gender = genderStr == "1" ? TamerGender.Female : TamerGender.Male;

		// Load all values from cookies (not Stats, which are incremental)
		// This prevents XP/gold duplication on editor restart
		CurrentTamer = new Tamer
		{
			Name = Connection.Local?.DisplayName ?? "Tamer",
			Gender = gender,
			Level = Math.Max( 1, Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}level" ), 1 ) ),
			TotalXP = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}xp" ), 0 ),
			Gold = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}gold" ), 100 ),
			Gems = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}gems" ), 0 ),
			ContractInk = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}ink" ), 10 ),
			SkillPoints = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}skill-points" ), 1 ),
			HighestExpeditionCleared = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}expedition-cleared" ), 0 ),
			ArenaRank = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}arena-rank" ), "Unranked" ),
			ArenaPoints = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}arena-points" ), 0 ),
			TotalBattlesWon = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}battles-won" ), 0 ),
			TotalBattlesLost = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}battles-lost" ), 0 ),
			ArenaWins = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}arena-wins" ), 0 ),
			ArenaLosses = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}arena-losses" ), 0 ),
			TotalMonstersCaught = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}caught" ), 0 ),
			TotalMonstersBred = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}bred" ), 0 ),
			TotalMonstersEvolved = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}evolved" ), 0 ),
			BossTokens = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}boss-tokens" ), 0 ),
			ActiveThemeId = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}active-theme" ), "default" ),
			ActiveTitleId = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}active-title" ), null ),
			ActiveLevelTitle = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}active-level-title" ), null ),
			// Online update fields (safe defaults for existing saves)
			TotalGoldEarned = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}gold-earned" ), 0 ),
			TotalItemsBought = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}items-bought" ), 0 ),
			TotalExpeditionsCompleted = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}expeditions-completed" ), 0 ),
			TotalTradesCompleted = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}trades-completed" ), 0 ),
			TotalMiniGamesPlayed = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}minigames-played" ), 0 ),
			ChatMessagesSent = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}chat-sent" ), 0 ),
			BossTokensSpent = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}boss-tokens-spent" ), 0 ),
			TotalDamageDealt = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}total-damage" ), 0 ),
			TotalKnockouts = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}total-knockouts" ), 0 ),
			ArenaWinStreak = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}arena-streak" ), 0 ),
			ArenaSetsCompleted = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}arena-sets" ), 0 ),
			FavoriteMonsterSpeciesId = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}fav-monster" ), null ),
			FavoriteExpeditionId = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}fav-expedition" ), null ),
			HasMasterInk = Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}master-ink" ), 0 ) == 1,
			TotalPlayTime = TimeSpan.FromMinutes( Game.Cookies.Get<int>( GetKey( $"{STAT_PREFIX}playtime-minutes" ), 0 ) )
		};

		// Load skill ranks from cookie (Dictionary<string, int>)
		var skillsJson = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}skill-ranks" ), "{}" );
		try
		{
			CurrentTamer.SkillRanks = JsonSerializer.Deserialize<Dictionary<string, int>>( skillsJson ) ?? new();
		}
		catch
		{
			CurrentTamer.SkillRanks = new();
		}

		// Migration: Try to load old format (List<string>) and convert to new format
		if ( CurrentTamer.SkillRanks.Count == 0 )
		{
			var oldSkillsJson = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}skills" ), "[]" );
			try
			{
				var oldSkills = JsonSerializer.Deserialize<List<string>>( oldSkillsJson ) ?? new();
				foreach ( var skillId in oldSkills )
				{
					CurrentTamer.SkillRanks[skillId] = 1;
				}
			}
			catch { }
		}

		// Migration: Grant retroactive skill points for existing accounts (v0.4.0)
		// Check if this account existed before the skill system was added
		var skillPointsMigrated = Game.Cookies.Get<bool>( GetKey( $"{STAT_PREFIX}skill-points-migrated" ), false );
		if ( !skillPointsMigrated && CurrentTamer.Level > 1 )
		{
			// Calculate total skill points they should have earned from leveling
			// 1 starting SP + points from each level up
			int totalPoints = 1; // Starting skill point
			for ( int level = 2; level <= CurrentTamer.Level; level++ )
			{
				totalPoints += Tamer.GetSkillPointsForLevel( level );
			}

			// Grant the full amount (replacing default of 1)
			CurrentTamer.SkillPoints = totalPoints;
			Log.Info( $"[Migration] Granted {totalPoints} retroactive skill points for level {CurrentTamer.Level} account" );

			// Mark as migrated so we don't do this again
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}skill-points-migrated" ), true );
		}

		// Load cleared bosses from cookie
		var bossesJson = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}cleared-bosses" ), "[]" );
		try
		{
			CurrentTamer.ClearedBosses = JsonSerializer.Deserialize<List<string>>( bossesJson ) ?? new();
		}
		catch
		{
			CurrentTamer.ClearedBosses = new();
		}

		// Load unlocked themes from cookie
		var themesJson = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}unlocked-themes" ), "[\"default\"]" );
		try
		{
			CurrentTamer.UnlockedThemes = JsonSerializer.Deserialize<List<string>>( themesJson ) ?? new() { "default" };
		}
		catch
		{
			CurrentTamer.UnlockedThemes = new() { "default" };
		}

		// Load unlocked titles from cookie
		var titlesJson = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}unlocked-titles" ), "[]" );
		try
		{
			CurrentTamer.UnlockedTitles = JsonSerializer.Deserialize<List<string>>( titlesJson ) ?? new();
		}
		catch
		{
			CurrentTamer.UnlockedTitles = new();
		}

		// Load inventory from cookie (Dictionary<string, int>)
		var inventoryJson = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}inventory" ), "{}" );
		try
		{
			CurrentTamer.Inventory = JsonSerializer.Deserialize<Dictionary<string, int>>( inventoryJson ) ?? new();
		}
		catch
		{
			CurrentTamer.Inventory = new();
		}

		// Load equipped relics from cookie (List<string>)
		var relicsJson = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}equipped-relics" ), "[]" );
		try
		{
			CurrentTamer.EquippedRelics = JsonSerializer.Deserialize<List<string>>( relicsJson ) ?? new();
		}
		catch
		{
			CurrentTamer.EquippedRelics = new();
		}

		// Load active item boosts from cookie
		var boostsJson = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}active-boosts" ), "[]" );
		try
		{
			CurrentTamer.ActiveBoosts = JsonSerializer.Deserialize<List<ActiveItemBoost>>( boostsJson ) ?? new();
			// Remove expired boosts
			CurrentTamer.ActiveBoosts = CurrentTamer.ActiveBoosts.Where( b => !b.IsExpired ).ToList();
		}
		catch
		{
			CurrentTamer.ActiveBoosts = new();
		}

		// Load achievement progress from cookie (Dictionary<string, AchievementProgress>)
		var achievementsJson = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}achievements" ), "{}" );
		try
		{
			CurrentTamer.Achievements = JsonSerializer.Deserialize<Dictionary<string, AchievementProgress>>( achievementsJson ) ?? new();
		}
		catch
		{
			CurrentTamer.Achievements = new();
		}

		// Load collected tamer cards from cookie
		var cardsJson = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}tamer-cards" ), "[]" );
		try
		{
			CurrentTamer.CollectedCards = JsonSerializer.Deserialize<List<CollectedTamerCard>>( cardsJson ) ?? new();
		}
		catch
		{
			CurrentTamer.CollectedCards = new();
		}

		// Load match history from cookie
		var matchHistoryJson = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}match-history" ), "[]" );
		try
		{
			CurrentTamer.MatchHistory = JsonSerializer.Deserialize<List<MatchHistoryEntry>>( matchHistoryJson ) ?? new();
		}
		catch
		{
			CurrentTamer.MatchHistory = new();
		}

		// Load card badges from cookie
		var badgesJson = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}card-badges" ), "[]" );
		try
		{
			CurrentTamer.CardBadges = JsonSerializer.Deserialize<List<string>>( badgesJson ) ?? new();
		}
		catch
		{
			CurrentTamer.CardBadges = new();
		}

		CurrentTamer.LastLogin = DateTime.UtcNow;

		Log.Info( $"Loaded tamer: {CurrentTamer.Name}, Level {CurrentTamer.Level}, XP {CurrentTamer.TotalXP}" );
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

	public void SaveToCloud()
	{
		if ( CurrentTamer == null ) return;

		try
		{
			// Save all values to cookies (not Stats, which are incremental and would duplicate values)
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}level" ), CurrentTamer.Level );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}xp" ), CurrentTamer.TotalXP );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}gold" ), CurrentTamer.Gold );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}gems" ), CurrentTamer.Gems );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}ink" ), CurrentTamer.ContractInk );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}skill-points" ), CurrentTamer.SkillPoints );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}expedition-cleared" ), CurrentTamer.HighestExpeditionCleared );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}arena-rank" ), CurrentTamer.ArenaRank );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}arena-points" ), CurrentTamer.ArenaPoints );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}battles-won" ), CurrentTamer.TotalBattlesWon );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}battles-lost" ), CurrentTamer.TotalBattlesLost );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}arena-wins" ), CurrentTamer.ArenaWins );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}arena-losses" ), CurrentTamer.ArenaLosses );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}caught" ), CurrentTamer.TotalMonstersCaught );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}bred" ), CurrentTamer.TotalMonstersBred );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}evolved" ), CurrentTamer.TotalMonstersEvolved );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}boss-tokens" ), CurrentTamer.BossTokens );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}active-theme" ), CurrentTamer.ActiveThemeId ?? "default" );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}active-title" ), CurrentTamer.ActiveTitleId ?? "" );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}active-level-title" ), CurrentTamer.ActiveLevelTitle ?? "" );

			// Online update fields
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}gold-earned" ), CurrentTamer.TotalGoldEarned );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}items-bought" ), CurrentTamer.TotalItemsBought );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}expeditions-completed" ), CurrentTamer.TotalExpeditionsCompleted );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}trades-completed" ), CurrentTamer.TotalTradesCompleted );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}minigames-played" ), CurrentTamer.TotalMiniGamesPlayed );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}chat-sent" ), CurrentTamer.ChatMessagesSent );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}boss-tokens-spent" ), CurrentTamer.BossTokensSpent );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}total-damage" ), CurrentTamer.TotalDamageDealt );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}total-knockouts" ), CurrentTamer.TotalKnockouts );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}arena-streak" ), CurrentTamer.ArenaWinStreak );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}arena-sets" ), CurrentTamer.ArenaSetsCompleted );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}fav-monster" ), CurrentTamer.FavoriteMonsterSpeciesId ?? "" );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}fav-expedition" ), CurrentTamer.FavoriteExpeditionId ?? "" );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}master-ink" ), CurrentTamer.HasMasterInk ? 1 : 0 );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}playtime-minutes" ), (int)CurrentTamer.TotalPlayTime.TotalMinutes );

			// Save achievement progress
			var achievementsJson = JsonSerializer.Serialize( CurrentTamer.Achievements ?? new() );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}achievements" ), achievementsJson );

			// Save collected tamer cards
			var cardsJson = JsonSerializer.Serialize( CurrentTamer.CollectedCards ?? new() );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}tamer-cards" ), cardsJson );

			// Save match history
			var historyJson = JsonSerializer.Serialize( CurrentTamer.MatchHistory ?? new() );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}match-history" ), historyJson );

			// Save card badges
			var badgesJson = JsonSerializer.Serialize( CurrentTamer.CardBadges ?? new() );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}card-badges" ), badgesJson );

			// Submit playtime to leaderboard
			Stats.SetValue( "total-playtime", (int)CurrentTamer.TotalPlayTime.TotalMinutes );

			// Save skill ranks to cookie (Dictionary<string, int>)
			var skillsJson = JsonSerializer.Serialize( CurrentTamer.SkillRanks );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}skill-ranks" ), skillsJson );

			// Save cleared bosses to cookie
			var bossesJson = JsonSerializer.Serialize( CurrentTamer.ClearedBosses );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}cleared-bosses" ), bossesJson );

			// Save unlocked themes to cookie
			var themesJson = JsonSerializer.Serialize( CurrentTamer.UnlockedThemes );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}unlocked-themes" ), themesJson );

			// Save unlocked titles to cookie
			var titlesJson = JsonSerializer.Serialize( CurrentTamer.UnlockedTitles );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}unlocked-titles" ), titlesJson );

			// Save inventory to cookie
			var inventoryJson = JsonSerializer.Serialize( CurrentTamer.Inventory );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}inventory" ), inventoryJson );

			// Save equipped relics to cookie
			var relicsJson = JsonSerializer.Serialize( CurrentTamer.EquippedRelics );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}equipped-relics" ), relicsJson );

			// Save active item boosts to cookie (filter out expired ones)
			var activeBoosts = CurrentTamer.ActiveBoosts?.Where( b => !b.IsExpired ).ToList() ?? new();
			var boostsJson = JsonSerializer.Serialize( activeBoosts );
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}active-boosts" ), boostsJson );

			// Save gender to cookie
			Game.Cookies.Set( GetKey( $"{STAT_PREFIX}gender" ), CurrentTamer.Gender == TamerGender.Female ? "1" : "0" );

			// Update slot info
			SaveSlotManager.Instance?.UpdateActiveSlotInfo();

			Log.Info( "Tamer data saved" );
		}
		catch ( Exception e )
		{
			Log.Warning( $"Failed to save tamer data: {e.Message}" );
		}
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

		CurrentTamer.Gold += amount;
		CurrentTamer.TotalGoldEarned += amount;
		OnGoldChanged?.Invoke( CurrentTamer.Gold );
		AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.TotalGoldEarned, CurrentTamer.TotalGoldEarned );
		Stats.SetValue( "total-gold", CurrentTamer.TotalGoldEarned );
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
			SaveToCloud();
		}
		else
		{
			Log.Info( $"[MarkBossCleared] '{expeditionId}' already in ClearedBosses, skipping" );
		}
	}

	// Cosmetics
	public bool HasTheme( string themeId )
	{
		return CurrentTamer.UnlockedThemes.Contains( themeId );
	}

	public bool UnlockTheme( string themeId )
	{
		if ( CurrentTamer.UnlockedThemes.Contains( themeId ) ) return false;
		CurrentTamer.UnlockedThemes.Add( themeId );
		SaveToCloud();
		return true;
	}

	public void SetActiveTheme( string themeId )
	{
		if ( !CurrentTamer.UnlockedThemes.Contains( themeId ) ) return;
		CurrentTamer.ActiveThemeId = themeId;
		OnThemeChanged?.Invoke( themeId );
		SaveToCloud();
	}

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

	// XP and leveling
	public void AddXP( int amount )
	{
		// Apply tamer XP boost from shop
		float tamerXPBoost = ShopManager.Instance?.GetBoostMultiplier( ShopItemType.TamerXPBoost ) ?? 1.0f;

		// Apply relic tamer XP bonus
		float relicTamerXP = ItemManager.Instance?.GetRelicBonus( ItemEffectType.PassiveTamerXP ) ?? 0;

		int boostedAmount = (int)(amount * tamerXPBoost * (1 + relicTamerXP / 100f));

		if ( CurrentTamer.AddXP( boostedAmount ) )
		{
			OnLevelUp?.Invoke( CurrentTamer.Level );
			AchievementManager.Instance?.CheckProgress( Data.AchievementRequirement.TamerLevel, CurrentTamer.Level );
			Stats.SetValue( "tamer-level", CurrentTamer.Level );
		}
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
			SkillPoints = 1,
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
			UnlockedThemes = new() { "default" },
			UnlockedTitles = new(),
			Inventory = new(),
			EquippedRelics = new(),
			ActiveBoosts = new(),
			ActiveThemeId = "default",
			ActiveTitleId = null,
			ActiveLevelTitle = null,
			LastLogin = DateTime.UtcNow
		};

		SaveToCloud();
		Log.Info( "Tamer data reset to defaults" );
	}

	/// <summary>
	/// Reload data from the current save slot
	/// </summary>
	public void ReloadFromSlot()
	{
		LoadFromCloud();
		Log.Info( $"TamerManager reloaded from slot {SaveSlotManager.Instance?.ActiveSlot}" );
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
