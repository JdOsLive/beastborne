using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// Infrastructure for Hyrule Warriors-style zone-attached side quests.
///
/// Launch scope: framework only + 2–3 safe placeholder quests. Full narrative
/// content (NPC storylines tied to handmade beasts) ships post-launch per the
/// pre-launch plan. Subscribes to existing gameplay events + exposes Track*
/// helpers mirroring <see cref="MissionManager"/>'s pattern so content-side
/// call sites don't need to know whether a daily mission or a side quest is
/// listening.
/// </summary>
public sealed class SideQuestManager : Component
{
	public static SideQuestManager Instance { get; private set; }

	private bool _hasHydrated;

	// ═══════════════════════════════════════════════════════════════
	// QUEST DEFINITIONS (launch placeholders)
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Launch roster of side quests. Safe/generic by design — named NPCs and
	/// story beats are deferred until handmade beasts ship post-launch.
	/// </summary>
	private static readonly List<SideQuest> Defs = new()
	{
		// ── Weaverton (Lv 1) ─────────────────────────────────────
		// Spring shearing — the village trades fresh wool for catch lures.
		// Sheepot drops Soft Spring Wool when defeated.
		new()
		{
			Id = "cove_wool_collection",
			Title = "Spring Shearing",
			Description = "The village's spinners need 5 bundles of Soft Spring Wool from Sheepots in the Weaverton Pasture.",
			NpcName = "Loomkeeper Bram",
			ZoneId = "saltmoor_cove",
			Objective = SideQuestObjectiveType.CollectMaterial,
			TargetId = "mat_sheepot",
			Target = 5,
			GoldReward = 200,
			XPReward = 80,
			ItemRewards = new() { ("catch_lure", 2) }
		},

		// ── Weaverwood (Lv 10) ──────────────────────────────────
		// Jackacabra cull — the herders' standing bounty. Each pelt
		// brought back means one fewer Sheepot lost to the Weaverwood
		// shadow at dusk.
		new()
		{
			Id = "forest_jackacabra_cull",
			Title = "Thinning the Shadow",
			Description = "Cull 8 Jackacabras in the Weaverwood — the herders are losing too many sheep.",
			NpcName = "Loomkeeper Bram",
			ZoneId = "saltmoor_forest",
			Objective = SideQuestObjectiveType.DefeatSpecies,
			TargetId = "jackacabra",
			Target = 8,
			GoldReward = 600,
			XPReward = 240,
			SkillPointReward = 1
		},

		// ── Weavermere (Lv 20) ─────────────────────────────────────
		// Up to the Mere — the dye-master sends apprentices to walk the
		// pond at dawn twice in a row, on the theory that the second
		// visit is when the lake decides whether to show you anything.
		new()
		{
			Id = "oldsalt_clear",
			Title = "Up to the Mere",
			Description = "Walk the Weavermere expedition twice — Wren says the pond never shows the same face two mornings running.",
			NpcName = "Dye-Master Wren",
			ZoneId = "old_saltmoor",
			Objective = SideQuestObjectiveType.ExpeditionCleared,
			TargetId = "old_saltmoor",
			Target = 2,
			GoldReward = 1200,
			XPReward = 500,
			SkillPointReward = 2,
			ItemRewards = new() { ("catch_prime", 1) }
		}
	};

	// ═══════════════════════════════════════════════════════════════
	// RUNTIME STATE
	// ═══════════════════════════════════════════════════════════════

	private readonly Dictionary<string, SideQuestState> _active = new();
	private readonly HashSet<string> _completedOneShots = new();

	public IReadOnlyCollection<SideQuest> AllDefinitions => Defs;
	public IEnumerable<SideQuestState> ActiveQuests => _active.Values;

	// Events — UI subscribes for live tracker updates.
	public Action<SideQuestState> OnQuestAccepted;
	public Action<SideQuestState> OnQuestProgress;
	public Action<SideQuestState> OnQuestCompleted;
	public Action<SideQuestState> OnQuestClaimed;

	// ═══════════════════════════════════════════════════════════════
	// LIFECYCLE
	// ═══════════════════════════════════════════════════════════════

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Log.Info( "SideQuestManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;
		var go = scene.CreateObject();
		go.Name = "SideQuestManager";
		go.Components.Create<SideQuestManager>();
	}

	protected override void OnStart()
	{
		if ( SaveService.Instance != null && SaveService.Instance.IsLoaded )
		{
			Hydrate();
		}
		else if ( SaveService.Instance != null )
		{
			SaveService.Instance.OnSaveLoaded += Hydrate;
		}

		if ( SaveService.Instance != null )
		{
			SaveService.Instance.OnSaveReset += HandleSaveReset;
		}

		// Subscribe to direct events. Tracked-style hooks (fusion, expedition)
		// are invoked by call sites via Track* helpers, mirroring MissionManager.
		if ( BattleManager.Instance != null )
		{
			BattleManager.Instance.OnMonsterDefeated += HandleMonsterDefeated;
		}
	}

	protected override void OnDestroy()
	{
		if ( BattleManager.Instance != null )
		{
			BattleManager.Instance.OnMonsterDefeated -= HandleMonsterDefeated;
		}
		if ( SaveService.Instance != null )
		{
			SaveService.Instance.OnSaveLoaded -= Hydrate;
			SaveService.Instance.OnSaveReset -= HandleSaveReset;
		}
	}

	private void HandleSaveReset()
	{
		_hasHydrated = false;
		_active.Clear();
		_completedOneShots.Clear();
		Hydrate();
	}

	private void Hydrate()
	{
		if ( _hasHydrated ) return;
		_hasHydrated = true;

		var section = SaveService.Instance?.CurrentBlob?.SideQuests;
		if ( section == null )
		{
			Log.Info( "[SideQuests] Hydrated: fresh (no saved state)" );
			return;
		}

		foreach ( var state in section.ActiveQuests ?? new() )
		{
			if ( state?.Id == null ) continue;
			_active[state.Id] = state;
		}
		foreach ( var id in section.CompletedOneShotIds ?? new() )
		{
			_completedOneShots.Add( id );
		}
		Log.Info( $"[SideQuests] Hydrated: {_active.Count} active, {_completedOneShots.Count} completed" );
	}

	private void Persist()
	{
		var service = SaveService.Instance;
		if ( service?.CurrentBlob == null ) return;
		service.CurrentBlob.SideQuests = new SideQuestSaveData
		{
			ActiveQuests = _active.Values.ToList(),
			CompletedOneShotIds = _completedOneShots.ToList()
		};
		service.MarkDirty( "sidequests" );
	}

	// ═══════════════════════════════════════════════════════════════
	// PUBLIC API — LOOKUP
	// ═══════════════════════════════════════════════════════════════

	public SideQuest GetDefinition( string id ) => Defs.FirstOrDefault( q => q.Id == id );

	public IEnumerable<SideQuest> GetQuestsForZone( string zoneId )
		=> Defs.Where( q => q.ZoneId == zoneId );

	public SideQuestState GetState( string id )
		=> _active.TryGetValue( id, out var s ) ? s : null;

	public bool IsAccepted( string id ) => _active.ContainsKey( id );

	public bool IsCompletedOneShot( string id ) => _completedOneShots.Contains( id );

	/// <summary>True when the player has never accepted this quest, or has
	/// already claimed it and it isn't repeatable. Also gated on the player
	/// having cleared the quest's zone at least once — keeps first-playthrough
	/// focus on the main loop, then side content opens up after each zone.</summary>
	public bool IsAvailable( string id )
	{
		var def = GetDefinition( id );
		if ( def == null ) return false;
		if ( IsAccepted( id ) ) return false;
		if ( !def.Repeatable && _completedOneShots.Contains( id ) ) return false;
		if ( !string.IsNullOrEmpty( def.ZoneId )
			&& ExpeditionManager.Instance?.HasClearedExpedition( def.ZoneId ) != true )
			return false;
		return true;
	}

	// ═══════════════════════════════════════════════════════════════
	// PUBLIC API — STATE MUTATIONS
	// ═══════════════════════════════════════════════════════════════

	public bool Accept( string id )
	{
		if ( !IsAvailable( id ) ) return false;

		var state = new SideQuestState
		{
			Id = id,
			Progress = 0,
			Completed = false,
			RewardClaimed = false,
			AcceptedTicks = DateTime.UtcNow.Ticks
		};
		_active[id] = state;
		OnQuestAccepted?.Invoke( state );
		Persist();
		Log.Info( $"[SideQuests] Accepted: {id}" );
		return true;
	}

	/// <summary>
	/// Claim rewards for a completed quest. Idempotent — calling twice on a
	/// one-shot returns false the second time.
	/// </summary>
	public bool Claim( string id )
	{
		if ( !_active.TryGetValue( id, out var state ) ) return false;
		if ( !state.Completed || state.RewardClaimed ) return false;

		var def = GetDefinition( id );
		if ( def == null ) return false;

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer != null )
		{
			if ( def.GoldReward > 0 ) tamer.Gold += def.GoldReward;
			if ( def.XPReward > 0 ) TamerManager.Instance?.AddXP( def.XPReward );
			if ( def.SkillPointReward > 0 ) tamer.SkillPoints += def.SkillPointReward;
			foreach ( var (itemId, qty) in def.ItemRewards ?? new() )
			{
				ItemManager.Instance?.AddItem( itemId, qty );
			}
		}

		state.RewardClaimed = true;
		if ( def.Repeatable )
		{
			// Reset so the player can re-accept.
			_active.Remove( id );
		}
		else
		{
			_completedOneShots.Add( id );
			_active.Remove( id );
		}

		OnQuestClaimed?.Invoke( state );
		Persist();
		Log.Info( $"[SideQuests] Claimed: {id} (gold={def.GoldReward}, xp={def.XPReward}, sp={def.SkillPointReward})" );
		return true;
	}

	// ═══════════════════════════════════════════════════════════════
	// EVENT HANDLERS / TRACKERS
	// ═══════════════════════════════════════════════════════════════

	private void HandleMonsterDefeated( Monster defeated )
	{
		if ( defeated == null ) return;
		// Only count defeats of enemy monsters, not player losses.
		if ( BattleManager.Instance?.PlayerTeam?.Any( m => m.Id == defeated.Id ) == true ) return;

		AdvanceMatching( SideQuestObjectiveType.DefeatSpecies, defeated.SpeciesId, 1 );
	}

	/// <summary>Call after a material item enters inventory. Hooks ItemManager / beast drops.</summary>
	public void TrackMaterialCollected( string itemId, int quantity )
	{
		if ( string.IsNullOrEmpty( itemId ) || quantity <= 0 ) return;
		AdvanceMatching( SideQuestObjectiveType.CollectMaterial, itemId, quantity );
	}

	/// <summary>Call when a fusion completes. Mirrors MissionManager.TrackFusion call sites.</summary>
	public void TrackFusion()
	{
		AdvanceMatching( SideQuestObjectiveType.FusionsCompleted, null, 1 );
	}

	/// <summary>Call when a monster is contracted/caught.</summary>
	public void TrackContract()
	{
		AdvanceMatching( SideQuestObjectiveType.MonstersContracted, null, 1 );
	}

	/// <summary>Call on expedition success.</summary>
	public void TrackExpeditionCleared( string expeditionId )
	{
		if ( string.IsNullOrEmpty( expeditionId ) ) return;
		AdvanceMatching( SideQuestObjectiveType.ExpeditionCleared, expeditionId, 1 );
	}

	private void AdvanceMatching( SideQuestObjectiveType type, string targetId, int amount )
	{
		if ( _active.Count == 0 ) return;
		bool dirty = false;

		foreach ( var state in _active.Values.ToList() )
		{
			if ( state.Completed ) continue;
			var def = GetDefinition( state.Id );
			if ( def == null || def.Objective != type ) continue;

			// TargetId is optional — FusionsCompleted / MonstersContracted ignore it.
			if ( !string.IsNullOrEmpty( def.TargetId ) && def.TargetId != targetId )
				continue;

			state.Progress = Math.Min( def.Target, state.Progress + amount );
			OnQuestProgress?.Invoke( state );
			dirty = true;

			if ( state.Progress >= def.Target )
			{
				state.Completed = true;
				OnQuestCompleted?.Invoke( state );
				NotificationManager.Instance?.AddNotification(
					NotificationType.Success,
					"Quest Complete",
					$"{def.Title} — ready to claim" );
				Log.Info( $"[SideQuests] Completed: {def.Id}" );
			}
		}

		if ( dirty ) Persist();
	}
}
