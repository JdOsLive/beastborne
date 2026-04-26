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
	public void AddNotification( NotificationType type, string title, string message, float duration = 5f, string iconPath = null )
	{
		var notification = new Notification
		{
			Type = type,
			Title = title,
			Message = message,
			Icon = GetIconForType( type ),
			IconPath = iconPath,
			Duration = duration
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
	public void NotifyEvolutionReady( string monsterName, string evolvesTo )
	{
		AddNotification(
			NotificationType.Evolution,
			LocalizationManager.Get( "notify.evolution_ready" ),
			LocalizationManager.Get( "notify.can_evolve", monsterName, evolvesTo ),
			8f
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
	public void NotifyCatch( string monsterName )
	{
		AddNotification(
			NotificationType.Catch,
			LocalizationManager.Get( "notify.monster_caught" ),
			LocalizationManager.Get( "notify.you_caught", monsterName ),
			5f
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
