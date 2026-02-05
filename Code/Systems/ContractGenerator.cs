using Beastborne.Core;
using Beastborne.Data;

namespace Beastborne.Systems;

/// <summary>
/// Generates contracts for caught monsters
/// </summary>
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

	/// <summary>
	/// Generate negotiable contract options for catching a wild monster
	/// Returns 3-4 contract options with varying difficulty/demands
	/// </summary>
	public static List<NegotiationOption> GenerateNegotiationOptions( MonsterSpecies species, Monster target, bool isBossContract = false )
	{
		var options = new List<NegotiationOption>();
		var random = new Random();

		// For Legendary/Mythic boss contracts, provide boosted success rates
		bool isRareBoss = isBossContract && species.BaseRarity >= Rarity.Legendary;

		// Base catch difficulty from species
		float baseDifficulty = 1.0f - species.BaseCatchRate;

		// HP modifier - lower HP makes negotiation easier
		float hpPercent = (float)target.CurrentHP / target.MaxHP;
		float hpModifier = 1.0f - (hpPercent * 0.5f); // 50% at full HP, 100% at 0 HP

		// Rarity modifier - boosted for rare bosses
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

		// Tamer skill bonus
		float skillBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.CatchRateBonus ) ?? 0;

		// Elite Ink bonus (+15% catch rate) - timed buff
		bool hasEliteInk = TamerManager.Instance?.CurrentTamer?.EliteInkExpiresAt > DateTime.Now;
		float eliteInkBonus = hasEliteInk ? 15f : 0f;

		float finalModifier = hpModifier * rarityModifier * (1 + (skillBonus + eliteInkBonus) / 100f);

		// Boss contract bonus - makes rare bosses much more catchable
		if ( isRareBoss )
		{
			finalModifier = Math.Max( finalModifier, 1.2f ); // Ensure good rates for rare bosses
		}

		// Generate 3-4 options with varying demands
		// Option 1: Generous offer (easy acceptance, demanding contract) - 3 ink
		var generousContract = GenerateContract( species );
		generousContract.PrimaryDemand.Intensity = Math.Min( 3, generousContract.PrimaryDemand.Intensity + 1 );
		options.Add( new NegotiationOption
		{
			Name = isRareBoss ? "Legendary Pact" : "Generous Offer",
			Description = isRareBoss ? "Offer to be their champion" : "Promise them whatever they want",
			Contract = generousContract,
			SuccessChance = isRareBoss ? 0.85f : Math.Min( 0.95f, 0.7f * finalModifier ),
			GoldCost = 0,
			InkCost = isRareBoss ? 5 : 3,
			IsBossContract = isBossContract
		} );

		// Option 2: Balanced offer (medium acceptance, normal contract) - 2 ink
		var balancedContract = GenerateContract( species );
		options.Add( new NegotiationOption
		{
			Name = isRareBoss ? "Mutual Respect" : "Fair Terms",
			Description = isRareBoss ? "Acknowledge their power as equals" : "A reasonable agreement for both parties",
			Contract = balancedContract,
			SuccessChance = isRareBoss ? 0.70f : Math.Min( 0.85f, 0.5f * finalModifier ),
			GoldCost = 0,
			InkCost = isRareBoss ? 3 : 2,
			IsBossContract = isBossContract
		} );

		// Option 3: Strict terms (harder acceptance, lighter contract) - 1 ink
		var strictContract = GenerateContract( species );
		strictContract.PrimaryDemand.Intensity = Math.Max( 1, strictContract.PrimaryDemand.Intensity - 1 );
		strictContract.SecondaryDemands.Clear(); // No secondary demands
		options.Add( new NegotiationOption
		{
			Name = isRareBoss ? "Bold Challenge" : "Strict Terms",
			Description = isRareBoss ? "Prove you're worthy of their service" : "Minimal obligations on your part",
			Contract = strictContract,
			SuccessChance = isRareBoss ? 0.50f : Math.Min( 0.65f, 0.3f * finalModifier ),
			GoldCost = 0,
			InkCost = isRareBoss ? 2 : 1,
			IsBossContract = isBossContract
		} );

		// Option 4: Bribery (gold for better chance) - 2 ink + gold
		int bribeCost = (int)(50 * (1 + (int)species.BaseRarity) * (1 + target.Level / 10f));
		if ( isRareBoss ) bribeCost *= 2; // Rare bosses cost more to bribe
		var bribeContract = GenerateContract( species );
		options.Add( new NegotiationOption
		{
			Name = isRareBoss ? "Royal Tribute" : "Golden Handshake",
			Description = isRareBoss ? $"Offer {bribeCost} gold as tribute" : $"Sweeten the deal with {bribeCost} gold",
			Contract = bribeContract,
			SuccessChance = isRareBoss ? 0.90f : Math.Min( 0.90f, 0.6f * finalModifier + 0.15f ),
			GoldCost = bribeCost,
			InkCost = isRareBoss ? 3 : 2,
			IsBossContract = isBossContract
		} );

		return options;
	}

	/// <summary>
	/// Attempt to negotiate a contract with a wild monster
	/// </summary>
	public static bool AttemptNegotiation( NegotiationOption option )
	{
		// Check if player can afford gold cost
		if ( option.GoldCost > 0 )
		{
			var tamer = TamerManager.Instance?.CurrentTamer;
			if ( tamer == null || tamer.Gold < option.GoldCost )
				return false;

			// Spend the gold
			TamerManager.Instance.SpendGold( option.GoldCost );
		}

		// Roll for success (Elite Ink is time-based, no consumption needed)
		var random = new Random();
		return random.NextDouble() < option.SuccessChance;
	}
}

/// <summary>
/// Represents a contract negotiation option presented to the player
/// </summary>
public class NegotiationOption
{
	public string Name { get; set; }
	public string Description { get; set; }
	public Contract Contract { get; set; }
	public float SuccessChance { get; set; }
	public int GoldCost { get; set; }
	public int InkCost { get; set; } = 1;
	public bool IsBossContract { get; set; } = false;

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
