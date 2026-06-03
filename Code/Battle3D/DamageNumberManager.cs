using Sandbox;
using System;
using System.Collections.Generic;

namespace Beastborne.Battle3D;

/// <summary>
/// Singleton that owns the live list of floating damage numbers shown by
/// the screen-space Razor overlay (DamageNumberOverlay).
///
/// Why screen-space: s&box's TextRenderer has no outline / stroke / shadow
/// / gradient API — only Text / FontSize / Scale / Color / HAlignment /
/// VAlignment. The "Manga Impact" damage-number visuals (Bungee Inline,
/// 8-direction stroke, KAPOW wedge, chromatic split, motion trails) cannot
/// be expressed there. CSS in Razor gives us text-shadow stacks, layered
/// ghosts, mix-blend-mode, keyframes, clip-path, etc.
///
/// The screen position is captured ONCE at spawn — the number leaves the
/// target and drifts on its own (as in the mockup), instead of glued to a
/// possibly-moving billboard. This also means a defender's KO animation
/// can't tear the damage number away mid-flight.
/// </summary>
public sealed class DamageNumberManager : Component
{
	public static DamageNumberManager Instance { get; private set; }

	/// <summary>One live floating-number entry. Razor reads this directly.</summary>
	public sealed class DamageNumberData
	{
		public Guid Id;
		public int Damage;
		public bool IsCrit;
		public bool IsSuper;
		public bool IsMiss;
		// 0..1 normalized screen coordinates captured at spawn.
		public Vector2 SpawnScreenPos;
		public float SpawnTime;
		public float Lifetime;
	}

	// Lifetimes match the mockup's keyframe loops, but in-game these are
	// one-shot (the animation runs to "forwards" final state then unmounts).
	private const float HitLifetime = 1.6f;
	private const float MissLifetime = 1.8f;

	// Spawn jitter so consecutive hits on the same target don't perfectly
	// stack. Horizontal is ±5% of viewport, matches mockup's drift feel.
	private const float HorizontalJitterFraction = 0.05f;
	// Vertical lift above the captured billboard top, in normalized screen
	// units. Small bump so the slamRise's translateY doesn't start INSIDE
	// the sprite.
	private const float VerticalLiftFraction = 0.02f;

	public List<DamageNumberData> Active { get; } = new();

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			GameObject.Tags.Add( "bb-persistent" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	protected override void OnDestroy()
	{
		// Clear the static so a stale reference past play mode doesn't make the
		// next session's OnAwake self-destruct the new manager (which would
		// silently kill damage numbers for the rest of the editor session).
		if ( Instance == this )
			Instance = null;
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;
		var go = scene.CreateObject();
		go.Name = "DamageNumberManager";
		go.Components.Create<DamageNumberManager>();
	}

	protected override void OnUpdate()
	{
		if ( Active.Count == 0 ) return;
		float now = Time.Now;
		// Walk backwards so list mutation while iterating is safe.
		for ( int i = Active.Count - 1; i >= 0; i-- )
		{
			var d = Active[i];
			if ( now - d.SpawnTime >= d.Lifetime )
			{
				Active.RemoveAt( i );
			}
		}
	}

	/// <summary>
	/// Purge all active numbers — call when a battle ends or the scene is torn
	/// down so leftover floats don't drift into the next view.
	/// </summary>
	public void Clear()
	{
		Active.Clear();
	}

	/// <summary>
	/// Spawn a hit number above the defender. Captures the defender's CURRENT
	/// screen position (not re-projected per frame) so the number drifts
	/// independently — like the mockup, not glued to a moving sprite.
	/// </summary>
	public void SpawnHit( Guid defenderId, int damage, bool crit, bool super )
	{
		var pos = GetSpawnPos( defenderId );
		if ( !pos.HasValue )
		{
			Log.Warning( $"DamageNumberManager.SpawnHit: GetSpawnPos returned null for defender {defenderId} — skipping (no billboard?)" );
			return;
		}

		var entry = new DamageNumberData
		{
			Id = Guid.NewGuid(),
			Damage = damage,
			IsCrit = crit,
			IsSuper = super,
			IsMiss = false,
			SpawnScreenPos = pos.Value,
			SpawnTime = Time.Now,
			Lifetime = HitLifetime,
		};
		Active.Add( entry );
		Log.Info( $"DamageNumberManager.SpawnHit: damage={damage} crit={crit} super={super} screenPos=({pos.Value.x:F2},{pos.Value.y:F2}) activeCount={Active.Count}" );
	}

	/// <summary>Spawn a "WHIFF" miss text. Sideways drift handled in CSS.</summary>
	public void SpawnMiss( Guid defenderId )
	{
		var pos = GetSpawnPos( defenderId );
		if ( !pos.HasValue ) return;

		Active.Add( new DamageNumberData
		{
			Id = Guid.NewGuid(),
			Damage = 0,
			IsCrit = false,
			IsSuper = false,
			IsMiss = true,
			SpawnScreenPos = pos.Value,
			SpawnTime = Time.Now,
			Lifetime = MissLifetime,
		} );
	}

	/// <summary>
	/// Compute spawn screen pos = defender screen pos + small upward offset +
	/// horizontal jitter so consecutive hits don't perfectly overlap.
	/// </summary>
	private static Vector2? GetSpawnPos( Guid defenderId )
	{
		var basePos = BattleSceneController.Instance?.GetMonsterScreenPosition( defenderId );
		if ( !basePos.HasValue ) return null;

		float jitterX = Random.Shared.Float( -HorizontalJitterFraction, HorizontalJitterFraction );
		// Screen Y in s&box's PointToScreenNormal: 0 = top, 1 = bottom.
		// "Up" = subtract from y.
		var spawn = basePos.Value + new Vector2( jitterX, -VerticalLiftFraction );

		// Clamp to roughly inside the viewport so we never spawn off-screen
		// (e.g. an enemy mid-slide-in during wave transition).
		spawn.x = MathX.Clamp( spawn.x, 0.05f, 0.95f );
		spawn.y = MathX.Clamp( spawn.y, 0.10f, 0.90f );

		return spawn;
	}
}
