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
	// Launch zones sit at:  Cove (22, 62) / Forest (52, 38) / Weavermere (78, 68)
	// Node icons are placed in orbit around each zone marker.
	// ═══════════════════════════════════════════════════════════════
	private static readonly List<TradeNode> Defs = new()
	{
		// ── Pasture Round cluster (Hollow Creek village) ────────────
		new()
		{
			Id = "cove_wishlift_request",
			Name = "Mom's Wish Jar",
			Description = "Mom's been keeping a little glass jar by the kitchen window since you were a kid. She wants to fill it with proper Wishlift drifts now that you can fetch them yourself.",
			NpcName = "Mom",
			ZoneId = "saltmoor_cove",
			MapPosX = 42.7f, MapPosY = 50.2f,
			Requirements = new() { ("mat_wishlift", 4) },
			GoldReward = 250,
			XPReward = 80,
			ItemRewards = new() { ("boost_def", 2) }
		},
		new()
		{
			Id = "cove_heartwell_request",
			Name = "Garden Watering",
			Description = "Old Lyle's tomato beds are wilting and the well's run shallow. He'll trade a bag of training treats for a few Heartwell springs to keep the garden going.",
			NpcName = "Farmer Lyle",
			ZoneId = "saltmoor_cove",
			MapPosX = 31.4f, MapPosY = 31.7f,
			Requirements = new() { ("mat_heartwell", 3) },
			GoldReward = 200,
			ItemRewards = new() { ("xp_treat", 2) }
		},
		new()
		{
			Id = "cove_twincoil_request",
			Name = "Old Sol's Tinkering",
			Description = "The retired miner spends his days fixing lanterns and shaping odd lures from scrap. Bring him Twincoil scales — both faces, mind — and he'll cobble you something useful.",
			NpcName = "Old Sol",
			ZoneId = "saltmoor_cove",
			MapPosX = 40.0f, MapPosY = 69.4f,
			Requirements = new() { ("mat_twincoil", 5) },
			GoldReward = 350,
			ItemRewards = new() { ("catch_lure", 2) }
		},

		// ── Weaverwood cluster — retuned around the handmade pool
		// after the AI-gen forest beasts retired pre-launch.
		new()
		{
			Id = "forest_gnoll_tribute",
			Name = "Gnollcap Tribute",
			Description = "The Forest Elder grants a single skill point in exchange for ten Gnollcap Petals — the Gnolls only let go of one when they're sure you mean the grove no harm.",
			NpcName = "Elder Mira",
			ZoneId = "saltmoor_forest",
			MapPosX = 71.7f, MapPosY = 66.3f,
			Requirements = new() { ("mat_gnoll", 10) },
			GoldCost = 500,
			SkillPointReward = 1
		},
		new()
		{
			Id = "forest_jackacabra_bounty",
			Name = "Herders' Bounty",
			Description = "The Herbalist will brew a strong elixir for anyone who brings five Hexbite Fangs back from the wood — every fang is one fewer raid on the pasture.",
			NpcName = "The Herbalist",
			ZoneId = "saltmoor_forest",
			MapPosX = 76.9f, MapPosY = 46.1f,
			Requirements = new() { ("mat_jackacabra", 5) },
			GoldCost = 300,
			ItemRewards = new() { ("xp_feast", 1), ("boost_atk", 2) }
		},
		new()
		{
			Id = "forest_twincoil_weave",
			Name = "Twincoil Weave",
			Description = "A wandering weaver offers rare thread in trade for eight Twincoil scales — the village says the doubled pattern weaves a cloth that warms both sides.",
			NpcName = "A Nervous Trader",
			ZoneId = "saltmoor_forest",
			MapPosX = 61.0f, MapPosY = 59.5f,
			Requirements = new() { ("mat_twincoil", 8) },
			GoldReward = 800,
			XPReward = 150,
			ItemRewards = new() { ("catch_prime", 1) }
		},

		// ── Weavermere cluster — artists' pond above Weaverton, where
		// the village's painters and dye-masters chase the colors no
		// loom can match. Trades retuned around handmade materials
		// after the AI-gen lake pool retired pre-launch.
		new()
		{
			Id = "oldsalt_painter_request",
			Name = "Painter's Request",
			Description = "The village painter is mixing a new green and swears Padlip pondlight gives it the glow she's been chasing for six years. Six motes ought to do it.",
			NpcName = "Painter Selene",
			ZoneId = "old_saltmoor",
			MapPosX = 66.1f, MapPosY = 33.6f,
			Requirements = new() { ("mat_padlip", 6) },
			GoldCost = 2000,
			ItemRewards = new() { ("relic_guardian_emblem", 1) }
		},
		new()
		{
			Id = "oldsalt_dye_pact",
			Name = "Dye-Master's Pact",
			Description = "The dye-master will share a recipe Weaverton has been guessing at for generations — for four Padlip pondlight motes, brought up while the dawn is still pink.",
			NpcName = "Dye-Master Wren",
			ZoneId = "old_saltmoor",
			MapPosX = 73.9f, MapPosY = 27.0f,
			Requirements = new() { ("mat_padlip", 4) },
			GoldReward = 1500,
			XPReward = 300,
			SkillPointReward = 1
		},
		new()
		{
			Id = "oldsalt_poets_offering",
			Name = "Poet's Offering",
			Description = "The wandering poet writes a verse for anyone who brings him five Padlip pondlight motes — he says he can hear the lake humming through them and the words come on their own.",
			NpcName = "The Wandering Poet",
			ZoneId = "old_saltmoor",
			MapPosX = 62.3f, MapPosY = 23.3f,
			Requirements = new() { ("mat_padlip", 5) },
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
