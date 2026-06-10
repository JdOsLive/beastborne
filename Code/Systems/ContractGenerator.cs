using Beastborne.Core;
using Beastborne.Data;

namespace Beastborne.Systems;

/// <summary>
/// Generates contracts for caught monsters
/// </summary>
// ── Persuasion Approach Types ──

public enum ApproachType
{
	// Always available (ink only)
	Kindness,       // Timid, Loyal
	Strength,       // Bold, Wild
	Charm,          // Bold, Greedy
	Respect,        // All (neutral)
	Intimidate,     // Timid, Wild — critical fail risk

	// Resource-based
	Wealth,         // Greedy — costs gold
	Offering,       // Timid, Greedy — costs consumable item
	Tribute,        // Bold, Loyal — costs boss tokens

	// Special mechanics
	Sacrifice,      // Wild, Bold — your beast takes damage
	Bond,           // Loyal, Wild — beast XP drain
	Patience,       // Loyal, Timid — skip next 2 turns
	Mystery         // All — random 10-90%
}

public static class ContractGenerator
{
	public static Contract GenerateContract( MonsterSpecies species )
	{
		var random = new Random();

		// Higher rarity = more demanding contracts
		int demandIntensity = species.BaseRarity switch
		{
			Rarity.Common => 1,
			Rarity.Uncommon => random.Next( 1, 3 ),
			Rarity.Rare => random.Next( 2, 4 ),
			Rarity.Epic => random.Next( 2, 4 ),
			Rarity.Legendary => 3,
			Rarity.Mythic => 3,
			_ => 1
		};

		// Generate primary demand based on element/personality
		var primaryDemand = GeneratePrimaryDemand( species, demandIntensity, random );

		// Maybe add secondary demands for rarer monsters
		var secondaryDemands = new List<ContractDemand>();
		if ( species.BaseRarity >= Rarity.Rare && random.Next( 100 ) < 50 )
		{
			var secondaryType = GetRandomDemandType( primaryDemand.Type, random );
			secondaryDemands.Add( new ContractDemand
			{
				Type = secondaryType,
				Intensity = Math.Max( 1, demandIntensity - 1 )
			} );
		}

		// Calculate starting satisfaction (affected by tamer skills)
		int baseSatisfaction = 75;
		float satisfactionBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.StartingSatisfactionBonus ) ?? 0;
		int startingSatisfaction = Math.Min( 100, baseSatisfaction + (int)satisfactionBonus );

		return new Contract
		{
			PrimaryDemand = primaryDemand,
			SecondaryDemands = secondaryDemands,
			Satisfaction = startingSatisfaction
		};
	}

	private static ContractDemand GeneratePrimaryDemand( MonsterSpecies species, int intensity, Random random )
	{
		// Element influences likely demand type
		// Each element has personality-driven affinities for certain demands
		ContractDemandType demandType = species.Element switch
		{
			// Fire - aggressive, wants battles and glory
			ElementType.Fire => random.Next( 100 ) switch
			{
				< 40 => ContractDemandType.Bloodthirsty,
				< 70 => ContractDemandType.Competitive,
				_ => ContractDemandType.Ambitious
			},
			// Water - balanced, social, appreciates comfort
			ElementType.Water => random.Next( 100 ) switch
			{
				< 40 => ContractDemandType.Social,
				< 70 => ContractDemandType.Lazy,
				_ => ContractDemandType.Greedy
			},
			// Earth - patient, values growth and rest
			ElementType.Earth => random.Next( 100 ) switch
			{
				< 40 => ContractDemandType.Lazy,
				< 70 => ContractDemandType.Ambitious,
				_ => ContractDemandType.Greedy
			},
			// Wind - free-spirited, social, competitive
			ElementType.Wind => random.Next( 100 ) switch
			{
				< 40 => ContractDemandType.Social,
				< 70 => ContractDemandType.Competitive,
				_ => ContractDemandType.Ambitious
			},
			// Electric - energetic, competitive, impatient
			ElementType.Electric => random.Next( 100 ) switch
			{
				< 40 => ContractDemandType.Competitive,
				< 70 => ContractDemandType.Bloodthirsty,
				_ => ContractDemandType.Ambitious
			},
			// Ice - solitary, patient, ambitious
			ElementType.Ice => random.Next( 100 ) switch
			{
				< 40 => ContractDemandType.Ambitious,
				< 70 => ContractDemandType.Lazy,
				_ => ContractDemandType.Greedy
			},
			// Nature - social, peaceful, values growth
			ElementType.Nature => random.Next( 100 ) switch
			{
				< 40 => ContractDemandType.Social,
				< 70 => ContractDemandType.Ambitious,
				_ => ContractDemandType.Lazy
			},
			// Metal - greedy, industrious, competitive
			ElementType.Metal => random.Next( 100 ) switch
			{
				< 40 => ContractDemandType.Greedy,
				< 70 => ContractDemandType.Competitive,
				_ => ContractDemandType.Ambitious
			},
			// Shadow - bloodthirsty, greedy, solitary
			ElementType.Shadow => random.Next( 100 ) switch
			{
				< 40 => ContractDemandType.Bloodthirsty,
				< 70 => ContractDemandType.Greedy,
				_ => ContractDemandType.Competitive
			},
			// Spirit - social, ambitious, spiritual
			ElementType.Spirit => random.Next( 100 ) switch
			{
				< 40 => ContractDemandType.Social,
				< 70 => ContractDemandType.Ambitious,
				_ => ContractDemandType.Lazy
			},
			// Neutral/default - balanced distribution
			_ => (ContractDemandType)random.Next( Enum.GetValues<ContractDemandType>().Length )
		};

		// Calculate required amount based on intensity
		int requiredAmount = demandType switch
		{
			ContractDemandType.Bloodthirsty => intensity * 3,    // 3/6/9 battles
			ContractDemandType.Greedy => intensity * 100,        // 100/200/300 gold
			ContractDemandType.Ambitious => intensity,           // 1/2/3 level ups
			ContractDemandType.Social => intensity * 2,          // 2/4/6 expeditions with companions
			ContractDemandType.Lazy => intensity * 2,            // 2/4/6 rest periods (expeditions not done)
			ContractDemandType.Competitive => intensity * 2,     // 2/4/6 arena wins
			_ => intensity * 3
		};

		return new ContractDemand
		{
			Type = demandType,
			Intensity = intensity,
			RequiredAmount = requiredAmount,
			CurrentProgress = 0
		};
	}

	private static ContractDemandType GetRandomDemandType( ContractDemandType exclude, Random random )
	{
		var types = Enum.GetValues<ContractDemandType>()
			.Where( t => t != exclude )
			.ToArray();

		return types[random.Next( types.Length )];
	}

	/// <summary>
	/// Apply skill bonuses to reduce contract demands
	/// </summary>
	public static void ApplyTamerBonuses( Contract contract )
	{
		if ( contract == null ) return;

		float demandReduction = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ContractDemandReduction ) ?? 0;

		// Reduce intensity based on tamer skills
		if ( demandReduction > 0 )
		{
			contract.PrimaryDemand.Intensity = Math.Max( 1, contract.PrimaryDemand.Intensity - (int)demandReduction );

			foreach ( var demand in contract.SecondaryDemands )
			{
				demand.Intensity = Math.Max( 1, demand.Intensity - (int)demandReduction );
			}
		}
	}

	// ── Approach Definitions ──

	private static readonly Dictionary<ApproachType, ApproachDef> Approaches = new()
	{
		[ApproachType.Kindness] = new ApproachDef
		{
			Name = "Kindness", Description = "Show compassion and patience",
			BaseSuccess = 0.55f, ExtraInkCost = 1,
			MatchPersonalities = new[] { BeastPersonality.Timid, BeastPersonality.Loyal },
			DemandIntensityMod = -1, Color = "#4090d0"
		},
		[ApproachType.Strength] = new ApproachDef
		{
			Name = "Strength", Description = "Demonstrate your power",
			BaseSuccess = 0.55f, ExtraInkCost = 1,
			MatchPersonalities = new[] { BeastPersonality.Bold, BeastPersonality.Wild },
			DemandIntensityMod = 1, Color = "#d05040"
		},
		[ApproachType.Charm] = new ApproachDef
		{
			Name = "Charm", Description = "Flattery and smooth talk",
			BaseSuccess = 0.50f, ExtraInkCost = 1,
			MatchPersonalities = new[] { BeastPersonality.Bold, BeastPersonality.Greedy },
			DemandIntensityMod = -1, Color = "#d070a0"
		},
		[ApproachType.Respect] = new ApproachDef
		{
			Name = "Respect", Description = "Acknowledge them as equals",
			BaseSuccess = 0.40f, ExtraInkCost = 0, IsNeutral = true,
			MatchPersonalities = Array.Empty<BeastPersonality>(),
			DemandIntensityMod = 0, Color = "#a0a0b0"
		},
		[ApproachType.Intimidate] = new ApproachDef
		{
			Name = "Intimidate", Description = "Threaten and pressure",
			BaseSuccess = 0.65f, ExtraInkCost = 2, HasCriticalFail = true,
			MatchPersonalities = new[] { BeastPersonality.Timid, BeastPersonality.Wild },
			DemandIntensityMod = 1, Color = "#a03030"
		},
		[ApproachType.Wealth] = new ApproachDef
		{
			Name = "Wealth", Description = "Bribe with gold",
			BaseSuccess = 0.55f, ExtraInkCost = 0, CostsGold = true,
			MatchPersonalities = new[] { BeastPersonality.Greedy },
			DemandIntensityMod = 0, Color = "#c8a840"
		},
		[ApproachType.Offering] = new ApproachDef
		{
			Name = "Offering", Description = "Gift a treat or berry",
			BaseSuccess = 0.60f, ExtraInkCost = 0, CostsItem = true,
			MatchPersonalities = new[] { BeastPersonality.Timid, BeastPersonality.Greedy },
			DemandIntensityMod = -1, Color = "#50b060"
		},
		[ApproachType.Tribute] = new ApproachDef
		{
			Name = "Tribute", Description = "Offer rare tokens of power",
			BaseSuccess = 0.70f, ExtraInkCost = 0, CostsBossTokens = true,
			MatchPersonalities = new[] { BeastPersonality.Bold, BeastPersonality.Loyal },
			DemandIntensityMod = 0, Color = "#9060c0"
		},
		[ApproachType.Sacrifice] = new ApproachDef
		{
			Name = "Sacrifice", Description = "Your beast offers its own strength",
			BaseSuccess = 0.70f, ExtraInkCost = 0, CostsPlayerHP = true,
			MatchPersonalities = new[] { BeastPersonality.Wild, BeastPersonality.Bold },
			DemandIntensityMod = -2, Color = "#c02020"
		},
		[ApproachType.Bond] = new ApproachDef
		{
			Name = "Bond", Description = "Share your beast's energy",
			BaseSuccess = 0.65f, ExtraInkCost = 0, CostsBeastXP = true,
			MatchPersonalities = new[] { BeastPersonality.Loyal, BeastPersonality.Wild },
			DemandIntensityMod = 0, Color = "#40b0a0"
		},
		[ApproachType.Patience] = new ApproachDef
		{
			Name = "Patience", Description = "Demonstrate commitment",
			BaseSuccess = 0.55f, ExtraInkCost = 0, CostsSkipTurns = true,
			MatchPersonalities = new[] { BeastPersonality.Loyal, BeastPersonality.Timid },
			DemandIntensityMod = 0, Color = "#6090b0"
		},
		[ApproachType.Mystery] = new ApproachDef
		{
			Name = "Mystery", Description = "Take a wild gamble",
			BaseSuccess = 0f, ExtraInkCost = 0, IsRandom = true, IsNeutral = true,
			MatchPersonalities = Array.Empty<BeastPersonality>(),
			DemandIntensityMod = 0, Color = "#e0e0e0"
		},
	};

	/// <summary>
	/// Get the approach definition for a given type
	/// </summary>
	public static ApproachDef GetApproach( ApproachType type ) => Approaches[type];

	/// <summary>
	/// Check if an approach matches a personality
	/// </summary>
	public static bool IsPersonalityMatch( ApproachType approach, BeastPersonality personality )
	{
		var def = Approaches[approach];
		return def.MatchPersonalities.Contains( personality );
	}

	/// <summary>
	/// Calculate final success chance for an approach against a target
	/// </summary>
	public static float CalculateSuccessChance( ApproachType approach, MonsterSpecies species, Monster target, bool isBossContract = false )
	{
		var def = Approaches[approach];

		// Mystery is fully random
		if ( def.IsRandom )
			return (float)(new Random().NextDouble() * 0.8f + 0.1f); // 10-90%

		float baseChance = def.BaseSuccess;

		// Personality matching
		if ( !def.IsNeutral )
		{
			if ( IsPersonalityMatch( approach, species.Personality ) )
				baseChance += 0.25f;
			else
				baseChance -= 0.15f;
		}

		// Intimidate penalty from previous failed intimidation
		baseChance -= target.IntimidatePenalty;

		// HP modifier - lower HP makes negotiation easier
		float hpPercent = target.MaxHP > 0 ? (float)target.CurrentHP / target.MaxHP : 1f;
		float hpModifier = 1.0f - (hpPercent * 0.5f);

		// Rarity modifier
		bool isRareBoss = isBossContract && species.BaseRarity >= Rarity.Legendary;
		float rarityModifier = isRareBoss ? 0.9f : species.BaseRarity switch
		{
			Rarity.Common => 1.0f,
			Rarity.Uncommon => 0.9f,
			Rarity.Rare => 0.75f,
			Rarity.Epic => 0.6f,
			Rarity.Legendary => 0.4f,
			Rarity.Mythic => 0.3f,
			_ => 1.0f
		};

		// Skill/perk bonuses
		float skillBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.CatchRateBonus ) ?? 0;
		bool hasEliteInk = TamerManager.Instance?.CurrentTamer?.EliteInkExpiresAt > DateTime.Now;
		float eliteInkBonus = hasEliteInk ? 15f : 0f;
		bool previouslyCaught = BeastiaryManager.Instance?.IsDiscovered( species.Id ) ?? false;
		float previousCatchBonus = previouslyCaught ? 15f : 0f;
		float guildCatchBonus = GuildManager.Instance?.GetCatchRateBonus() ?? 0f;

		float finalModifier = hpModifier * rarityModifier * (1 + (skillBonus + eliteInkBonus + previousCatchBonus + guildCatchBonus) / 100f);

		if ( isRareBoss )
			finalModifier = Math.Max( finalModifier, 1.2f );

		return Math.Min( 0.95f, Math.Max( 0.05f, baseChance * finalModifier ) );
	}

	/// <summary>
	/// Calculate gold cost for the Wealth approach
	/// </summary>
	public static int CalculateGoldCost( MonsterSpecies species, Monster target )
	{
		return (int)(50 * (1 + (int)species.BaseRarity) * (1 + target.Level / 10f));
	}

	/// <summary>
	/// Calculate boss token cost for the Tribute approach
	/// </summary>
	public static int CalculateTributeCost( MonsterSpecies species )
	{
		return species.BaseRarity switch
		{
			Rarity.Common => 1,
			Rarity.Uncommon => 2,
			Rarity.Rare => 3,
			Rarity.Epic => 5,
			Rarity.Legendary => 8,
			Rarity.Mythic => 12,
			_ => 2
		};
	}

	/// <summary>
	/// Calculate HP cost for the Sacrifice approach (% of player beast max HP)
	/// </summary>
	public static int CalculateSacrificeHP( Monster playerBeast )
	{
		return Math.Max( 1, (int)(playerBeast.MaxHP * 0.3f) ); // 30% of max HP
	}

	/// <summary>
	/// Calculate XP drain for the Bond approach
	/// </summary>
	public static int CalculateBondXPCost( Monster playerBeast )
	{
		// Approximate XP cost based on level (scales with progression)
		int xpCost = (int)(50 * Math.Pow( playerBeast.Level, 1.2 ));
		return Math.Max( 10, xpCost );
	}

	/// <summary>
	/// Generate 4 curated negotiation options from the pool of 12 approaches
	/// </summary>
	public static List<NegotiationOption> GenerateNegotiationOptions( MonsterSpecies species, Monster target, bool isBossContract = false )
	{
		var random = new Random();
		var curated = CurateApproaches( species, target, random );
		var options = new List<NegotiationOption>();

		// Escalating retry cost
		int retryPenalty = target.ContractAttemptsThisBattle;

		foreach ( var approachType in curated )
		{
			var def = Approaches[approachType];
			float successChance = CalculateSuccessChance( approachType, species, target, isBossContract );

			// Calculate costs
			int inkCost = def.ExtraInkCost + retryPenalty; // approach cost + retry penalty (entry fee already spent)
			int goldCost = def.CostsGold ? CalculateGoldCost( species, target ) : 0;
			int tokenCost = def.CostsBossTokens ? CalculateTributeCost( species ) : 0;

			// Generate contract with approach-specific demand intensity
			var contract = GenerateContract( species );
			contract.PrimaryDemand.Intensity = Math.Clamp(
				contract.PrimaryDemand.Intensity + def.DemandIntensityMod, 1, 3 );
			if ( def.DemandIntensityMod <= -2 )
				contract.SecondaryDemands.Clear();

			options.Add( new NegotiationOption
			{
				Approach = approachType,
				Name = def.Name,
				Description = def.Description,
				Contract = contract,
				SuccessChance = successChance,
				GoldCost = goldCost,
				InkCost = inkCost,
				BossTokenCost = tokenCost,
				IsBossContract = isBossContract,
				Color = def.Color,
				IsPersonalityMatch = !def.IsNeutral && IsPersonalityMatch( approachType, species.Personality ),
				HasCriticalFail = def.HasCriticalFail,
				CostsPlayerHP = def.CostsPlayerHP,
				CostsBeastXP = def.CostsBeastXP,
				CostsSkipTurns = def.CostsSkipTurns,
				CostsItem = def.CostsItem,
			} );
		}

		return options;
	}

	/// <summary>
	/// Curate 4 approaches from the pool of 12
	/// Rules: always RESPECT + 2 personality-matched + 1 random/resource
	/// </summary>
	private static List<ApproachType> CurateApproaches( MonsterSpecies species, Monster target, Random random )
	{
		var result = new List<ApproachType>();
		var personality = species.Personality;

		// 1. Always include RESPECT
		result.Add( ApproachType.Respect );

		// 2. Find personality-matched approaches
		var matched = Approaches
			.Where( kvp => kvp.Value.MatchPersonalities.Contains( personality ) )
			.Select( kvp => kvp.Key )
			.OrderBy( _ => random.Next() )
			.Take( 2 )
			.ToList();
		result.AddRange( matched );

		// 3. Fill remaining slot(s) with random/resource approach
		var remaining = Approaches.Keys
			.Where( a => !result.Contains( a ) )
			.Where( a => CanAffordApproach( a, target ) )
			.OrderBy( _ => random.Next() )
			.ToList();

		// Prefer MYSTERY occasionally (~30%)
		if ( random.NextDouble() < 0.3f && !result.Contains( ApproachType.Mystery ) )
			result.Add( ApproachType.Mystery );

		// Fill to 4 total
		foreach ( var approach in remaining )
		{
			if ( result.Count >= 4 ) break;
			if ( !result.Contains( approach ) )
				result.Add( approach );
		}

		return result.Take( 4 ).ToList();
	}

	/// <summary>
	/// Check if the player can potentially use an approach (has the required resource type)
	/// </summary>
	private static bool CanAffordApproach( ApproachType approach, Monster target )
	{
		var def = Approaches[approach];
		if ( def.CostsGold )
			return (TamerManager.Instance?.CurrentTamer?.Gold ?? 0) > 0;
		if ( def.CostsBossTokens )
			return (TamerManager.Instance?.CurrentTamer?.BossTokens ?? 0) > 0;
		if ( def.CostsPlayerHP )
			return true; // Always technically available, but will fail if beast faints
		if ( def.CostsBeastXP )
			return true;
		if ( def.CostsItem )
			return true; // Check actual item availability at negotiation time
		return true;
	}

	/// <summary>
	/// Attempt to negotiate a contract with a wild monster
	/// Returns: (success, isCriticalFail)
	/// </summary>
	public static (bool success, bool criticalFail) AttemptNegotiation( NegotiationOption option )
	{
		// Spend gold if needed
		if ( option.GoldCost > 0 )
		{
			var tamer = TamerManager.Instance?.CurrentTamer;
			if ( tamer == null || tamer.Gold < option.GoldCost )
				return (false, false);
			TamerManager.Instance.SpendGold( option.GoldCost );
		}

		// Spend boss tokens if needed
		if ( option.BossTokenCost > 0 )
		{
			var tamer = TamerManager.Instance?.CurrentTamer;
			if ( tamer == null || tamer.BossTokens < option.BossTokenCost )
				return (false, false);
			tamer.BossTokens -= option.BossTokenCost;
		}

		// Roll for success
		var random = new Random();
		double roll = random.NextDouble();
		bool success = roll < option.SuccessChance;

		// Check for INTIMIDATE critical fail
		bool criticalFail = false;
		if ( !success && option.HasCriticalFail && roll >= (1.0 - 0.25) )
		{
			// Bottom 25% of failure range = critical fail
			criticalFail = true;
		}

		return (success, criticalFail);
	}

	/// <summary>
	/// Pick the best negotiation option from a generated set using ONLY
	/// player-visible information — the same SuccessChance the contract UI
	/// displays (AttemptNegotiation rolls against that exact value, so this
	/// never peeks at hidden RNG). Used by smart Auto-Contract.
	///
	/// Filters to options the player can actually afford right now. When
	/// simpleCostsOnly is true, approaches with side costs the automated
	/// path doesn't implement (HP sacrifice, beast XP drain, skip-turns,
	/// item offerings) are excluded.
	///
	/// "Best" = highest success chance; deterministic tie-break:
	/// safer (no critical-fail risk) → cheaper ink → cheaper gold →
	/// cheaper boss tokens → earliest in the list.
	/// </summary>
	public static NegotiationOption PickBestOption( List<NegotiationOption> options, int availableInk, int availableGold, int availableBossTokens, bool simpleCostsOnly = false )
	{
		if ( options == null || options.Count == 0 ) return null;

		NegotiationOption best = null;
		foreach ( var option in options )
		{
			if ( option == null ) continue;
			if ( option.InkCost > availableInk ) continue;
			if ( option.GoldCost > availableGold ) continue;
			if ( option.BossTokenCost > availableBossTokens ) continue;
			if ( simpleCostsOnly && (option.CostsPlayerHP || option.CostsBeastXP || option.CostsSkipTurns || option.CostsItem) ) continue;

			// Strict comparison — on a full tie the earlier option wins,
			// keeping the pick deterministic for a given option list.
			if ( best == null || IsBetterOption( option, best ) )
				best = option;
		}

		return best;
	}

	private static bool IsBetterOption( NegotiationOption a, NegotiationOption b )
	{
		if ( a.SuccessChance != b.SuccessChance ) return a.SuccessChance > b.SuccessChance;
		if ( a.HasCriticalFail != b.HasCriticalFail ) return !a.HasCriticalFail;
		if ( a.InkCost != b.InkCost ) return a.InkCost < b.InkCost;
		if ( a.GoldCost != b.GoldCost ) return a.GoldCost < b.GoldCost;
		if ( a.BossTokenCost != b.BossTokenCost ) return a.BossTokenCost < b.BossTokenCost;
		return false;
	}
}

/// <summary>
/// Definition of a persuasion approach
/// </summary>
public class ApproachDef
{
	public string Name { get; set; }
	public string Description { get; set; }
	public float BaseSuccess { get; set; }
	public int ExtraInkCost { get; set; }
	public BeastPersonality[] MatchPersonalities { get; set; } = Array.Empty<BeastPersonality>();
	public int DemandIntensityMod { get; set; }
	public string Color { get; set; }
	public bool IsNeutral { get; set; }
	public bool IsRandom { get; set; }
	public bool HasCriticalFail { get; set; }
	public bool CostsGold { get; set; }
	public bool CostsBossTokens { get; set; }
	public bool CostsPlayerHP { get; set; }
	public bool CostsBeastXP { get; set; }
	public bool CostsSkipTurns { get; set; }
	public bool CostsItem { get; set; }
}

/// <summary>
/// Represents a contract negotiation option presented to the player
/// </summary>
public class NegotiationOption
{
	public ApproachType Approach { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public Contract Contract { get; set; }
	public float SuccessChance { get; set; }
	public int GoldCost { get; set; }
	public int InkCost { get; set; } = 1;
	public int BossTokenCost { get; set; }
	public bool IsBossContract { get; set; } = false;
	public string Color { get; set; } = "#a0a0b0";
	public bool IsPersonalityMatch { get; set; }
	public bool HasCriticalFail { get; set; }
	public bool CostsPlayerHP { get; set; }
	public bool CostsBeastXP { get; set; }
	public bool CostsSkipTurns { get; set; }
	public bool CostsItem { get; set; }

	public string GetSuccessText()
	{
		int percent = (int)(SuccessChance * 100);
		return $"{percent}%";
	}

	public string GetSuccessColor()
	{
		return SuccessChance switch
		{
			>= 0.7f => "#4ade80",  // Green
			>= 0.5f => "#fbbf24",  // Yellow
			>= 0.3f => "#fb923c",  // Orange
			_ => "#f87171"         // Red
		};
	}
}
