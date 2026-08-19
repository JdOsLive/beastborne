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

public enum BeastPersonality
{
	Bold,       // Confident, aggressive — responds to STRENGTH, TRIBUTE
	Timid,      // Shy, cautious — responds to KINDNESS, PATIENCE
	Greedy,     // Materialistic — responds to WEALTH, OFFERING, CHARM
	Loyal,      // Honor-bound — responds to KINDNESS, TRIBUTE, BOND
	Wild        // Untamed, instinctive — responds to STRENGTH, SACRIFICE
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

	/// <summary>
	/// Base Stat Total — sum of six base stats. Pokemon convention.
	/// Used for tier sorting and balance audits. NOT for in-game display —
	/// that's PowerRating on the individual Monster (which accounts for level + genes).
	/// </summary>
	public int BaseStatTotal => BaseHP + BaseATK + BaseDEF + BaseSpA + BaseSpD + BaseSPD;

	// Stat growth per level
	public float HPGrowth { get; set; }
	public float ATKGrowth { get; set; }
	public float DEFGrowth { get; set; }
	public float SpAGrowth { get; set; }
	public float SpDGrowth { get; set; }
	public float SPDGrowth { get; set; }

	public ElementType Element { get; set; }

	// Optional secondary element for dual-type beasts (e.g. Manehelm =
	// Fire/Metal, Lochmaw = Water/Shadow). Null = single-type.
	// Battle defense multiplies both type effectivenesses Pokemon-style;
	// UI element badges should render both icons stacked when set.
	public ElementType? SecondaryElement { get; set; }

	public Rarity BaseRarity { get; set; }

	// Evolution chain
	public string EvolvesFrom { get; set; }
	public string EvolvesTo { get; set; }
	public int EvolutionLevel { get; set; }

	/// <summary>
	/// Escape hatch for the Pattern Book's hard rule: a fusion pattern's
	/// result must normally be STANDALONE (no EvolvesFrom/EvolvesTo) so the
	/// loom can never counterfeit leveling evolution. A species flagged
	/// FusionOnly is exempt — it exists ONLY as a fusion result and may sit
	/// inside an evolution line. No species uses this yet (future-proofing);
	/// FusionPatterns.Validate enforces the rule at load.
	/// </summary>
	public bool FusionOnly { get; set; } = false;

	// Catchability
	public bool IsCatchable { get; set; } = true;
	public float BaseCatchRate { get; set; } = 0.5f;

	/// <summary>
	/// Base XP this species yields when defeated. Used in Pokemon-style formula:
	/// <c>xp = BaseExpYield × defeatedLevel / 7 × levelRatio</c>
	/// where levelRatio = clamp(defeatedLevel / participantLevel, 0.5, 2.0).
	/// Target bands (per .claude/balance-knowledge/reference-values.md):
	///   Common: 50-80 · Uncommon: 120-160 · Rare: 200-280 · Starter: 100 (rare to fight)
	/// Default 60 is a sane zone-1 Common baseline.
	/// </summary>
	public int BaseExpYield { get; set; } = 60;

	/// <summary>
	/// The themed Monster-Hunter-style material this species drops on every KO.
	/// Item ID pattern is <c>mat_{speciesId}</c>; display name is the evocative word
	/// (e.g. "Snapped Twig", "Lost Feather"). ItemManager auto-registers a Material
	/// item from this for every species that has it set. Null/empty = no drop.
	/// </summary>
	public string SignatureDropName { get; set; }

	/// <summary>
	/// One-line flavor description for the signature material drop. Shown in the
	/// inventory tooltip. e.g. "A springy green shoot snapped clean from the beast."
	/// </summary>
	public string SignatureDropDescription { get; set; }

	/// <summary>Item ID of the material dropped on defeat. Pattern: <c>mat_{speciesId}</c>.</summary>
	public string SignatureDropItemId => string.IsNullOrEmpty( SignatureDropName ) ? null : $"mat_{Id}";

	/// <summary>True if this species has a themed signature material drop registered.</summary>
	public bool HasSignatureDrop => !string.IsNullOrEmpty( SignatureDropName );

	// Personality — affects contract negotiation approach effectiveness
	public BeastPersonality Personality { get; set; } = BeastPersonality.Bold;
	public string PersonalityHint { get; set; } = "This creature watches you cautiously...";

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

	// Per-species scale multiplier applied to grid-card sprites in the
	// Beastbook. Native sprite art varies in pixel footprint — some beasts
	// render smaller than siblings at the same CSS size. CardScale lets us
	// nudge individual species up/down without touching the base layout.
	// Default 1.0 = no scaling; only detail-pane portraits ignore this
	// value (they already have room to show the full body).
	public float CardScale { get; set; } = 1.0f;

	// Per-species translate offsets (pixels) applied on the grid-card
	// sprite, composed with CardScale inside the same transform. Lets us
	// nudge individual beasts off-center when the art's natural anchor
	// doesn't sit where the card wants it. Positive X = right, positive
	// Y = down. Paired with transform-origin: center bottom, so scaling
	// stays feet-grounded while the offset shifts the whole portrait.
	// Defaults 0/0 = centered. Only the grid card uses these; detail-pane
	// portraits, evolution sprites, and move icons ignore them.
	public float CardOffsetX { get; set; } = 0f;
	public float CardOffsetY { get; set; } = 0f;

	// Per-species offsets + scale for the roster mini-card sprite. The
	// mini card frames the beast inside a 150×146 sprite area; native
	// art varies in pixel footprint and body weight, so these knobs let
	// us nudge each species into a balanced framing without re-cropping
	// source art. Positive X = right, positive Y = down. MiniScale defaults
	// to 1.0 (no scaling). Edited via the dev editor on MonsterRosterPanel.
	public float MiniOffsetX { get; set; } = 0f;
	public float MiniOffsetY { get; set; } = 0f;
	public float MiniScale { get; set; } = 1.0f;

	// Per-species offsets + scale for the small "Now Active" detail-panel
	// icon (the square sprite shown next to the selected beast's stats).
	// Same mechanic as MiniScale/Offset but tuned independently — the
	// detail icon's frame is much smaller and naturally needs different
	// values. Defaults: 1.0 scale, centered.
	public float DetailIconScale { get; set; } = 1.0f;
	public float DetailIconOffsetX { get; set; } = 0f;
	public float DetailIconOffsetY { get; set; } = 0f;

	// Beastbook UI metadata (art pipeline). None of these affect gameplay.
	// IsAIGenerated = true → the UI shows an amber "AI placeholder" banner so
	// players know the art is provisional. ArtistCredit is shown in the
	// detail-pane footer when set; null = hide credit.
	public bool IsAIGenerated { get; set; } = false;
	public string ArtistCredit { get; set; } = null;
}
