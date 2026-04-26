using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;

namespace Beastborne.Core;

/// <summary>
/// Manages pending gifts delivered to the player by the Beastborne dev team
/// (admin-granted rewards via the web dashboard). Fetches the pending list on
/// save-load and whenever the player returns to the main menu, caches it in
/// memory, and exposes a <see cref="ClaimAllAsync"/> that applies the rewards
/// to the tamer and flushes a single cloud save.
///
/// Lifecycle:
///   1. <see cref="EnsureInstance"/> (wired from <c>GameManager.InitializeManagers</c>)
///   2. On save-load, fire <see cref="RefreshAsync"/>
///   3. Subscribe to <c>GameManager.OnStateChanged</c> — whenever the game
///      returns to MainMenu, re-fetch the list. One-shot per transition, not
///      a tick-level poll.
/// </summary>
public sealed class GiftManager : Component
{
	public static GiftManager Instance { get; private set; }

	/// <summary>
	/// Currently cached unclaimed gifts (server-side sort preserved). Empty
	/// when there are no pending rewards or the fetch has not yet completed.
	/// </summary>
	public List<PendingGift> PendingGifts { get; private set; } = new();

	public int PendingCount => PendingGifts.Count;
	public bool HasPending => PendingGifts.Count > 0;

	/// <summary>
	/// Fires after a refresh whenever at least one gift is pending. The UI
	/// layer listens and auto-opens the inbox popup on the main menu.
	/// </summary>
	public event Action OnGiftsAvailable;

	/// <summary>Fires after a successful claim — UI can close the popup.</summary>
	public event Action OnGiftsClaimed;

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Log.Info( "GiftManager initialized" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	protected override void OnStart()
	{
		// Kick the initial fetch once the save blob is loaded AND we have a
		// tamer — both are needed because ClaimAllAsync writes to the tamer.
		if ( SaveService.Instance != null && SaveService.Instance.IsLoaded )
		{
			_ = RefreshAsync();
		}
		else if ( SaveService.Instance != null )
		{
			SaveService.Instance.OnSaveLoaded += () => _ = RefreshAsync();
		}

		// Re-fetch on every return-to-main-menu. Single shot per transition,
		// NOT a per-frame tick — server side is cheap but we still respect it.
		if ( GameManager.Instance != null )
		{
			GameManager.Instance.OnStateChanged += HandleStateChanged;
		}
	}

	protected override void OnDestroy()
	{
		if ( GameManager.Instance != null )
		{
			GameManager.Instance.OnStateChanged -= HandleStateChanged;
		}
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "GiftManager";
		go.Components.Create<GiftManager>();
	}

	private void HandleStateChanged( GameState oldState, GameState newState )
	{
		if ( newState == GameState.MainMenu && oldState != GameState.MainMenu )
		{
			_ = RefreshAsync();
		}
	}

	/// <summary>
	/// Pull the latest pending-gift list from the server and cache it. Fires
	/// <see cref="OnGiftsAvailable"/> if the refresh results in a non-empty list.
	/// </summary>
	public async Task RefreshAsync()
	{
		if ( TamerManager.Instance?.CurrentTamer == null ) return;

		var fresh = await GiftApiClient.GetPendingAsync();
		PendingGifts = fresh ?? new List<PendingGift>();

		if ( PendingGifts.Count > 0 )
		{
			Log.Info( $"[GiftManager] {PendingGifts.Count} pending gift(s) for player" );
			OnGiftsAvailable?.Invoke();
		}
	}

	/// <summary>
	/// Claim every currently-pending gift. On server success, applies each
	/// reward to the tamer, fires a single cloud save, and posts one staggered
	/// notification per claimed gift. Safe no-op if there are no pending gifts.
	/// </summary>
	public async Task ClaimAllAsync()
	{
		if ( !HasPending ) return;

		var tamerMgr = TamerManager.Instance;
		var tamer = tamerMgr?.CurrentTamer;
		if ( tamer == null )
		{
			Log.Warning( "[GiftManager] ClaimAllAsync: no tamer loaded" );
			return;
		}

		var ids = PendingGifts.Select( g => g.Id ).ToList();
		var claimed = await GiftApiClient.ClaimAsync( ids );

		if ( claimed == null || claimed.Count == 0 )
		{
			Log.Warning( "[GiftManager] ClaimAllAsync: server returned no claimed gifts" );
			return;
		}

		// Apply every reward to the tamer snapshot.
		foreach ( var gift in claimed )
		{
			tamer.Gold += gift.Gold;
			tamer.Gems += gift.Gems;
			tamer.ContractInk += gift.Ink;
			tamer.BossTokens += gift.BossTokens;

			if ( gift.Items != null && gift.Items.Count > 0 )
			{
				var itemMgr = ItemManager.Instance;
				if ( itemMgr == null )
				{
					Log.Warning( "[GiftManager] ItemManager missing — item rewards dropped on floor" );
				}
				else
				{
					foreach ( var kv in gift.Items )
					{
						if ( string.IsNullOrEmpty( kv.Key ) || kv.Value <= 0 ) continue;
						itemMgr.AddItem( kv.Key, kv.Value );
					}
				}
			}
		}

		// Persist the tamer snapshot + flush once for the whole claim batch.
		tamerMgr.SaveToCloud();
		if ( SaveService.Instance != null )
		{
			try
			{
				await SaveService.Instance.FlushAsync();
			}
			catch ( Exception e )
			{
				Log.Warning( $"[GiftManager] FlushAsync threw: {e.Message}" );
			}
		}

		// Clear the cache — anything not in the server response stays pending
		// and will be picked up by the next refresh.
		var claimedIds = new HashSet<int>( claimed.Select( g => g.Id ) );
		PendingGifts = PendingGifts.Where( g => !claimedIds.Contains( g.Id ) ).ToList();

		OnGiftsClaimed?.Invoke();

		// Staggered notifications — one per claimed gift, 300ms apart so a
		// 5-gift batch doesn't slam the notification stack in a single frame.
		for ( int i = 0; i < claimed.Count; i++ )
		{
			var gift = claimed[i];
			ShowGiftNotification( gift );
			if ( i < claimed.Count - 1 )
			{
				try { await Task.Delay( 300 ); }
				catch { /* task cancellations during scene teardown are fine */ }
			}
		}
	}

	// ─────────────────────────────────────────────────────────
	// Notification formatting
	// ─────────────────────────────────────────────────────────

	private static void ShowGiftNotification( PendingGift gift )
	{
		var notifier = NotificationManager.Instance;
		if ( notifier == null ) return;

		var title = "Gift received!";
		var message = BuildGiftSummary( gift );
		notifier.AddNotification( NotificationType.Success, title, message, 6f );
	}

	/// <summary>
	/// Build the single-line "💰 Received: X gold, Y gems … — 'reason' from Sender"
	/// blurb. Reason trimmed to 60 chars with ellipsis; sender suffix omitted
	/// when the server didn't stamp a createdBy.
	/// </summary>
	private static string BuildGiftSummary( PendingGift gift )
	{
		var parts = new List<string>();
		if ( gift.Gold > 0 ) parts.Add( $"{gift.Gold:N0} gold" );
		if ( gift.Gems > 0 ) parts.Add( $"{gift.Gems:N0} gems" );
		if ( gift.Ink > 0 ) parts.Add( $"{gift.Ink:N0} ink" );
		if ( gift.BossTokens > 0 ) parts.Add( $"{gift.BossTokens:N0} boss tokens" );
		if ( gift.Items != null )
		{
			foreach ( var kv in gift.Items )
			{
				if ( string.IsNullOrEmpty( kv.Key ) || kv.Value <= 0 ) continue;
				parts.Add( $"{kv.Value} × {kv.Key}" );
			}
		}

		var rewards = parts.Count > 0 ? string.Join( ", ", parts ) : "a mystery reward";
		var summary = $"💰 Received: {rewards}";

		if ( !string.IsNullOrWhiteSpace( gift.Reason ) )
		{
			var reason = gift.Reason.Length > 60 ? gift.Reason.Substring( 0, 60 ) + "…" : gift.Reason;
			summary += $" — '{reason}'";
		}

		if ( !string.IsNullOrWhiteSpace( gift.CreatedBy ) )
		{
			summary += $" from {gift.CreatedBy}";
		}

		return summary;
	}
}
