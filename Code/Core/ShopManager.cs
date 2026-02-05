using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Network;
using Beastborne.Data;

namespace Beastborne.Core;

/// <summary>
/// Manages the in-game shop and active boosts
/// </summary>
public sealed class ShopManager : Component
{
	public static ShopManager Instance { get; private set; }

	// Maximum number of different active boosts at once (per player)
	private const int MAX_ACTIVE_BOOSTS = 4;

	// Shop inventory
	private List<ShopItem> _shopItems = new();
	public IReadOnlyList<ShopItem> ShopItems => _shopItems;

	// Active boosts (personal)
	private List<ActiveBoost> _activeBoosts = new();
	public IReadOnlyList<ActiveBoost> ActiveBoosts => _activeBoosts;

	// Server-wide boosts (applies to all players)
	private List<ServerBoost> _serverBoosts = new();
	public IReadOnlyList<ServerBoost> ServerBoosts => _serverBoosts;

	// Events
	public Action<ShopItem> OnItemPurchased;
	public Action<ActiveBoost> OnBoostExpired;
	public Action<ServerBoost> OnServerBoostActivated;
	public Action<ServerBoost> OnServerBoostExpired;
	public Action OnShopRefreshed;

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			InitializeShop();
			Log.Info( "ShopManager initialized" );
		}
		else
		{
			Log.Info( "ShopManager already exists, removing duplicate" );
			Destroy();
		}
	}

	protected override void OnStart()
	{
		// Request any active server boosts from other players when joining
		if ( GameNetworkSystem.IsActive )
		{
			// Delay slightly to ensure network is ready
			_ = RequestServerBoostsDelayed();
		}
	}

	private async Task RequestServerBoostsDelayed()
	{
		await Task.Delay( 1000 ); // Wait 1 second for network to stabilize
		RequestServerBoosts();
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "ShopManager";
		go.Components.Create<ShopManager>();
	}

	/// <summary>
	/// Initialize shop with available items
	/// </summary>
	private void InitializeShop()
	{
		_shopItems = new List<ShopItem>
		{
			// Contract Ink - for catching monsters
			new ShopItem
			{
				Id = "ink_small",
				Name = "Contract Ink (5)",
				Description = "Used to capture wild monsters. Each capture attempt uses 1 ink.",
				IconPath = "ui/icons/ink.png",
				Type = ShopItemType.ContractInk,
				Currency = CurrencyType.Gold,
				Price = 2500,
				Quantity = 5
			},
			new ShopItem
			{
				Id = "ink_large",
				Name = "Contract Ink (20)",
				Description = "A bulk pack of contract ink for serious tamers.",
				IconPath = "ui/icons/ink.png",
				Type = ShopItemType.ContractInk,
				Currency = CurrencyType.Gold,
				Price = 8000,
				Quantity = 20
			},
			new ShopItem
			{
				Id = "ink_mega",
				Name = "Contract Ink (100)",
				Description = "A massive stockpile of contract ink. For those who catch them all!",
				IconPath = "ui/icons/ink.png",
				Type = ShopItemType.ContractInk,
				Currency = CurrencyType.Gold,
				Price = 35000,
				Quantity = 100,
				RequiredLevel = 20
			},

			// Tamer XP Boosts
			new ShopItem
			{
				Id = "tamer_xp_30",
				Name = "Tamer XP Boost (30 min)",
				Description = "Double tamer XP gain for 30 minutes. Level up faster!",
				IconPath = "ui/items/boosts/tamer_xp_scroll.png",
				Type = ShopItemType.TamerXPBoost,
				Currency = CurrencyType.Gold,
				Price = 25000,
				BoostMultiplier = 2.0f,
				BoostDurationMinutes = 30,
				RequiredLevel = 5
			},
			new ShopItem
			{
				Id = "tamer_xp_120",
				Name = "Tamer XP Boost (2 hours)",
				Description = "Double tamer XP gain for 2 hours. Great for grinding!",
				IconPath = "ui/items/boosts/tamer_xp_scroll.png",
				Type = ShopItemType.TamerXPBoost,
				Currency = CurrencyType.Gold,
				Price = 85000,
				BoostMultiplier = 2.0f,
				BoostDurationMinutes = 120,
				RequiredLevel = 15
			},

			// Beast XP Boosts
			new ShopItem
			{
				Id = "beast_xp_30",
				Name = "Beast XP Boost (30 min)",
				Description = "Double XP for your monsters for 30 minutes.",
				IconPath = "ui/items/boosts/beast_xp_tome.png",
				Type = ShopItemType.BeastXPBoost,
				Currency = CurrencyType.Gold,
				Price = 25000,
				BoostMultiplier = 2.0f,
				BoostDurationMinutes = 30,
				RequiredLevel = 5
			},
			new ShopItem
			{
				Id = "beast_xp_120",
				Name = "Beast XP Boost (2 hours)",
				Description = "Double XP for your monsters for 2 hours.",
				IconPath = "ui/items/boosts/beast_xp_tome.png",
				Type = ShopItemType.BeastXPBoost,
				Currency = CurrencyType.Gold,
				Price = 85000,
				BoostMultiplier = 2.0f,
				BoostDurationMinutes = 120,
				RequiredLevel = 15
			},

			// Server-wide XP Boosts (benefit everyone!)
			new ShopItem
			{
				Id = "server_tamer_xp_60",
				Name = "Server Tamer XP (1 hour)",
				Description = "Double tamer XP for ALL players for 1 hour! Be a hero!",
				IconPath = "ui/items/boosts/tamer_xp_scroll.png",
				Type = ShopItemType.TamerXPBoost,
				Currency = CurrencyType.Gold,
				Price = 500000,
				BoostMultiplier = 2.0f,
				BoostDurationMinutes = 60,
				RequiredLevel = 20
			},
			new ShopItem
			{
				Id = "server_beast_xp_60",
				Name = "Server Beast XP (1 hour)",
				Description = "Double beast XP for ALL players for 1 hour! Be a hero!",
				IconPath = "ui/items/boosts/beast_xp_tome.png",
				Type = ShopItemType.BeastXPBoost,
				Currency = CurrencyType.Gold,
				Price = 500000,
				BoostMultiplier = 2.0f,
				BoostDurationMinutes = 60,
				RequiredLevel = 20
			},

			// Gold Boosts
			new ShopItem
			{
				Id = "gold_boost_30",
				Name = "Gold Boost (30 min)",
				Description = "50% more gold from battles for 30 minutes.",
				IconPath = "ui/items/boosts/gold_multiplier.png",
				Type = ShopItemType.GoldBoost,
				Currency = CurrencyType.Gold,
				Price = 20000,
				BoostMultiplier = 1.5f,
				BoostDurationMinutes = 30,
				RequiredLevel = 5
			},
			new ShopItem
			{
				Id = "gold_boost_60",
				Name = "Gold Boost (1 hour)",
				Description = "50% more gold from battles for 1 hour.",
				IconPath = "ui/items/boosts/gold_multiplier.png",
				Type = ShopItemType.GoldBoost,
				Currency = CurrencyType.Gold,
				Price = 35000,
				BoostMultiplier = 1.5f,
				BoostDurationMinutes = 60,
				RequiredLevel = 10
			},

			// Server-wide Gold Boost
			new ShopItem
			{
				Id = "server_gold_boost_60",
				Name = "Server Gold Boost (1 hour)",
				Description = "50% more gold for ALL players for 1 hour! Be a hero!",
				IconPath = "ui/items/boosts/gold_multiplier.png",
				Type = ShopItemType.GoldBoost,
				Currency = CurrencyType.Gold,
				Price = 400000,
				BoostMultiplier = 1.5f,
				BoostDurationMinutes = 60,
				RequiredLevel = 20
			},

			// Rare Encounter Boost
			new ShopItem
			{
				Id = "rare_boost",
				Name = "Lucky Charm (1 hour)",
				Description = "Increased chance to encounter rare monsters.",
				IconPath = "ui/items/boosts/lucky_clover.png",
				Type = ShopItemType.RareEncounter,
				Currency = CurrencyType.Gold,
				Price = 50000,
				BoostMultiplier = 2.0f,
				BoostDurationMinutes = 60,
				RequiredLevel = 10
			},
			new ShopItem
			{
				Id = "rare_boost_long",
				Name = "Lucky Charm (3 hours)",
				Description = "Greatly increased chance to encounter rare monsters for 3 hours.",
				IconPath = "ui/items/boosts/lucky_clover.png",
				Type = ShopItemType.RareEncounter,
				Currency = CurrencyType.Gold,
				Price = 125000,
				BoostMultiplier = 2.5f,
				BoostDurationMinutes = 180,
				RequiredLevel = 20
			},

			// Server-wide Lucky Charm
			new ShopItem
			{
				Id = "server_rare_boost_60",
				Name = "Server Lucky Charm (1 hour)",
				Description = "Increased rare encounter chance for ALL players for 1 hour! Be a hero!",
				IconPath = "ui/items/boosts/lucky_clover.png",
				Type = ShopItemType.RareEncounter,
				Currency = CurrencyType.Gold,
				Price = 600000,
				BoostMultiplier = 2.0f,
				BoostDurationMinutes = 60,
				RequiredLevel = 25
			},

			// Monster Slots
			new ShopItem
			{
				Id = "monster_slot_medium",
				Name = "Storage Expansion (+50)",
				Description = "Expand your monster box by 50 slots. For the growing collection!",
				IconPath = "ui/icons/slot.png",
				Type = ShopItemType.MonsterSlot,
				Currency = CurrencyType.Gold,
				Price = 175000,
				Quantity = 50,
				RequiredLevel = 10
			},
			new ShopItem
			{
				Id = "monster_slot_large",
				Name = "Storage Expansion (+100)",
				Description = "Massively expand your monster box by 100 slots. Best value for collectors!",
				IconPath = "ui/icons/slot.png",
				Type = ShopItemType.MonsterSlot,
				Currency = CurrencyType.Gold,
				Price = 300000,
				Quantity = 100,
				RequiredLevel = 20
			}
		};
	}

	/// <summary>
	/// Get items filtered by category
	/// </summary>
	public List<ShopItem> GetItemsByType( ShopItemType type )
	{
		return _shopItems.Where( i => i.Type == type ).ToList();
	}

	/// <summary>
	/// Get all purchasable items for player's level
	/// </summary>
	public List<ShopItem> GetAvailableItems( int playerLevel )
	{
		return _shopItems.Where( i => i.RequiredLevel <= playerLevel && i.IsInStock ).ToList();
	}

	/// <summary>
	/// Get the discounted price for an item (applies Bargain Hunter + Savvy Shopper skills)
	/// </summary>
	public int GetDiscountedPrice( ShopItem item )
	{
		if ( item.Currency != CurrencyType.Gold )
			return item.Price; // Gems aren't discounted

		// Base discount from Bargain Hunter
		float discount = TamerManager.Instance?.GetSkillBonus( SkillEffectType.ShopDiscount ) ?? 0;

		// Stacking discount from Savvy Shopper (extra % per 100k gold owned)
		float stackingBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.DiscountStackingBonus ) ?? 0;
		if ( stackingBonus > 0 )
		{
			int currentGold = TamerManager.Instance?.CurrentTamer?.Gold ?? 0;
			int goldTiers = currentGold / 100000; // Per 100k gold
			float extraDiscount = stackingBonus * goldTiers;
			// Cap at 5x the base bonus (25% max at rank 5)
			extraDiscount = Math.Min( extraDiscount, stackingBonus * 5 );
			discount += extraDiscount;
		}

		if ( discount <= 0 )
			return item.Price;

		// Cap total discount at 50%
		discount = Math.Min( discount, 50f );

		int discountedPrice = (int)(item.Price * (1 - discount / 100f));
		return Math.Max( 1, discountedPrice ); // Minimum 1 gold
	}

	/// <summary>
	/// Get the discounted price for an item by ID
	/// </summary>
	public int GetDiscountedPrice( string itemId )
	{
		var item = _shopItems.FirstOrDefault( i => i.Id == itemId );
		return item != null ? GetDiscountedPrice( item ) : 0;
	}

	/// <summary>
	/// Attempt to purchase an item
	/// </summary>
	public bool PurchaseItem( string itemId )
	{
		var item = _shopItems.FirstOrDefault( i => i.Id == itemId );
		if ( item == null )
		{
			Log.Warning( $"Shop item not found: {itemId}" );
			return false;
		}

		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return false;

		// Check level requirement
		if ( tamer.Level < item.RequiredLevel )
		{
			Log.Info( $"Level {item.RequiredLevel} required for {item.Name}" );
			return false;
		}

		// Check stock
		if ( !item.IsInStock )
		{
			Log.Info( $"{item.Name} is out of stock" );
			return false;
		}

		// For boost items, check if we can activate before spending currency
		bool isBoostItem = item.Type == ShopItemType.TamerXPBoost ||
						   item.Type == ShopItemType.BeastXPBoost ||
						   item.Type == ShopItemType.XPBoost ||
						   item.Type == ShopItemType.GoldBoost ||
						   item.Type == ShopItemType.RareEncounter ||
						   item.Type == ShopItemType.LuckyCharm;

		bool isServerBoost = item.Id.StartsWith( "server_" );

		if ( isBoostItem )
		{
			// Server boosts go to inventory - no pre-activation check needed
			// Personal boosts still need the active boost check
			if ( !isServerBoost )
			{
				// Check personal boost limit and if same type is already active
				if ( !CanActivateBoost( item.Type ) )
				{
					Log.Info( $"Cannot purchase {item.Name}: boost already active or max {MAX_ACTIVE_BOOSTS} boosts reached" );
					return false;
				}
			}
		}

		// Check and spend currency (apply shop discount for gold purchases)
		int finalPrice = GetDiscountedPrice( item );
		bool purchased = item.Currency switch
		{
			CurrencyType.Gold => TamerManager.Instance.SpendGold( finalPrice ),
			CurrencyType.Gems => TamerManager.Instance.SpendGems( item.Price ),
			_ => false
		};

		if ( !purchased )
		{
			Log.Info( $"Cannot afford {item.Name}" );
			return false;
		}

		// Apply the item effect
		if ( !ApplyItemEffect( item ) )
		{
			// Refund on failure (shouldn't happen due to pre-check, but just in case)
			if ( item.Currency == CurrencyType.Gold )
				TamerManager.Instance?.AddGold( finalPrice );
			else
				TamerManager.Instance?.AddGems( item.Price );

			Log.Warning( $"Failed to apply effect for {item.Name}, refunded" );
			return false;
		}

		// Update stock
		if ( item.MaxStock > 0 )
		{
			item.CurrentStock--;
		}

		OnItemPurchased?.Invoke( item );
		Log.Info( $"Purchased {item.Name}" );
		return true;
	}

	/// <summary>
	/// Apply the effect of a purchased item
	/// </summary>
	/// <returns>True if effect was applied successfully</returns>
	private bool ApplyItemEffect( ShopItem item )
	{
		// Check if this is a server-wide boost
		bool isServerBoost = item.Id.StartsWith( "server_" );

		switch ( item.Type )
		{
			case ShopItemType.ContractInk:
				TamerManager.Instance?.AddContractInk( item.Quantity );
				return true;

			case ShopItemType.TamerXPBoost:
			case ShopItemType.BeastXPBoost:
			case ShopItemType.XPBoost:
			case ShopItemType.GoldBoost:
			case ShopItemType.RareEncounter:
			case ShopItemType.LuckyCharm:
				if ( isServerBoost )
				{
					// Add boost to inventory instead of activating immediately
					var inventoryItemId = GetInventoryItemIdForBoost( item.Type, item.BoostDurationMinutes );
					if ( inventoryItemId != null )
					{
						ItemManager.Instance?.AddItem( inventoryItemId, 1 );
						TamerManager.Instance?.SaveToCloud();
						Log.Info( $"Added server boost to inventory: {inventoryItemId}" );
						return true;
					}
					return false;
				}
				else
				{
					return AddBoost( item.Type, item.BoostMultiplier, item.BoostDurationMinutes );
				}

			case ShopItemType.SkillReset:
				ResetSkills();
				return true;

			case ShopItemType.MonsterSlot:
				return MonsterManager.Instance?.IncreaseMaxMonsters( item.Quantity ) ?? false;

			case ShopItemType.Revive:
				ReviveAllMonsters();
				return true;

			default:
				return true;
		}
	}

	// Maximum stackable boost duration (8 hours)
	private const int MAX_BOOST_DURATION_MINUTES = 480;

	/// <summary>
	/// Map a shop boost type to an inventory item ID
	/// </summary>
	private string GetInventoryItemIdForBoost( ShopItemType type, int durationMinutes )
	{
		// Determine duration suffix (1h or 2h)
		var durationSuffix = durationMinutes <= 60 ? "1h" : "2h";

		return type switch
		{
			ShopItemType.TamerXPBoost => $"boost_tamer_xp_{durationSuffix}",
			ShopItemType.BeastXPBoost => $"boost_beast_xp_{durationSuffix}",
			ShopItemType.GoldBoost => $"boost_gold_{durationSuffix}",
			ShopItemType.LuckyCharm => $"boost_lucky_{durationSuffix}",
			ShopItemType.RareEncounter => $"boost_rare_{durationSuffix}",
			_ => null
		};
	}

	/// <summary>
	/// Add a timed boost effect (stacks up to 8 hours for same type)
	/// </summary>
	private bool AddBoost( ShopItemType type, float multiplier, int durationMinutes )
	{
		// Apply Amplifier skill bonus (increases boost potency)
		float potencyBonus = TamerManager.Instance?.GetSkillBonus( SkillEffectType.BoostPotencyBonus ) ?? 0;
		if ( potencyBonus > 0 )
		{
			// For multipliers > 1, add the bonus to the extra portion
			// e.g., 2.0x with 25% bonus = 1.0 + (1.0 * 1.25) = 2.25x
			float extra = multiplier - 1.0f;
			extra *= (1 + potencyBonus / 100f);
			multiplier = 1.0f + extra;
		}

		// Check if boost already exists - extend duration if so (up to 8 hours max)
		var existing = _activeBoosts.FirstOrDefault( b => b.Type == type && !b.IsExpired );
		if ( existing != null )
		{
			// Calculate new expiration with cap at 8 hours from now
			var maxExpiration = DateTime.UtcNow.AddMinutes( MAX_BOOST_DURATION_MINUTES );
			var newExpiration = existing.ExpiresAt.AddMinutes( durationMinutes );

			if ( newExpiration > maxExpiration )
			{
				// Already at or near cap
				if ( existing.ExpiresAt >= maxExpiration )
				{
					Log.Info( $"Cannot extend boost: {type} already at maximum 8 hour duration" );
					return false;
				}
				newExpiration = maxExpiration;
			}

			existing.ExpiresAt = newExpiration;
			existing.Multiplier = Math.Max( existing.Multiplier, multiplier );
			Log.Info( $"Extended boost {type} duration" );
			return true;
		}

		// Check max boost limit for new boosts
		var activeCount = _activeBoosts.Count( b => !b.IsExpired );
		if ( activeCount >= MAX_ACTIVE_BOOSTS )
		{
			Log.Warning( $"Cannot add boost: max {MAX_ACTIVE_BOOSTS} active boosts reached" );
			return false;
		}

		_activeBoosts.Add( new ActiveBoost
		{
			Type = type,
			Multiplier = multiplier,
			ExpiresAt = DateTime.UtcNow.AddMinutes( durationMinutes )
		} );
		return true;
	}

	/// <summary>
	/// Check if player can activate another boost (same type stacks up to 8 hours)
	/// </summary>
	public bool CanActivateBoost( ShopItemType type )
	{
		// Check if same type already active - can extend if not at max duration
		var existing = _activeBoosts.FirstOrDefault( b => b.Type == type && !b.IsExpired );
		if ( existing != null )
		{
			// Can extend if not already at 8 hour cap
			var maxExpiration = DateTime.UtcNow.AddMinutes( MAX_BOOST_DURATION_MINUTES );
			return existing.ExpiresAt < maxExpiration;
		}

		// Check max limit for new boost types
		var activeCount = _activeBoosts.Count( b => !b.IsExpired );
		return activeCount < MAX_ACTIVE_BOOSTS;
	}

	/// <summary>
	/// Check if a specific boost type is currently active
	/// </summary>
	public bool IsBoostActive( ShopItemType type )
	{
		return _activeBoosts.Any( b => b.Type == type && !b.IsExpired )
			|| _serverBoosts.Any( b => b.Type == type && !b.IsExpired );
	}

	/// <summary>
	/// Get remaining boost slots available
	/// </summary>
	public int GetRemainingBoostSlots()
	{
		var activeCount = _activeBoosts.Count( b => !b.IsExpired );
		return Math.Max( 0, MAX_ACTIVE_BOOSTS - activeCount );
	}

	/// <summary>
	/// Check if a server-wide boost can be activated (stacks up to 8 hours)
	/// </summary>
	public bool CanActivateServerBoost( ShopItemType type )
	{
		var existing = _serverBoosts.FirstOrDefault( b => b.Type == type && !b.IsExpired );
		if ( existing != null )
		{
			// Can extend if not already at 8 hour cap
			var maxExpiration = DateTime.UtcNow.AddMinutes( MAX_BOOST_DURATION_MINUTES );
			return existing.ExpiresAt < maxExpiration;
		}
		return true;
	}

	/// <summary>
	/// Get the remaining time on a server boost
	/// </summary>
	public TimeSpan GetServerBoostTimeRemaining( ShopItemType type )
	{
		var existing = _serverBoosts.FirstOrDefault( b => b.Type == type && !b.IsExpired );
		return existing?.TimeRemaining ?? TimeSpan.Zero;
	}

	/// <summary>
	/// Activate a server-wide boost (benefits all players) - stacks up to 8 hours
	/// </summary>
	/// <returns>True if activated successfully</returns>
	public bool ActivateServerBoost( ShopItemType type, float multiplier, int durationMinutes )
	{
		var playerName = TamerManager.Instance?.CurrentTamer?.Name ?? "Someone";
		var steamId = (long)(Sandbox.Utility.Steam.SteamId);

		// Check if boost already exists - extend duration if so (up to 8 hours max)
		var existing = _serverBoosts.FirstOrDefault( b => b.Type == type && !b.IsExpired );
		if ( existing != null )
		{
			// Calculate new expiration with cap at 8 hours from now
			var maxExpiration = DateTime.UtcNow.AddMinutes( MAX_BOOST_DURATION_MINUTES );
			var newExpiration = existing.ExpiresAt.AddMinutes( durationMinutes );

			if ( newExpiration > maxExpiration )
			{
				// Already at or near cap
				if ( existing.ExpiresAt >= maxExpiration )
				{
					Log.Info( $"Cannot extend server boost: {type} already at maximum 8 hour duration" );
					return false;
				}
				newExpiration = maxExpiration;
			}

			existing.ExpiresAt = newExpiration;
			existing.Multiplier = Math.Max( existing.Multiplier, multiplier );
			existing.ActivatedBy = playerName;
			existing.ActivatedBySteamId = steamId;

			// Broadcast extension to chat
			var extendBoostName = type switch
			{
				ShopItemType.TamerXPBoost => "Tamer XP Boost",
				ShopItemType.BeastXPBoost => "Beast XP Boost",
				ShopItemType.GoldBoost => "Gold Boost",
				ShopItemType.RareEncounter => "Lucky Charm",
				ShopItemType.LuckyCharm => "Lucky Charm",
				_ => "Boost"
			};
			ChatManager.Instance?.SendSystemAnnouncement(
				$"{playerName} extended the server-wide {extendBoostName} by {durationMinutes} minutes!",
				ChatMessageType.Achievement
			);

			// Broadcast to network
			if ( GameNetworkSystem.IsActive )
			{
				BroadcastServerBoost( type, multiplier, durationMinutes, playerName, steamId );
			}

			Log.Info( $"Extended server boost {type} duration by {playerName}" );
			return true;
		}

		var serverBoost = new ServerBoost
		{
			Type = type,
			Multiplier = multiplier,
			ExpiresAt = DateTime.UtcNow.AddMinutes( durationMinutes ),
			ActivatedBy = playerName,
			ActivatedBySteamId = steamId
		};
		_serverBoosts.Add( serverBoost );
		OnServerBoostActivated?.Invoke( serverBoost );

		// Broadcast to chat
		var boostName = type switch
		{
			ShopItemType.TamerXPBoost => "Tamer XP Boost",
			ShopItemType.BeastXPBoost => "Beast XP Boost",
			ShopItemType.GoldBoost => "Gold Boost",
			ShopItemType.RareEncounter => "Lucky Charm",
			ShopItemType.LuckyCharm => "Lucky Charm",
			_ => "Boost"
		};
		ChatManager.Instance?.SendSystemAnnouncement(
			$"{playerName} activated a server-wide {boostName} for {durationMinutes} minutes!",
			ChatMessageType.Achievement
		);

		// Broadcast to network
		if ( GameNetworkSystem.IsActive )
		{
			BroadcastServerBoost( type, multiplier, durationMinutes, playerName, steamId );
		}

		Log.Info( $"Server boost activated: {type} by {playerName}" );
		return true;
	}

	/// <summary>
	/// Broadcast server boost to all players
	/// </summary>
	[Rpc.Broadcast]
	private void BroadcastServerBoost( ShopItemType type, float multiplier, int durationMinutes, string playerName, long steamId )
	{
		// Don't process our own broadcast
		if ( steamId == (long)Sandbox.Utility.Steam.SteamId ) return;

		var existing = _serverBoosts.FirstOrDefault( b => b.Type == type );
		if ( existing != null )
		{
			existing.ExpiresAt = existing.ExpiresAt.AddMinutes( durationMinutes );
			existing.Multiplier = Math.Max( existing.Multiplier, multiplier );
		}
		else
		{
			var serverBoost = new ServerBoost
			{
				Type = type,
				Multiplier = multiplier,
				ExpiresAt = DateTime.UtcNow.AddMinutes( durationMinutes ),
				ActivatedBy = playerName,
				ActivatedBySteamId = steamId
			};
			_serverBoosts.Add( serverBoost );
			OnServerBoostActivated?.Invoke( serverBoost );
		}
	}

	/// <summary>
	/// Request active server boosts from other players (call when joining)
	/// </summary>
	public void RequestServerBoosts()
	{
		if ( !GameNetworkSystem.IsActive ) return;

		var mySteamId = (long)Sandbox.Utility.Steam.SteamId;
		BroadcastBoostRequest( mySteamId );
		Log.Info( "Requesting server boosts from other players" );
	}

	/// <summary>
	/// Broadcast a request for server boosts
	/// </summary>
	[Rpc.Broadcast]
	private void BroadcastBoostRequest( long requestingSteamId )
	{
		// Don't respond to our own request
		if ( requestingSteamId == (long)Sandbox.Utility.Steam.SteamId ) return;

		// Share all active server boosts with the requesting player
		foreach ( var boost in _serverBoosts.Where( b => !b.IsExpired ) )
		{
			int remainingMinutes = (int)boost.TimeRemaining.TotalMinutes;
			if ( remainingMinutes > 0 )
			{
				ShareServerBoostWithPlayer( boost.Type, boost.Multiplier, remainingMinutes, boost.ActivatedBy, boost.ActivatedBySteamId, requestingSteamId );
			}
		}
	}

	/// <summary>
	/// Share a server boost with a specific player who just joined
	/// </summary>
	[Rpc.Broadcast]
	private void ShareServerBoostWithPlayer( ShopItemType type, float multiplier, int remainingMinutes, string activatedBy, long activatedBySteamId, long targetSteamId )
	{
		// Only process if we're the target
		if ( targetSteamId != (long)Sandbox.Utility.Steam.SteamId ) return;

		var existing = _serverBoosts.FirstOrDefault( b => b.Type == type );
		if ( existing != null )
		{
			// Update if the shared boost has more time
			var newExpiration = DateTime.UtcNow.AddMinutes( remainingMinutes );
			if ( newExpiration > existing.ExpiresAt )
			{
				existing.ExpiresAt = newExpiration;
			}
		}
		else
		{
			var serverBoost = new ServerBoost
			{
				Type = type,
				Multiplier = multiplier,
				ExpiresAt = DateTime.UtcNow.AddMinutes( remainingMinutes ),
				ActivatedBy = activatedBy,
				ActivatedBySteamId = activatedBySteamId
			};
			_serverBoosts.Add( serverBoost );
			OnServerBoostActivated?.Invoke( serverBoost );
			Log.Info( $"Received server boost {type} from sync ({remainingMinutes} min remaining)" );
		}
	}

	/// <summary>
	/// Get active boost multiplier for a specific type (includes personal and server boosts)
	/// </summary>
	public float GetBoostMultiplier( ShopItemType type )
	{
		float personalMultiplier = 1.0f;
		float serverMultiplier = 1.0f;

		var personalBoost = _activeBoosts.FirstOrDefault( b => b.Type == type && !b.IsExpired );
		if ( personalBoost != null )
			personalMultiplier = personalBoost.Multiplier;

		var serverBoost = _serverBoosts.FirstOrDefault( b => b.Type == type && !b.IsExpired );
		if ( serverBoost != null )
			serverMultiplier = serverBoost.Multiplier;

		// Return the higher of the two (they don't stack multiplicatively)
		return Math.Max( personalMultiplier, serverMultiplier );
	}

	/// <summary>
	/// Get the active boost for a type (personal boost takes priority for display)
	/// </summary>
	public ActiveBoost GetActiveBoost( ShopItemType type )
	{
		return _activeBoosts.FirstOrDefault( b => b.Type == type && !b.IsExpired );
	}

	/// <summary>
	/// Get the active server boost for a type
	/// </summary>
	public ServerBoost GetServerBoost( ShopItemType type )
	{
		return _serverBoosts.FirstOrDefault( b => b.Type == type && !b.IsExpired );
	}

	/// <summary>
	/// Check if a boost is active (personal or server)
	/// </summary>
	public bool HasActiveBoost( ShopItemType type )
	{
		return _activeBoosts.Any( b => b.Type == type && !b.IsExpired )
			|| _serverBoosts.Any( b => b.Type == type && !b.IsExpired );
	}

	/// <summary>
	/// Reset all skill points
	/// </summary>
	private void ResetSkills()
	{
		var tamer = TamerManager.Instance?.CurrentTamer;
		var skillTree = TamerManager.Instance?.SkillTree;
		if ( tamer == null || skillTree == null ) return;

		// Calculate total points spent by summing each skill's cost
		int refundedPoints = 0;
		foreach ( var skillId in tamer.UnlockedSkills )
		{
			var node = skillTree.GetNode( skillId );
			if ( node != null )
				refundedPoints += node.SkillPointCost;
		}

		tamer.SkillPoints += refundedPoints;
		tamer.UnlockedSkills.Clear();

		Log.Info( $"Reset skills, refunded {refundedPoints} points" );
	}

	/// <summary>
	/// Revive all monsters in party
	/// </summary>
	private void ReviveAllMonsters()
	{
		var monsters = MonsterManager.Instance?.OwnedMonsters;
		if ( monsters == null ) return;

		foreach ( var monster in monsters )
		{
			monster.CurrentHP = monster.MaxHP;
		}

		Log.Info( "Revived all monsters" );
	}

	protected override void OnUpdate()
	{
		// Check for expired personal boosts
		var expired = _activeBoosts.Where( b => b.IsExpired ).ToList();
		foreach ( var boost in expired )
		{
			_activeBoosts.Remove( boost );
			OnBoostExpired?.Invoke( boost );
			Log.Info( $"Boost expired: {boost.Type}" );
		}

		// Check for expired server boosts
		var expiredServer = _serverBoosts.Where( b => b.IsExpired ).ToList();
		foreach ( var boost in expiredServer )
		{
			_serverBoosts.Remove( boost );
			OnServerBoostExpired?.Invoke( boost );
			Log.Info( $"Server boost expired: {boost.Type}" );
		}
	}

	/// <summary>
	/// Get all active boosts (both personal and server) for UI display
	/// </summary>
	public List<(ShopItemType Type, float Multiplier, TimeSpan TimeRemaining, bool IsServer, string ActivatedBy)> GetAllActiveBoosts()
	{
		var result = new List<(ShopItemType, float, TimeSpan, bool, string)>();

		foreach ( var boost in _activeBoosts.Where( b => !b.IsExpired ) )
		{
			result.Add( (boost.Type, boost.Multiplier, boost.TimeRemaining, false, null) );
		}

		foreach ( var boost in _serverBoosts.Where( b => !b.IsExpired ) )
		{
			result.Add( (boost.Type, boost.Multiplier, boost.TimeRemaining, true, boost.ActivatedBy) );
		}

		return result;
	}
}
