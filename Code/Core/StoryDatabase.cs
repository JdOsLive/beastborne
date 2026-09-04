using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beastborne.Core;

/// <summary>
/// When a story beat fires. One component (BbDialogue) plays all three.
/// </summary>
public enum StoryTrigger
{
	/// <summary>After EMBARK on a stage that has a beat — the run starts once the beat ends.</summary>
	BeforeEmbark,
	/// <summary>On the map, the first time a newly unlocked area is reachable (chained after the unlock reveal).</summary>
	OnAreaUnlocked,
	/// <summary>On the map, after RETURN TO MAP from a run whose boss was defeated.</summary>
	AfterBoss,
}

/// <summary>Which edge of the dialogue slab the speaker's portrait sits on. Left = the player.</summary>
public enum StorySide
{
	Left,
	Right,
}

public sealed class StoryLine
{
	public string Speaker { get; init; }
	public StorySide Side { get; init; }
	public string Text { get; init; }

	public StoryLine( string speaker, StorySide side, string text )
	{
		Speaker = speaker;
		Side = side;
		Text = text;
	}
}

public sealed class StoryBeat
{
	public string Id { get; init; }
	public StoryTrigger Trigger { get; init; }
	public string ExpeditionId { get; init; }
	public List<StoryLine> Lines { get; init; } = new();
}

/// <summary>
/// Hand-authored story beats (client-only data). Beats play ONCE per save —
/// see <see cref="Data.Tamer.SeenStoryBeats"/> — and are looked up by
/// (trigger, expedition). Nothing here is localized yet: the copy below is
/// PLACEHOLDER — a three-beat test script for Weaverwood so the dialogue
/// component can be built and felt before a writer touches it.
/// </summary>
public static class StoryDatabase
{
	// ── Weaverwood (saltmoor_forest) — PLACEHOLDER COPY ──────────────────────
	// Speaker names are deliberately obvious placeholders ("Tamer" is the
	// player; "Warden Ilsa" is a stand-in NPC). Replace the whole block when
	// the real script lands; keep the Ids stable (they are save keys).
	private static readonly List<StoryBeat> _beats = new()
	{
		// PLACEHOLDER COPY — fires on the map once Weaverwood unlocks (after the reveal).
		new StoryBeat
		{
			Id = "weaverwood.unlocked",
			Trigger = StoryTrigger.OnAreaUnlocked,
			ExpeditionId = "saltmoor_forest",
			Lines =
			{
				new StoryLine( "Warden Ilsa", StorySide.Right, "So the pasture let you through. Most folk turn back at the first fog on the forest road." ),
				new StoryLine( "Tamer", StorySide.Left, "The herders said they're coming back a sheep short more often than they'd like." ),
				new StoryLine( "Warden Ilsa", StorySide.Right, "They are. Something in the old dye-groves has stopped being shy. Go and see — and bring your beasts fed." ),
			}
		},

		// PLACEHOLDER COPY — fires after EMBARK into Weaverwood, before the first battle.
		new StoryBeat
		{
			Id = "weaverwood.embark",
			Trigger = StoryTrigger.BeforeEmbark,
			ExpeditionId = "saltmoor_forest",
			Lines =
			{
				new StoryLine( "Tamer", StorySide.Left, "Pinewood ahead. The path is quieter than the pasture was." ),
				new StoryLine( "Warden Ilsa", StorySide.Right, "Quiet is the forest listening. Keep to the marked trees and count your Sheepots at every bend." ),
				new StoryLine( "Warden Ilsa", StorySide.Right, "And if the fog thickens near the groves, that is not weather. Stand your ground there." ),
				new StoryLine( "Tamer", StorySide.Left, "Understood. Let's go." ),
			}
		},

		// PLACEHOLDER COPY — fires on the map after RETURN TO MAP from a Weaverwood boss clear.
		new StoryBeat
		{
			Id = "weaverwood.boss",
			Trigger = StoryTrigger.AfterBoss,
			ExpeditionId = "saltmoor_forest",
			Lines =
			{
				new StoryLine( "Warden Ilsa", StorySide.Right, "The groves have gone still. Whatever was taking the sheep won't be taking any more this season." ),
				new StoryLine( "Tamer", StorySide.Left, "It wasn't the only thing moving up there. The mist was coming down from higher ground." ),
				new StoryLine( "Warden Ilsa", StorySide.Right, "From Weavermere. The painters have been talking about the pond for weeks. Rest first — then go and look." ),
			}
		},
	};

	public static IReadOnlyList<StoryBeat> All => _beats;

	public static StoryBeat Get( string id )
		=> string.IsNullOrEmpty( id ) ? null : _beats.FirstOrDefault( b => b.Id == id );

	/// <summary>The beat for a (trigger, expedition) pair, or null when the stage has no story there.</summary>
	public static StoryBeat Find( StoryTrigger trigger, string expeditionId )
		=> string.IsNullOrEmpty( expeditionId )
			? null
			: _beats.FirstOrDefault( b => b.Trigger == trigger && b.ExpeditionId == expeditionId );
}

/// <summary>
/// One in-flight request for the dialogue component: the beat, plus what to
/// do once it has been read (or skipped). BeforeEmbark uses OnComplete to
/// resume the embark it interrupted.
/// </summary>
public sealed class StoryRequest
{
	public StoryBeat Beat { get; init; }
	public Action OnComplete { get; init; }
	/// <summary>Dev replay — the beat plays even if seen (it is still marked seen after).</summary>
	public bool Force { get; init; }
}

/// <summary>
/// The static hand-off between game code and <c>BbDialogue</c> (Code/UI/Components).
///
/// Two lanes into the player:
///  · <see cref="PlayNow"/> — play immediately (BeforeEmbark, dev replay). The
///    component starts it on its next tick once the screen is free (no battle
///    transition, no other modal, in game).
///  · <see cref="QueueBeat"/> + <see cref="PlayPending"/> — queue for the MAP.
///    ExpeditionManager queues OnAreaUnlocked/AfterBoss beats at clear time;
///    the map's unlock-reveal lane calls <see cref="HoldPending"/> when its
///    zoom starts and <see cref="PlayPending"/> when it lands, so the beat is
///    chained after the reveal. If nobody releases the queue, the component
///    auto-releases it after <see cref="AutoReleaseDelay"/> seconds of the
///    map sitting idle (so a beat can never be lost to a missing call).
///
/// Core never references UI: the component polls <see cref="TakeNext"/> and
/// reports back through <see cref="Finish"/> / <see cref="Abort"/>.
/// </summary>
public static class StoryDirector
{
	/// <summary>Seconds the map must sit idle before pending beats self-release.</summary>
	public const float AutoReleaseDelay = 3.5f;

	// Queued for the map, not yet released (beat ids, in order).
	private static readonly List<string> _pending = new();
	// Released / immediate — waiting for the component to pick them up.
	private static readonly Queue<StoryRequest> _released = new();
	// Beats finished this session even if the save could not be written —
	// guards the BeforeEmbark intercept against re-firing forever.
	private static readonly HashSet<string> _seenThisSession = new();
	private static bool _hold;

	/// <summary>The beat the component is currently playing, or null.</summary>
	public static StoryRequest Current { get; private set; }

	public static bool IsPlaying => Current != null;
	public static bool HasPending => _pending.Count > 0;
	public static bool HasReleased => _released.Count > 0;
	/// <summary>True while the map's reveal has asked the queue to wait (see <see cref="HoldPending"/>).</summary>
	public static bool IsHeld => _hold;

	public static bool HasSeen( string beatId )
	{
		if ( string.IsNullOrEmpty( beatId ) ) return false;
		if ( _seenThisSession.Contains( beatId ) ) return true;
		var tamer = TamerManager.Instance?.CurrentTamer;
		return tamer?.SeenStoryBeats != null && tamer.SeenStoryBeats.Contains( beatId );
	}

	/// <summary>
	/// Queue an unseen beat for the map. Returns true if it was added (false:
	/// unknown id, already seen, or already queued).
	/// </summary>
	public static bool QueueBeat( string beatId )
	{
		var beat = StoryDatabase.Get( beatId );
		if ( beat == null ) return false;
		if ( HasSeen( beatId ) ) return false;
		if ( _pending.Contains( beatId ) ) return false;
		if ( _released.Any( r => r.Beat.Id == beatId ) ) return false;
		if ( Current?.Beat.Id == beatId ) return false;
		_pending.Add( beatId );
		return true;
	}

	/// <summary>
	/// The map's reveal lane calls this when its zoom/dim starts so the
	/// auto-release timer cannot fire mid-reveal. Cleared by <see cref="PlayPending"/>.
	/// </summary>
	public static void HoldPending() => _hold = true;

	/// <summary>Release every queued beat to the component, in queue order. Clears any hold.</summary>
	public static void PlayPending()
	{
		_hold = false;
		if ( _pending.Count == 0 ) return;
		foreach ( var id in _pending )
		{
			var beat = StoryDatabase.Get( id );
			if ( beat == null || HasSeen( id ) ) continue;
			_released.Enqueue( new StoryRequest { Beat = beat } );
		}
		_pending.Clear();
	}

	/// <summary>
	/// Play a beat as soon as the screen is free. Returns false when the beat
	/// is unknown, or already seen and not forced.
	/// </summary>
	public static bool PlayNow( string beatId, Action onComplete = null, bool force = false )
	{
		var beat = StoryDatabase.Get( beatId );
		if ( beat == null ) return false;
		if ( !force && HasSeen( beatId ) ) return false;
		_pending.Remove( beatId );
		_released.Enqueue( new StoryRequest { Beat = beat, OnComplete = onComplete, Force = force } );
		return true;
	}

	/// <summary>Component side: claim the next released request. Null when nothing is waiting.</summary>
	public static StoryRequest TakeNext()
	{
		if ( Current != null ) return null;
		if ( _released.Count == 0 ) return null;
		Current = _released.Dequeue();
		return Current;
	}

	/// <summary>
	/// Component side: the beat was read to the end or skipped. Marks it seen,
	/// saves, and runs OnComplete — unless the caller takes the completion over
	/// (<paramref name="runOnComplete"/> false: BbDialogue wraps a BeforeEmbark
	/// resume in the battle blackout itself).
	/// </summary>
	public static void Finish( StoryRequest request, bool runOnComplete = true )
	{
		if ( request == null ) return;
		if ( Current == request ) Current = null;
		MarkSeen( request.Beat.Id );
		if ( !runOnComplete ) return;
		try { request.OnComplete?.Invoke(); }
		catch ( Exception ex ) { Log.Warning( $"[Story] OnComplete for '{request.Beat.Id}' threw: {ex.Message}" ); }
	}

	/// <summary>
	/// Component side: the scene went away under the beat (left the game). The
	/// beat is NOT marked seen and its OnComplete is dropped — a BeforeEmbark
	/// resume must never start a run from the main menu. Queues are cleared;
	/// the map's idle scan re-queues unlock beats on the next visit.
	/// </summary>
	public static void Abort()
	{
		Current = null;
		_released.Clear();
		_pending.Clear();
		_hold = false;
	}

	public static void MarkSeen( string beatId )
	{
		if ( string.IsNullOrEmpty( beatId ) ) return;
		_seenThisSession.Add( beatId );
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;
		tamer.SeenStoryBeats ??= new HashSet<string>();
		if ( tamer.SeenStoryBeats.Add( beatId ) )
			TamerManager.Instance.SaveToCloud();
	}

	/// <summary>Forget every seen beat (dev). Saves.</summary>
	public static void ResetSeen()
	{
		_seenThisSession.Clear();
		var tamer = TamerManager.Instance?.CurrentTamer;
		if ( tamer == null ) return;
		tamer.SeenStoryBeats ??= new HashSet<string>();
		tamer.SeenStoryBeats.Clear();
		TamerManager.Instance.SaveToCloud();
	}

	// ── dev_story <beatId|list|reset> ─────────────────────────────────────
	// list  → every beat with its trigger, expedition, line count, seen flag
	// reset → forget all seen beats (they fire again on their triggers)
	// <id>  → replay that beat now, seen or not (e.g. dev_story weaverwood.embark)
	[ConCmd( "dev_story" )]
	public static void DevStory( string arg = "list" )
	{
		arg = (arg ?? "list").Trim();

		if ( arg.Equals( "list", StringComparison.OrdinalIgnoreCase ) )
		{
			// One Log.Info per line — a multi-line string truncates after the first newline.
			Log.Info( $"[dev_story] {StoryDatabase.All.Count} beat(s) · playing={Current?.Beat.Id ?? "-"} · pending={_pending.Count} · released={_released.Count} · held={_hold}" );
			foreach ( var b in StoryDatabase.All )
				Log.Info( $"[dev_story]  {b.Id}  ({b.Trigger} · {b.ExpeditionId} · {b.Lines.Count} lines)  seen={HasSeen( b.Id )}" );
			return;
		}

		if ( arg.Equals( "skip", StringComparison.OrdinalIgnoreCase ) )
		{
			var cur = Current;
			if ( cur == null ) { Log.Info( "[dev_story] nothing is playing." ); return; }
			Finish( cur, runOnComplete: false );
			Log.Info( $"[dev_story] skipped '{cur.Beat.Id}' (marked seen)." );
			return;
		}

		if ( arg.Equals( "reset", StringComparison.OrdinalIgnoreCase ) )
		{
			ResetSeen();
			Log.Info( "[dev_story] seen beats cleared — every beat fires again on its trigger." );
			return;
		}

		if ( StoryDatabase.Get( arg ) == null )
		{
			Log.Warning( $"[dev_story] unknown beat '{arg}' — try: dev_story list" );
			return;
		}

		PlayNow( arg, force: true );
		Log.Info( $"[dev_story] queued '{arg}' — plays as soon as the screen is free." );
	}
}
