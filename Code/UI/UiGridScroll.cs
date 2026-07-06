using System;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace Beastborne.UI;

/// <summary>
/// Keyboard-navigation scroll helper. When a grid is driven by W/A/S/D the
/// selection can move outside the scroll viewport — these helpers nudge the
/// scroll container so the selected card stays on screen.
///
/// Uses the same `Box.Rect` ⇄ `ScrollOffset` unit convention as the existing
/// MonsterRosterPanel.ScrollFocusedDetailIntoView — both are screen-space.
/// </summary>
public static class UiGridScroll
{
	/// <summary>
	/// Scroll `scroll` so the `index`-th child carrying `cardClass` (and not
	/// `excludeClass`, if given) is fully visible. Card order must match the
	/// caller's selection index. No-op if nothing is laid out yet.
	/// </summary>
	public static void ScrollIndexIntoView( Panel scroll, int index, string cardClass,
		string excludeClass = null, float margin = 24f )
	{
		if ( scroll == null || index < 0 ) return;
		IntoView( scroll, FindIndexed( scroll, index, cardClass, excludeClass ), margin );
	}

	/// <summary>
	/// Compute-only variant (2026-07-06 nav-animations pass): returns the
	/// offset ScrollIndexIntoView would apply, or null when the card is
	/// already fully visible. Lets callers GLIDE toward the target (a
	/// per-tick lerp) instead of snapping — behavior of the applying
	/// overloads is unchanged.
	/// </summary>
	public static float? ComputeIndexOffset( Panel scroll, int index, string cardClass,
		string excludeClass = null, float margin = 24f )
	{
		if ( scroll == null || index < 0 ) return null;
		return ComputeOffset( scroll, FindIndexed( scroll, index, cardClass, excludeClass ), margin );
	}

	/// <summary>
	/// Adjust `scroll`'s vertical ScrollOffset so `target` is fully visible
	/// with a small margin. Only scrolls when the target is actually clipped.
	/// </summary>
	public static void IntoView( Panel scroll, Panel target, float margin = 24f )
	{
		var newOff = ComputeOffset( scroll, target, margin );
		if ( newOff.HasValue )
			scroll.ScrollOffset = new Vector2( scroll.ScrollOffset.x, newOff.Value );
	}

	/// <summary>
	/// Shared core: the offset that brings `target` fully into view, or
	/// null when no scroll is needed (already visible / not laid out).
	/// </summary>
	public static float? ComputeOffset( Panel scroll, Panel target, float margin = 24f )
	{
		if ( scroll == null || target == null ) return null;

		var view = scroll.Box.Rect;
		var card = target.Box.Rect;
		if ( view.Height <= 0f || card.Height <= 0f ) return null;

		float curOff = scroll.ScrollOffset.y;
		// Card position in the scroll container's content space.
		float cardTop = card.Top - view.Top + curOff;
		float cardBottom = cardTop + card.Height;

		float newOff = curOff;
		if ( cardTop - margin < curOff )
			newOff = cardTop - margin;                       // clipped above the viewport
		else if ( cardBottom + margin > curOff + view.Height )
			newOff = cardBottom + margin - view.Height;      // clipped below the viewport

		newOff = Math.Max( 0f, newOff );
		return Math.Abs( newOff - curOff ) > 0.5f ? newOff : null;
	}

	// Nth child carrying cardClass (skipping excludeClass) — cards must be
	// DIRECT children of the scroll (scroll-container law).
	private static Panel FindIndexed( Panel scroll, int index, string cardClass, string excludeClass )
	{
		int n = 0;
		foreach ( var c in scroll.Children )
		{
			if ( !c.HasClass( cardClass ) ) continue;
			if ( excludeClass != null && c.HasClass( excludeClass ) ) continue;
			if ( n == index ) return c;
			n++;
		}
		return null;
	}
}
