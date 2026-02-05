using System.Collections.Generic;

namespace Beastborne.Data;

public enum ElementType
{
	Neutral,
	Fire,
	Water,
	Earth,
	Wind,
	Electric,
	Ice,
	Nature,
	Metal,
	Shadow,
	Spirit
}

public enum Rarity
{
	Common,
	Uncommon,
	Rare,
	Epic,
	Legendary,  // Very rare evolved forms and bosses
	Mythic      // Ultra-rare but catchable - the ultimate collector's prize
}

/// <summary>
/// Base species definition - static data for each monster type
/// </summary>
public class MonsterSpecies
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string IconPath { get; set; }

	// Base stats (level 1)
	public int BaseHP { get; set; }
	public int BaseATK { get; set; }
	public int BaseDEF { get; set; }
	public int BaseSpA { get; set; }  // Special Attack
	public int BaseSpD { get; set; }  // Special Defense
	public int BaseSPD { get; set; }  // Speed

	// Stat growth per level
	public float HPGrowth { get; set; }
	public float ATKGrowth { get; set; }
	public float DEFGrowth { get; set; }
	public float SpAGrowth { get; set; }
	public float SpDGrowth { get; set; }
	public float SPDGrowth { get; set; }

	public ElementType Element { get; set; }
	public Rarity BaseRarity { get; set; }

	// Evolution chain
	public string EvolvesFrom { get; set; }
	public string EvolvesTo { get; set; }
	public int EvolutionLevel { get; set; }

	// Catchability
	public bool IsCatchable { get; set; } = true;
	public float BaseCatchRate { get; set; } = 0.5f;

	// Traits pool - possible traits this species can have
	public List<string> PossibleTraits { get; set; } = new();

	// Learnable moves - moves this species can learn at various levels
	public List<LearnableMove> LearnableMoves { get; set; } = new();

	// Animation frames for UI (idle animation)
	public List<string> AnimationFrames { get; set; } = new();
	public float AnimationFrameRate { get; set; } = 8f;

	// Beastiary number for organization (e.g., #001, #002)
	public int BeastiaryNumber { get; set; }

	// Per-monster icon offset for beastiary positioning (pixels)
	public float IconOffsetX { get; set; } = 0f;
	public float IconOffsetY { get; set; } = 0f;
}
