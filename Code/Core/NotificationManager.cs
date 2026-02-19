using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

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
	ExpeditionUnlock
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

		if ( Instance == this )
		{
			Instance = null;
		}
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
		CheckForNewExpeditionUnlocks( newLevel );
	}

	private void UpdateUnlockedExpeditions()
	{
		_previouslyUnlockedExpeditions.Clear();
		var tamerLevel = TamerManager.Instance?.CurrentTamer?.Level ?? 1;
		var expeditions = ExpeditionManager.Instance?.Expeditions;

		if ( expeditions == null ) return;

		foreach ( var expedition in expeditions )
		{
			if ( tamerLevel >= expedition.RequiredLevel )
			{
				_previouslyUnlockedExpeditions.Add( expedition.Id );
			}
		}
	}

	private void CheckForNewExpeditionUnlocks( int newLevel )
	{
		var expeditions = ExpeditionManager.Instance?.Expeditions;
		if ( expeditions == null ) return;

		foreach ( var expedition in expeditions )
		{
			// Check if this expedition is now unlocked but wasn't before
			if ( newLevel >= expedition.RequiredLevel && !_previouslyUnlockedExpeditions.Contains( expedition.Id ) )
			{
				NotifyExpeditionUnlock( expedition.Name );
				_previouslyUnlockedExpeditions.Add( expedition.Id );
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

		OnNotificationAdded?.Invoke( notification );

		Log.Info( $"[Notification] {type}: {title} - {message}" );
	}

	/// <summary>
	/// Notify that a monster is ready to evolve
	/// </summary>
	public void NotifyEvolutionReady( string monsterName, string evolvesTo )
	{
		AddNotification(
			NotificationType.Evolution,
			"Evolution Ready!",
			$"{monsterName} can evolve to {evolvesTo}",
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
			"Server Boost Activated!",
			$"{activatedBy} activated {boostName} for everyone!",
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
			"Ranked Battle",
			$"{playerName} is searching for a ranked match",
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
			"Monster Caught!",
			$"You caught {monsterName}!",
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
			"Level Up!",
			$"You reached Tamer Level {newLevel}!",
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
			"New Area Unlocked!",
			$"{expeditionName} is now available!",
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
			_ => "•"
		};
	}
}
