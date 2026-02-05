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
			ActiveTitleId = Game.Cookies.Get<string>( GetKey( $"{STAT_PREFIX}active-title" ), null )
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
		OnGoldChanged?.Invoke( CurrentTamer.Gold );
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
		OnBossTokensChanged?.Invoke( CurrentTamer.BossTokens );
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
}
