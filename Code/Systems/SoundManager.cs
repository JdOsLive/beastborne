using Sandbox;

namespace Beastborne.Systems;

/// <summary>
/// Manages UI and game sound effects using custom sounds.
/// </summary>
public static class SoundManager
{
	// UI Sounds (custom)
	private const string UI_HOVER = "sounds/ui/littleswoosh1b.sound";
	private const string UI_CLICK = "sounds/ui/button.sound";
	private const string UI_BACK = "sounds/ui/close.sound";
	private const string UI_FORWARD = "sounds/ui/open.sound";
	private const string UI_DENY = "sounds/ui/error.sound";
	private const string UI_SELECT = "sounds/ui/button.sound";
	private const string UI_TOGGLE_ON = "sounds/ui/button.sound";
	private const string UI_TOGGLE_OFF = "sounds/ui/close.sound";
	private const string UI_NOTIFICATION = "sounds/ui/notification.sound";
	private const string UI_POPUP = "sounds/ui/popup.sound";
	private const string UI_SUCCESS = "sounds/ui/success.sound";
	private const string UI_MAXIMIZE = "sounds/ui/maximize.sound";
	private const string UI_MINIMIZE = "sounds/ui/minimize.sound";

	// Battle Sounds (s&box built-in)
	private const string BATTLE_HIT = "player.attack.hit";
	private const string BATTLE_MISS = "player.attack.miss";
	private const string BATTLE_CRIT = "player.attack.hit.crit";
	private const string BATTLE_START = "ui.navigate.forward";
	private const string BATTLE_VICTORY = "player.levelup";
	private const string BATTLE_DEFEAT = "ui.navigate.deny";

	// Game Sounds (s&box built-in)
	private const string REWARD_GOLD = "player.pickup.coin";
	private const string REWARD_XP = "player.levelup";
	private const string MONSTER_CATCH = "player.levelup";
	private const string EVOLUTION = "player.levelup";

	// Volume settings
	private static float _masterVolume = 1.0f;
	private static float _uiVolume = 0.5f;
	private static float _sfxVolume = 0.7f;

	public static float MasterVolume
	{
		get => _masterVolume;
		set => _masterVolume = value.Clamp( 0f, 1f );
	}

	public static float UIVolume
	{
		get => _uiVolume;
		set => _uiVolume = value.Clamp( 0f, 1f );
	}

	public static float SFXVolume
	{
		get => _sfxVolume;
		set => _sfxVolume = value.Clamp( 0f, 1f );
	}

	/// <summary>
	/// Play a sound with the given volume multiplier.
	/// </summary>
	private static void PlaySound( string soundName, float volumeMultiplier = 1f )
	{
		if ( string.IsNullOrEmpty( soundName ) ) return;

		var volume = _masterVolume * volumeMultiplier;
		if ( volume <= 0 ) return;

		try
		{
			var sound = Sound.Play( soundName );
			if ( sound.IsValid() )
			{
				sound.Volume = volume;
			}
		}
		catch ( System.Exception e )
		{
			Log.Warning( $"SoundManager: Failed to play sound '{soundName}': {e.Message}" );
		}
	}

	// ==========================================
	// UI Sound Methods
	// ==========================================

	/// <summary>
	/// Play when hovering over a button or interactive element.
	/// </summary>
	public static void PlayHover()
	{
		PlaySound( UI_HOVER, _uiVolume * 0.3f );
	}

	/// <summary>
	/// Play when clicking a button.
	/// </summary>
	public static void PlayClick()
	{
		PlaySound( UI_CLICK, _uiVolume );
	}

	/// <summary>
	/// Play when navigating back (closing panels, canceling).
	/// </summary>
	public static void PlayBack()
	{
		PlaySound( UI_BACK, _uiVolume );
	}

	/// <summary>
	/// Play when navigating forward (opening panels, confirming).
	/// </summary>
	public static void PlayForward()
	{
		PlaySound( UI_FORWARD, _uiVolume );
	}

	/// <summary>
	/// Play when an action is denied or invalid.
	/// </summary>
	public static void PlayDeny()
	{
		PlaySound( UI_DENY, _uiVolume );
	}

	/// <summary>
	/// Play when selecting an item.
	/// </summary>
	public static void PlaySelect()
	{
		PlaySound( UI_SELECT, _uiVolume * 0.8f );
	}

	/// <summary>
	/// Play when switching tabs.
	/// </summary>
	public static void PlayTabSwitch()
	{
		PlaySound( UI_FORWARD, _uiVolume * 0.6f );
	}

	/// <summary>
	/// Play when toggling an option on.
	/// </summary>
	public static void PlayToggleOn()
	{
		PlaySound( UI_TOGGLE_ON, _uiVolume * 0.7f );
	}

	/// <summary>
	/// Play when toggling an option off.
	/// </summary>
	public static void PlayToggleOff()
	{
		PlaySound( UI_TOGGLE_OFF, _uiVolume * 0.7f );
	}

	/// <summary>
	/// Play when a notification pops up.
	/// </summary>
	public static void PlayNotification()
	{
		PlaySound( UI_NOTIFICATION, _uiVolume * 0.6f );
	}

	/// <summary>
	/// Play when a popup/modal appears.
	/// </summary>
	public static void PlayPopup()
	{
		PlaySound( UI_POPUP, _uiVolume * 0.7f );
	}

	/// <summary>
	/// Play when an action succeeds.
	/// </summary>
	public static void PlaySuccess()
	{
		PlaySound( UI_SUCCESS, _uiVolume * 0.8f );
	}

	/// <summary>
	/// Play when maximizing/expanding a panel.
	/// </summary>
	public static void PlayMaximize()
	{
		PlaySound( UI_MAXIMIZE, _uiVolume * 0.6f );
	}

	/// <summary>
	/// Play when minimizing/collapsing a panel.
	/// </summary>
	public static void PlayMinimize()
	{
		PlaySound( UI_MINIMIZE, _uiVolume * 0.6f );
	}

	// ==========================================
	// Battle Sound Methods
	// ==========================================

	/// <summary>
	/// Play when an attack hits.
	/// </summary>
	public static void PlayAttackHit()
	{
		PlaySound( BATTLE_HIT, _sfxVolume );
	}

	/// <summary>
	/// Play when an attack misses.
	/// </summary>
	public static void PlayAttackMiss()
	{
		PlaySound( BATTLE_MISS, _sfxVolume * 0.6f );
	}

	/// <summary>
	/// Play when a critical hit lands.
	/// </summary>
	public static void PlayCriticalHit()
	{
		PlaySound( BATTLE_CRIT, _sfxVolume * 1.2f );
	}

	/// <summary>
	/// Play when a battle starts.
	/// </summary>
	public static void PlayBattleStart()
	{
		PlaySound( BATTLE_START, _sfxVolume );
	}

	/// <summary>
	/// Play when the player wins a battle.
	/// </summary>
	public static void PlayVictory()
	{
		PlaySound( BATTLE_VICTORY, _sfxVolume );
	}

	/// <summary>
	/// Play when the player loses a battle.
	/// </summary>
	public static void PlayDefeat()
	{
		PlaySound( BATTLE_DEFEAT, _sfxVolume );
	}

	// ==========================================
	// Game Sound Methods
	// ==========================================

	/// <summary>
	/// Play when receiving gold.
	/// </summary>
	public static void PlayGoldReward()
	{
		PlaySound( REWARD_GOLD, _sfxVolume * 0.8f );
	}

	/// <summary>
	/// Play when receiving XP or leveling up.
	/// </summary>
	public static void PlayXPReward()
	{
		PlaySound( REWARD_XP, _sfxVolume );
	}

	/// <summary>
	/// Play when catching a monster.
	/// </summary>
	public static void PlayMonsterCatch()
	{
		PlaySound( UI_SUCCESS, _sfxVolume );
	}

	/// <summary>
	/// Play when a monster evolves.
	/// </summary>
	public static void PlayEvolution()
	{
		PlaySound( UI_SUCCESS, _sfxVolume * 1.2f );
	}
}
