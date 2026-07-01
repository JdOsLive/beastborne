namespace Beastborne.UI;

/// <summary>
/// The C# half of the Beastborne token layer. Doc: .claude/ui-knowledge/tokens.md.
///
/// s&box has NO CSS custom properties — the 2026-07-01 spike proved both the
/// `--x:` declaration and the `var()` function are rejected by the parser — so
/// tokens reach the UI two ways:
///   1. SHARED PRIMITIVE COMPONENTS (Code/UI/Primitives/) — their SCSS is written
///      once, so its values ARE the tokens for static chrome.
///   2. THIS CLASS — for inline `style=""`, interpolated Razor attributes, and
///      dynamic per-element/per-rarity tints.
/// A recolor = edit here + the matching primitive SCSS. Panel SCSS that can't use
/// a primitive carries a token-header comment banner synced to tokens.md.
/// </summary>
public static class BbTokens
{
	// ── Surfaces ─────────────────────────────────────────────────────────
	public const string Bg = "#0a0912";          // app/page background
	public const string BgRoot = "#04060f";      // deepest (menu base)
	public const string Panel = "#15121f";       // panel slab
	public const string Card = "#1c1830";        // raised card
	public const string CardFill = "rgba(20, 20, 35, 0.95)";
	public const string Slot = "#131019";        // empty slot

	// ── Semantic accents (fixed meaning — never recolored for variety) ──
	public const string Gold = "#ffce3a";        // highlight · "go" · primary
	public const string GoldLo = "#f59312";      // gold gradient end
	public const string GoldInk = "#2a1605";     // dark text ON gold
	public const string Violet = "#9b6cff";      // selection · focus · "you"
	public const string PurpleDeep = "#7b4ddb";
	public const string Orange = "#ee5421";      // live · embark · alert
	public const string Red = "#e0414a";         // destructive · release
	public const string Green = "#3fb45e";       // success · confirm
	public const string Blue = "#3f8fe0";        // info · neutral action
	public const string Discord = "#5865f2";

	// ── Text ─────────────────────────────────────────────────────────────
	public const string Text = "#f4f1ea";
	public const string TextDim = "rgba(214, 206, 236, 0.6)";

	// ── Radii (px) — the rounder sweep scale; ring = host + 10 concentric ──
	public const int RChip = 11;
	public const int RButton = 16;
	public const int RCard = 22;
	public const int RHero = 26;
	public const int RSlab = 30;

	// ── Motion ───────────────────────────────────────────────────────────
	public const string EaseGlide = "cubic-bezier(0.22, 1, 0.36, 1)"; // the ring/signature ease
	public const float TSnap = 0.14f;   // interaction feedback
	public const float TCard = 0.16f;   // card hover
	public const float TGlide = 0.27f;  // selector glide

	// ── Element palette v1 (Fable 5, 2026-07-01) ────────────────────────
	// Two-tone system: dark Fill + bright Rim so cards stay dark and beasts
	// pop. Rims sit at matched brightness ("neon edge"); fills are deep shades
	// of the same hue. Shadow is pushed magenta-ward so it never fights the
	// selection violet; Electric is nudged greener than UI gold.
	public static (string Rim, string Fill) Element( string element ) => element?.ToLowerInvariant() switch
	{
		"fire" => ("#ff6a3d", "#7c2410"),
		"water" => ("#4aa8ff", "#1e3a8a"),
		"earth" => ("#d9a054", "#6b3f14"),
		"wind" => ("#2dd4bf", "#0f766e"),
		"electric" => ("#f7e024", "#806c00"),
		"ice" => ("#9fe8ff", "#155e75"),
		"nature" => ("#4ade80", "#14532d"),
		"metal" => ("#c3cdd9", "#3f4b5c"),
		"shadow" => ("#c26bff", "#4a1580"),
		"spirit" => ("#ff6bd6", "#86185d"),
		_ => ("#a8a29e", "#44403c"), // neutral / unknown
	};

	// ── Rarity ladder (stable) ───────────────────────────────────────────
	public static string Rarity( string rarity ) => rarity?.ToLowerInvariant() switch
	{
		"common" => "#9ca3af",
		"uncommon" => "#22c55e",
		"rare" => "#3b82f6",
		"epic" => "#a855f7",
		"legendary" => "#fbbf24", // dark text on legendary
		"mythic" => "#ec4899",
		_ => "#9ca3af",
	};
}
