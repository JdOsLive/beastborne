using Sandbox;
using Sandbox.Network;
using Sandbox.Services;
using Beastborne.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Beastborne.Core;

/// <summary>
/// Manages player-to-player trading via RPC broadcast.
/// Follows the same networking pattern as CompetitiveManager.
/// </summary>
public sealed class TradingManager : Component, Component.INetworkListener
{
	public static TradingManager Instance { get; private set; }

	private const float TRADE_COOLDOWN_SECONDS = 30f;
	private const float LOCK_COUNTDOWN_SECONDS = 5f;
	private const float REQUEST_TIMEOUT_SECONDS = 30f;

	// Current trade session (null when not trading)
	public TradeSession CurrentTrade { get; private set; }

	// Lock-in countdown
	public float LockCountdown { get; private set; } = 0;
	private bool _lockCountdownActive = false;

	// Cooldown
	private DateTime _lastTradeCompleted = DateTime.MinValue;
	public float CooldownRemaining => Math.Max( 0, TRADE_COOLDOWN_SECONDS - (float)(DateTime.UtcNow - _lastTradeCompleted).TotalSeconds );
	public bool IsOnCooldown => CooldownRemaining > 0;

	// Pending incoming request
	public string PendingRequestFromName { get; private set; }
	public string PendingRequestFromConnectionId { get; private set; }
	private DateTime _pendingRequestTime;

	// Local connection info
	private string LocalConnectionId => Connection.Local?.Id.ToString() ?? "";
	private string LocalPlayerName => Connection.Local?.DisplayName ?? "Player";
	private long LocalSteamId => Connection.Local?.SteamId ?? 0;

	// Events
	public Action<string, string> OnTradeRequestReceived; // connectionId, playerName
	public Action OnTradeOpened;
	public Action OnTradeUpdated;
	public Action<bool> OnTradeCompleted; // true = success
	public Action<string> OnTradeCancelled; // reason
	public Action OnLockCountdownStarted;
	public Action OnLockCountdownCancelled;

	// ═══════════════════════════════════════════════════════════════
	// LIFECYCLE
	// ═══════════════════════════════════════════════════════════════

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Log.Info( "TradingManager initialized" );
		}
		else
		{
			Destroy();
		}
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "TradingManager";
		go.Flags = GameObjectFlags.DontDestroyOnLoad;
		go.Components.Create<TradingManager>();
	}

	protected override void OnUpdate()
	{
		// Tick lock countdown
		if ( _lockCountdownActive && CurrentTrade?.State == TradeState.Locked )
		{
			LockCountdown -= Time.Delta;
			if ( LockCountdown <= 0 )
			{
				_lockCountdownActive = false;
				ExecuteTrade();
			}
		}

		// Timeout pending requests
		if ( PendingRequestFromConnectionId != null )
		{
			if ( (DateTime.UtcNow - _pendingRequestTime).TotalSeconds > REQUEST_TIMEOUT_SECONDS )
			{
				PendingRequestFromConnectionId = null;
				PendingRequestFromName = null;
				Log.Info( "[Trade] Pending request timed out" );
			}
		}
	}

	void INetworkListener.OnActive( Connection connection )
	{
		Log.Info( $"[Trade] Player connected: {connection.DisplayName}" );
	}

	void INetworkListener.OnDisconnected( Connection connection )
	{
		// If our trade partner disconnected, cancel the trade
		if ( CurrentTrade != null )
		{
			var partnerId = CurrentTrade.GetPartnerConnectionId( LocalConnectionId );
			if ( connection.Id.ToString() == partnerId )
			{
				CancelTradeLocal( "Partner disconnected" );
			}
		}

		// Clear pending request if it was from the disconnected player
		if ( PendingRequestFromConnectionId == connection.Id.ToString() )
		{
			PendingRequestFromConnectionId = null;
			PendingRequestFromName = null;
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// TRADE REQUEST FLOW
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Send a trade request to another player
	/// </summary>
	public bool SendTradeRequest( string targetConnectionId, string targetName )
	{
		if ( CurrentTrade != null )
		{
			Log.Warning( "[Trade] Already in a trade" );
			return false;
		}

		if ( IsOnCooldown )
		{
			Log.Warning( $"[Trade] On cooldown: {CooldownRemaining:F0}s remaining" );
			return false;
		}

		if ( !GameNetworkSystem.IsActive )
		{
			Log.Warning( "[Trade] Not connected to a server" );
			return false;
		}

		BroadcastTradeRequest( LocalConnectionId, LocalSteamId, LocalPlayerName, targetConnectionId );
		Log.Info( $"[Trade] Sent trade request to {targetName}" );
		return true;
	}

	/// <summary>
	/// Accept a pending trade request
	/// </summary>
	public void AcceptTradeRequest()
	{
		if ( PendingRequestFromConnectionId == null ) return;

		var requesterId = PendingRequestFromConnectionId;
		var requesterName = PendingRequestFromName;

		// Create the trade session locally
		CurrentTrade = new TradeSession
		{
			Player1ConnectionId = requesterId,
			Player2ConnectionId = LocalConnectionId,
			Player1Name = requesterName,
			Player2Name = LocalPlayerName,
			State = TradeState.Open
		};

		PendingRequestFromConnectionId = null;
		PendingRequestFromName = null;

		BroadcastTradeAccept( LocalConnectionId, requesterId );
		OnTradeOpened?.Invoke();
		Log.Info( $"[Trade] Accepted trade with {requesterName}" );
	}

	/// <summary>
	/// Decline a pending trade request
	/// </summary>
	public void DeclineTradeRequest()
	{
		if ( PendingRequestFromConnectionId == null ) return;

		BroadcastTradeDecline( LocalConnectionId, PendingRequestFromConnectionId );
		PendingRequestFromConnectionId = null;
		PendingRequestFromName = null;
	}

	// ═══════════════════════════════════════════════════════════════
	// OFFER MANAGEMENT
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Add a monster to your trade offer
	/// </summary>
	public bool AddMonsterToOffer( Guid monsterId )
	{
		if ( CurrentTrade == null || CurrentTrade.State != TradeState.Open ) return false;

		var offer = CurrentTrade.GetMyOffer( LocalConnectionId );
		if ( offer.IsReady ) return false;
		if ( offer.OfferedMonsterIds.Contains( monsterId ) ) return false;
		if ( offer.OfferedMonsterIds.Count >= 3 ) return false; // Max 3 monsters per trade

		// Validate monster exists and is tradeable
		var monster = MonsterManager.Instance?.OwnedMonsters.FirstOrDefault( m => m.Id == monsterId );
		if ( monster == null ) return false;
		if ( monster.IsInExpedition || monster.IsInArenaTeam ) return false;

		offer.OfferedMonsterIds.Add( monsterId );
		SyncOffer();
		return true;
	}

	/// <summary>
	/// Remove a monster from your trade offer
	/// </summary>
	public bool RemoveMonsterFromOffer( Guid monsterId )
	{
		if ( CurrentTrade == null || CurrentTrade.State != TradeState.Open ) return false;

		var offer = CurrentTrade.GetMyOffer( LocalConnectionId );
		if ( offer.IsReady ) return false;

		if ( offer.OfferedMonsterIds.Remove( monsterId ) )
		{
			SyncOffer();
			return true;
		}
		return false;
	}

	/// <summary>
	/// Add an item to your trade offer
	/// </summary>
	public bool AddItemToOffer( string itemId, int quantity = 1 )
	{
		if ( CurrentTrade == null || CurrentTrade.State != TradeState.Open ) return false;

		var offer = CurrentTrade.GetMyOffer( LocalConnectionId );
		if ( offer.IsReady ) return false;

		// Validate item exists and is tradeable
		var item = ItemManager.Instance?.GetItem( itemId );
		if ( item == null ) return false;
		if ( item.Category == ItemCategory.QuestItem ) return false;

		int owned = ItemManager.Instance?.GetItemCount( itemId ) ?? 0;
		int alreadyOffered = offer.OfferedItems.GetValueOrDefault( itemId, 0 );
		if ( alreadyOffered + quantity > owned ) return false;

		offer.OfferedItems[itemId] = alreadyOffered + quantity;
		SyncOffer();
		return true;
	}

	/// <summary>
	/// Remove an item from your trade offer
	/// </summary>
	public bool RemoveItemFromOffer( string itemId, int quantity = 1 )
	{
		if ( CurrentTrade == null || CurrentTrade.State != TradeState.Open ) return false;

		var offer = CurrentTrade.GetMyOffer( LocalConnectionId );
		if ( offer.IsReady ) return false;

		if ( !offer.OfferedItems.ContainsKey( itemId ) ) return false;

		offer.OfferedItems[itemId] -= quantity;
		if ( offer.OfferedItems[itemId] <= 0 )
			offer.OfferedItems.Remove( itemId );

		SyncOffer();
		return true;
	}

	/// <summary>
	/// Toggle ready state
	/// </summary>
	public void SetReady( bool ready )
	{
		if ( CurrentTrade == null || CurrentTrade.State != TradeState.Open ) return;

		var offer = CurrentTrade.GetMyOffer( LocalConnectionId );
		offer.IsReady = ready;

		var partnerId = CurrentTrade.GetPartnerConnectionId( LocalConnectionId );
		BroadcastReadyState( LocalConnectionId, partnerId, ready );

		// Check if both ready
		if ( CurrentTrade.BothReady )
		{
			StartLockCountdown();
		}

		OnTradeUpdated?.Invoke();
	}

	/// <summary>
	/// Cancel the current trade
	/// </summary>
	public void CancelTrade()
	{
		if ( CurrentTrade == null ) return;

		var partnerId = CurrentTrade.GetPartnerConnectionId( LocalConnectionId );
		BroadcastTradeCancel( LocalConnectionId, partnerId );
		CancelTradeLocal( "You cancelled the trade" );
	}

	// ═══════════════════════════════════════════════════════════════
	// SYNC & LOCK
	// ═══════════════════════════════════════════════════════════════

	private void SyncOffer()
	{
		if ( CurrentTrade == null ) return;

		var offer = CurrentTrade.GetMyOffer( LocalConnectionId );
		var partnerId = CurrentTrade.GetPartnerConnectionId( LocalConnectionId );

		// Serialize offer data
		var monsterIds = JsonSerializer.Serialize( offer.OfferedMonsterIds.Select( id => id.ToString() ).ToList() );
		var items = JsonSerializer.Serialize( offer.OfferedItems );

		// Serialize monster preview data for partner
		var monsterPreviews = SerializeMonsterPreviews( offer.OfferedMonsterIds );

		BroadcastOfferUpdate( LocalConnectionId, partnerId, monsterIds, items, monsterPreviews );
		OnTradeUpdated?.Invoke();
	}

	private void StartLockCountdown()
	{
		if ( CurrentTrade == null ) return;

		CurrentTrade.State = TradeState.Locked;
		CurrentTrade.GetMyOffer( LocalConnectionId ).IsLocked = true;
		LockCountdown = LOCK_COUNTDOWN_SECONDS;
		_lockCountdownActive = true;

		OnLockCountdownStarted?.Invoke();
		Log.Info( "[Trade] Lock countdown started" );
	}

	private void CancelTradeLocal( string reason )
	{
		_lockCountdownActive = false;
		LockCountdown = 0;
		CurrentTrade = null;
		PendingRequestFromConnectionId = null;
		PendingRequestFromName = null;
		OnTradeCancelled?.Invoke( reason );
		Log.Info( $"[Trade] Cancelled: {reason}" );
	}

	// ═══════════════════════════════════════════════════════════════
	// TRADE EXECUTION
	// ═══════════════════════════════════════════════════════════════

	private void ExecuteTrade()
	{
		if ( CurrentTrade == null || CurrentTrade.State != TradeState.Locked ) return;

		var myOffer = CurrentTrade.GetMyOffer( LocalConnectionId );
		var partnerOffer = CurrentTrade.GetPartnerOffer( LocalConnectionId );
		var partnerId = CurrentTrade.GetPartnerConnectionId( LocalConnectionId );

		// Validate I still have everything I offered
		if ( !ValidateOffer( myOffer ) )
		{
			BroadcastTradeCancel( LocalConnectionId, partnerId );
			CancelTradeLocal( "Your offer could not be validated" );
			return;
		}

		// Serialize monsters BEFORE removing them (they won't be found after removal)
		var monstersToSend = new List<Monster>();
		foreach ( var monsterId in myOffer.OfferedMonsterIds )
		{
			var monster = MonsterManager.Instance?.OwnedMonsters.FirstOrDefault( m => m.Id == monsterId );
			if ( monster != null ) monstersToSend.Add( monster );
		}
		var monsterData = SerializeMonstersForTrade( monstersToSend );

		// Now remove my offered items/monsters
		foreach ( var monsterId in myOffer.OfferedMonsterIds )
		{
			MonsterManager.Instance?.RemoveMonster( monsterId );
		}
		foreach ( var (itemId, qty) in myOffer.OfferedItems )
		{
			ItemManager.Instance?.RemoveItem( itemId, qty );
		}
		var itemData = JsonSerializer.Serialize( myOffer.OfferedItems );

		BroadcastTradeExecute( LocalConnectionId, partnerId, monsterData, itemData );

		// Complete on my side
		CurrentTrade.State = TradeState.Completed;
		_lastTradeCompleted = DateTime.UtcNow;
		_lockCountdownActive = false;

		// Track achievements
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer != null )
		{
			tamer.TotalTradesCompleted++;
			AchievementManager.Instance?.CheckProgress( AchievementRequirement.TotalTradesCompleted, tamer.TotalTradesCompleted );
			Stats.SetValue( "trades-done", tamer.TotalTradesCompleted );
		}

		// Collect trade partner's tamer card
		var partnerName = CurrentTrade.GetPartnerName( LocalConnectionId );
		var partnerConn = Connection.All.FirstOrDefault( c => c.Id.ToString() == partnerId );
		if ( partnerConn != null && partnerConn.SteamId != 0 )
		{
			var profile = ChatManager.Instance?.GetProfileByConnectionId( partnerId );
			TamerManager.Instance?.CollectTamerCard(
				partnerConn.SteamId,
				partnerName,
				profile?.Level ?? 0,
				profile?.ArenaRank,
				profile?.ArenaPoints ?? 0,
				profile?.FavoriteMonsterSpeciesId,
				profile?.AchievementCount ?? 0,
				0f,
				gender: profile?.Gender,
				favoriteExpeditionId: profile?.FavoriteExpeditionId,
				title: profile?.Title,
				titleColor: profile?.TitleColor,
				arenaWins: profile?.ArenaWins ?? 0,
				arenaLosses: profile?.ArenaLosses ?? 0,
				highestExpedition: profile?.HighestExpedition ?? 0,
				monstersCaught: profile?.MonstersCaught ?? 0,
				totalPlayTimeMinutes: profile?.TotalPlayTimeMinutes ?? 0,
				battlesWon: profile?.BattlesWon ?? 0,
				monstersBred: profile?.MonstersBred ?? 0,
				monstersEvolved: profile?.MonstersEvolved ?? 0,
				totalExpeditionsCompleted: profile?.TotalExpeditionsCompleted ?? 0,
				totalTradesCompleted: profile?.TotalTradesCompleted ?? 0
			);
		}

		TamerManager.Instance?.SaveToCloud();
		OnTradeCompleted?.Invoke( true );

		CurrentTrade = null;
		Log.Info( "[Trade] Trade executed successfully" );
	}

	private bool ValidateOffer( TradeOffer offer )
	{
		// Validate monsters
		foreach ( var monsterId in offer.OfferedMonsterIds )
		{
			var monster = MonsterManager.Instance?.OwnedMonsters.FirstOrDefault( m => m.Id == monsterId );
			if ( monster == null ) return false;
			if ( monster.IsInExpedition || monster.IsInArenaTeam ) return false;
		}

		// Validate items
		foreach ( var (itemId, qty) in offer.OfferedItems )
		{
			int owned = ItemManager.Instance?.GetItemCount( itemId ) ?? 0;
			if ( owned < qty ) return false;

			var item = ItemManager.Instance?.GetItem( itemId );
			if ( item?.Category == ItemCategory.QuestItem ) return false;
		}

		return true;
	}

	// ═══════════════════════════════════════════════════════════════
	// SERIALIZATION
	// ═══════════════════════════════════════════════════════════════

	private string SerializeMonsterPreviews( List<Guid> monsterIds )
	{
		var previews = new List<TradeMonsterData>();
		foreach ( var id in monsterIds )
		{
			var m = MonsterManager.Instance?.OwnedMonsters.FirstOrDefault( x => x.Id == id );
			if ( m == null ) continue;

			previews.Add( MonsterToTradeData( m ) );
		}
		return JsonSerializer.Serialize( previews );
	}

	private string SerializeMonstersForTrade( List<Monster> monsters )
	{
		var data = monsters.Select( MonsterToTradeData ).ToList();
		return JsonSerializer.Serialize( data );
	}

	private TradeMonsterData MonsterToTradeData( Monster m )
	{
		return new TradeMonsterData
		{
			S = m.SpeciesId,
			N = m.Nickname,
			Lv = m.Level,
			XP = m.CurrentXP,
			HP = m.MaxHP,
			Atk = m.ATK,
			Def = m.DEF,
			SpA = m.SpA,
			SpD = m.SpD,
			Spe = m.SPD,
			GHP = m.Genetics?.HPGene ?? 0,
			GAtk = m.Genetics?.ATKGene ?? 0,
			GDef = m.Genetics?.DEFGene ?? 0,
			GSpA = m.Genetics?.SpAGene ?? 0,
			GSpD = m.Genetics?.SpDGene ?? 0,
			GSpe = m.Genetics?.SPDGene ?? 0,
			Nat = (int)(m.Genetics?.Nature ?? 0),
			M = m.Moves?.Select( mv => mv.MoveId ).ToList() ?? new(),
			PP = m.Moves?.Select( mv => mv.CurrentPP ).ToList() ?? new(),
			T = m.Traits != null ? new List<string>( m.Traits ) : new(),
			I = m.HeldItemId,
			Gen = m.Generation,
			Bred = m.Contract == null,
			OTN = m.OriginalTrainerName,
			OTI = m.OriginalTrainerId,
			BF = m.BattlesFought,
			TD = m.TotalDamageDealt,
			TK = m.TotalKnockouts,
			BD = m.BossesDefeated,
			EC = m.ExpeditionsCompleted
		};
	}

	private Monster TradeDataToMonster( TradeMonsterData d )
	{
		var monster = new Monster
		{
			Id = Guid.NewGuid(), // New ID for the traded monster
			SpeciesId = d.S,
			Nickname = d.N,
			Level = d.Lv,
			CurrentXP = d.XP,
			MaxHP = d.HP,
			ATK = d.Atk,
			DEF = d.Def,
			SpA = d.SpA,
			SpD = d.SpD,
			SPD = d.Spe,
			Genetics = new Genetics
			{
				HPGene = d.GHP,
				ATKGene = d.GAtk,
				DEFGene = d.GDef,
				SpAGene = d.GSpA,
				SpDGene = d.GSpD,
				SPDGene = d.GSpe,
				Nature = (NatureType)d.Nat
			},
			HeldItemId = d.I,
			Generation = d.Gen,
			BattlesFought = d.BF,
			TotalDamageDealt = d.TD,
			TotalKnockouts = d.TK,
			BossesDefeated = d.BD,
			ExpeditionsCompleted = d.EC,
			ObtainedAt = DateTime.UtcNow,
			OriginalTrainerName = d.OTN,
			OriginalTrainerId = d.OTI
		};

		// Reconstruct moves
		if ( d.M != null )
		{
			for ( int i = 0; i < d.M.Count; i++ )
			{
				monster.Moves.Add( new MonsterMove
				{
					MoveId = d.M[i],
					CurrentPP = d.PP != null && i < d.PP.Count ? d.PP[i] : 5
				} );
			}
		}

		// Reconstruct traits
		if ( d.T != null )
			monster.Traits = new List<string>( d.T );

		// Recalculate stats from genetics (ensures correctness)
		MonsterManager.Instance?.RecalculateStats( monster );
		monster.FullHeal();

		// Add journal entry
		monster.AddJournalEntry(
			$"Received in a trade!",
			JournalEntryType.Milestone
		);

		return monster;
	}

	// ═══════════════════════════════════════════════════════════════
	// RPC BROADCASTS
	// ═══════════════════════════════════════════════════════════════

	[Rpc.Broadcast]
	private void BroadcastTradeRequest( string senderConnectionId, long senderSteamId, string senderName, string targetConnectionId )
	{
		// Only process if we are the target
		if ( targetConnectionId != LocalConnectionId ) return;
		if ( senderConnectionId == LocalConnectionId ) return;

		// Ignore if already in a trade
		if ( CurrentTrade != null ) return;

		PendingRequestFromConnectionId = senderConnectionId;
		PendingRequestFromName = senderName;
		_pendingRequestTime = DateTime.UtcNow;

		OnTradeRequestReceived?.Invoke( senderConnectionId, senderName );
		Log.Info( $"[Trade] Received trade request from {senderName}" );
	}

	[Rpc.Broadcast]
	private void BroadcastTradeAccept( string accepterConnectionId, string requesterConnectionId )
	{
		// Only process if we are the original requester
		if ( requesterConnectionId != LocalConnectionId ) return;
		if ( accepterConnectionId == LocalConnectionId ) return;

		// Get accepter's name from connections
		var accepterName = Connection.All.FirstOrDefault( c => c.Id.ToString() == accepterConnectionId )?.DisplayName ?? "Player";

		CurrentTrade = new TradeSession
		{
			Player1ConnectionId = LocalConnectionId,
			Player2ConnectionId = accepterConnectionId,
			Player1Name = LocalPlayerName,
			Player2Name = accepterName,
			State = TradeState.Open
		};

		OnTradeOpened?.Invoke();
		Log.Info( $"[Trade] {accepterName} accepted trade request" );
	}

	[Rpc.Broadcast]
	private void BroadcastTradeDecline( string declinerConnectionId, string requesterConnectionId )
	{
		if ( requesterConnectionId != LocalConnectionId ) return;
		if ( declinerConnectionId == LocalConnectionId ) return;

		NotificationManager.Instance?.AddNotification(
			NotificationType.Info,
			"Trade Declined",
			"The player declined your trade request.",
			5f
		);
	}

	[Rpc.Broadcast]
	private void BroadcastOfferUpdate( string senderConnectionId, string targetConnectionId, string monsterIds, string items, string monsterPreviews )
	{
		if ( targetConnectionId != LocalConnectionId ) return;
		if ( senderConnectionId == LocalConnectionId ) return;
		if ( CurrentTrade == null ) return;

		try
		{
			var partnerOffer = CurrentTrade.GetPartnerOffer( LocalConnectionId );

			// Update partner's offered monster previews (we store the preview data locally)
			var idList = JsonSerializer.Deserialize<List<string>>( monsterIds ) ?? new();
			partnerOffer.OfferedMonsterIds = idList.Select( id => Guid.TryParse( id, out var g ) ? g : Guid.Empty )
				.Where( g => g != Guid.Empty ).ToList();

			partnerOffer.OfferedItems = JsonSerializer.Deserialize<Dictionary<string, int>>( items ) ?? new();

			// Store partner monster previews for display
			_partnerMonsterPreviews = JsonSerializer.Deserialize<List<TradeMonsterData>>( monsterPreviews ) ?? new();

			OnTradeUpdated?.Invoke();
		}
		catch ( Exception e )
		{
			Log.Warning( $"[Trade] Failed to parse offer update: {e.Message}" );
		}
	}

	// Partner monster preview data for UI display
	private List<TradeMonsterData> _partnerMonsterPreviews = new();
	public IReadOnlyList<TradeMonsterData> PartnerMonsterPreviews => _partnerMonsterPreviews;

	[Rpc.Broadcast]
	private void BroadcastReadyState( string senderConnectionId, string targetConnectionId, bool isReady )
	{
		if ( targetConnectionId != LocalConnectionId ) return;
		if ( senderConnectionId == LocalConnectionId ) return;
		if ( CurrentTrade == null ) return;

		var partnerOffer = CurrentTrade.GetPartnerOffer( LocalConnectionId );
		partnerOffer.IsReady = isReady;

		// If partner unreadied, cancel lock countdown
		if ( !isReady && CurrentTrade.State == TradeState.Locked )
		{
			CurrentTrade.State = TradeState.Open;
			_lockCountdownActive = false;
			CurrentTrade.GetMyOffer( LocalConnectionId ).IsLocked = false;
			partnerOffer.IsLocked = false;
			OnLockCountdownCancelled?.Invoke();
		}

		// If both ready, start lock
		if ( CurrentTrade.BothReady && CurrentTrade.State == TradeState.Open )
		{
			StartLockCountdown();
		}

		OnTradeUpdated?.Invoke();
	}

	[Rpc.Broadcast]
	private void BroadcastTradeCancel( string senderConnectionId, string targetConnectionId )
	{
		if ( targetConnectionId != LocalConnectionId ) return;
		if ( senderConnectionId == LocalConnectionId ) return;

		CancelTradeLocal( "Partner cancelled the trade" );
	}

	[Rpc.Broadcast]
	private void BroadcastTradeExecute( string senderConnectionId, string targetConnectionId, string monsterData, string itemData )
	{
		if ( targetConnectionId != LocalConnectionId ) return;
		if ( senderConnectionId == LocalConnectionId ) return;

		try
		{
			// Receive monsters from partner
			if ( !string.IsNullOrEmpty( monsterData ) && monsterData != "[]" )
			{
				var monsters = JsonSerializer.Deserialize<List<TradeMonsterData>>( monsterData ) ?? new();
				foreach ( var data in monsters )
				{
					var monster = TradeDataToMonster( data );
					MonsterManager.Instance?.AddMonster( monster );
					Log.Info( $"[Trade] Received monster: {monster.Nickname} Lv.{monster.Level}" );
				}
			}

			// Receive items from partner
			if ( !string.IsNullOrEmpty( itemData ) && itemData != "{}" )
			{
				var items = JsonSerializer.Deserialize<Dictionary<string, int>>( itemData ) ?? new();
				foreach ( var (itemId, qty) in items )
				{
					ItemManager.Instance?.AddItem( itemId, qty );
					Log.Info( $"[Trade] Received item: {itemId} x{qty}" );
				}
			}

			Log.Info( "[Trade] Received partner's trade items" );
		}
		catch ( Exception e )
		{
			Log.Warning( $"[Trade] Failed to receive trade items: {e.Message}" );
		}
	}

	// ═══════════════════════════════════════════════════════════════
	// HELPERS
	// ═══════════════════════════════════════════════════════════════

	public bool IsInTrade => CurrentTrade != null;
	public bool HasPendingRequest => PendingRequestFromConnectionId != null;

	/// <summary>
	/// Get monster preview data from a specific monster ID in my offer
	/// </summary>
	public Monster GetMyOfferedMonster( Guid monsterId )
	{
		return MonsterManager.Instance?.OwnedMonsters.FirstOrDefault( m => m.Id == monsterId );
	}

	/// <summary>
	/// Get partner monster preview from index
	/// </summary>
	public TradeMonsterData GetPartnerMonsterPreview( int index )
	{
		if ( index < 0 || index >= _partnerMonsterPreviews.Count ) return null;
		return _partnerMonsterPreviews[index];
	}

	/// <summary>
	/// Get the species name for a trade monster preview
	/// </summary>
	public string GetSpeciesName( string speciesId )
	{
		return MonsterManager.Instance?.GetSpecies( speciesId )?.Name ?? speciesId;
	}
}
