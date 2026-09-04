using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Beastborne.Data;
using Beastborne.Systems;
using Achievement = Beastborne.Data.Achievement;

namespace Beastborne.Core;

/// <summary>
/// Types of notifications that can be displayed
/// </summary>
public enum NotificationType
{
	Info,
	Success,
	Warning,
	Evolution,
	ServerBoost,
	RankedBattle,
	Catch,
	TamerLevelUp,
	ExpeditionUnlock,
	Achievement
}

/// <summary>
/// Represents a single notification
/// </summary>
public class Notification
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public NotificationType Type { get; set; }
	public string Title { get; set; }
	public string Message { get; set; }
	public string Icon { get; set; }
	public string IconPath { get; set; } // Image path for pixel art icons (overrides emoji Icon)
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public float Duration { get; set; } = 5f; // seconds
	public bool IsExpired => (DateTime.UtcNow - CreatedAt).TotalSeconds >= Duration;
	public float Progress => Math.Clamp( 1f - (float)(DateTime.UtcNow - CreatedAt).TotalSeconds / Duration, 0f, 1f );
	public bool HasImageIcon => !string.IsNullOrEmpty( IconPath );

	/// <summary>
	/// Deep-link target for the PawPad ALERTS app (2026-08-29): "monsters" /
	/// "monsters:{guid}" · "skills" · "expedition" · "online" · "online:guild" ·
	/// "effects" · "achievements" · null (no route — the tap only marks read).
	/// Set at the fire site; defaults per Type via NotificationManager.DefaultRoute.
	/// Guild events fire as plain Success/Info/Warning, so they pass it explicitly.
	/// </summary>
	public string Route { get; set; }
}

/// <summary>
/// Manages game notifications displayed to the player
/// </summary>
public sealed class NotificationManager : Component
{
	public static NotificationManager Instance { get; private set; }

	private List<Notification> _notifications = new();
	private List<Notification> _history = new();
	private const int MAX_NOTIFICATIONS = 5;
	private const int MAX_HISTORY = 50;

	public event Action<Notification> OnNotificationAdded;
	public event Action<Notification> OnNotificationRemoved;

	public IReadOnlyList<Notification> ActiveNotifications => _notifications;
	public IReadOnlyList<Notification> NotificationHistory => _history;
	public int UnreadCount { get; private set; }

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
		}
		else
		{
			Log.Info( "NotificationManager already exists, removing duplicate" );
			Destroy();
		}
	}

	// Track which expeditions were already unlocked before a level up
	private HashSet<string> _previouslyUnlockedExpeditions = new();

	protected override void OnStart()
	{
		// Subscribe to server boost events
		if ( ShopManager.Instance != null )
		{
			ShopManager.Instance.OnServerBoostActivated += OnServerBoostActivated;
			// Boost expiry (2026-09-04): ShopManager already polls both lists
			// in OnUpdate and raises these — no timer of our own.
			ShopManager.Instance.OnBoostExpired += OnPersonalBoostExpired;
			ShopManager.Instance.OnServerBoostExpired += OnServerBoostExpired;
		}

		// Subscribe to competitive events (ranked battle searching)
		if ( CompetitiveManager.Instance != null )
		{
			CompetitiveManager.Instance.OnPlayerSearchingRanked += OnPlayerSearchingRanked;
		}

		// Subscribe to tamer level up events
		if ( TamerManager.Instance != null )
		{
			TamerManager.Instance.OnLevelUp += OnTamerLevelUp;
		}

		// Subscribe to achievement unlocks
		if ( AchievementManager.Instance != null )
		{
			AchievementManager.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
		}

		// Initialize previously unlocked expeditions
		UpdateUnlockedExpeditions();
	}

	protected override void OnDestroy()
	{
		// Unsubscribe from events
		if ( ShopManager.Instance != null )
		{
			ShopManager.Instance.OnServerBoostActivated -= OnServerBoostActivated;
			ShopManager.Instance.OnBoostExpired -= OnPersonalBoostExpired;
			ShopManager.Instance.OnServerBoostExpired -= OnServerBoostExpired;
		}

		if ( CompetitiveManager.Instance != null )
		{
			CompetitiveManager.Instance.OnPlayerSearchingRanked -= OnPlayerSearchingRanked;
		}

		if ( TamerManager.Instance != null )
		{
			TamerManager.Instance.OnLevelUp -= OnTamerLevelUp;
		}

		if ( AchievementManager.Instance != null )
		{
			AchievementManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
		}

		if ( Instance == this )
		{
			Instance = null;
		}
	}

	private void OnAchievementUnlocked( Achievement achievement )
	{
		if ( achievement == null ) return;
		NotifyAchievementUnlocked( achievement );
	}

	private void OnServerBoostActivated( Data.ServerBoost boost )
	{
		if ( boost == null ) return;

		// Don't notify for boosts we activated ourselves
		var mySteamId = (long)Sandbox.Utility.Steam.SteamId;
		if ( boost.ActivatedBySteamId == mySteamId ) return;

		string boostName = GetBoostName( boost.Type );
		string iconPath = GetBoostIconPath( boost.Type );
		NotifyServerBoost( boost.ActivatedBy ?? "Someone", boostName, iconPath );
	}

	private void OnPersonalBoostExpired( Data.ActiveBoost boost )
	{
		if ( boost == null ) return;
		NotifyBoostExpired( GetBoostName( boost.Type ), false, GetBoostIconPath( boost.Type ) );
	}

	private void OnServerBoostExpired( Data.ServerBoost boost )
	{
		if ( boost == null ) return;
		NotifyBoostExpired( GetBoostName( boost.Type ), true, GetBoostIconPath( boost.Type ) );
	}

	private void OnPlayerSearchingRanked( string playerName )
	{
		if ( string.IsNullOrEmpty( playerName ) ) return;
		NotifyRankedSearch( playerName );
	}

	private void OnTamerLevelUp( int newLevel )
	{
		NotifyTamerLevelUp( newLevel );
		// Expedition unlocks are gated on clearing the previous expedition,
		// NOT on tamer level. RequiredLevel is just the recommended level
		// shown to the player. Unlock notifications fire from the expedition
		// complete path (see <see cref="CheckForNewExpeditionUnlocks"/>).
	}

	private void UpdateUnlockedExpeditions()
	{
		_previouslyUnlockedExpeditions.Clear();
		var tamer = TamerManager.Instance?.CurrentTamer;
		var expeditions = ExpeditionManager.Instance?.Expeditions;
		if ( tamer == null || expeditions == null ) return;

		// Seed with every expedition the player has already unlocked via
		// prior clears so we don't re-fire an "unlocked" toast on each boot.
		// Cleared-index is 1-based count; zone at index N is unlocked once
		// the player has cleared index N-1 (so HighestExpeditionCleared >= N).
		int highest = tamer.HighestExpeditionCleared;
		for ( int i = 0; i < expeditions.Count; i++ )
		{
			if ( i <= highest )
			{
				_previouslyUnlockedExpeditions.Add( expeditions[i].Id );
			}
		}
	}

	/// <summary>
	/// Called by ExpeditionManager after a successful expedition clear —
	/// fires a one-shot toast announcing the next zone is now available.
	/// </summary>
	public void CheckForNewExpeditionUnlocks()
	{
		var expeditions = ExpeditionManager.Instance?.Expeditions;
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( expeditions == null || tamer == null ) return;

		int highest = tamer.HighestExpeditionCleared;
		for ( int i = 0; i < expeditions.Count; i++ )
		{
			if ( i <= highest && !_previouslyUnlockedExpeditions.Contains( expeditions[i].Id ) )
			{
				NotifyExpeditionUnlock( expeditions[i].Name );
				_previouslyUnlockedExpeditions.Add( expeditions[i].Id );
			}
		}
	}

	private string GetBoostName( Data.ShopItemType type )
	{
		return type switch
		{
			Data.ShopItemType.TamerXPBoost => "2x Tamer XP",
			Data.ShopItemType.BeastXPBoost => "2x Beast XP",
			Data.ShopItemType.XPBoost => "2x XP",
			Data.ShopItemType.GoldBoost => "2x Gold",
			Data.ShopItemType.RareEncounter => "Rare Radar",
			Data.ShopItemType.LuckyCharm => "Lucky Charm",
			_ => type.ToString()
		};
	}

	private string GetBoostIconPath( Data.ShopItemType type )
	{
		return type switch
		{
			Data.ShopItemType.TamerXPBoost => "/ui/items/boosts/tamer_xp_scroll.png",
			Data.ShopItemType.BeastXPBoost => "/ui/items/boosts/beast_xp_tome.png",
			Data.ShopItemType.XPBoost => "/ui/items/boosts/tamer_xp_scroll.png",
			Data.ShopItemType.GoldBoost => "/ui/items/boosts/gold_multiplier.png",
			Data.ShopItemType.RareEncounter => "/ui/items/boosts/rare_radar.png",
			Data.ShopItemType.LuckyCharm => "/ui/items/boosts/lucky_clover.png",
			_ => null
		};
	}

	protected override void OnUpdate()
	{
		// Remove expired notifications
		var expired = _notifications.Where( n => n.IsExpired ).ToList();
		foreach ( var notification in expired )
		{
			_notifications.Remove( notification );
			OnNotificationRemoved?.Invoke( notification );
		}
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;

		var go = scene.CreateObject();
		go.Name = "NotificationManager";
		go.Components.Create<NotificationManager>();
	}

	/// <summary>
	/// Add a new notification
	/// </summary>
	public void AddNotification( NotificationType type, string title, string message, float duration = 5f, string iconPath = null, string route = null )
	{
		var notification = new Notification
		{
			Type = type,
			Title = title,
			Message = message,
			Icon = GetIconForType( type ),
			IconPath = iconPath,
			Duration = duration,
			// PawPad deep-link (2026-08-29): explicit route wins; otherwise the
			// per-type default (null for Info/Success/Warning — no subject).
			Route = route ?? DefaultRoute( type )
		};

		// Remove oldest if at max capacity
		while ( _notifications.Count >= MAX_NOTIFICATIONS )
		{
			var oldest = _notifications[0];
			_notifications.RemoveAt( 0 );
			OnNotificationRemoved?.Invoke( oldest );
		}

		_notifications.Add( notification );

		// Add to history
		_history.Insert( 0, notification );
		while ( _history.Count > MAX_HISTORY )
		{
			_history.RemoveAt( _history.Count - 1 );
		}
		UnreadCount++;

		// Per-type sound
		PlaySoundForType( type );

		OnNotificationAdded?.Invoke( notification );

		Log.Info( $"[Notification] {type}: {title} - {message}" );
	}

	private void PlaySoundForType( NotificationType type )
	{
		switch ( type )
		{
			case NotificationType.Achievement:
			case NotificationType.Success:
				SoundManager.PlaySuccess();
				break;
			case NotificationType.Evolution:
				SoundManager.PlayEvolution();
				break;
			case NotificationType.Catch:
				SoundManager.PlayMonsterCatch();
				break;
			case NotificationType.ServerBoost:
			case NotificationType.RankedBattle:
			case NotificationType.ExpeditionUnlock:
			case NotificationType.TamerLevelUp:
				SoundManager.PlayForward();
				break;
			case NotificationType.Warning:
				SoundManager.PlayDeny();
				break;
			case NotificationType.Info:
			default:
				SoundManager.PlayNotification();
				break;
		}
	}

	/// <summary>
	/// Notify that an achievement was unlocked
	/// </summary>
	public void NotifyAchievementUnlocked( Achievement achievement )
	{
		AddNotification(
			NotificationType.Achievement,
			LocalizationManager.Get( "ui.notification.achievement_title" ),
			$"{achievement.Name} — {LocalizationManager.Get( "ui.notification.achievement_claim_hint" )}",
			8f,
			achievement.IconPath
		);
	}

	/// <summary>
	/// Notify that a monster is ready to evolve
	/// </summary>
	public void NotifyEvolutionReady( string monsterName, string evolvesTo, Guid monsterId = default )
	{
		AddNotification(
			NotificationType.Evolution,
			LocalizationManager.Get( "notify.evolution_ready" ),
			LocalizationManager.Get( "notify.can_evolve", monsterName, evolvesTo ),
			8f,
			route: monsterId == Guid.Empty ? "monsters" : $"monsters:{monsterId}"
		);
	}

	/// <summary>
	/// Notify that a server boost was activated
	/// </summary>
	public void NotifyServerBoost( string activatedBy, string boostName, string iconPath = null )
	{
		AddNotification(
			NotificationType.ServerBoost,
			LocalizationManager.Get( "notify.server_boost" ),
			LocalizationManager.Get( "notify.server_boost_desc", activatedBy, boostName ),
			10f,
			iconPath
		);
	}

	// ── Boost activation / expiry (2026-09-04) ─────────────────────────
	// "It's active now" beat for the player's OWN boosts — the phone alert
	// (and its badge) is the whole beat; the Effects widget lists the boost.
	// Fires from ItemManager.UseItem (wave/attempt/minute consumables) and
	// ItemManager.UseBoost (server-wide scrolls). Same type + route as the
	// other-players' server-boost alert so it lands in the effects lane.

	private string _lastBoostNotifyKey;
	private DateTime _lastBoostNotifyAt;

	/// <summary>
	/// Title reads "&lt;Item&gt; active for &lt;duration&gt;" (a trailing "(1h)"
	/// in the item name is dropped — the duration already says it); the
	/// message is the item's own effect line. Identical fires inside one
	/// second collapse into one alert (guards a per-quantity double call).
	/// </summary>
	public void NotifyBoostActive( string itemName, string durationText, string effectText, string iconPath = null )
	{
		if ( string.IsNullOrWhiteSpace( itemName ) ) return;

		string name = StripTrailingParenthetical( itemName );
		string title = string.IsNullOrWhiteSpace( durationText )
			? LocalizationManager.Get( "notify.boost_active_short", name )
			: LocalizationManager.Get( "notify.boost_active", name, durationText );
		string message = effectText ?? "";

		string key = title + "|" + message;
		if ( key == _lastBoostNotifyKey && (DateTime.UtcNow - _lastBoostNotifyAt).TotalSeconds < 1.0 )
			return;
		_lastBoostNotifyKey = key;
		_lastBoostNotifyAt = DateTime.UtcNow;

		AddNotification(
			NotificationType.ServerBoost,
			title,
			message,
			8f,
			iconPath,
			"effects"
		);
	}

	/// <summary>
	/// A timed boost ran out. Only wired to hooks ShopManager already raises
	/// (personal + server-wide shop boosts) — there is no timer here.
	/// </summary>
	public void NotifyBoostExpired( string boostName, bool serverWide, string iconPath = null )
	{
		if ( string.IsNullOrWhiteSpace( boostName ) ) return;

		AddNotification(
			NotificationType.Info,
			LocalizationManager.Get( "notify.boost_expired", boostName ),
			LocalizationManager.Get( serverWide ? "notify.boost_expired_server_desc" : "notify.boost_expired_desc" ),
			6f,
			iconPath,
			"effects"
		);
	}

	/// <summary>
	/// Honest clock text for a minute count: "1h" · "1h 30m" · "30m". Zero or
	/// negative → "" (caller falls back to the duration-less title).
	/// </summary>
	public static string FormatBoostDuration( int minutes )
	{
		if ( minutes <= 0 ) return "";
		int h = minutes / 60;
		int m = minutes % 60;
		if ( h > 0 && m > 0 ) return $"{h}h {m}m";
		if ( h > 0 ) return $"{h}h";
		return $"{m}m";
	}

	/// <summary>
	/// Use-counted boosts: "8 waves" · "12 attempts" (contract lures). Zero or
	/// negative → "" (caller falls back to the duration-less title).
	/// </summary>
	public static string FormatUseDuration( int uses, bool attempts )
	{
		if ( uses <= 0 ) return "";
		return LocalizationManager.Get( attempts ? "notify.duration_attempts" : "notify.duration_waves", uses );
	}

	/// <summary>"Gold Boost (1h)" → "Gold Boost". Anything else passes through.</summary>
	private static string StripTrailingParenthetical( string name )
	{
		string trimmed = name.Trim();
		if ( !trimmed.EndsWith( ")" ) ) return trimmed;
		int open = trimmed.LastIndexOf( " (" );
		if ( open <= 0 ) return trimmed;
		string head = trimmed.Substring( 0, open ).Trim();
		return head.Length > 0 ? head : trimmed;
	}

	/// <summary>
	/// Notify that someone is searching for a ranked battle
	/// </summary>
	public void NotifyRankedSearch( string playerName )
	{
		AddNotification(
			NotificationType.RankedBattle,
			LocalizationManager.Get( "notify.ranked_battle" ),
			LocalizationManager.Get( "notify.ranked_search", playerName ),
			6f
		);
	}

	/// <summary>
	/// Notify that a monster was caught
	/// </summary>
	public void NotifyCatch( string monsterName, Guid monsterId = default )
	{
		AddNotification(
			NotificationType.Catch,
			LocalizationManager.Get( "notify.monster_caught" ),
			LocalizationManager.Get( "notify.you_caught", monsterName ),
			5f,
			route: monsterId == Guid.Empty ? "monsters" : $"monsters:{monsterId}"
		);
	}

	/// <summary>
	/// Notify that the tamer leveled up
	/// </summary>
	public void NotifyTamerLevelUp( int newLevel )
	{
		// Check if level up notifications are enabled
		if ( SettingsManager.Instance?.Settings?.ShowLevelUpNotifications == false )
			return;

		AddNotification(
			NotificationType.TamerLevelUp,
			LocalizationManager.Get( "notify.level_up" ),
			LocalizationManager.Get( "notify.reached_level", newLevel ),
			6f
		);
	}

	/// <summary>
	/// Notify that a new expedition area was unlocked
	/// </summary>
	public void NotifyExpeditionUnlock( string expeditionName )
	{
		AddNotification(
			NotificationType.ExpeditionUnlock,
			LocalizationManager.Get( "notify.new_area" ),
			LocalizationManager.Get( "notify.area_available", expeditionName ),
			8f
		);
	}

	/// <summary>
	/// Remove a specific notification
	/// </summary>
	public void RemoveNotification( Guid id )
	{
		var notification = _notifications.FirstOrDefault( n => n.Id == id );
		if ( notification != null )
		{
			_notifications.Remove( notification );
			OnNotificationRemoved?.Invoke( notification );
		}
	}

	/// <summary>
	/// Clear all active notifications
	/// </summary>
	public void ClearAll()
	{
		var toRemove = _notifications.ToList();
		_notifications.Clear();
		foreach ( var n in toRemove )
		{
			OnNotificationRemoved?.Invoke( n );
		}
	}

	/// <summary>
	/// Mark all notifications as read (resets unread counter)
	/// </summary>
	public void MarkAllRead()
	{
		UnreadCount = 0;
	}

	/// <summary>
	/// Clear notification history
	/// </summary>
	public void ClearHistory()
	{
		_history.Clear();
		UnreadCount = 0;
	}

	/// <summary>
	/// Per-type deep-link default for the PawPad ALERTS app. Types whose
	/// subject is implied by the type route without a fire-site argument;
	/// the generic Info/Success/Warning carry no route unless the caller
	/// passes one (guild events do).
	/// </summary>
	public static string DefaultRoute( NotificationType type )
	{
		return type switch
		{
			NotificationType.Achievement => "achievements",
			NotificationType.Catch => "monsters",
			NotificationType.Evolution => "monsters",
			NotificationType.TamerLevelUp => "skills",
			NotificationType.ExpeditionUnlock => "expedition",
			NotificationType.ServerBoost => "effects",
			NotificationType.RankedBattle => "online",
			_ => null
		};
	}

	private string GetIconForType( NotificationType type )
	{
		return type switch
		{
			NotificationType.Info => "ℹ",
			NotificationType.Success => "✓",
			NotificationType.Warning => "⚠",
			NotificationType.Evolution => "✦",
			NotificationType.ServerBoost => "🚀",
			NotificationType.RankedBattle => "⚔",
			NotificationType.Catch => "🎯",
			NotificationType.TamerLevelUp => "⬆",
			NotificationType.ExpeditionUnlock => "🗺",
			NotificationType.Achievement => "★",
			_ => "•"
		};
	}
}
