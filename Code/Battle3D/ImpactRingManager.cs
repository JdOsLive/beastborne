using Sandbox;
using System;
using System.Collections.Generic;

namespace Beastborne.Battle3D;

/// <summary>
/// Singleton that owns the live list of localized impact rings drawn by the
/// screen-space Razor overlay (ImpactRingOverlay). Mirrors DamageNumberManager.
///
/// Crit ring is ALWAYS warm yellow (#fde047) — never element-keyed. Players
/// learn "yellow = crit" as a stable signal across every move type.
/// Super-effective ring is ALWAYS orange (#fb923c) — matches the SUPER chip
/// + super damage-number color. Coherent with the rest of the super
/// vocabulary.
///
/// Both can fire on a single hit (super-crit) — the yellow ring fires
/// immediately, orange chases ~60ms behind for a quick "double burst".
/// </summary>
public sealed class ImpactRingManager : Component
{
	public static ImpactRingManager Instance { get; private set; }

	/// <summary>One live impact ring entry. Razor reads this directly.</summary>
	public sealed class ImpactRingData
	{
		public Guid Id;
		// Captured screen pos at spawn (0..1 normalized). NO jitter — rings
		// should sit exactly at the impact point.
		public Vector2 SpawnScreenPos;
		// Hex color string ("#fde047" / "#fb923c") used as inline CSS border-color.
		public string Color;
		public float SpawnTime;
		public float Lifetime;
		public bool IsCrit;
	}

	// Total animation duration (ringExpand keyframe is 0.4s ease-out forwards).
	private const float RingLifetime = 0.4f;

	public List<ImpactRingData> Active { get; } = new();

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
		// silently kill impact rings for the rest of the editor session).
		if ( Instance == this )
			Instance = null;
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;
		var go = scene.CreateObject();
		go.Name = "ImpactRingManager";
		go.Components.Create<ImpactRingManager>();
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
	/// Purge all active rings — call when a battle ends or the scene is torn
	/// down so leftover rings don't drift into the next view.
	/// </summary>
	public void Clear()
	{
		Active.Clear();
	}

	/// <summary>
	/// Spawn an impact ring at the defender's current screen position. NO
	/// jitter — rings sit exactly at the impact point.
	/// </summary>
	public void SpawnRing( Guid defenderId, string colorHex, bool isCrit )
	{
		var pos = BattleSceneController.Instance?.GetMonsterScreenPosition( defenderId );
		if ( !pos.HasValue )
		{
			Log.Warning( $"ImpactRingManager.SpawnRing: GetMonsterScreenPosition returned null for {defenderId} — skipping" );
			return;
		}

		// Clamp to viewport so we never spawn off-screen (e.g. an enemy mid
		// slide-in during a wave transition right after a KO).
		var screenPos = pos.Value;
		screenPos.x = MathX.Clamp( screenPos.x, 0.05f, 0.95f );
		screenPos.y = MathX.Clamp( screenPos.y, 0.10f, 0.90f );

		Active.Add( new ImpactRingData
		{
			Id = Guid.NewGuid(),
			SpawnScreenPos = screenPos,
			Color = colorHex,
			SpawnTime = Time.Now,
			Lifetime = RingLifetime,
			IsCrit = isCrit,
		} );
		Log.Info( $"ImpactRingManager.SpawnRing: color={colorHex} crit={isCrit} screenPos=({screenPos.x:F2},{screenPos.y:F2}) activeCount={Active.Count}" );
	}
}
