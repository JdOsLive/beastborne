using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// Age of Calamity "Request Tile" style trade stations. Small clickable icons
/// sit on the world map; opening one shows a cost → reward panel and the player
/// pays materials + gold to receive consumables, skill points, or other rewards.
///
/// All transactions are instant. No timers, no queues, no RNG on reward.
/// </summary>
public sealed class TradeNodeManager : Component
{
	public static TradeNodeManager Instance { get; private set; }

	private bool _hasHydrated;
	private readonly Dictionary<string, TradeNodeState> _states = new();

	public enum TradeResult
	{
		Success,
		MissingMaterials,
		NotEnoughGold,
		AlreadyCompleted,
		UnknownNode
	}

	// ═══════════════════════════════════════════════════════════════
	// LAUNCH NODES
	// Positions (MapPosX / MapPosY) are % of the world-map backdrop.
	// Launch zones sit at:  Cove (22, 62) / Forest (52, 38) / Old Saltmoor (78, 68)
	// Node icons are placed in orbit around each zone marker.
	// ═══════════════════════════════════════════════════════════════
	private static readonly List<TradeNode> Defs = new()
	{
		// ── Saltmoor Cove cluster ────────────────────────────────────
		new()
		{
			Id = "cove_dewdrop_exchange",
			Name = "Well-keeper's Request",
			Description = "The well-keeper needs fresh Dewdroplets to bless the village cisterns before the dry season.",
			NpcName = "Well-keeper Nia",
			ZoneId = "saltmoor_cove",
			MapPosX = 14f, MapPosY = 56f,
			Requirements = new() { ("mat_dewdrop", 6) },
			GoldReward = 300,
			XPReward = 80,
			ItemRewards = new() { ("boost_def", 2) }
		},
		new()
		{
			Id = "cove_twig_bundle",
			Name = "Kindling Bundle",
			Description = "Trade a bundle of Snapped Twigs for a handful of lures the fisherfolk have set aside.",
			NpcName = "Saltkeeper Bram",
			ZoneId = "saltmoor_cove",
			MapPosX = 29f, MapPosY = 68f,
			Requirements = new() { ("mat_twigsnap", 4) },
			GoldReward = 150,
			ItemRewards = new() { ("catch_lure", 3) }
		},

		// ── Saltmoor Forest cluster ──────────────────────────────────
		new()
		{
			Id = "forest_moss_tribute",
			Name = "Elder Moss Tribute",
			Description = "The Forest Elder grants a single skill point in exchange for elder moss only found deep in the canopy.",
			NpcName = "Elder Mira",
			ZoneId = "saltmoor_forest",
			MapPosX = 46f, MapPosY = 32f,
			Requirements = new() { ("mat_mosscreep", 10) },
			GoldCost = 500,
			SkillPointReward = 1
		},
		new()
		{
			Id = "forest_pollen_trade",
			Name = "Pollenpuff Remedy",
			Description = "The Herbalist will brew a strong elixir for anyone who brings enough Pollen Burst sacs.",
			NpcName = "The Herbalist",
			ZoneId = "saltmoor_forest",
			MapPosX = 58f, MapPosY = 44f,
			Requirements = new() { ("mat_pollenpuff", 5) },
			GoldCost = 300,
			ItemRewards = new() { ("xp_feast", 1), ("boost_atk", 2) }
		},
		new()
		{
			Id = "forest_whisker_bundle",
			Name = "Whisker Weave",
			Description = "A wandering weaver offers rare thread in trade for Whiskerwind whiskers.",
			NpcName = "A Nervous Trader",
			ZoneId = "saltmoor_forest",
			MapPosX = 49f, MapPosY = 46f,
			Requirements = new() { ("mat_whiskerwind", 8) },
			GoldReward = 800,
			XPReward = 150,
			ItemRewards = new() { ("catch_prime", 1) }
		},

		// ── Old Saltmoor cluster ─────────────────────────────────────
		new()
		{
			Id = "oldsalt_mirror_exchange",
			Name = "Collector's Reflection",
			Description = "The Collector will part with a prized relic — if enough Mirrorpond shards can be gathered.",
			NpcName = "The Collector",
			ZoneId = "old_saltmoor",
			MapPosX = 72f, MapPosY = 74f,
			Requirements = new() { ("mat_mirrorpond", 6) },
			GoldCost = 2000,
			ItemRewards = new() { ("relic_guardian_emblem", 1) }
		},
		new()
		{
			Id = "oldsalt_coral_pact",
			Name = "Coral Pact",
			Description = "The archaeologist traces old coastal maps in exchange for Coralheim fragments.",
			NpcName = "Archaeologist Ven",
			ZoneId = "old_saltmoor",
			MapPosX = 85f, MapPosY = 62f,
			Requirements = new() { ("mat_coralheim", 4) },
			GoldReward = 1500,
			XPReward = 300,
			SkillPointReward = 1
		},
		new()
		{
			Id = "oldsalt_streamling_cache",
			Name = "Storm-cache",
			Description = "A scout buried a cache before the great storm. The scout will dig it up again for enough Streamling essence.",
			NpcName = "A Wary Scout",
			ZoneId = "old_saltmoor",
			MapPosX = 80f, MapPosY = 80f,
			Requirements = new() { ("mat_streamling", 5) },
			GoldCost = 800,
			ItemRewards = new() { ("catch_prime", 1), ("gold_bell", 2) }
		}
	};

	public IReadOnlyCollection<TradeNode> AllNodes => Defs;

	public IEnumerable<TradeNode> GetNodesForZone( string zoneId )
		=> Defs.Where( n => n.ZoneId == zoneId );

	public TradeNode GetNode( string id ) => Defs.FirstOrDefault( n => n.Id == id );

	public TradeNodeState GetState( string id )
	{
		if ( _states.TryGetValue( id, out var s ) ) return s;
		var fresh = new TradeNodeState { Id = id };
		_states[id] = fresh;
		return fresh;
	}

	public bool IsLocked( string id )
	{
		var node = GetNode( id );
		if ( node == null ) return true;
		if ( !node.Repeatable && GetState( id ).TimesCompleted > 0 ) return true;
		return false;
	}

	// Events
	public Action<TradeNode, TradeResult> OnTradeAttempted;
	public Action<TradeNode> OnTradeCompleted;

	// ═══════════════════════════════════════════════════════════════
	// LIFECYCLE
	// ═══════════════════════════════════════════════════════════════

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Log.Info( "TradeNodeManager initialized" );
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
		go.Name = "TradeNodeManager";
		go.Components.Create<TradeNodeManager>();
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
	}

	protected override void OnDestroy()
	{
		if ( SaveService.Instance != null )
		{
			SaveService.Instance.OnSaveLoaded -= Hydrate;
			SaveService.Instance.OnSaveReset -= HandleSaveReset;
		}
	}

	private void HandleSaveReset()
	{
		_hasHydrated = false;
		_states.Clear();
		Hydrate();
	}

	private void Hydrate()
	{
		if ( _hasHydrated ) return;
		_hasHydrated = true;

		var section = SaveService.Instance?.CurrentBlob?.TradeNodes;
		if ( section == null )
		{
			Log.Info( "[TradeNodes] Hydrated: fresh" );
			return;
		}

		foreach ( var s in section.Nodes ?? new() )
		{
			if ( s?.Id == null ) continue;
			_states[s.Id] = s;
		}
		Log.Info( $"[TradeNodes] Hydrated: {_states.Count} node states" );
	}

	private void Persist()
	{
		var service = SaveService.Instance;
		if ( service?.CurrentBlob == null ) return;
		service.CurrentBlob.TradeNodes = new TradeNodeSaveData
		{
			Nodes = _states.Values.ToList()
		};
		service.MarkDirty( "tradenodes" );
	}

	// ═══════════════════════════════════════════════════════════════
	// EXCHANGE
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Attempt to run a trade. Returns the result and, on success, applies the
	/// cost and reward atomically. No partial state — either the trade is valid
	/// and runs to completion, or nothing changes.
	/// </summary>
	public TradeResult Execute( string nodeId )
	{
		var node = GetNode( nodeId );
		if ( node == null ) return TradeResult.UnknownNode;

		if ( IsLocked( nodeId ) )
		{
			OnTradeAttempted?.Invoke( node, TradeResult.AlreadyCompleted );
			return TradeResult.AlreadyCompleted;
		}

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return TradeResult.UnknownNode;

		// Verify funds.
		if ( node.GoldCost > 0 && tamer.Gold < node.GoldCost )
		{
			OnTradeAttempted?.Invoke( node, TradeResult.NotEnoughGold );
			return TradeResult.NotEnoughGold;
		}

		// Verify materials.
		foreach ( var (itemId, qty) in node.Requirements )
		{
			if ( !tamer.Inventory.TryGetValue( itemId, out var have ) || have < qty )
			{
				OnTradeAttempted?.Invoke( node, TradeResult.MissingMaterials );
				return TradeResult.MissingMaterials;
			}
		}

		// ── Apply cost ──
		if ( node.GoldCost > 0 ) tamer.Gold -= node.GoldCost;
		foreach ( var (itemId, qty) in node.Requirements )
		{
			tamer.Inventory[itemId] -= qty;
			if ( tamer.Inventory[itemId] <= 0 ) tamer.Inventory.Remove( itemId );
		}

		// ── Apply reward ──
		if ( node.GoldReward > 0 ) tamer.Gold += node.GoldReward;
		if ( node.XPReward > 0 ) TamerManager.Instance?.AddXP( node.XPReward );
		if ( node.SkillPointReward > 0 ) tamer.SkillPoints += node.SkillPointReward;
		foreach ( var (itemId, qty) in node.ItemRewards )
		{
			ItemManager.Instance?.AddItem( itemId, qty );
		}

		// ── Record + notify ──
		var state = GetState( nodeId );
		state.TimesCompleted++;
		state.LastCompletedTicks = DateTime.UtcNow.Ticks;

		NotificationManager.Instance?.AddNotification(
			NotificationType.Success,
			"Trade Complete",
			$"{node.Name} — rewards claimed" );

		OnTradeCompleted?.Invoke( node );
		OnTradeAttempted?.Invoke( node, TradeResult.Success );
		Persist();
		Log.Info( $"[TradeNodes] Executed: {nodeId} (cost: {node.GoldCost}g + {node.Requirements.Count} item stacks)" );
		return TradeResult.Success;
	}

	/// <summary>Convenience: can the player afford this trade right now?</summary>
	public bool CanAfford( string nodeId )
	{
		var node = GetNode( nodeId );
		if ( node == null ) return false;
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return false;
		if ( node.GoldCost > 0 && tamer.Gold < node.GoldCost ) return false;
		foreach ( var (itemId, qty) in node.Requirements )
		{
			if ( !tamer.Inventory.TryGetValue( itemId, out var have ) || have < qty ) return false;
		}
		return true;
	}
}
