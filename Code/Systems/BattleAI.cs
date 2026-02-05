using System;
using System.Collections.Generic;
using System.Linq;
using Beastborne.Data;
using Beastborne.Core;

namespace Beastborne.Systems;

/// <summary>
/// AI system for selecting moves and making battle decisions
/// </summary>
public static class BattleAI
{
	private static Random _random = new Random();

	/// <summary>
	/// Select the best action for a monster to take
	/// </summary>
	public static MoveChoice SelectAction( Monster attacker, Monster defender, BattleState state, List<Monster> team, bool isPlayer )
	{
		var species = MonsterManager.Instance?.GetSpecies( attacker.SpeciesId );
		if ( species == null )
		{
			return CreateBasicAttack( attacker );
		}

		// Get available moves with PP
		var availableMoves = GetAvailableMoves( attacker );
		if ( availableMoves.Count == 0 )
		{
			return CreateStruggle( attacker ); // No PP left, use Struggle
		}

		// Check if we should swap
		var swapChoice = ConsiderSwap( attacker, defender, state, team, isPlayer );
		if ( swapChoice != null )
		{
			return swapChoice;
		}

		// Score each move
		var scoredMoves = new List<(MoveDefinition move, float score)>();
		foreach ( var move in availableMoves )
		{
			float score = ScoreMove( attacker, defender, move, state );
			scoredMoves.Add( (move, score) );
		}

		// Sort by score descending
		scoredMoves.Sort( (a, b) => b.score.CompareTo( a.score ) );

		// Add some randomness - don't always pick the best move
		// 70% chance to pick best, 20% second best, 10% random
		MoveDefinition selectedMove;
		float roll = (float)_random.NextDouble();
		if ( roll < 0.70f || scoredMoves.Count == 1 )
		{
			selectedMove = scoredMoves[0].move;
		}
		else if ( roll < 0.90f && scoredMoves.Count > 1 )
		{
			selectedMove = scoredMoves[1].move;
		}
		else
		{
			selectedMove = scoredMoves[_random.Next( scoredMoves.Count )].move;
		}

		return new MoveChoice
		{
			ActionType = BattleActionType.Attack,
			MonsterId = attacker.Id,
			MoveId = selectedMove.Id,
			Priority = BattleSimulator.GetEffectivePriority( attacker, selectedMove ),
			Speed = BattleSimulator.GetEffectiveSPD( attacker, state )
		};
	}

	/// <summary>
	/// Get all moves the monster can use (have PP)
	/// </summary>
	private static List<MoveDefinition> GetAvailableMoves( Monster monster )
	{
		var moves = new List<MoveDefinition>();
		if ( monster.Moves == null ) return moves;

		foreach ( var monsterMove in monster.Moves )
		{
			if ( monsterMove.CurrentPP > 0 )
			{
				var moveDef = MoveDatabase.GetMove( monsterMove.MoveId );
				if ( moveDef != null )
				{
					moves.Add( moveDef );
				}
			}
		}

		return moves;
	}

	/// <summary>
	/// Score a move based on various factors
	/// Higher score = better move choice
	/// </summary>
	private static float ScoreMove( Monster attacker, Monster defender, MoveDefinition move, BattleState state )
	{
		float score = 0f;

		var attackerSpecies = MonsterManager.Instance?.GetSpecies( attacker.SpeciesId );
		var defenderSpecies = MonsterManager.Instance?.GetSpecies( defender.SpeciesId );

		// Base score from move power (normalized)
		if ( move.Category != MoveCategory.Status )
		{
			score += move.BasePower / 10f; // 100 power = 10 points
		}

		// Type effectiveness bonus
		if ( defenderSpecies != null )
		{
			float typeMultiplier = GetTypeEffectiveness( move.Element, defenderSpecies.Element );
			if ( typeMultiplier >= 2.0f )
				score += 30f; // Super effective
			else if ( typeMultiplier >= 1.5f )
				score += 15f;
			else if ( typeMultiplier <= 0.5f )
				score -= 20f; // Not very effective
			else if ( typeMultiplier == 0f )
				score -= 100f; // Immune
		}

		// STAB bonus (Same Type Attack Bonus)
		if ( attackerSpecies != null && move.Element == attackerSpecies.Element )
		{
			score += 5f;
		}

		// Accuracy penalty for low accuracy moves
		if ( move.Accuracy < 100 )
		{
			score -= (100 - move.Accuracy) * 0.2f;
		}

		// Status move scoring
		if ( move.Category == MoveCategory.Status )
		{
			score += ScoreStatusMove( attacker, defender, move, state );
		}

		// Move effect scoring
		score += ScoreMoveEffects( attacker, defender, move, state );

		// Consider if this move could KO the enemy
		if ( move.Category != MoveCategory.Status && defender.CurrentHP > 0 )
		{
			int estimatedDamage = EstimateDamage( attacker, defender, move, state );
			if ( estimatedDamage >= defender.CurrentHP )
			{
				score += 25f; // Potential KO bonus
			}
		}

		// Penalty for recharge moves unless we can KO
		if ( move.Effects?.Exists( e => e.Type == MoveEffectType.Recharge ) == true )
		{
			int estimatedDamage = EstimateDamage( attacker, defender, move, state );
			if ( estimatedDamage < defender.CurrentHP )
			{
				score -= 15f; // Don't use recharge moves unless it KOs
			}
		}

		return score;
	}

	/// <summary>
	/// Score status moves specifically
	/// </summary>
	private static float ScoreStatusMove( Monster attacker, Monster defender, MoveDefinition move, BattleState state )
	{
		float score = 0f;

		foreach ( var effect in move.Effects ?? new List<MoveEffect>() )
		{
			// Don't apply status if target already has one
			if ( IsStatusEffect( effect.Type ) && !effect.TargetsSelf )
			{
				if ( state.HasAnyStatus( defender.Id ) )
				{
					score -= 50f; // Target already has status
				}
				else
				{
					score += 15f; // Applying status is good
				}
			}

			// Healing moves
			if ( effect.Type == MoveEffectType.Heal || effect.Type == MoveEffectType.FullHeal )
			{
				float hpPercent = (float)attacker.CurrentHP / attacker.MaxHP;
				if ( hpPercent < 0.3f )
					score += 40f; // Heal when low HP
				else if ( hpPercent < 0.5f )
					score += 20f;
				else if ( hpPercent > 0.8f )
					score -= 30f; // Don't heal when healthy
			}

			// Stat boosting moves
			if ( IsStatBoostEffect( effect.Type ) && effect.TargetsSelf )
			{
				int currentStage = state.GetStatStage( attacker.Id, GetStatIndexFromEffect( effect.Type ) );
				if ( currentStage >= 4 )
					score -= 20f; // Already boosted high
				else if ( currentStage <= 0 )
					score += 15f; // Boosting is good
			}

			// Stat lowering moves on enemy
			if ( IsStatLowerEffect( effect.Type ) && !effect.TargetsSelf )
			{
				int currentStage = state.GetStatStage( defender.Id, GetStatIndexFromEffect( effect.Type ) );
				if ( currentStage <= -4 )
					score -= 20f; // Already lowered a lot
				else
					score += 10f;
			}

			// Guard move
			if ( effect.Type == MoveEffectType.Guard )
			{
				// Penalize repeated guards
				if ( state.ConsecutiveGuards.TryGetValue( attacker.Id, out int guards ) && guards > 0 )
				{
					score -= guards * 20f;
				}
				// Use guard when low HP
				float hpPercent = (float)attacker.CurrentHP / attacker.MaxHP;
				if ( hpPercent < 0.3f )
					score += 20f;
			}

			// Cleanse move
			if ( effect.Type == MoveEffectType.Cleanse && effect.TargetsSelf )
			{
				if ( state.HasAnyStatus( attacker.Id ) )
					score += 25f; // Good to cleanse when afflicted
				else
					score -= 30f; // Don't cleanse when healthy
			}
		}

		return score;
	}

	/// <summary>
	/// Score move secondary effects
	/// </summary>
	private static float ScoreMoveEffects( Monster attacker, Monster defender, MoveDefinition move, BattleState state )
	{
		float score = 0f;

		foreach ( var effect in move.Effects ?? new List<MoveEffect>() )
		{
			// Status chance on damaging move
			if ( IsStatusEffect( effect.Type ) && move.Category != MoveCategory.Status )
			{
				if ( !state.HasAnyStatus( defender.Id ) )
				{
					score += effect.Chance * 10f; // Bonus for chance to inflict status
				}
			}

			// Flinch is good if we're faster
			if ( effect.Type == MoveEffectType.Flinch && attacker.SPD > defender.SPD )
			{
				score += effect.Chance * 8f;
			}

			// Drain is always good
			if ( effect.Type == MoveEffectType.Drain )
			{
				float hpPercent = (float)attacker.CurrentHP / attacker.MaxHP;
				score += (1f - hpPercent) * 10f; // More valuable when low HP
			}

			// Recoil penalty
			if ( effect.Type == MoveEffectType.Recoil )
			{
				float hpPercent = (float)attacker.CurrentHP / attacker.MaxHP;
				score -= effect.Value * 0.1f;
				if ( hpPercent < 0.3f )
					score -= 10f; // Don't use recoil moves when low HP
			}

			// Crit boost
			if ( effect.Type == MoveEffectType.CritBoost )
			{
				score += 3f;
			}
		}

		return score;
	}

	/// <summary>
	/// Consider whether to swap to a different monster
	/// </summary>
	private static MoveChoice ConsiderSwap( Monster attacker, Monster defender, BattleState state, List<Monster> team, bool isPlayer )
	{
		// Don't consider swap if we're the only one alive
		var aliveTeammates = team.Where( m => m.Id != attacker.Id && m.CurrentHP > 0 ).ToList();
		if ( aliveTeammates.Count == 0 )
			return null;

		var attackerSpecies = MonsterManager.Instance?.GetSpecies( attacker.SpeciesId );
		var defenderSpecies = MonsterManager.Instance?.GetSpecies( defender.SpeciesId );

		// Check if we're at severe type disadvantage
		bool atDisadvantage = false;
		if ( attackerSpecies != null && defenderSpecies != null )
		{
			float theirEffectiveness = GetTypeEffectiveness( defenderSpecies.Element, attackerSpecies.Element );
			if ( theirEffectiveness >= 2.0f )
				atDisadvantage = true;
		}

		// Consider swapping if at low HP and type disadvantage
		float hpPercent = (float)attacker.CurrentHP / attacker.MaxHP;
		bool shouldSwap = false;

		if ( hpPercent < 0.2f && atDisadvantage )
			shouldSwap = _random.NextDouble() < 0.6f;
		else if ( hpPercent < 0.3f && atDisadvantage )
			shouldSwap = _random.NextDouble() < 0.3f;

		if ( !shouldSwap )
			return null;

		// Find the best teammate to swap to
		Monster bestSwap = null;
		float bestScore = float.MinValue;

		foreach ( var teammate in aliveTeammates )
		{
			var teammateSpecies = MonsterManager.Instance?.GetSpecies( teammate.SpeciesId );
			if ( teammateSpecies == null ) continue;

			float score = 0f;

			// Type advantage against enemy
			float ourEffectiveness = GetTypeEffectiveness( teammateSpecies.Element, defenderSpecies?.Element ?? ElementType.Neutral );
			if ( ourEffectiveness >= 2.0f )
				score += 30f;
			else if ( ourEffectiveness >= 1.5f )
				score += 15f;

			// Resistance to enemy type
			float theirEffectiveness = GetTypeEffectiveness( defenderSpecies?.Element ?? ElementType.Neutral, teammateSpecies.Element );
			if ( theirEffectiveness <= 0.5f )
				score += 20f;
			else if ( theirEffectiveness >= 2.0f )
				score -= 30f;

			// HP consideration
			score += (float)teammate.CurrentHP / teammate.MaxHP * 10f;

			if ( score > bestScore )
			{
				bestScore = score;
				bestSwap = teammate;
			}
		}

		if ( bestSwap != null && bestScore > 0 )
		{
			int swapIndex = team.IndexOf( bestSwap );
			return new MoveChoice
			{
				ActionType = BattleActionType.Swap,
				MonsterId = attacker.Id,
				SwapToIndex = swapIndex,
				Priority = -6, // Swaps have low priority
				Speed = BattleSimulator.GetEffectiveSPD( attacker, state )
			};
		}

		return null;
	}

	/// <summary>
	/// Estimate damage for KO calculation
	/// </summary>
	private static int EstimateDamage( Monster attacker, Monster defender, MoveDefinition move, BattleState state )
	{
		if ( move.BasePower == 0 ) return 0;

		var attackerSpecies = MonsterManager.Instance?.GetSpecies( attacker.SpeciesId );
		var defenderSpecies = MonsterManager.Instance?.GetSpecies( defender.SpeciesId );

		// Choose stats based on move category
		int atkStat = move.Category == MoveCategory.Physical ? attacker.ATK : attacker.SpA;
		int defStat = move.Category == MoveCategory.Physical ? defender.DEF : defender.SpD;

		// Apply stat stages
		int atkStage = state.GetStatStage( attacker.Id, move.Category == MoveCategory.Physical ? StatIndex.ATK : StatIndex.SpA );
		int defStage = state.GetStatStage( defender.Id, move.Category == MoveCategory.Physical ? StatIndex.DEF : StatIndex.SpD );
		atkStat = (int)(atkStat * BattleState.GetStatMultiplier( atkStage ));
		defStat = (int)(defStat * BattleState.GetStatMultiplier( defStage ));

		// Base damage
		float damage = (atkStat * 2.0f * move.BasePower / 100f) - (defStat * 0.5f);
		damage = Math.Max( 1, damage );

		// STAB
		if ( attackerSpecies != null && move.Element == attackerSpecies.Element )
		{
			damage *= 1.5f;
		}

		// Type effectiveness
		if ( defenderSpecies != null )
		{
			damage *= GetTypeEffectiveness( move.Element, defenderSpecies.Element );
		}

		return (int)damage;
	}

	/// <summary>
	/// Create a basic attack choice when no moves available
	/// </summary>
	private static MoveChoice CreateBasicAttack( Monster attacker )
	{
		// Use first available move or struggle
		if ( attacker.Moves?.Count > 0 )
		{
			var move = attacker.Moves[0];
			var moveDef = MoveDatabase.GetMove( move.MoveId );
			return new MoveChoice
			{
				ActionType = BattleActionType.Attack,
				MonsterId = attacker.Id,
				MoveId = move.MoveId,
				Priority = moveDef != null ? BattleSimulator.GetEffectivePriority( attacker, moveDef ) : 0,
				Speed = attacker.SPD // Fallback - no state available in static helper
			};
		}

		return CreateStruggle( attacker );
	}

	/// <summary>
	/// Create a Struggle move when no PP left
	/// </summary>
	private static MoveChoice CreateStruggle( Monster attacker )
	{
		return new MoveChoice
		{
			ActionType = BattleActionType.Attack,
			MonsterId = attacker.Id,
			MoveId = "struggle", // Special move that always works
			Priority = 0,
			Speed = attacker.SPD // Fallback - no state available in static helper
		};
	}

	/// <summary>
	/// Get type effectiveness multiplier
	/// </summary>
	public static float GetTypeEffectiveness( ElementType attackType, ElementType defendType )
	{
		return (attackType, defendType) switch
		{
			// Fire
			(ElementType.Fire, ElementType.Nature) => 2.0f,
			(ElementType.Fire, ElementType.Ice) => 2.0f,
			(ElementType.Fire, ElementType.Metal) => 2.0f,
			(ElementType.Fire, ElementType.Water) => 0.5f,
			(ElementType.Fire, ElementType.Earth) => 0.5f,
			(ElementType.Fire, ElementType.Fire) => 0.5f,

			// Water
			(ElementType.Water, ElementType.Fire) => 2.0f,
			(ElementType.Water, ElementType.Earth) => 2.0f,
			(ElementType.Water, ElementType.Water) => 0.5f,
			(ElementType.Water, ElementType.Nature) => 0.5f,
			(ElementType.Water, ElementType.Electric) => 0.5f,

			// Earth
			(ElementType.Earth, ElementType.Fire) => 2.0f,
			(ElementType.Earth, ElementType.Electric) => 2.0f,
			(ElementType.Earth, ElementType.Metal) => 2.0f,
			(ElementType.Earth, ElementType.Wind) => 0f, // Immune (flying)
			(ElementType.Earth, ElementType.Nature) => 0.5f,

			// Wind
			(ElementType.Wind, ElementType.Nature) => 2.0f,
			(ElementType.Wind, ElementType.Earth) => 2.0f,
			(ElementType.Wind, ElementType.Electric) => 0.5f,
			(ElementType.Wind, ElementType.Metal) => 0.5f,

			// Electric
			(ElementType.Electric, ElementType.Water) => 2.0f,
			(ElementType.Electric, ElementType.Wind) => 2.0f,
			(ElementType.Electric, ElementType.Earth) => 0f, // Immune (grounded)
			(ElementType.Electric, ElementType.Electric) => 0.5f,

			// Ice
			(ElementType.Ice, ElementType.Nature) => 2.0f,
			(ElementType.Ice, ElementType.Earth) => 2.0f,
			(ElementType.Ice, ElementType.Wind) => 2.0f,
			(ElementType.Ice, ElementType.Fire) => 0.5f,
			(ElementType.Ice, ElementType.Water) => 0.5f,
			(ElementType.Ice, ElementType.Metal) => 0.5f,
			(ElementType.Ice, ElementType.Ice) => 0.5f,

			// Nature
			(ElementType.Nature, ElementType.Water) => 2.0f,
			(ElementType.Nature, ElementType.Earth) => 2.0f,
			(ElementType.Nature, ElementType.Fire) => 0.5f,
			(ElementType.Nature, ElementType.Wind) => 0.5f,
			(ElementType.Nature, ElementType.Ice) => 0.5f,
			(ElementType.Nature, ElementType.Metal) => 0.5f,
			(ElementType.Nature, ElementType.Nature) => 0.5f,

			// Metal
			(ElementType.Metal, ElementType.Ice) => 2.0f,
			(ElementType.Metal, ElementType.Spirit) => 2.0f,
			(ElementType.Metal, ElementType.Fire) => 0.5f,
			(ElementType.Metal, ElementType.Water) => 0.5f,
			(ElementType.Metal, ElementType.Electric) => 0.5f,
			(ElementType.Metal, ElementType.Metal) => 0.5f,

			// Shadow
			(ElementType.Shadow, ElementType.Spirit) => 2.0f,
			(ElementType.Shadow, ElementType.Shadow) => 2.0f,

			// Spirit
			(ElementType.Spirit, ElementType.Shadow) => 2.0f,
			(ElementType.Spirit, ElementType.Spirit) => 0.5f,
			(ElementType.Spirit, ElementType.Metal) => 0.5f,

			_ => 1.0f
		};
	}

	/// <summary>
	/// Check if effect type is a status condition
	/// </summary>
	private static bool IsStatusEffect( MoveEffectType type )
	{
		return type == MoveEffectType.Burn ||
			   type == MoveEffectType.Freeze ||
			   type == MoveEffectType.Paralyze ||
			   type == MoveEffectType.Poison ||
			   type == MoveEffectType.Sleep ||
			   type == MoveEffectType.Confuse;
	}

	/// <summary>
	/// Check if effect is a stat boost
	/// </summary>
	private static bool IsStatBoostEffect( MoveEffectType type )
	{
		return type == MoveEffectType.RaiseATK ||
			   type == MoveEffectType.RaiseDEF ||
			   type == MoveEffectType.RaiseSpA ||
			   type == MoveEffectType.RaiseSpD ||
			   type == MoveEffectType.RaiseSPD ||
			   type == MoveEffectType.RaiseAccuracy ||
			   type == MoveEffectType.RaiseEvasion;
	}

	/// <summary>
	/// Check if effect is a stat lower
	/// </summary>
	private static bool IsStatLowerEffect( MoveEffectType type )
	{
		return type == MoveEffectType.LowerATK ||
			   type == MoveEffectType.LowerDEF ||
			   type == MoveEffectType.LowerSpA ||
			   type == MoveEffectType.LowerSpD ||
			   type == MoveEffectType.LowerSPD ||
			   type == MoveEffectType.LowerAccuracy ||
			   type == MoveEffectType.LowerEvasion;
	}

	/// <summary>
	/// Get stat index from effect type
	/// </summary>
	private static StatIndex GetStatIndexFromEffect( MoveEffectType type )
	{
		return type switch
		{
			MoveEffectType.RaiseATK or MoveEffectType.LowerATK => StatIndex.ATK,
			MoveEffectType.RaiseDEF or MoveEffectType.LowerDEF => StatIndex.DEF,
			MoveEffectType.RaiseSpA or MoveEffectType.LowerSpA => StatIndex.SpA,
			MoveEffectType.RaiseSpD or MoveEffectType.LowerSpD => StatIndex.SpD,
			MoveEffectType.RaiseSPD or MoveEffectType.LowerSPD => StatIndex.SPD,
			MoveEffectType.RaiseAccuracy or MoveEffectType.LowerAccuracy => StatIndex.Accuracy,
			MoveEffectType.RaiseEvasion or MoveEffectType.LowerEvasion => StatIndex.Evasion,
			_ => StatIndex.ATK
		};
	}
}
