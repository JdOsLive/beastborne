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
	/// Default battle playback speed (1.0, 2.0, or 4.0)
	/// </summary>
	public float DefaultBattleSpeed { get; set; } = 1.0f;

	/// <summary>
	/// Whether auto-battle is enabled by default when starting expeditions
	/// </summary>
	public bool DefaultAutoBattle { get; set; } = false;

	/// <summary>
	/// Whether auto-retry is enabled by default when starting expeditions
	/// </summary>
	public bool DefaultAutoRetry { get; set; } = false;

	/// <summary>
	/// Whether auto-contract is enabled by default when starting expeditions
	/// </summary>
	public bool DefaultAutoContract { get; set; } = false;

	/// <summary>
	/// Default negotiation strategy (-1 = skip, 0 = generous, 1 = fair, 2 = strict, 3 = gold)
	/// </summary>
	public int DefaultNegotiationStrategy { get; set; } = 1;

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
	/// Use compact card view (smaller cards, more visible)
	/// </summary>
	public bool CompactCardView { get; set; } = false;

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
			DefaultBattleSpeed = DefaultBattleSpeed,
			DefaultAutoBattle = DefaultAutoBattle,
			DefaultAutoRetry = DefaultAutoRetry,
			DefaultAutoContract = DefaultAutoContract,
			DefaultNegotiationStrategy = DefaultNegotiationStrategy,
			UseAutoContractSpeciesFilter = UseAutoContractSpeciesFilter,
			AutoContractEnabledSpecies = AutoContractEnabledSpecies,
			SkipBattleAnimations = SkipBattleAnimations,

			// Display
			ShowDamageNumbers = ShowDamageNumbers,
			ShowTypeEffectiveness = ShowTypeEffectiveness,
			ShowGeneticsOnCards = ShowGeneticsOnCards,
			CompactCardView = CompactCardView,
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
