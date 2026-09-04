using System;

namespace Beastborne.Data;

public enum NatureType
{
	Balanced,     // No effect
	Ferocious,    // +ATK, -DEF
	Stalwart,     // +DEF, -ATK
	Restless,     // +SPD, -HP
	Enduring,     // +HP, -SPD
	Reckless,     // +ATK, -SPD
	Stoic,        // +DEF, -SPD
	Skittish,     // +SPD, -DEF
	Vigorous,     // +HP, -ATK
	Ruthless,     // +ATK, -HP
	Nimble,       // +SPD, -ATK

	// New natures for Special stats
	Mystical,     // +SpA, -ATK (prefers special attacks)
	Resolute,     // +SpD, -SpA (specially defensive)
	Arcane,       // +SpA, -DEF (glass cannon special attacker)
	Warded,       // +SpD, -SPD (slow but specially bulky)
	Cunning,      // +SpA, -HP (frail special attacker)
	Serene        // +SpD, -ATK (calm special wall)
}

/// <summary>
/// Genetic values that affect stat calculations
/// Each gene ranges from 0-30
/// </summary>
public class Genetics
{
	// Maximum gene value
	public const int MaxGeneValue = 30;

	// Maximum possible total value (6 stats * 30)
	public const int MaxTotalValue = MaxGeneValue * 6; // 180

	public int HPGene { get; set; }
	public int ATKGene { get; set; }
	public int DEFGene { get; set; }
	[System.Text.Json.Serialization.JsonPropertyName("sp_a_gene")]
	public int SpAGene { get; set; }  // Special Attack gene
	[System.Text.Json.Serialization.JsonPropertyName("sp_d_gene")]
	public int SpDGene { get; set; }  // Special Defense gene
	[System.Text.Json.Serialization.JsonPropertyName("speed_gene")]
	public int SPDGene { get; set; }  // Speed gene

	/// <summary>
	/// Returns the gene value clamped to 0-30 range
	/// </summary>
	public int GetGene( string stat ) => Math.Clamp( stat switch
	{
		"HP" => HPGene,
		"ATK" => ATKGene,
		"DEF" => DEFGene,
		"SpA" => SpAGene,
		"SpD" => SpDGene,
		"SPD" => SPDGene,
		_ => 0
	}, 0, MaxGeneValue );

	public NatureType Nature { get; set; }

	// Calculate total genetic value (max 180)
	public int TotalValue => GetGene( "HP" ) + GetGene( "ATK" ) + GetGene( "DEF" ) + GetGene( "SpA" ) + GetGene( "SpD" ) + GetGene( "SPD" );

	// Quality rating based on total genes
	public string QualityRating
	{
		get
		{
			float percentage = TotalValue / (float)MaxTotalValue;
			return percentage switch
			{
				>= 0.9f => "Perfect",
				>= 0.75f => "Excellent",
				>= 0.6f => "Great",
				>= 0.4f => "Good",
				>= 0.2f => "Average",
				_ => "Poor"
			};
		}
	}

	public static Genetics GenerateRandom()
	{
		var random = new Random();
		return new Genetics
		{
			HPGene = random.Next( 0, MaxGeneValue + 1 ),
			ATKGene = random.Next( 0, MaxGeneValue + 1 ),
			DEFGene = random.Next( 0, MaxGeneValue + 1 ),
			SpAGene = random.Next( 0, MaxGeneValue + 1 ),
			SpDGene = random.Next( 0, MaxGeneValue + 1 ),
			SPDGene = random.Next( 0, MaxGeneValue + 1 ),
			Nature = (NatureType)random.Next( 0, Enum.GetValues<NatureType>().Length )
		};
	}

	public static Genetics GenerateFromParents( Genetics parent1, Genetics parent2 )
	{
		var random = new Random();

		// Each gene has a chance to come from either parent, with slight mutation
		int InheritGene( int gene1, int gene2 )
		{
			// 45% from parent1, 45% from parent2, 10% random mutation
			int roll = random.Next( 100 );
			int baseGene;

			if ( roll < 45 )
				baseGene = gene1;
			else if ( roll < 90 )
				baseGene = gene2;
			else
				baseGene = random.Next( 0, MaxGeneValue + 1 );

			// Small mutation chance (+/- 1-5)
			if ( random.Next( 100 ) < 20 )
			{
				int mutation = random.Next( -5, 6 );
				baseGene = Math.Clamp( baseGene + mutation, 0, MaxGeneValue );
			}

			return baseGene;
		}

		return new Genetics
		{
			HPGene = InheritGene( parent1.HPGene, parent2.HPGene ),
			ATKGene = InheritGene( parent1.ATKGene, parent2.ATKGene ),
			DEFGene = InheritGene( parent1.DEFGene, parent2.DEFGene ),
			SpAGene = InheritGene( parent1.SpAGene, parent2.SpAGene ),
			SpDGene = InheritGene( parent1.SpDGene, parent2.SpDGene ),
			SPDGene = InheritGene( parent1.SPDGene, parent2.SPDGene ),
			Nature = random.Next( 2 ) == 0 ? parent1.Nature : parent2.Nature
		};
	}

	/// <summary>
	/// The stat a nature raises and the stat it lowers (each by 10%). Balanced
	/// returns (null, null). ONE source for the rune-shop grouping, the rune
	/// descriptions and the nature text — never parse strings for this.
	/// </summary>
	public static (string Up, string Down) GetNatureStats( NatureType nature ) => nature switch
	{
		NatureType.Ferocious => ("ATK", "DEF"),
		NatureType.Stalwart => ("DEF", "ATK"),
		NatureType.Restless => ("SPD", "HP"),
		NatureType.Enduring => ("HP", "SPD"),
		NatureType.Reckless => ("ATK", "SPD"),
		NatureType.Stoic => ("DEF", "SPD"),
		NatureType.Skittish => ("SPD", "DEF"),
		NatureType.Vigorous => ("HP", "ATK"),
		NatureType.Ruthless => ("ATK", "HP"),
		NatureType.Nimble => ("SPD", "ATK"),
		NatureType.Mystical => ("SpA", "ATK"),
		NatureType.Resolute => ("SpD", "SpA"),
		NatureType.Arcane => ("SpA", "DEF"),
		NatureType.Warded => ("SpD", "SPD"),
		NatureType.Cunning => ("SpA", "HP"),
		NatureType.Serene => ("SpD", "ATK"),
		_ => (null, null)
	};

	/// <summary>"+10% ATK, -10% DEF" for a nature; "No effect" for Balanced.</summary>
	public static string GetNatureDescription( NatureType nature )
	{
		var (up, down) = GetNatureStats( nature );
		return up == null ? "No effect" : $"+10% {up}, -10% {down}";
	}

	public string GetNatureDescription() => GetNatureDescription( Nature );
}
