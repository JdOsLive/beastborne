using System;
using System.Collections.Generic;
using System.Linq;
using Beastborne.Data;
using Beastborne.Core;

namespace Beastborne.Systems;

/// <summary>
/// Handles combat damage calculations and battle logic
/// </summary>
public static class BattleSimulator
{
	private static Random _defaultRandom = new Random();
	private static Random _seededRandom = null;
	private static bool _useSeededRandom = false;

	/// <summary>
	/// Get the current random instance (seeded or default)
	/// </summary>
	private static Random CurrentRandom => _useSeededRandom && _seededRandom != null ? _seededRandom : _defaultRandom;

	/// <summary>
	/// Set a seed for deterministic battle simulation (for online play)
	/// </summary>
	public static void SetSeed( int seed )
	{
		_seededRandom = new Random( seed );
		_useSeededRandom = true;
		Log.Info( $"[BattleSimulator] Set battle seed: {seed}" );
	}

	/// <summary>
	/// Clear the seeded random (return to default random)
	/// </summary>
	public static void ClearSeed()
	{
		_useSeededRandom = false;
		_seededRandom = null;
	}

	/// <summary>
	/// Calculate damage dealt from attacker to defender
	/// </summary>
	public static DamageResult CalculateDamage( Monster attacker, Monster defender )
	{
		var result = new DamageResult();

		// Damage formula using ratio (ATK/DEF) instead of subtraction
		// Base power of 50 for basic attacks, base +7 for low-level viability
		float levelFactor = (2f * attacker.Level / 5f) + 7f;
		float atkDefRatio = (float)attacker.ATK / Math.Max( 1, defender.DEF );
		float baseDamage = (levelFactor * 50f * atkDefRatio) / 20f + 2f;
		baseDamage = Math.Max( 1, baseDamage ); // Minimum 1 damage

		// Element effectiveness
		float elementModifier = GetElementModifier( attacker, defender );
		result.ElementModifier = elementModifier;
		baseDamage *= elementModifier;

		// Critical hit chance (base 5%)
		float critChance = 0.05f;
		float critBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.CritChanceBonus ) ?? 0;
		critChance += critBonus / 100f;

		// Add held item crit chance bonus
		float heldCritBonus = ItemManager.Instance?.GetHeldItemBonus( attacker, ItemEffectType.HeldCritChance ) ?? 0;
		critChance += heldCritBonus / 100f;

		if ( CurrentRandom.NextDouble() < critChance )
		{
			result.IsCritical = true;
			float critMultiplier = 1.5f;
			float critDamageBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.CritDamageBonus ) ?? 0;
			critMultiplier += critDamageBonus / 100f;

			// Add held item crit damage bonus
			float heldCritDamageBonus = ItemManager.Instance?.GetHeldItemBonus( attacker, ItemEffectType.HeldCritDamage ) ?? 0;
			critMultiplier += heldCritDamageBonus / 100f;

			baseDamage *= critMultiplier;
		}

		// Random variance (+/- 10%)
		float variance = 0.9f + (float)CurrentRandom.NextDouble() * 0.2f;
		baseDamage *= variance;

		// Apply skill damage bonus
		float damageBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.AllMonsterATKPercent ) ?? 0;
		baseDamage *= (1 + damageBonus / 100f);

		result.Damage = (int)Math.Max( 1, baseDamage );
		result.IsSuperEffective = elementModifier > 1.0f;
		result.IsResisted = elementModifier < 1.0f;

		return result;
	}

	/// <summary>
	/// Get element effectiveness modifier
	/// </summary>
	private static float GetElementModifier( Monster attacker, Monster defender )
	{
		var attackerSpecies = MonsterManager.Instance?.GetSpecies( attacker.SpeciesId );
		var defenderSpecies = MonsterManager.Instance?.GetSpecies( defender.SpeciesId );

		if ( attackerSpecies == null || defenderSpecies == null )
			return 1.0f;

		var attackElement = attackerSpecies.Element;
		var defendElement = defenderSpecies.Element;

		// Element advantage table
		// Fire > Wind, Wind > Earth, Earth > Water, Water > Fire
		// Light <> Dark (both super effective against each other)
		return (attackElement, defendElement) switch
		{
			// Classic elemental cycle
			(ElementType.Fire, ElementType.Nature) => 1.5f,
			(ElementType.Fire, ElementType.Ice) => 1.5f,
			(ElementType.Fire, ElementType.Water) => 0.5f,

			(ElementType.Water, ElementType.Fire) => 1.5f,
			(ElementType.Water, ElementType.Earth) => 0.5f,
			(ElementType.Water, ElementType.Electric) => 0.5f,

			(ElementType.Earth, ElementType.Water) => 1.5f,
			(ElementType.Earth, ElementType.Electric) => 1.5f,
			(ElementType.Earth, ElementType.Wind) => 0.5f,

			(ElementType.Wind, ElementType.Earth) => 1.5f,
			(ElementType.Wind, ElementType.Fire) => 0.5f,

			// New elements
			(ElementType.Electric, ElementType.Water) => 1.5f,
			(ElementType.Electric, ElementType.Wind) => 1.5f,
			(ElementType.Electric, ElementType.Earth) => 0.5f,

			(ElementType.Ice, ElementType.Wind) => 1.5f,
			(ElementType.Ice, ElementType.Nature) => 1.5f,
			(ElementType.Ice, ElementType.Fire) => 0.5f,

			(ElementType.Nature, ElementType.Water) => 1.5f,
			(ElementType.Nature, ElementType.Earth) => 1.5f,
			(ElementType.Nature, ElementType.Fire) => 0.5f,

			(ElementType.Metal, ElementType.Ice) => 1.5f,
			(ElementType.Metal, ElementType.Spirit) => 1.5f,
			(ElementType.Metal, ElementType.Fire) => 0.5f,

			// Shadow and Spirit counter each other
			(ElementType.Shadow, ElementType.Spirit) => 1.5f,
			(ElementType.Spirit, ElementType.Shadow) => 1.5f,

			_ => 1.0f
		};
	}

	/// <summary>
	/// Determine turn order for a battle
	/// </summary>
	public static List<Monster> GetTurnOrder( List<Monster> team1, List<Monster> team2 )
	{
		var allMonsters = team1.Concat( team2 ).ToList();

		// Sort by speed (descending), with random tiebreaker
		return allMonsters
			.Where( m => m.CurrentHP > 0 )
			.OrderByDescending( m => m.SPD )
			.ThenBy( _ => CurrentRandom.Next() )
			.ToList();
	}

	/// <summary>
	/// Get effective speed including trait bonuses
	/// </summary>
	public static int GetEffectiveSPD( Monster monster, BattleState state )
	{
		int baseSPD = monster.SPD;
		float multiplier = 1.0f;

		// Apply held item SPD bonus
		if ( ItemManager.Instance != null )
		{
			float spdBonus = ItemManager.Instance.GetHeldItemBonus( monster, ItemEffectType.HeldSPDBonus );
			if ( spdBonus > 0 )
				baseSPD = (int)(baseSPD * (1 + spdBonus / 100f));
		}

		foreach ( var traitId in monster.Traits ?? new List<string>() )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait?.Effects == null ) continue;

			foreach ( var effect in trait.Effects )
			{
				if ( effect.Type != TraitEffectType.SPDBonus ) continue;

				// Check condition
				bool conditionMet = effect.Condition switch
				{
					"has_status" => state?.HasAnyStatus( monster.Id ) ?? false,
					"per_turn" => true, // Momentum - handled separately via stacking
					_ => true
				};

				if ( conditionMet )
				{
					multiplier += effect.Value / 100f;
					Log.Info( $"[{trait.Name}] {monster.Nickname}'s SPD boosted by {effect.Value}%!" );
				}
			}
		}

		// Apply stat stage from battle state
		if ( state != null )
		{
			float stageMultiplier = BattleState.GetStatMultiplier( state.GetStatStage( monster.Id, StatIndex.SPD ) );
			multiplier *= stageMultiplier;
		}

		return (int)(baseSPD * multiplier);
	}

	/// <summary>
	/// Apply on-switch-out effects for a monster (e.g., Vital Recovery, Cleansing Retreat)
	/// Returns messages about what happened
	/// </summary>
	public static List<string> ApplyOnSwitchOutEffects( Monster monster, BattleState state )
	{
		var messages = new List<string>();

		foreach ( var traitId in monster.Traits ?? new List<string>() )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait?.Effects == null ) continue;

			foreach ( var effect in trait.Effects )
			{
				if ( effect.Condition != "on_switch" ) continue;

				switch ( effect.Type )
				{
					case TraitEffectType.HealingBonus:
						// Vital Recovery - heal on switch out
						int healAmount = (int)(monster.MaxHP * effect.Value / 100f);
						monster.CurrentHP = Math.Min( monster.MaxHP, monster.CurrentHP + healAmount );
						string healMsg = $"[{trait.Name}] {monster.Nickname} recovered {healAmount} HP when switching out!";
						Log.Info( healMsg );
						messages.Add( healMsg );
						break;

					case TraitEffectType.StatusResistance:
						// Cleansing Retreat - cure all status on switch out
						if ( state != null && state.HasAnyStatus( monster.Id ) )
						{
							state.ClearStatuses( monster.Id );
							string cureMsg = $"[{trait.Name}] {monster.Nickname}'s status conditions were cured when switching out!";
							Log.Info( cureMsg );
							messages.Add( cureMsg );
						}
						break;
				}
			}
		}

		return messages;
	}

	/// <summary>
	/// Get effective priority for a move including trait bonuses (e.g., Trickster)
	/// </summary>
	public static int GetEffectivePriority( Monster monster, MoveDefinition move )
	{
		if ( move == null ) return 0;

		int basePriority = move.Priority;

		foreach ( var traitId in monster.Traits ?? new List<string>() )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait?.Effects == null ) continue;

			foreach ( var effect in trait.Effects )
			{
				if ( effect.Type != TraitEffectType.PriorityBonus ) continue;

				// Check condition (e.g., "status_moves" for Trickster)
				bool conditionMet = effect.Condition switch
				{
					"status_moves" => move.Category == MoveCategory.Status,
					_ => true
				};

				if ( conditionMet )
				{
					basePriority += (int)effect.Value;
					Log.Info( $"[{trait.Name}] {monster.Nickname}'s {move.Name} priority increased by {effect.Value}!" );
				}
			}
		}

		return basePriority;
	}

	/// <summary>
	/// Apply damage to a monster
	/// </summary>
	public static void ApplyDamage( Monster target, int damage )
	{
		// Check for OHKO protection (Enduring Will trait)
		// If at full HP and damage would KO, survive with 1 HP instead
		bool wasFullHP = target.CurrentHP == target.MaxHP;
		bool wouldKO = damage >= target.CurrentHP;

		if ( wasFullHP && wouldKO && HasOHKOProtection( target ) )
		{
			target.CurrentHP = 1;
			Log.Info( $"[Enduring Will] {target.Nickname} survived a fatal blow with 1 HP!" );
			return;
		}

		target.CurrentHP = Math.Max( 0, target.CurrentHP - damage );
	}

	/// <summary>
	/// Apply contact damage from defender traits (e.g., Barbed Hide)
	/// Returns the damage dealt to the attacker and any messages
	/// </summary>
	public static (int damage, string message) ApplyContactDamage( Monster attacker, Monster defender, MoveDefinition move )
	{
		// Only contact/physical moves trigger contact damage
		if ( move == null || move.Category != MoveCategory.Physical )
			return (0, null);

		foreach ( var traitId in defender.Traits ?? new List<string>() )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait?.Effects == null ) continue;

			foreach ( var effect in trait.Effects )
			{
				if ( effect.Type == TraitEffectType.ContactDamage )
				{
					int contactDamage = (int)(attacker.MaxHP * effect.Value / 100f);
					attacker.CurrentHP = Math.Max( 0, attacker.CurrentHP - contactDamage );
					string message = $"[{trait.Name}] {attacker.Nickname} took {contactDamage} damage from {defender.Nickname}'s {trait.Name}!";
					Log.Info( message );
					return (contactDamage, message);
				}
			}
		}

		return (0, null);
	}

	/// <summary>
	/// Check if a monster has OHKO protection (e.g., Enduring Will trait)
	/// </summary>
	private static bool HasOHKOProtection( Monster monster )
	{
		if ( monster.Traits == null || monster.Traits.Count == 0 )
			return false;

		foreach ( var traitId in monster.Traits )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait?.Effects == null ) continue;

			foreach ( var effect in trait.Effects )
			{
				if ( effect.Condition == "ohko_protection" )
					return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Check if a team is defeated (all monsters at 0 HP)
	/// </summary>
	public static bool IsTeamDefeated( List<Monster> team )
	{
		return team.All( m => m.CurrentHP <= 0 );
	}

	/// <summary>
	/// Select the best target for an attacker
	/// </summary>
	public static Monster SelectTarget( Monster attacker, List<Monster> enemies )
	{
		var aliveEnemies = enemies.Where( e => e.CurrentHP > 0 ).ToList();
		if ( aliveEnemies.Count == 0 ) return null;

		// AI targeting: prefer low HP enemies, then element advantage
		var attackerSpecies = MonsterManager.Instance?.GetSpecies( attacker.SpeciesId );

		// First check for element advantage
		foreach ( var enemy in aliveEnemies )
		{
			var defenderSpecies = MonsterManager.Instance?.GetSpecies( enemy.SpeciesId );
			if ( defenderSpecies != null && attackerSpecies != null )
			{
				float modifier = GetElementModifier( attacker, enemy );
				if ( modifier > 1.0f )
				{
					return enemy;
				}
			}
		}

		// Otherwise target lowest HP
		return aliveEnemies.OrderBy( e => e.CurrentHP ).First();
	}

	/// <summary>
	/// Calculate XP gained from defeating a monster
	/// </summary>
	public static int CalculateXPGain( Monster defeated, int winnerLevel )
	{
		var species = MonsterManager.Instance?.GetSpecies( defeated.SpeciesId );
		if ( species == null ) return 10;

		// Base XP from level and rarity
		int baseXP = defeated.Level * 3;
		float rarityMultiplier = species.BaseRarity switch
		{
			Rarity.Uncommon => 1.25f,
			Rarity.Rare => 1.5f,
			Rarity.Epic => 2.0f,
			Rarity.Legendary => 3.0f,
			Rarity.Mythic => 5.0f,
			_ => 1.0f
		};

		// Level difference bonus/penalty
		int levelDiff = defeated.Level - winnerLevel;
		float levelModifier = 1.0f + (levelDiff * 0.1f);
		levelModifier = Math.Clamp( levelModifier, 0.5f, 2.0f );

		// Apply skill bonus
		float xpBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.XPGainBonus ) ?? 0;

		// Apply beast XP boost from shop
		float beastXPBoost = ShopManager.Instance?.GetBoostMultiplier( ShopItemType.BeastXPBoost ) ?? 1.0f;

		// Apply relic bonus
		float relicXPBonus = ItemManager.Instance?.GetRelicBonus( ItemEffectType.PassiveXPGain ) ?? 0;

		return (int)(baseXP * rarityMultiplier * levelModifier * (1 + xpBonus / 100f) * beastXPBoost * (1 + relicXPBonus / 100f));
	}

	/// <summary>
	/// Calculate gold earned from defeating a monster
	/// </summary>
	public static int CalculateGoldDrop( Monster defeated )
	{
		var species = MonsterManager.Instance?.GetSpecies( defeated.SpeciesId );
		if ( species == null ) return 5;

		int baseGold = 3 + (int)(defeated.Level * 1.3f);
		float rarityMultiplier = species.BaseRarity switch
		{
			Rarity.Uncommon => 1.25f,
			Rarity.Rare => 1.5f,
			Rarity.Epic => 2.0f,
			Rarity.Legendary => 3.0f,
			Rarity.Mythic => 5.0f,
			_ => 1.0f
		};

		// Random variance
		float variance = 0.8f + (float)CurrentRandom.NextDouble() * 0.4f;

		// Apply skill bonus (GoldDropBonus from Fortune branch)
		float goldBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.GoldDropBonus ) ?? 0;

		// Apply gold boost from shop
		float goldBoost = ShopManager.Instance?.GetBoostMultiplier( ShopItemType.GoldBoost ) ?? 1.0f;

		// Apply relic bonus
		float relicGoldBonus = ItemManager.Instance?.GetRelicBonus( ItemEffectType.PassiveGoldFind ) ?? 0;

		int gold = (int)(baseGold * rarityMultiplier * variance * (1 + goldBonus / 100f) * goldBoost * (1 + relicGoldBonus / 100f));

		// Check for double drop chance (Jackpot skill)
		float doubleChance = TamerManager.Instance?.GetSkillBonus( SkillEffectType.DoubleDropChance ) ?? 0;
		if ( doubleChance > 0 && CurrentRandom.NextDouble() < (doubleChance / 100f) )
		{
			gold *= 2;
		}

		return gold;
	}

	/// <summary>
	/// Simulate an entire battle between two teams
	/// </summary>
	public static BattleResult SimulateBattle( List<Monster> playerTeam, List<Monster> enemyTeam, int? seed = null )
	{
		Log.Info( $"[BattleSimulator] SimulateBattle: playerTeam={playerTeam?.Count ?? 0}, enemyTeam={enemyTeam?.Count ?? 0}, seed={seed}" );

		// Set seed if provided (for deterministic online battles)
		if ( seed.HasValue )
		{
			SetSeed( seed.Value );
		}

		var result = new BattleResult();
		result.Turns = new List<BattleTurn>();

		// Clone monsters so we don't modify originals during simulation
		Log.Info( "[BattleSimulator] Cloning player team..." );
		var players = new List<Monster>();
		foreach ( var m in playerTeam )
		{
			if ( m == null )
			{
				Log.Warning( "[BattleSimulator] Found null monster in player team!" );
				continue;
			}
			Log.Info( $"[BattleSimulator] Cloning player monster: {m.Nickname ?? "unnamed"} (HP={m.MaxHP}, ATK={m.ATK})" );
			players.Add( m.Clone() );
		}

		Log.Info( "[BattleSimulator] Cloning enemy team..." );
		var enemies = new List<Monster>();
		foreach ( var m in enemyTeam )
		{
			if ( m == null )
			{
				Log.Warning( "[BattleSimulator] Found null monster in enemy team!" );
				continue;
			}
			Log.Info( $"[BattleSimulator] Cloning enemy monster: {m.Nickname ?? "unnamed"} (HP={m.MaxHP}, ATK={m.ATK})" );
			enemies.Add( m.Clone() );
		}

		int turnNumber = 0;
		const int maxTurns = 100; // Prevent infinite loops

		while ( !IsTeamDefeated( players ) && !IsTeamDefeated( enemies ) && turnNumber < maxTurns )
		{
			turnNumber++;
			var turnOrder = GetTurnOrder( players, enemies );

			foreach ( var attacker in turnOrder )
			{
				if ( attacker.CurrentHP <= 0 ) continue;
				if ( IsTeamDefeated( players ) || IsTeamDefeated( enemies ) ) break;

				// Determine which team the attacker is on
				bool isPlayer = players.Contains( attacker );
				var targetTeam = isPlayer ? enemies : players;

				var target = SelectTarget( attacker, targetTeam );
				if ( target == null ) continue;

				var damageResult = CalculateDamage( attacker, target );
				ApplyDamage( target, damageResult.Damage );

				var turn = new BattleTurn
				{
					TurnNumber = turnNumber,
					AttackerId = attacker.Id,
					AttackerName = attacker.Nickname,
					DefenderId = target.Id,
					DefenderName = target.Nickname,
					Damage = damageResult.Damage,
					IsCritical = damageResult.IsCritical,
					IsSuperEffective = damageResult.IsSuperEffective,
					IsResisted = damageResult.IsResisted,
					DefenderHPAfter = target.CurrentHP,
					IsPlayerAttacker = isPlayer
				};

				result.Turns.Add( turn );
			}
		}

		result.PlayerWon = !IsTeamDefeated( players );
		result.TotalTurns = turnNumber;

		// Calculate rewards
		if ( result.PlayerWon && players.Count > 0 && enemies.Count > 0 )
		{
			int avgLevel = (int)players.Average( p => p.Level );
			foreach ( var enemy in enemies )
			{
				if ( enemy == null ) continue;
				result.TotalXP += CalculateXPGain( enemy, avgLevel );
				result.TotalGold += CalculateGoldDrop( enemy );
			}

			// Calculate item drops if we're in an expedition
			var expedition = ExpeditionManager.Instance?.CurrentExpedition;
			if ( expedition != null && ItemManager.Instance != null )
			{
				bool isBoss = ExpeditionManager.Instance.IsBossWave;
				bool isRareBoss = ExpeditionManager.Instance.IsRareBossEncounter;

				foreach ( var enemy in enemies )
				{
					if ( enemy == null ) continue;
					var drops = ItemManager.Instance.CalculateDrop(
						enemy,
						expedition.Id,
						expedition.Element,
						expedition.BaseEnemyLevel,
						isBoss,
						isRareBoss
					);
					result.ItemDrops.AddRange( drops );
				}
			}
		}

		// Clear seed after simulation
		if ( seed.HasValue )
		{
			ClearSeed();
		}

		return result;
	}

	// ============================================
	// NEW MOVE-BASED BATTLE SYSTEM
	// ============================================

	/// <summary>
	/// Calculate damage using a specific move (new system)
	/// </summary>
	public static DamageResult CalculateDamage( Monster attacker, Monster defender, MoveDefinition move, BattleState state )
	{
		var result = new DamageResult();
		result.MoveName = move.Name;
		result.MoveId = move.Id;
		result.MoveElement = move.Element;
		result.MoveCategory = move.Category;

		// Status moves don't deal direct damage
		if ( move.Category == MoveCategory.Status )
		{
			result.Damage = 0;
			return result;
		}

		// Get attacker and defender species
		var attackerSpecies = MonsterManager.Instance?.GetSpecies( attacker.SpeciesId );
		var defenderSpecies = MonsterManager.Instance?.GetSpecies( defender.SpeciesId );

		// Choose attack and defense stats based on move category
		int atkStat = move.Category == MoveCategory.Physical ? attacker.ATK : attacker.SpA;
		int defStat = move.Category == MoveCategory.Physical ? defender.DEF : defender.SpD;

		// Apply held item bonuses
		if ( ItemManager.Instance != null )
		{
			// Attacker's held item ATK/SpA bonus
			if ( move.Category == MoveCategory.Physical )
			{
				float atkBonus = ItemManager.Instance.GetHeldItemBonus( attacker, ItemEffectType.HeldATKBonus );
				if ( atkBonus > 0 )
					atkStat = (int)(atkStat * (1 + atkBonus / 100f));
			}
			else
			{
				float spaBonus = ItemManager.Instance.GetHeldItemBonus( attacker, ItemEffectType.HeldSpABonus );
				if ( spaBonus > 0 )
					atkStat = (int)(atkStat * (1 + spaBonus / 100f));
			}

			// Defender's held item DEF/SpD bonus
			if ( move.Category == MoveCategory.Physical )
			{
				float defBonus = ItemManager.Instance.GetHeldItemBonus( defender, ItemEffectType.HeldDEFBonus );
				if ( defBonus > 0 )
					defStat = (int)(defStat * (1 + defBonus / 100f));
			}
			else
			{
				float spdBonus = ItemManager.Instance.GetHeldItemBonus( defender, ItemEffectType.HeldSpDBonus );
				if ( spdBonus > 0 )
					defStat = (int)(defStat * (1 + spdBonus / 100f));
			}

			// Attacker's damage taken modifier (Glass Cannon effect - increases damage taken)
			float damageTakenMod = ItemManager.Instance.GetHeldItemBonus( defender, ItemEffectType.HeldDamageTaken );
			if ( damageTakenMod != 0 )
			{
				// This will be applied to final damage later if needed, or reduce defense
				defStat = (int)(defStat * (1 - damageTakenMod / 100f));
			}
		}

		// Apply stat stages from battle state
		if ( state != null )
		{
			StatIndex atkIndex = move.Category == MoveCategory.Physical ? StatIndex.ATK : StatIndex.SpA;
			StatIndex defIndex = move.Category == MoveCategory.Physical ? StatIndex.DEF : StatIndex.SpD;

			float atkMultiplier = BattleState.GetStatMultiplier( state.GetStatStage( attacker.Id, atkIndex ) );
			float defMultiplier = BattleState.GetStatMultiplier( state.GetStatStage( defender.Id, defIndex ) );

			atkStat = (int)(atkStat * atkMultiplier);
			defStat = (int)(defStat * defMultiplier);

			// Apply burn ATK reduction for physical moves
			if ( move.Category == MoveCategory.Physical && state.HasStatus( attacker.Id, StatusCondition.Burn ) )
			{
				atkStat = (int)(atkStat * 0.5f);
			}
		}

		// Apply defender's DEFBonus/SpDBonusBattle traits (e.g., Wild Harden, Hardened Resolve)
		foreach ( var traitId in defender.Traits ?? new List<string>() )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait?.Effects == null ) continue;

			foreach ( var effect in trait.Effects )
			{
				bool conditionMet = CheckDefenderTraitCondition( effect.Condition, defender, state );
				if ( !conditionMet ) continue;

				if ( effect.Type == TraitEffectType.DEFBonus && move.Category == MoveCategory.Physical )
				{
					float bonus = effect.Value / 100f;
					defStat = (int)(defStat * (1 + bonus));
					Log.Info( $"[{trait.Name}] {defender.Nickname}'s DEF increased by {effect.Value}%!" );
				}
				else if ( effect.Type == TraitEffectType.SpDBonusBattle && move.Category == MoveCategory.Special )
				{
					float bonus = effect.Value / 100f;
					defStat = (int)(defStat * (1 + bonus));
					Log.Info( $"[{trait.Name}] {defender.Nickname}'s SpD increased by {effect.Value}%!" );
				}
			}
		}

		// Damage formula: ((2*Level/5+7) * Power * ATK/DEF) / 20 + 2
		// Uses ratio (ATK/DEF) so high DEF reduces but doesn't nullify damage
		// Base of +7 ensures low-level monsters deal meaningful damage
		float levelFactor = (2f * attacker.Level / 5f) + 7f;
		float atkDefRatio = (float)atkStat / Math.Max( 1, defStat );
		float baseDamage = (levelFactor * move.BasePower * atkDefRatio) / 20f + 2f;
		baseDamage = Math.Max( 1, baseDamage );

		// STAB (Same Type Attack Bonus) - 50% boost if move type matches monster type
		if ( attackerSpecies != null && move.Element == attackerSpecies.Element )
		{
			result.HasSTAB = true;
			baseDamage *= 1.5f;
		}

		// Type effectiveness (use move's element, not monster's)
		float typeMultiplier = BattleAI.GetTypeEffectiveness( move.Element, defenderSpecies?.Element ?? ElementType.Neutral );
		result.ElementModifier = typeMultiplier;
		result.IsSuperEffective = typeMultiplier >= 1.5f;
		result.IsResisted = typeMultiplier < 1.0f && typeMultiplier > 0f;
		result.IsImmune = typeMultiplier == 0f;

		if ( typeMultiplier == 0f )
		{
			result.Damage = 0;
			return result;
		}

		// Check defender traits for ElementImmunity (e.g., Skyborne vs Earth)
		foreach ( var traitId in defender.Traits ?? new List<string>() )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait?.Effects == null ) continue;

			foreach ( var effect in trait.Effects )
			{
				if ( effect.Type == TraitEffectType.ElementImmunity && effect.AffectedElement == move.Element )
				{
					result.Damage = 0;
					result.IsImmune = true;
					Log.Info( $"[{trait.Name}] {defender.Nickname} is immune to {move.Element} moves!" );
					return result;
				}

				if ( effect.Type == TraitEffectType.ElementAbsorption && effect.AffectedElement == move.Element )
				{
					// Heal instead of damage
					int healAmount = (int)(defender.MaxHP * 0.25f);
					defender.CurrentHP = Math.Min( defender.MaxHP, defender.CurrentHP + healAmount );
					result.Damage = 0;
					result.WasAbsorbed = true;
					Log.Info( $"[{trait.Name}] {defender.Nickname} absorbed {move.Element} and healed {healAmount} HP!" );
					return result;
				}
			}
		}

		baseDamage *= typeMultiplier;

		// Check defender traits for ElementResistance (e.g., Thermal Hide vs Fire/Ice)
		foreach ( var traitId in defender.Traits ?? new List<string>() )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait?.Effects == null ) continue;

			foreach ( var effect in trait.Effects )
			{
				if ( effect.Type == TraitEffectType.ElementResistance && effect.AffectedElement == move.Element )
				{
					float reduction = effect.Value / 100f;
					baseDamage *= (1 - reduction);
					Log.Info( $"[{trait.Name}] {defender.Nickname} resists {move.Element} by {effect.Value}%!" );
				}
			}
		}

		// Apply trait bonuses
		baseDamage *= GetTraitDamageMultiplier( attacker, defender, move, state );

		// Critical hit calculation
		float critChance = 0.0625f; // Base 6.25%
		float critBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.CritChanceBonus ) ?? 0;
		critChance += critBonus / 100f;

		// CritBoost effect on move
		if ( move.Effects?.Exists( e => e.Type == MoveEffectType.CritBoost ) == true )
		{
			critChance += 0.125f; // +12.5%
		}

		// CritBonus from traits (e.g., Fortunate Strike)
		foreach ( var traitId in attacker.Traits ?? new List<string>() )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait?.Effects != null )
			{
				foreach ( var effect in trait.Effects )
				{
					if ( effect.Type == TraitEffectType.CritBonus )
					{
						critChance += effect.Value / 100f;
						Log.Info( $"[{trait.Name}] {attacker.Nickname}'s crit chance increased by {effect.Value}%" );
					}
				}
			}
		}

		// Held item crit chance bonus
		float heldCritBonus = ItemManager.Instance?.GetHeldItemBonus( attacker, ItemEffectType.HeldCritChance ) ?? 0;
		critChance += heldCritBonus / 100f;

		// Relic crit chance bonus
		float relicCritBonus = ItemManager.Instance?.GetRelicBonus( ItemEffectType.PassiveCritRate ) ?? 0;
		critChance += relicCritBonus / 100f;

		if ( CurrentRandom.NextDouble() < critChance )
		{
			result.IsCritical = true;
			float critMultiplier = 1.5f;

			// Apply skill tree crit damage bonus
			float skillCritBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.CritDamageBonus ) ?? 0;
			critMultiplier += skillCritBonus / 100f;

			// Held item crit damage bonus
			float heldCritDamageBonus = ItemManager.Instance?.GetHeldItemBonus( attacker, ItemEffectType.HeldCritDamage ) ?? 0;
			critMultiplier += heldCritDamageBonus / 100f;

			// Check for precision_hunter trait (increased crit damage)
			foreach ( var traitId in attacker.Traits ?? new List<string>() )
			{
				var trait = TraitDatabase.GetTrait( traitId );
				if ( trait?.Effects != null )
				{
					foreach ( var effect in trait.Effects )
					{
						if ( effect.Type == TraitEffectType.CritDamageBonus )
						{
							critMultiplier += effect.Value / 100f;
							Log.Info( $"[{trait.Name}] {attacker.Nickname}'s critical hit deals +{effect.Value}% damage!" );
						}
					}
				}
			}

			baseDamage *= critMultiplier;
		}

		// Random variance (+/- 10%)
		float variance = 0.9f + (float)CurrentRandom.NextDouble() * 0.2f;
		baseDamage *= variance;

		// Apply tamer skill bonuses
		float damageBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.AllMonsterATKPercent ) ?? 0;
		baseDamage *= (1 + damageBonus / 100f);

		// Apply boss damage bonuses if defender is a boss
		if ( defender.IsBoss )
		{
			// Base boss damage bonus
			float bossDmgBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.BossDamageBonus ) ?? 0;
			baseDamage *= (1 + bossDmgBonus / 100f);

			// Check for boss tier-specific bonuses
			var bossState = BattleManager.Instance?.CurrentBossState ?? ExpeditionManager.Instance?.CurrentBossState;
			if ( bossState?.BossData != null )
			{
				var bossTier = bossState.BossData.Tier;

				// Higher tier damage bonus (Giant Killer) - applies to Elite+ bosses
				if ( bossTier >= BossTier.Elite )
				{
					float tierBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.HigherTierDamageBonus ) ?? 0;
					baseDamage *= (1 + tierBonus / 100f);
				}

				// Mythic damage bonus (Mythbreaker) - applies only to Mythic bosses
				if ( bossTier == BossTier.Mythic )
				{
					float mythicBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.MythicDamageBonus ) ?? 0;
					baseDamage *= (1 + mythicBonus / 100f);
				}

				// Phase damage bonus - applies during phase transitions (when boss just transitioned)
				if ( bossState.JustTransitioned )
				{
					float phaseBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.PhaseDamageBonus ) ?? 0;
					baseDamage *= (1 + phaseBonus / 100f);
				}
			}
		}

		// Apply boss damage reduction if attacker is a boss (player takes less damage from bosses)
		if ( attacker.IsBoss )
		{
			float bossReduction = TamerManager.Instance?.GetSkillBonus( SkillEffectType.BossDamageReduction ) ?? 0;
			baseDamage *= (1 - bossReduction / 100f);
		}

		// Shield effect reduces damage
		if ( state?.HasEffect( defender.Id, MoveEffectType.Shield ) == true )
		{
			float shieldValue = state.GetEffectValue( defender.Id, MoveEffectType.Shield );
			baseDamage *= (1 - shieldValue);
		}

		result.Damage = (int)Math.Max( 1, baseDamage );
		return result;
	}

	/// <summary>
	/// Get damage multiplier from traits
	/// </summary>
	private static float GetTraitDamageMultiplier( Monster attacker, Monster defender, MoveDefinition move, BattleState state )
	{
		float multiplier = 1.0f;
		var attackerSpecies = MonsterManager.Instance?.GetSpecies( attacker.SpeciesId );

		foreach ( var traitId in attacker.Traits ?? new List<string>() )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait?.Effects == null ) continue;

			foreach ( var effect in trait.Effects )
			{
				bool conditionMet = CheckTraitCondition( effect.Condition, attacker, defender, move, state );
				if ( !conditionMet ) continue;

				switch ( effect.Type )
				{
					case TraitEffectType.DamageBonus:
						multiplier *= (1 + effect.Value / 100f);
						Log.Info( $"[{trait.Name}] {attacker.Nickname}'s damage increased by {effect.Value}%!" );
						break;

					case TraitEffectType.ElementDamageBonus:
						if ( effect.AffectedElement == move.Element )
						{
							multiplier *= (1 + effect.Value / 100f);
							Log.Info( $"[{trait.Name}] {attacker.Nickname}'s {move.Element} damage increased by {effect.Value}%!" );
						}
						break;

					case TraitEffectType.ATKBonus:
						// Check for on_ko condition (Bloodlust)
						if ( effect.Condition == "on_ko" )
						{
							int koCount = state?.GetKOCount( attacker.Id ) ?? 0;
							if ( koCount > 0 )
							{
								float koBonus = effect.Value * koCount / 100f;
								multiplier *= (1 + koBonus);
								Log.Info( $"[{trait.Name}] {attacker.Nickname}'s ATK boosted by {effect.Value * koCount}% ({koCount} KOs)!" );
							}
						}
						else if ( move.Category == MoveCategory.Physical )
						{
							multiplier *= (1 + effect.Value / 100f);
							Log.Info( $"[{trait.Name}] {attacker.Nickname}'s ATK increased by {effect.Value}%!" );
						}
						break;

					case TraitEffectType.SpABonus:
						if ( move.Category == MoveCategory.Special )
						{
							multiplier *= (1 + effect.Value / 100f);
							Log.Info( $"[{trait.Name}] {attacker.Nickname}'s SpA increased by {effect.Value}%!" );
						}
						break;

					case TraitEffectType.LowHPATKBonus:
						if ( move.Category == MoveCategory.Physical && attacker.HPPercent < 0.33f )
						{
							multiplier *= (1 + effect.Value / 100f);
							Log.Info( $"[{trait.Name}] {attacker.Nickname}'s ATK boosted by {effect.Value}% (low HP)!" );
						}
						break;

					case TraitEffectType.LowHPSpABonus:
						if ( move.Category == MoveCategory.Special && attacker.HPPercent < 0.33f )
						{
							multiplier *= (1 + effect.Value / 100f);
							Log.Info( $"[{trait.Name}] {attacker.Nickname}'s SpA boosted by {effect.Value}% (low HP)!" );
						}
						break;
				}
			}
		}

		// Check defender's traits for Menacing Aura (reduces attacker's damage)
		foreach ( var traitId in defender.Traits ?? new List<string>() )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait?.Effects == null ) continue;

			foreach ( var effect in trait.Effects )
			{
				// Menacing Aura: ATKBonus with "on_enemy" condition means reduce enemy ATK
				if ( effect.Type == TraitEffectType.ATKBonus && effect.Condition == "on_enemy" )
				{
					// Negative value means reduction
					float reduction = effect.Value / 100f; // e.g., -20 -> -0.2
					multiplier *= (1 + reduction); // multiplier * 0.8 = 20% reduction
					Log.Info( $"[{trait.Name}] {attacker.Nickname}'s damage reduced by {-effect.Value}% due to {defender.Nickname}'s {trait.Name}!" );
				}
			}
		}

		return multiplier;
	}

	/// <summary>
	/// Check if a trait condition is met
	/// </summary>
	private static bool CheckTraitCondition( string condition, Monster attacker, Monster defender, MoveDefinition move, BattleState state )
	{
		if ( string.IsNullOrEmpty( condition ) )
			return true;

		return condition switch
		{
			"below_33_hp" => attacker.HPPercent < 0.33f,
			"above_80_hp" => attacker.HPPercent > 0.80f,
			"first_turn" => state?.IsFirstTurn ?? false,
			"has_status" => state?.HasAnyStatus( attacker.Id ) ?? false,
			"same_type" => MonsterManager.Instance?.GetSpecies( attacker.SpeciesId )?.Element == move.Element,
			"base_power_60_or_less" => move.BasePower <= 60,
			"has_secondary_effect" => move.Effects?.Count > 0,
			_ => true
		};
	}

	/// <summary>
	/// Check if a defender's trait condition is met
	/// </summary>
	private static bool CheckDefenderTraitCondition( string condition, Monster defender, BattleState state )
	{
		if ( string.IsNullOrEmpty( condition ) )
			return true;

		return condition switch
		{
			"below_33_hp" => defender.HPPercent < 0.33f,
			"above_80_hp" => defender.HPPercent > 0.80f,
			"has_status" => state?.HasAnyStatus( defender.Id ) ?? false,
			_ => true
		};
	}

	/// <summary>
	/// Apply the effects of a move (status, stat changes, etc.)
	/// Returns list of effect messages for UI
	/// </summary>
	public static List<string> ApplyMoveEffects( Monster attacker, Monster defender, MoveDefinition move, DamageResult damageResult, BattleState state )
	{
		var messages = new List<string>();

		if ( move.Effects == null || move.Effects.Count == 0 )
			return messages;

		foreach ( var effect in move.Effects )
		{
			// Check if effect applies (based on chance)
			if ( effect.Chance < 1.0f && CurrentRandom.NextDouble() > effect.Chance )
				continue;

			Monster target = effect.TargetsSelf ? attacker : defender;

			switch ( effect.Type )
			{
				// Status conditions
				case MoveEffectType.Burn:
					if ( state.AddStatus( target.Id, StatusCondition.Burn, effect.Duration ) )
						messages.Add( $"{target.Nickname} was burned!" );
					break;

				case MoveEffectType.Freeze:
					if ( state.AddStatus( target.Id, StatusCondition.Freeze, effect.Duration ) )
						messages.Add( $"{target.Nickname} was frozen solid!" );
					break;

				case MoveEffectType.Paralyze:
					if ( state.AddStatus( target.Id, StatusCondition.Paralyze, effect.Duration ) )
						messages.Add( $"{target.Nickname} is paralyzed!" );
					break;

				case MoveEffectType.Poison:
					if ( state.AddStatus( target.Id, StatusCondition.Poison, effect.Duration ) )
						messages.Add( $"{target.Nickname} was poisoned!" );
					break;

				case MoveEffectType.Sleep:
					if ( state.AddStatus( target.Id, StatusCondition.Sleep, effect.Duration > 0 ? effect.Duration : CurrentRandom.Next( 1, 4 ) ) )
						messages.Add( $"{target.Nickname} fell asleep!" );
					break;

				case MoveEffectType.Confuse:
					if ( state.AddStatus( target.Id, StatusCondition.Confuse, effect.Duration > 0 ? effect.Duration : CurrentRandom.Next( 2, 6 ) ) )
						messages.Add( $"{target.Nickname} became confused!" );
					break;

				// Stat modifications
				case MoveEffectType.RaiseATK:
				case MoveEffectType.RaiseDEF:
				case MoveEffectType.RaiseSpA:
				case MoveEffectType.RaiseSpD:
				case MoveEffectType.RaiseSPD:
				case MoveEffectType.RaiseAccuracy:
				case MoveEffectType.RaiseEvasion:
					messages.Add( ApplyStatChange( target, effect.Type, (int)effect.Value, state, true ) );
					break;

				case MoveEffectType.LowerATK:
				case MoveEffectType.LowerDEF:
				case MoveEffectType.LowerSpA:
				case MoveEffectType.LowerSpD:
				case MoveEffectType.LowerSPD:
				case MoveEffectType.LowerAccuracy:
				case MoveEffectType.LowerEvasion:
					messages.Add( ApplyStatChange( target, effect.Type, (int)effect.Value, state, false ) );
					break;

				// Healing
				case MoveEffectType.Heal:
					float healingBoost = 1 + (ItemManager.Instance?.GetRelicBonus( ItemEffectType.PassiveHealingBoost ) ?? 0) / 100f;
					int healAmount = (int)(target.MaxHP * effect.Value * healingBoost);
					int actualHeal = Math.Min( healAmount, target.MaxHP - target.CurrentHP );
					target.CurrentHP = Math.Min( target.CurrentHP + healAmount, target.MaxHP );
					if ( actualHeal > 0 )
						messages.Add( $"{target.Nickname} recovered {actualHeal} HP!" );
					break;

				case MoveEffectType.FullHeal:
					int fullHealAmount = target.MaxHP - target.CurrentHP;
					target.CurrentHP = target.MaxHP;
					if ( fullHealAmount > 0 )
						messages.Add( $"{target.Nickname} fully recovered!" );
					break;

				// Recoil
				case MoveEffectType.Recoil:
					if ( damageResult.Damage > 0 )
					{
						int recoilDamage = (int)(damageResult.Damage * effect.Value);

						// Check for reckless_charge trait (recoil reduction)
						foreach ( var traitId in attacker.Traits ?? new List<string>() )
						{
							var trait = TraitDatabase.GetTrait( traitId );
							if ( trait?.Effects?.Exists( e => e.Type == TraitEffectType.RecoilReduction ) == true )
							{
								var reductionEffect = trait.Effects.Find( e => e.Type == TraitEffectType.RecoilReduction );
								recoilDamage = (int)(recoilDamage * (1 - reductionEffect.Value / 100f));
							}
						}

						if ( recoilDamage > 0 )
						{
							attacker.CurrentHP = Math.Max( 0, attacker.CurrentHP - recoilDamage );
							messages.Add( $"{attacker.Nickname} took {recoilDamage} recoil damage!" );
						}
					}
					break;

				// Drain
				case MoveEffectType.Drain:
					if ( damageResult.Damage > 0 )
					{
						int drainAmount = (int)(damageResult.Damage * effect.Value);
						attacker.CurrentHP = Math.Min( attacker.CurrentHP + drainAmount, attacker.MaxHP );
						messages.Add( $"{attacker.Nickname} drained {drainAmount} HP!" );
					}
					break;

				// Shield
				case MoveEffectType.Shield:
					state.AddEffect( target.Id, MoveEffectType.Shield, effect.Value, effect.Duration > 0 ? effect.Duration : 3 );
					messages.Add( $"{target.Nickname} is protected by a shield!" );
					break;

				// Flinch (target loses next turn if hit first)
				case MoveEffectType.Flinch:
					// Mark in state that target should flinch
					state.AddEffect( target.Id, MoveEffectType.Flinch, 1, 1 );
					messages.Add( $"{target.Nickname} flinched!" );
					break;

				// Guard
				case MoveEffectType.Guard:
					state.AddEffect( target.Id, MoveEffectType.Guard, 1, 1 );
					messages.Add( $"{target.Nickname} is bracing for impact!" );
					break;

				// Cleanse
				case MoveEffectType.Cleanse:
					state.ClearStatuses( target.Id );
					messages.Add( $"{target.Nickname} was cleansed of all ailments!" );
					break;
			}
		}

		return messages;
	}

	/// <summary>
	/// Apply a stat change and return a message
	/// </summary>
	private static string ApplyStatChange( Monster target, MoveEffectType effectType, int stages, BattleState state, bool isRaise )
	{
		StatIndex statIndex = effectType switch
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

		string statName = statIndex switch
		{
			StatIndex.ATK => "Attack",
			StatIndex.DEF => "Defense",
			StatIndex.SpA => "Sp. Attack",
			StatIndex.SpD => "Sp. Defense",
			StatIndex.SPD => "Speed",
			StatIndex.Accuracy => "accuracy",
			StatIndex.Evasion => "evasion",
			_ => "stat"
		};

		int change = isRaise ? stages : -stages;
		int actualChange = state.ModifyStatStage( target.Id, statIndex, change );

		if ( actualChange == 0 )
		{
			return isRaise
				? $"{target.Nickname}'s {statName} won't go any higher!"
				: $"{target.Nickname}'s {statName} won't go any lower!";
		}

		string changeDesc = Math.Abs( actualChange ) switch
		{
			1 => isRaise ? "rose" : "fell",
			2 => isRaise ? "rose sharply" : "harshly fell",
			_ => isRaise ? "rose drastically" : "severely fell"
		};

		return $"{target.Nickname}'s {statName} {changeDesc}!";
	}

	/// <summary>
	/// Process status damage at end of turn (burn, poison)
	/// </summary>
	public static List<string> ProcessStatusDamage( Monster monster, BattleState state )
	{
		var messages = new List<string>();

		foreach ( var status in state.GetStatuses( monster.Id ) )
		{
			switch ( status.Condition )
			{
				case StatusCondition.Burn:
					int burnDamage = Math.Max( 1, monster.MaxHP / 16 );
					monster.CurrentHP = Math.Max( 0, monster.CurrentHP - burnDamage );
					messages.Add( $"{monster.Nickname} is hurt by its burn! (-{burnDamage} HP)" );
					break;

				case StatusCondition.Poison:
					int poisonDamage = Math.Max( 1, monster.MaxHP / 8 );
					monster.CurrentHP = Math.Max( 0, monster.CurrentHP - poisonDamage );
					messages.Add( $"{monster.Nickname} is hurt by poison! (-{poisonDamage} HP)" );
					break;
			}
		}

		return messages;
	}

	/// <summary>
	/// Check if a monster can act this turn (sleep, paralysis, freeze, confusion)
	/// </summary>
	public static (bool canAct, string message) CheckCanAct( Monster monster, BattleState state )
	{
		var statuses = state.GetStatuses( monster.Id );

		// Check for flinch
		if ( state.HasEffect( monster.Id, MoveEffectType.Flinch ) )
		{
			return (false, $"{monster.Nickname} flinched and couldn't move!");
		}

		foreach ( var status in statuses )
		{
			switch ( status.Condition )
			{
				case StatusCondition.Sleep:
					// Chance to wake up each turn
					if ( status.TurnsRemaining <= 0 || CurrentRandom.NextDouble() < 0.33f )
					{
						state.RemoveStatus( monster.Id, StatusCondition.Sleep );
						return (true, $"{monster.Nickname} woke up!");
					}
					return (false, $"{monster.Nickname} is fast asleep!");

				case StatusCondition.Freeze:
					// 20% chance to thaw each turn
					if ( CurrentRandom.NextDouble() < 0.2f )
					{
						state.RemoveStatus( monster.Id, StatusCondition.Freeze );
						return (true, $"{monster.Nickname} thawed out!");
					}
					return (false, $"{monster.Nickname} is frozen solid!");

				case StatusCondition.Paralyze:
					// 25% chance to be fully paralyzed
					if ( CurrentRandom.NextDouble() < 0.25f )
					{
						return (false, $"{monster.Nickname} is paralyzed and can't move!");
					}
					break;

				case StatusCondition.Confuse:
					// 33% chance to hurt self
					if ( CurrentRandom.NextDouble() < 0.33f )
					{
						int confusionDamage = Math.Max( 1, monster.ATK / 4 );
						monster.CurrentHP = Math.Max( 0, monster.CurrentHP - confusionDamage );
						return (false, $"{monster.Nickname} hurt itself in confusion! (-{confusionDamage} HP)");
					}
					// Chance to snap out
					if ( status.TurnsRemaining <= 0 || CurrentRandom.NextDouble() < 0.25f )
					{
						state.RemoveStatus( monster.Id, StatusCondition.Confuse );
						return (true, $"{monster.Nickname} snapped out of confusion!");
					}
					break;
			}
		}

		return (true, null);
	}

	/// <summary>
	/// Check if an attack hits based on accuracy and evasion
	/// </summary>
	public static bool CheckAccuracy( Monster attacker, Monster defender, MoveDefinition move, BattleState state )
	{
		if ( move.Accuracy >= 100 )
			return true;

		float baseAccuracy = move.Accuracy / 100f;

		// Apply accuracy/evasion stages
		int accStage = state?.GetStatStage( attacker.Id, StatIndex.Accuracy ) ?? 0;
		int evaStage = state?.GetStatStage( defender.Id, StatIndex.Evasion ) ?? 0;

		float accMultiplier = BattleState.GetAccuracyMultiplier( accStage );
		float evaMultiplier = BattleState.GetAccuracyMultiplier( evaStage );

		float hitChance = baseAccuracy * accMultiplier / evaMultiplier;

		// Check for trait accuracy bonuses
		foreach ( var traitId in attacker.Traits ?? new List<string>() )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait?.Effects != null )
			{
				foreach ( var effect in trait.Effects )
				{
					if ( effect.Type == TraitEffectType.AccuracyBonus )
					{
						hitChance += effect.Value / 100f;
					}
				}
			}
		}

		// Check for evasion bonus traits on defender
		foreach ( var traitId in defender.Traits ?? new List<string>() )
		{
			var trait = TraitDatabase.GetTrait( traitId );
			if ( trait?.Effects != null )
			{
				foreach ( var effect in trait.Effects )
				{
					if ( effect.Type == TraitEffectType.EvasionBonus )
					{
						hitChance -= effect.Value / 100f;
					}
				}
			}
		}

		return CurrentRandom.NextDouble() < hitChance;
	}

	/// <summary>
	/// Use PP for a move
	/// </summary>
	public static void UsePP( Monster monster, string moveId )
	{
		var monsterMove = monster.Moves?.Find( m => m.MoveId == moveId );
		monsterMove?.UsePP();
	}

	/// <summary>
	/// Simulate a full battle with the new move-based system
	/// </summary>
	public static BattleResult SimulateBattleWithMoves( List<Monster> playerTeam, List<Monster> enemyTeam, int? seed = null, bool useAI = true )
	{
		Log.Info( $"[BattleSimulator] SimulateBattleWithMoves: players={playerTeam?.Count}, enemies={enemyTeam?.Count}" );

		if ( seed.HasValue )
			SetSeed( seed.Value );

		var result = new BattleResult();
		result.Turns = new List<BattleTurn>();

		// Clone monsters
		var players = playerTeam.Where( m => m != null ).Select( m => m.Clone() ).ToList();
		var enemies = enemyTeam.Where( m => m != null ).Select( m => m.Clone() ).ToList();

		if ( players.Count == 0 || enemies.Count == 0 )
		{
			result.PlayerWon = players.Count > 0;
			return result;
		}

		var state = new BattleState();

		// Initialize all monsters
		foreach ( var m in players.Concat( enemies ) )
		{
			state.InitializeMonster( m.Id );
		}

		const int maxTurns = 100;

		while ( !IsTeamDefeated( players ) && !IsTeamDefeated( enemies ) && state.TurnNumber < maxTurns )
		{
			state.TurnNumber++;

			// Get player's active monster
			var playerActive = GetActiveMonster( players, state.PlayerActiveIndex );
			if ( playerActive == null )
				break;

			// Get all alive enemies (horde-style: all attack)
			var aliveEnemies = enemies.Where( e => e != null && e.CurrentHP > 0 ).ToList();
			if ( aliveEnemies.Count == 0 )
				break;

			// Player targets the first alive enemy
			var playerTarget = aliveEnemies[0];

			// Build action list: Player + ALL alive enemies
			var actions = new List<(MoveChoice choice, Monster actor, Monster target, bool isPlayer)>();

			// Player's action
			var playerChoice = BattleAI.SelectAction( playerActive, playerTarget, state, players, true );
			actions.Add( (playerChoice, playerActive, playerTarget, true) );

			// ALL alive enemies attack player
			foreach ( var enemy in aliveEnemies )
			{
				var enemyChoice = BattleAI.SelectAction( enemy, playerActive, state, enemies, false );
				actions.Add( (enemyChoice, enemy, playerActive, false) );
			}

			// Sort actions by priority, then speed
			actions.Sort( ( a, b ) =>
			{
				int priorityCompare = b.choice.Priority.CompareTo( a.choice.Priority );
				if ( priorityCompare != 0 ) return priorityCompare;
				return b.choice.Speed.CompareTo( a.choice.Speed );
			} );

			// Execute actions
			foreach ( var (choice, actor, target, isPlayer) in actions )
			{
				if ( actor.CurrentHP <= 0 ) continue;
				if ( target.CurrentHP <= 0 ) continue;
				if ( IsTeamDefeated( players ) || IsTeamDefeated( enemies ) ) break;

				// Handle swap (only player can swap)
				if ( choice.ActionType == BattleActionType.Swap && isPlayer )
				{
					// Apply on-switch-out effects to the monster being switched out
					var switchOutEffects = ApplyOnSwitchOutEffects( actor, state );

					state.PlayerActiveIndex = choice.SwapToIndex;
					var newActive = players[choice.SwapToIndex];
					result.Turns.Add( new BattleTurn
					{
						TurnNumber = state.TurnNumber,
						AttackerId = actor.Id,
						AttackerName = actor.Nickname,
						DefenderId = newActive.Id,
						DefenderName = newActive.Nickname,
						IsPlayerAttacker = isPlayer,
						IsSwap = true,
						SwapToName = newActive.Nickname,
						EffectMessages = switchOutEffects
					} );
					continue;
				}

				// Check if can act
				var (canAct, actMessage) = CheckCanAct( actor, state );
				if ( !canAct )
				{
					result.Turns.Add( new BattleTurn
					{
						TurnNumber = state.TurnNumber,
						AttackerId = actor.Id,
						AttackerName = actor.Nickname,
						IsPlayerAttacker = isPlayer,
						StatusMessage = actMessage
					} );
					continue;
				}

				// Get move
				var move = MoveDatabase.GetMove( choice.MoveId );
				if ( move == null )
				{
					move = GetStruggleMove();
				}

				// Reset guard counter if not using guard
				if ( move.Effects?.Exists( e => e.Type == MoveEffectType.Guard ) != true )
				{
					state.ResetGuardCounter( actor.Id );
				}

				// Check accuracy
				bool hits = CheckAccuracy( actor, target, move, state );
				if ( !hits )
				{
					result.Turns.Add( new BattleTurn
					{
						TurnNumber = state.TurnNumber,
						AttackerId = actor.Id,
						AttackerName = actor.Nickname,
						DefenderId = target.Id,
						DefenderName = target.Nickname,
						IsPlayerAttacker = isPlayer,
						MoveName = move.Name,
						IsMiss = true
					} );
					UsePP( actor, choice.MoveId );
					continue;
				}

				// Check if target has Guard active
				if ( state.HasEffect( target.Id, MoveEffectType.Guard ) && move.Category != MoveCategory.Status )
				{
					result.Turns.Add( new BattleTurn
					{
						TurnNumber = state.TurnNumber,
						AttackerId = actor.Id,
						AttackerName = actor.Nickname,
						DefenderId = target.Id,
						DefenderName = target.Nickname,
						IsPlayerAttacker = isPlayer,
						MoveName = move.Name,
						Damage = 0,
						StatusMessage = $"{target.Nickname} protected itself!"
					} );
					UsePP( actor, choice.MoveId );
					continue;
				}

				// Calculate and apply damage
				var damageResult = CalculateDamage( actor, target, move, state );

				if ( damageResult.Damage > 0 )
				{
					ApplyDamage( target, damageResult.Damage );
				}

				// Apply move effects
				var effectMessages = ApplyMoveEffects( actor, target, move, damageResult, state );

				// Record KO for Bloodlust trait
				if ( target.CurrentHP <= 0 && damageResult.Damage > 0 )
				{
					state?.RecordKO( actor.Id );
				}

				// Apply contact damage from defender traits (e.g., Barbed Hide)
				if ( damageResult.Damage > 0 && target.CurrentHP > 0 )
				{
					var (contactDmg, contactMsg) = ApplyContactDamage( actor, target, move );
					if ( contactMsg != null )
					{
						effectMessages.Add( contactMsg );
					}
				}

				// Use PP
				UsePP( actor, choice.MoveId );

				// Record turn
				result.Turns.Add( new BattleTurn
				{
					TurnNumber = state.TurnNumber,
					AttackerId = actor.Id,
					AttackerName = actor.Nickname,
					DefenderId = target.Id,
					DefenderName = target.Nickname,
					Damage = damageResult.Damage,
					IsCritical = damageResult.IsCritical,
					IsSuperEffective = damageResult.IsSuperEffective,
					IsResisted = damageResult.IsResisted,
					DefenderHPAfter = target.CurrentHP,
					IsPlayerAttacker = isPlayer,
					MoveName = move.Name,
					MoveElement = move.Element,
					EffectMessages = effectMessages,
					HasSTAB = damageResult.HasSTAB
				} );

				// Auto-swap player if KO'd (enemies don't swap - horde style)
				if ( target.CurrentHP <= 0 && !isPlayer )
				{
					int nextAlive = -1;
					for ( int i = 0; i < players.Count; i++ )
					{
						if ( players[i].CurrentHP > 0 )
						{
							nextAlive = i;
							break;
						}
					}
					if ( nextAlive >= 0 )
					{
						state.PlayerActiveIndex = nextAlive;
					}
				}
			}

			// End of turn processing
			foreach ( var monster in players.Concat( enemies ).Where( m => m.CurrentHP > 0 ) )
			{
				var statusMessages = ProcessStatusDamage( monster, state );
				foreach ( var msg in statusMessages )
				{
					result.Turns.Add( new BattleTurn
					{
						TurnNumber = state.TurnNumber,
						AttackerId = monster.Id,
						AttackerName = monster.Nickname,
						StatusMessage = msg,
						DefenderHPAfter = monster.CurrentHP
					} );
				}
			}

			state.ProcessEndOfTurn();
		}

		result.PlayerWon = !IsTeamDefeated( players );
		result.TotalTurns = state.TurnNumber;

		// Calculate rewards
		if ( result.PlayerWon && players.Count > 0 )
		{
			int avgLevel = (int)players.Average( p => p.Level );
			foreach ( var enemy in enemies )
			{
				result.TotalXP += CalculateXPGain( enemy, avgLevel );
				result.TotalGold += CalculateGoldDrop( enemy );
			}

			// Calculate item drops if we're in an expedition
			var expedition = ExpeditionManager.Instance?.CurrentExpedition;
			if ( expedition != null && ItemManager.Instance != null )
			{
				bool isBoss = ExpeditionManager.Instance.IsBossWave;
				bool isRareBoss = ExpeditionManager.Instance.IsRareBossEncounter;

				foreach ( var enemy in enemies )
				{
					if ( enemy == null ) continue;
					var drops = ItemManager.Instance.CalculateDrop(
						enemy,
						expedition.Id,
						expedition.Element,
						expedition.BaseEnemyLevel,
						isBoss,
						isRareBoss
					);
					result.ItemDrops.AddRange( drops );
				}
			}
		}

		if ( seed.HasValue )
			ClearSeed();

		return result;
	}

	/// <summary>
	/// Get the active monster from a team, handling KOs
	/// </summary>
	private static Monster GetActiveMonster( List<Monster> team, int activeIndex )
	{
		if ( activeIndex < 0 || activeIndex >= team.Count )
			activeIndex = 0;

		var active = team[activeIndex];
		if ( active.CurrentHP > 0 )
			return active;

		// Find first alive monster
		for ( int i = 0; i < team.Count; i++ )
		{
			if ( team[i].CurrentHP > 0 )
				return team[i];
		}

		return null;
	}

	/// <summary>
	/// Get the Struggle move for when no PP is left
	/// </summary>
	private static MoveDefinition GetStruggleMove()
	{
		return new MoveDefinition
		{
			Id = "struggle",
			Name = "Struggle",
			Description = "A desperate attack used when no moves are available.",
			Element = ElementType.Neutral,
			Category = MoveCategory.Physical,
			BasePower = 50,
			Accuracy = 100,
			MaxPP = 999,
			Effects = new List<MoveEffect>
			{
				new MoveEffect { Type = MoveEffectType.Recoil, Value = 0.25f, TargetsSelf = true }
			}
		};
	}

	// ============================================
	// TURN-BY-TURN EXECUTION FOR MANUAL MODE
	// ============================================

	/// <summary>
	/// Execute a single turn with player's chosen move
	/// Returns the battle turns generated for this turn cycle
	/// In horde mode (expedition): ALL alive enemies attack each turn
	/// In arena mode: Only active enemy attacks (1v1 with swaps)
	/// </summary>
	public static List<BattleTurn> ExecuteSingleTurn(
		List<Monster> playerTeam,
		List<Monster> enemyTeam,
		BattleState state,
		string playerMoveId,
		Guid? playerTargetId = null,
		int swapToIndex = -1 )
	{
		var turns = new List<BattleTurn>();

		state.TurnNumber++;

		// Get player's active monster
		var playerActive = GetActiveMonster( playerTeam, state.PlayerActiveIndex );
		if ( playerActive == null )
			return turns;

		// Get all alive enemies
		var aliveEnemies = enemyTeam.Where( e => e != null && e.CurrentHP > 0 ).ToList();
		if ( aliveEnemies.Count == 0 )
			return turns;

		// Determine player's target
		Monster playerTarget;
		if ( state.IsArenaMode )
		{
			// Arena mode: Always target the active enemy (1v1)
			playerTarget = GetActiveMonster( enemyTeam, state.EnemyActiveIndex );
			if ( playerTarget == null || playerTarget.CurrentHP <= 0 )
			{
				// Find next alive enemy if active is KO'd
				playerTarget = aliveEnemies[0];
			}
		}
		else
		{
			// Horde mode: Use selected target if provided and valid, otherwise first alive
			playerTarget = null;
			if ( playerTargetId.HasValue && playerTargetId.Value != Guid.Empty )
			{
				playerTarget = aliveEnemies.FirstOrDefault( e => e.Id == playerTargetId.Value );
			}
			if ( playerTarget == null )
			{
				playerTarget = aliveEnemies[0];
			}
		}

		// Build action list
		var actions = new List<(MoveChoice choice, Monster actor, Monster target, bool isPlayer)>();

		// Get player's choice from their selected move
		MoveChoice playerChoice;
		int playerEffectiveSPD = GetEffectiveSPD( playerActive, state );
		if ( playerMoveId == "swap" )
		{
			// Use provided swap index, or find a valid target if not specified
			int swapIdx = swapToIndex >= 0 ? swapToIndex : FindSwapTarget( playerTeam, state.PlayerActiveIndex );
			playerChoice = new MoveChoice
			{
				ActionType = BattleActionType.Swap,
				SwapToIndex = swapIdx,
				Priority = 6,
				Speed = playerEffectiveSPD
			};
		}
		else
		{
			var move = MoveDatabase.GetMove( playerMoveId );
			playerChoice = new MoveChoice
			{
				ActionType = BattleActionType.Attack,
				MoveId = playerMoveId,
				Priority = move != null ? GetEffectivePriority( playerActive, move ) : 0,
				Speed = playerEffectiveSPD
			};
		}

		actions.Add( (playerChoice, playerActive, playerTarget, true) );

		// Add enemy actions
		if ( state.IsArenaMode )
		{
			// Arena mode: Only the active enemy attacks (1v1)
			var enemyActive = GetActiveMonster( enemyTeam, state.EnemyActiveIndex );
			Log.Info( $"[ExecuteSingleTurn] Arena mode: EnemyActiveIndex={state.EnemyActiveIndex}, enemyActive={enemyActive?.Nickname ?? "null"}, HP={enemyActive?.CurrentHP ?? 0}" );
			if ( enemyActive != null && enemyActive.CurrentHP > 0 )
			{
				var enemyChoice = BattleAI.SelectAction( enemyActive, playerActive, state, enemyTeam, false );
				Log.Info( $"[ExecuteSingleTurn] Enemy choice: ActionType={enemyChoice.ActionType}, MoveId={enemyChoice.MoveId}" );
				actions.Add( (enemyChoice, enemyActive, playerActive, false) );
			}
			else
			{
				Log.Warning( $"[ExecuteSingleTurn] No valid enemy active monster!" );
			}
		}
		else
		{
			// Horde mode: ALL alive enemies get to attack the player's active monster
			foreach ( var enemy in aliveEnemies )
			{
				var enemyChoice = BattleAI.SelectAction( enemy, playerActive, state, enemyTeam, false );
				actions.Add( (enemyChoice, enemy, playerActive, false) );
			}
		}

		// Sort actions by priority, then speed
		actions.Sort( ( a, b ) =>
		{
			int priorityCompare = b.choice.Priority.CompareTo( a.choice.Priority );
			if ( priorityCompare != 0 ) return priorityCompare;
			return b.choice.Speed.CompareTo( a.choice.Speed );
		} );

		Log.Info( $"[ExecuteSingleTurn] Total actions to execute: {actions.Count}" );
		foreach ( var (c, a, t, isP) in actions )
		{
			Log.Info( $"  Action: {a.Nickname} (isPlayer={isP}) -> {t.Nickname}, Move={c.MoveId ?? "swap"}" );
		}

		// Execute actions
		foreach ( var (choice, actor, target, isPlayer) in actions )
		{
			Log.Info( $"[ExecuteSingleTurn] Checking action: {actor.Nickname} (HP={actor.CurrentHP}) -> {target.Nickname} (HP={target.CurrentHP})" );
			if ( actor.CurrentHP <= 0 )
			{
				Log.Info( $"[ExecuteSingleTurn] Skipping - actor is KO'd" );
				continue;
			}
			if ( target.CurrentHP <= 0 )
			{
				Log.Info( $"[ExecuteSingleTurn] Skipping - target is KO'd" );
				continue;
			}
			if ( IsTeamDefeated( playerTeam ) || IsTeamDefeated( enemyTeam ) )
			{
				Log.Info( $"[ExecuteSingleTurn] Breaking - team defeated" );
				break;
			}

			var turnResults = ExecuteAction( choice, actor, target, isPlayer, playerTeam, enemyTeam, state );
			Log.Info( $"[ExecuteSingleTurn] Executed action, got {turnResults.Count} turn results" );
			turns.AddRange( turnResults );
		}

		// Arena mode: Auto-swap to next beast when active is KO'd
		if ( state.IsArenaMode )
		{
			// Check if enemy's active beast is KO'd
			var enemyActive = GetActiveMonster( enemyTeam, state.EnemyActiveIndex );
			if ( enemyActive == null || enemyActive.CurrentHP <= 0 )
			{
				// Find next alive enemy
				for ( int i = 0; i < enemyTeam.Count; i++ )
				{
					if ( enemyTeam[i] != null && enemyTeam[i].CurrentHP > 0 )
					{
						var oldActive = enemyActive;
						state.EnemyActiveIndex = i;
						var newActive = enemyTeam[i];
						turns.Add( new BattleTurn
						{
							TurnNumber = state.TurnNumber,
							AttackerId = oldActive?.Id ?? Guid.Empty,
							AttackerName = oldActive?.Nickname ?? "Enemy",
							DefenderId = newActive.Id,
							DefenderName = newActive.Nickname,
							IsPlayerAttacker = false,
							IsSwap = true,
							SwapToName = newActive.Nickname,
							StatusMessage = $"{newActive.Nickname} was sent out!"
						} );
						break;
					}
				}
			}

			// Check if player's active beast is KO'd
			var playerActiveCheck = GetActiveMonster( playerTeam, state.PlayerActiveIndex );
			if ( playerActiveCheck == null || playerActiveCheck.CurrentHP <= 0 )
			{
				// Find next alive player monster
				for ( int i = 0; i < playerTeam.Count; i++ )
				{
					if ( playerTeam[i] != null && playerTeam[i].CurrentHP > 0 )
					{
						var oldActive = playerActiveCheck;
						state.PlayerActiveIndex = i;
						var newActive = playerTeam[i];
						turns.Add( new BattleTurn
						{
							TurnNumber = state.TurnNumber,
							AttackerId = oldActive?.Id ?? Guid.Empty,
							AttackerName = oldActive?.Nickname ?? "Player",
							DefenderId = newActive.Id,
							DefenderName = newActive.Nickname,
							IsPlayerAttacker = true,
							IsSwap = true,
							SwapToName = newActive.Nickname,
							StatusMessage = $"{newActive.Nickname} was sent out!"
						} );
						break;
					}
				}
			}
		}

		// End of turn processing (status damage)
		foreach ( var monster in playerTeam.Concat( enemyTeam ).Where( m => m.CurrentHP > 0 ) )
		{
			var statusMessages = ProcessStatusDamage( monster, state );
			foreach ( var msg in statusMessages )
			{
				turns.Add( new BattleTurn
				{
					TurnNumber = state.TurnNumber,
					AttackerId = monster.Id,
					AttackerName = monster.Nickname,
					StatusMessage = msg,
					DefenderHPAfter = monster.CurrentHP,
					IsPlayerAttacker = playerTeam.Contains( monster )
				} );
			}
		}

		state.ProcessEndOfTurn();

		Log.Info( $"[ExecuteSingleTurn] Returning {turns.Count} total turns" );
		return turns;
	}

	/// <summary>
	/// Execute a single action (attack or swap)
	/// </summary>
	private static List<BattleTurn> ExecuteAction(
		MoveChoice choice,
		Monster actor,
		Monster target,
		bool isPlayer,
		List<Monster> playerTeam,
		List<Monster> enemyTeam,
		BattleState state )
	{
		var turns = new List<BattleTurn>();

		// Handle swap
		if ( choice.ActionType == BattleActionType.Swap )
		{
			// Apply on-switch-out effects to the monster being switched out
			var switchOutEffects = ApplyOnSwitchOutEffects( actor, state );

			var team = isPlayer ? playerTeam : enemyTeam;
			if ( isPlayer )
				state.PlayerActiveIndex = choice.SwapToIndex;
			else
				state.EnemyActiveIndex = choice.SwapToIndex;

			var newActive = team[choice.SwapToIndex];
			turns.Add( new BattleTurn
			{
				TurnNumber = state.TurnNumber,
				AttackerId = actor.Id,
				AttackerName = actor.Nickname,
				DefenderId = newActive.Id,
				DefenderName = newActive.Nickname,
				IsPlayerAttacker = isPlayer,
				IsSwap = true,
				SwapToName = newActive.Nickname,
				EffectMessages = switchOutEffects
			} );
			return turns;
		}

		// Check if can act
		var (canAct, actMessage) = CheckCanAct( actor, state );
		if ( !canAct )
		{
			turns.Add( new BattleTurn
			{
				TurnNumber = state.TurnNumber,
				AttackerId = actor.Id,
				AttackerName = actor.Nickname,
				IsPlayerAttacker = isPlayer,
				StatusMessage = actMessage
			} );
			return turns;
		}

		// Get move
		var move = MoveDatabase.GetMove( choice.MoveId );
		if ( move == null )
		{
			move = GetStruggleMove();
		}

		// Reset guard counter if not using guard
		if ( move.Effects?.Exists( e => e.Type == MoveEffectType.Guard ) != true )
		{
			state.ResetGuardCounter( actor.Id );
		}

		// Check accuracy
		bool hits = CheckAccuracy( actor, target, move, state );
		if ( !hits )
		{
			turns.Add( new BattleTurn
			{
				TurnNumber = state.TurnNumber,
				AttackerId = actor.Id,
				AttackerName = actor.Nickname,
				DefenderId = target.Id,
				DefenderName = target.Nickname,
				IsPlayerAttacker = isPlayer,
				MoveName = move.Name,
				MoveElement = move.Element,
				IsMiss = true
			} );
			UsePP( actor, choice.MoveId );
			return turns;
		}

		// Check if target has Guard active
		if ( state.HasEffect( target.Id, MoveEffectType.Guard ) && move.Category != MoveCategory.Status )
		{
			turns.Add( new BattleTurn
			{
				TurnNumber = state.TurnNumber,
				AttackerId = actor.Id,
				AttackerName = actor.Nickname,
				DefenderId = target.Id,
				DefenderName = target.Nickname,
				IsPlayerAttacker = isPlayer,
				MoveName = move.Name,
				MoveElement = move.Element,
				Damage = 0,
				StatusMessage = $"{target.Nickname} protected itself!"
			} );
			UsePP( actor, choice.MoveId );
			return turns;
		}

		// Calculate and apply damage
		var damageResult = CalculateDamage( actor, target, move, state );

		if ( damageResult.Damage > 0 )
		{
			ApplyDamage( target, damageResult.Damage );
		}

		// Apply move effects
		var effectMessages = ApplyMoveEffects( actor, target, move, damageResult, state );

		// Record KO for Bloodlust trait
		if ( target.CurrentHP <= 0 && damageResult.Damage > 0 )
		{
			state?.RecordKO( actor.Id );
		}

		// Apply contact damage from defender traits (e.g., Barbed Hide)
		if ( damageResult.Damage > 0 && target.CurrentHP > 0 )
		{
			var (contactDmg, contactMsg) = ApplyContactDamage( actor, target, move );
			if ( contactMsg != null )
			{
				effectMessages.Add( contactMsg );
			}
		}

		// Use PP
		UsePP( actor, choice.MoveId );

		// Record turn
		turns.Add( new BattleTurn
		{
			TurnNumber = state.TurnNumber,
			AttackerId = actor.Id,
			AttackerName = actor.Nickname,
			DefenderId = target.Id,
			DefenderName = target.Nickname,
			Damage = damageResult.Damage,
			IsCritical = damageResult.IsCritical,
			IsSuperEffective = damageResult.IsSuperEffective,
			IsResisted = damageResult.IsResisted,
			DefenderHPAfter = target.CurrentHP,
			IsPlayerAttacker = isPlayer,
			MoveName = move.Name,
			MoveElement = move.Element,
			EffectMessages = effectMessages,
			HasSTAB = damageResult.HasSTAB
		} );

		// Check for KO and auto-swap (only for player's team - enemies don't swap, they all attack)
		if ( target.CurrentHP <= 0 && !isPlayer )
		{
			// Player's monster was KO'd - auto-swap to next alive
			int nextAlive = -1;
			for ( int i = 0; i < playerTeam.Count; i++ )
			{
				if ( playerTeam[i].CurrentHP > 0 )
				{
					nextAlive = i;
					break;
				}
			}

			if ( nextAlive >= 0 )
			{
				state.PlayerActiveIndex = nextAlive;
			}
		}
		// Enemies don't auto-swap - they all participate each turn

		return turns;
	}

	/// <summary>
	/// Find a valid swap target index
	/// </summary>
	private static int FindSwapTarget( List<Monster> team, int currentIndex )
	{
		for ( int i = 0; i < team.Count; i++ )
		{
			if ( i != currentIndex && team[i].CurrentHP > 0 )
				return i;
		}
		return currentIndex; // No swap available
	}

	/// <summary>
	/// Check if battle is over
	/// </summary>
	public static bool IsBattleOver( List<Monster> playerTeam, List<Monster> enemyTeam )
	{
		return IsTeamDefeated( playerTeam ) || IsTeamDefeated( enemyTeam );
	}

	// ============================================
	// BOSS PHASE SYSTEM
	// ============================================

	/// <summary>
	/// Check if a boss should transition to the next phase based on current HP
	/// </summary>
	public static PhaseTransitionResult CheckBossPhaseTransition( Monster boss, ActiveBossState bossState )
	{
		if ( boss == null || bossState?.BossData?.Phases == null || bossState.BossData.Phases.Count == 0 )
			return null;

		float hpPercent = (float)boss.CurrentHP / boss.MaxHP;

		if ( !bossState.ShouldTransitionPhase( hpPercent ) )
			return null;

		var nextPhase = bossState.GetNextPhase( hpPercent );
		if ( nextPhase == null )
			return null;

		// Apply phase stat multipliers to the boss
		ApplyPhaseMultipliers( boss, nextPhase );

		return new PhaseTransitionResult
		{
			Phase = nextPhase,
			Message = nextPhase.TransitionMessage,
			Ability = nextPhase.Ability,
			SummonSpeciesId = nextPhase.SummonSpeciesId
		};
	}

	/// <summary>
	/// Apply stat multipliers from a boss phase
	/// </summary>
	private static void ApplyPhaseMultipliers( Monster boss, BossPhase phase )
	{
		// Apply multipliers to current stats
		if ( phase.ATKMultiplier != 1.0f )
		{
			boss.ATK = (int)(boss.ATK * phase.ATKMultiplier);
			Log.Info( $"Boss {boss.Nickname} ATK increased to {boss.ATK}" );
		}

		if ( phase.DEFMultiplier != 1.0f )
		{
			boss.DEF = (int)(boss.DEF * phase.DEFMultiplier);
			Log.Info( $"Boss {boss.Nickname} DEF increased to {boss.DEF}" );
		}

		if ( phase.SPDMultiplier != 1.0f )
		{
			boss.SPD = (int)(boss.SPD * phase.SPDMultiplier);
			Log.Info( $"Boss {boss.Nickname} SPD changed to {boss.SPD}" );
		}
	}

	/// <summary>
	/// Execute a boss ability triggered by phase transition
	/// Returns a list of effect messages
	/// </summary>
	public static List<string> ExecuteBossAbility( Monster boss, BossPhase phase, List<Monster> playerTeam, BattleState state )
	{
		var messages = new List<string>();

		switch ( phase.Ability )
		{
			case BossAbilityType.Enrage:
				// Already applied via ATK multiplier
				messages.Add( $"{boss.Nickname} is enraged!" );
				break;

			case BossAbilityType.Shield:
				// Already applied via DEF multiplier
				messages.Add( $"{boss.Nickname} raises a defensive shield!" );
				break;

			case BossAbilityType.Regenerate:
				int healAmount = (int)(boss.MaxHP * 0.15f);
				boss.CurrentHP = Math.Min( boss.MaxHP, boss.CurrentHP + healAmount );
				messages.Add( $"{boss.Nickname} regenerates {healAmount} HP!" );
				break;

			case BossAbilityType.AreaDamage:
				int areaDamage = (int)(boss.ATK * 0.5f);
				foreach ( var player in playerTeam.Where( p => p.CurrentHP > 0 ) )
				{
					player.CurrentHP = Math.Max( 0, player.CurrentHP - areaDamage );
					messages.Add( $"{player.Nickname} takes {areaDamage} damage from the shockwave!" );
				}
				break;

			case BossAbilityType.SpeedBoost:
				// Already applied via SPD multiplier
				messages.Add( $"{boss.Nickname}'s speed surges!" );
				break;

			case BossAbilityType.SummonMinion:
				// Minion summoning is handled by the caller (ExpeditionManager or BattleManager)
				messages.Add( $"{boss.Nickname} summons an ally!" );
				break;

			case BossAbilityType.ElementalShift:
				messages.Add( $"{boss.Nickname} shifts elemental energy!" );
				break;
		}

		return messages;
	}
}

/// <summary>
/// Result of a boss phase transition
/// </summary>
public class PhaseTransitionResult
{
	public BossPhase Phase { get; set; }
	public string Message { get; set; }
	public BossAbilityType Ability { get; set; }
	public string SummonSpeciesId { get; set; }
}

/// <summary>
/// Result of a single damage calculation
/// </summary>
public class DamageResult
{
	public int Damage { get; set; }
	public bool IsCritical { get; set; }
	public bool IsSuperEffective { get; set; }
	public bool IsResisted { get; set; }
	public bool IsImmune { get; set; }
	public bool HasSTAB { get; set; }
	public bool WasAbsorbed { get; set; } // Element was absorbed and healed the target
	public float ElementModifier { get; set; } = 1.0f;

	// Move info
	public string MoveName { get; set; }
	public string MoveId { get; set; }
	public ElementType MoveElement { get; set; }
	public MoveCategory MoveCategory { get; set; }
}

/// <summary>
/// A single turn in a battle
/// </summary>
public class BattleTurn
{
	public int TurnNumber { get; set; }
	public Guid AttackerId { get; set; }
	public string AttackerName { get; set; }
	public Guid DefenderId { get; set; }
	public string DefenderName { get; set; }
	public int Damage { get; set; }
	public bool IsCritical { get; set; }
	public bool IsSuperEffective { get; set; }
	public bool IsResisted { get; set; }
	public int DefenderHPAfter { get; set; }
	public bool IsPlayerAttacker { get; set; }

	// Move-based combat additions
	public string MoveName { get; set; }
	public ElementType MoveElement { get; set; }
	public bool HasSTAB { get; set; }
	public bool IsMiss { get; set; }
	public bool IsSwap { get; set; }
	public string SwapToName { get; set; }
	public string StatusMessage { get; set; }
	public List<string> EffectMessages { get; set; }
}

/// <summary>
/// Complete result of a battle simulation
/// </summary>
public class BattleResult
{
	public bool PlayerWon { get; set; }
	public int TotalTurns { get; set; }
	public int TotalXP { get; set; }
	public int TotalGold { get; set; }
	public int TotalGems { get; set; }
	public List<BattleTurn> Turns { get; set; }

	// Item drops from defeated enemies
	public List<(string ItemId, int Quantity)> ItemDrops { get; set; } = new();
}

/// <summary>
/// Extension methods
/// </summary>
public static class MonsterExtensions
{
	public static int ToInt( this double d )
	{
		return (int)d;
	}
}
