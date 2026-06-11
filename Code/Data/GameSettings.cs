namespace Beastborne.Data;

/// <summary>
/// Stores all game settings that can be configured by the player.
/// These are saved per-slot via cookies.
/// </summary>
public class GameSettings
{
	// ============================================
	// BATTLE SETTINGS
	// ============================================

	/// <summary>
	/// Whether auto-contract is enabled by default when starting expeditions
	/// </summary>
	public bool DefaultAutoContract { get; set; } = false;

	// (DefaultNegotiationStrategy was retired — Auto-Contract now picks the
	// best approach per beast via ContractGenerator.PickBestOption.)

	/// <summary>
	/// Whether to use species filter for auto-contract (if false, all species are auto-contracted)
	/// </summary>
	public bool UseAutoContractSpeciesFilter { get; set; } = false;

	/// <summary>
	/// Species IDs that are enabled for auto-contract (comma-separated string for storage)
	/// When empty with filter enabled, nothing will be auto-contracted
	/// </summary>
	public string AutoContractEnabledSpecies { get; set; } = "";

	/// <summary>
	/// Skip battle animations for instant resolve
	/// </summary>
	public bool SkipBattleAnimations { get; set; } = false;

	/// <summary>
	/// Camera shake on hits/crits during battle
	/// </summary>
	public bool ScreenShake { get; set; } = true;

	// ============================================
	// AUDIO SETTINGS (0..1 — defaults mirror SoundManager/RadioManager statics)
	// ============================================

	/// <summary>
	/// Master volume — scales every sound in the game (UI, SFX, music)
	/// </summary>
	public float MasterVolume { get; set; } = 1.0f;

	/// <summary>
	/// UI sound volume (clicks, hovers, popups)
	/// </summary>
	public float UIVolume { get; set; } = 0.5f;

	/// <summary>
	/// Music volume (the radio system)
	/// </summary>
	public float MusicVolume { get; set; } = 0.3f;

	// ============================================
	// DISPLAY SETTINGS
	// ============================================

	/// <summary>
	/// Show floating damage numbers during battle
	/// </summary>
	public bool ShowDamageNumbers { get; set; } = true;

	/// <summary>
	/// Show type effectiveness hints (Super Effective, Not Very Effective)
	/// </summary>
	public bool ShowTypeEffectiveness { get; set; } = true;

	/// <summary>
	/// Show genetics on monster cards in roster view
	/// </summary>
	public bool ShowGeneticsOnCards { get; set; } = false;

	/// <summary>
	/// Show power ratings on monster cards
	/// </summary>
	public bool ShowPowerRatings { get; set; } = true;

	// ============================================
	// NOTIFICATION SETTINGS
	// ============================================

	/// <summary>
	/// Show contract expiring warnings when battles remaining is at or below this value
	/// </summary>
	public int ContractWarningThreshold { get; set; } = 5;

	/// <summary>
	/// Show level up notifications
	/// </summary>
	public bool ShowLevelUpNotifications { get; set; } = true;

	/// <summary>
	/// Show new beast discovered alerts
	/// </summary>
	public bool ShowDiscoveryAlerts { get; set; } = true;

	// ============================================
	// CONFIRMATION SETTINGS
	// ============================================

	/// <summary>
	/// Show confirmation dialog before releasing beasts
	/// </summary>
	public bool ConfirmBeforeRelease { get; set; } = true;

	/// <summary>
	/// Show confirmation dialog before fusion
	/// </summary>
	public bool ConfirmBeforeFusion { get; set; } = true;

	/// <summary>
	/// Show confirmation dialog for purchases above this gold amount (0 = never)
	/// </summary>
	public int ConfirmPurchaseThreshold { get; set; } = 1000;

	// ============================================
	// ACCESSIBILITY SETTINGS
	// ============================================

	/// <summary>
	/// Use larger text throughout the UI
	/// </summary>
	public bool LargerTextMode { get; set; } = false;

	/// <summary>
	/// Use high contrast colors for element types
	/// </summary>
	public bool HighContrastMode { get; set; } = false;

	/// <summary>
	/// Create a deep copy of the settings
	/// </summary>
	public GameSettings Clone()
	{
		return new GameSettings
		{
			// Battle
			DefaultAutoContract = DefaultAutoContract,
			UseAutoContractSpeciesFilter = UseAutoContractSpeciesFilter,
			AutoContractEnabledSpecies = AutoContractEnabledSpecies,
			SkipBattleAnimations = SkipBattleAnimations,
			ScreenShake = ScreenShake,

			// Audio
			MasterVolume = MasterVolume,
			UIVolume = UIVolume,
			MusicVolume = MusicVolume,

			// Display
			ShowDamageNumbers = ShowDamageNumbers,
			ShowTypeEffectiveness = ShowTypeEffectiveness,
			ShowGeneticsOnCards = ShowGeneticsOnCards,
			ShowPowerRatings = ShowPowerRatings,

			// Notifications
			ContractWarningThreshold = ContractWarningThreshold,
			ShowLevelUpNotifications = ShowLevelUpNotifications,
			ShowDiscoveryAlerts = ShowDiscoveryAlerts,

			// Confirmations
			ConfirmBeforeRelease = ConfirmBeforeRelease,
			ConfirmBeforeFusion = ConfirmBeforeFusion,
			ConfirmPurchaseThreshold = ConfirmPurchaseThreshold,

			// Accessibility
			LargerTextMode = LargerTextMode,
			HighContrastMode = HighContrastMode
		};
	}
}
