using System.Collections.Generic;
using System.Linq;

namespace Beastborne.Data;

/// <summary>
/// The complete skill tree containing all nodes organized into branches.
/// Hex-cluster flower layout — keystone top, T1 petals left/right, T2 below.
/// </summary>
public class SkillTree
{
	public List<SkillNode> Nodes { get; set; } = new();

	// Alias for Nodes property (used by UI)
	public List<SkillNode> AllNodes => Nodes;

	public SkillNode GetNode( string id )
	{
		return Nodes.FirstOrDefault( n => n.Id == id );
	}

	public List<SkillNode> GetNodesByBranch( SkillBranch branch )
	{
		return Nodes.Where( n => n.Branch == branch ).OrderBy( n => n.Order ).ToList();
	}

	// Legacy method for backward compatibility
	public List<SkillNode> GetNodesByCategory( SkillCategory category )
	{
		return Nodes.Where( n => n.Category == category ).ToList();
	}

	/// <summary>
	/// Calculate total skill points spent in a branch
	/// </summary>
	public int GetPointsSpentInBranch( SkillBranch branch, Dictionary<string, int> skillRanks )
	{
		int total = 0;
		foreach ( var node in Nodes.Where( n => n.Branch == branch ) )
		{
			int rank = skillRanks.GetValueOrDefault( node.Id, 0 );
			total += rank * node.CostPerRank;
		}
		return total;
	}

	public List<SkillNode> GetUnlockableNodes( Dictionary<string, int> skillRanks )
	{
		return Nodes.Where( n => CanUnlockInternal( n, skillRanks ) ).ToList();
	}

	private bool CanUnlockInternal( SkillNode node, Dictionary<string, int> skillRanks )
	{
		int currentRank = skillRanks.GetValueOrDefault( node.Id, 0 );
		if ( currentRank >= node.MaxRank ) return false;

		int branchPoints = GetPointsSpentInBranch( node.Branch, skillRanks );
		if ( branchPoints < node.RequiredBranchPoints ) return false;

		if ( !string.IsNullOrEmpty( node.RequiredSkillId ) )
		{
			int reqRank = skillRanks.GetValueOrDefault( node.RequiredSkillId, 0 );
			if ( reqRank < node.RequiredSkillRank ) return false;
		}

		return true;
	}

	public bool CanUnlock( string nodeId, Dictionary<string, int> skillRanks, int availablePoints )
	{
		var node = GetNode( nodeId );
		if ( node == null ) return false;
		if ( availablePoints < node.CostPerRank ) return false;

		return CanUnlockInternal( node, skillRanks );
	}

	// Cluster-canvas is ~258px wide (260px cluster - 2px border). Hex-slot is
	// 58px. Center column X = (258 - 58) / 2 = 100.
	private const int KEYSTONE_X = 100;
	private const int KEYSTONE_Y = 32;
	private const int T1_LEFT_X  = 20;
	private const int T1_LEFT_Y  = 108;
	private const int T1_RIGHT_X = 180;
	private const int T1_RIGHT_Y = 108;

	// Capstone/T2 outer-ring slot constants intentionally removed for the
	// launch tree. When post-launch updates introduce 4th/5th talents per
	// branch, re-add the coord constants + Slot enum members + SlotCoords
	// arms together — keeps the dead-code surface zero in the meantime.

	/// <summary>
	/// Generate the launch-set skill tree: 5 branches × 3 talents each (15 total).
	/// Each branch = keystone + T1 petal + T2 advanced. Max-all = 55 SP, which
	/// matches the lv 50 cap (5 starting SP + 50 from leveling). Players can
	/// fully max the tree — post-launch updates raise the level cap AND add new
	/// talents (Arcane Focus, Devastating Blows, Cost Reduction, Nature Bond,
	/// Team Spirit, Pathfinder, Giant Killer, Phase Breaker, Mythbreaker, etc.)
	/// so every update is a real progression beat.
	/// </summary>
	public static SkillTree CreateDefault()
	{
		var tree = new SkillTree();

		void Add( string id, string name, string desc, SkillBranch branch, Slot slot,
			int maxRank, int costPerRank, int gate, SkillEffectType effectType, float effectValue,
			string requiredSkillId = null, int requiredSkillRank = 1 )
		{
			var (tier, x, y) = SlotCoords( slot );
			tree.Nodes.Add( new SkillNode
			{
				Id = id,
				Name = name,
				Description = desc,
				Branch = branch,
				Tier = tier,
				Order = tree.Nodes.Count(n => n.Branch == branch),
				GridX = x,
				GridY = y,
				MaxRank = maxRank,
				CostPerRank = costPerRank,
				RequiredBranchPoints = gate,
				RequiredSkillId = requiredSkillId,
				RequiredSkillRank = requiredSkillRank,
				Effects = new() { new SkillEffect { Type = effectType, Value = effectValue } }
			} );
		}

		// Progression is per-node prereq chain (not branch-point pools):
		// keystone is free-to-invest; the two petals require 1 rank in the
		// keystone. Simple "buy the top, then the branches unlock" model —
		// reads clearer than tier-point gates and gives each investment an
		// immediate downstream payoff.

		// ========== POWER — 3 talents, 12 SP ==========
		Add( "power_might",    "Might",         "A tamer's first lesson: teach them to hit harder.",
			SkillBranch.Power, Slot.Keystone, maxRank: 3, costPerRank: 1, gate: 0,
			SkillEffectType.AllMonsterATKPercent, 3 );

		Add( "power_vitality", "Vitality",      "Deeper lungs, stronger hearts. Your beasts carry more fight.",
			SkillBranch.Power, Slot.T1Left, maxRank: 3, costPerRank: 1, gate: 0,
			SkillEffectType.AllMonsterHPPercent, 3,
			requiredSkillId: "power_might", requiredSkillRank: 1 );

		Add( "power_criteye",  "Critical Eye",  "Spot the opening, strike the weak point.",
			SkillBranch.Power, Slot.T1Right, maxRank: 3, costPerRank: 2, gate: 0,
			SkillEffectType.CritChanceBonus, 2,
			requiredSkillId: "power_might", requiredSkillRank: 1 );

		// ========== FORTUNE — 3 talents, 12 SP ==========
		Add( "fortune_goldrush", "Gold Rush",   "Loose pockets, lucky finds. Every victory shines a little brighter.",
			SkillBranch.Fortune, Slot.Keystone, maxRank: 3, costPerRank: 1, gate: 0,
			SkillEffectType.GoldDropBonus, 5 );

		Add( "fortune_bargain",  "Bargain Hunter", "Know the value of a coin. Merchants respect it.",
			SkillBranch.Fortune, Slot.T1Left, maxRank: 3, costPerRank: 1, gate: 0,
			SkillEffectType.ShopDiscount, 3,
			requiredSkillId: "fortune_goldrush", requiredSkillRank: 1 );

		Add( "fortune_jackpot",  "Jackpot",     "Fortune favours the ones who keep showing up.",
			SkillBranch.Fortune, Slot.T1Right, maxRank: 3, costPerRank: 2, gate: 0,
			SkillEffectType.DoubleDropChance, 2,
			requiredSkillId: "fortune_goldrush", requiredSkillRank: 1 );

		// ========== FUSION — 3 talents, 11 SP ==========
		Add( "fusion_genesurge",    "Gene Surge",      "Awaken a deeper spark in every union.",
			SkillBranch.Fusion, Slot.Keystone, maxRank: 3, costPerRank: 1, gate: 0,
			SkillEffectType.GeneBonusFlat, 1 );

		Add( "fusion_inheritance",  "Inheritance",     "Study the bloodline. Carry the best forward.",
			SkillBranch.Fusion, Slot.T1Left, maxRank: 2, costPerRank: 2, gate: 0,
			SkillEffectType.GeneticInheritanceBonus, 5,
			requiredSkillId: "fusion_genesurge", requiredSkillRank: 1 );

		Add( "fusion_mutation",     "Mutation Chance", "Sometimes the strange spark is the strongest.",
			SkillBranch.Fusion, Slot.T1Right, maxRank: 2, costPerRank: 2, gate: 0,
			SkillEffectType.MutationChance, 3,
			requiredSkillId: "fusion_genesurge", requiredSkillRank: 1 );

		// ========== EXPEDITION — 3 talents, 10 SP ==========
		Add( "exp_prospector", "Prospector",  "Every expedition route has a few extra coins, if you know where to look.",
			SkillBranch.Expedition, Slot.Keystone, maxRank: 3, costPerRank: 1, gate: 0,
			SkillEffectType.ExpeditionGoldBonus, 5 );

		Add( "exp_scout",      "Scout",       "A trained eye finds beasts a careless tamer walks right past.",
			SkillBranch.Expedition, Slot.T1Left, maxRank: 3, costPerRank: 1, gate: 0,
			SkillEffectType.EncounterRateBonus, 5,
			requiredSkillId: "exp_prospector", requiredSkillRank: 1 );

		Add( "exp_luckyfind",  "Lucky Find",  "The best finds come to the patient and the curious.",
			SkillBranch.Expedition, Slot.T1Right, maxRank: 2, costPerRank: 2, gate: 0,
			SkillEffectType.RareItemChance, 4,
			requiredSkillId: "exp_prospector", requiredSkillRank: 1 );

		// ========== MASTERY — 3 talents, 10 SP ==========
		Add( "mastery_slayer",     "Boss Slayer",     "You've walked this hallway before. Strike like you remember it.",
			SkillBranch.Mastery, Slot.Keystone, maxRank: 3, costPerRank: 1, gate: 0,
			SkillEffectType.BossDamageBonus, 5 );

		Add( "mastery_resilience", "Resilience",      "The stone you don't flinch from can't hurt you.",
			SkillBranch.Mastery, Slot.T1Left, maxRank: 3, costPerRank: 1, gate: 0,
			SkillEffectType.BossDamageReduction, 3,
			requiredSkillId: "mastery_slayer", requiredSkillRank: 1 );

		Add( "mastery_tokens",     "Token Collector", "Every defeat leaves a mark; leave with it on you.",
			SkillBranch.Mastery, Slot.T1Right, maxRank: 2, costPerRank: 2, gate: 0,
			SkillEffectType.BossTokenBonus, 10,
			requiredSkillId: "mastery_slayer", requiredSkillRank: 1 );

		return tree;
	}

	// Slot positions within a 3-node cluster triangle. Launch tree uses only
	// these three slots; outer-ring (Capstone/T2) entries will be added back
	// alongside the new nodes when post-launch updates extend the tree.
	private enum Slot { Keystone, T1Left, T1Right }

	private static (int tier, int x, int y) SlotCoords( Slot slot )
	{
		// Launch tree has no tier gating — every node is freely available.
		// T1Right is a POSITION label, not a tier. All visible nodes report
		// tier 1 so the tooltip doesn't display misleading tier info.
		// Post-launch updates can introduce real T2/T3 tiers alongside
		// `RequiredBranchPoints` gates when the tree grows.
		return slot switch
		{
			Slot.Keystone => (1, KEYSTONE_X, KEYSTONE_Y),
			Slot.T1Left   => (1, T1_LEFT_X,  T1_LEFT_Y),
			Slot.T1Right  => (1, T1_RIGHT_X, T1_RIGHT_Y),
			_             => (1, KEYSTONE_X, KEYSTONE_Y),
		};
	}
}
