using Sandbox;
using Beastborne.Data;
using Beastborne.Systems;

namespace Beastborne.Battle3D;

/// <summary>
/// Orchestrates attack visual effects based on move element, damage type, and intensity.
/// Uses HDR-bright colors that trigger bloom for natural glow effects.
/// </summary>
public static class AttackVFXManager
{
	/// <summary>
	/// Element color palettes — HDR values (>1.0) trigger bloom for glow
	/// </summary>
	public static (Color primary, Color secondary) GetElementColors( ElementType element )
	{
		return element switch
		{
			ElementType.Fire     => (new Color( 2.0f, 0.6f, 0.1f ), new Color( 2.0f, 1.5f, 0.3f )),
			ElementType.Water    => (new Color( 0.2f, 0.8f, 2.0f ), new Color( 0.5f, 1.5f, 2.0f )),
			ElementType.Earth    => (new Color( 1.2f, 0.8f, 0.3f ), new Color( 1.5f, 1.2f, 0.6f )),
			ElementType.Wind     => (new Color( 1.5f, 2.0f, 1.5f ), new Color( 2.0f, 2.0f, 2.0f )),
			ElementType.Electric => (new Color( 2.0f, 1.8f, 0.2f ), new Color( 1.0f, 1.5f, 2.0f )),
			ElementType.Ice      => (new Color( 0.8f, 1.5f, 2.0f ), new Color( 2.0f, 2.0f, 2.0f )),
			ElementType.Nature   => (new Color( 0.3f, 1.8f, 0.3f ), new Color( 1.5f, 2.0f, 0.8f )),
			ElementType.Metal    => (new Color( 1.8f, 1.8f, 2.0f ), new Color( 1.2f, 1.2f, 1.4f )),
			ElementType.Shadow   => (new Color( 1.0f, 0.2f, 2.0f ), new Color( 0.5f, 0.1f, 1.0f )),
			ElementType.Spirit   => (new Color( 2.0f, 0.8f, 1.5f ), new Color( 2.0f, 1.5f, 0.5f )),
			_                    => (new Color( 2.0f, 2.0f, 2.0f ), new Color( 1.5f, 1.5f, 1.8f )),
		};
	}

	/// <summary>
	/// Spawn the full set of VFX for an attack turn
	/// </summary>
	public static void PlayAttackVFX( BattleTurn turn, MonsterBillboard attacker, MonsterBillboard defender, GameObject parent )
	{
		if ( defender == null ) return;

		var (primary, secondary) = GetElementColors( turn.MoveElement );
		var hitPos = defender.WorldPosition + Vector3.Up * 10f;
		var isCrit = turn.IsCritical;
		var isSuper = turn.IsSuperEffective;

		// 1. Slash effect(s)
		SpawnSlash( hitPos, primary, isCrit ? -35f : -25f, isCrit ? 1.3f : 1f, parent );
		if ( isCrit )
		{
			// Double slash for crits (X pattern)
			SpawnSlash( hitPos, secondary, 25f, 1.2f, parent );
		}

		// 2. Impact burst (expanding glow + ring)
		SpawnImpactBurst( hitPos, primary, secondary, isCrit || isSuper, parent );

		// 3. Sparks flying outward
		var sparkCount = isCrit ? 8 : isSuper ? 6 : 4;
		for ( int i = 0; i < sparkCount; i++ )
		{
			SpawnSpark( hitPos, primary, secondary, isCrit, parent );
		}

		// 4. Ambient glow behind the hit
		SpawnGlow( hitPos, primary, isCrit ? 1.5f : 1f, parent );
	}

	/// <summary>
	/// Spawn VFX for a miss
	/// </summary>
	public static void PlayMissVFX( MonsterBillboard defender, GameObject parent )
	{
		if ( defender == null ) return;
		var hitPos = defender.WorldPosition + Vector3.Up * 10f;

		// Just a small faint swoosh
		SpawnSlash( hitPos, new Color( 0.5f, 0.5f, 0.6f, 0.5f ), -20f, 0.6f, parent );
	}

	private static void SpawnSlash( Vector3 pos, Color color, float angle, float scale, GameObject parent )
	{
		var go = new GameObject( true, "VFX_Slash" );
		go.Parent = parent;
		go.WorldPosition = pos;

		var effect = go.Components.Create<SlashEffect>();
		effect.Setup( color, angle, scale );
	}

	private static void SpawnImpactBurst( Vector3 pos, Color primary, Color secondary, bool intense, GameObject parent )
	{
		// Expanding ring
		var ringGo = new GameObject( true, "VFX_Ring" );
		ringGo.Parent = parent;
		ringGo.WorldPosition = pos;

		var ring = ringGo.Components.Create<ImpactBurstEffect>();
		ring.Setup( primary, "ring", intense ? 1.4f : 1f );

		// Center glow circle
		var circleGo = new GameObject( true, "VFX_Circle" );
		circleGo.Parent = parent;
		circleGo.WorldPosition = pos + new Vector3( -1f, 0f, 0f ); // Slight offset to avoid z-fight

		var circle = circleGo.Components.Create<ImpactBurstEffect>();
		circle.Setup( secondary, "circle", intense ? 1.2f : 0.8f );
	}

	private static void SpawnSpark( Vector3 pos, Color primary, Color secondary, bool intense, GameObject parent )
	{
		var go = new GameObject( true, "VFX_Spark" );
		go.Parent = parent;
		go.WorldPosition = pos;

		var spark = go.Components.Create<SparkEffect>();
		var color = Random.Shared.Float() > 0.5f ? primary : secondary;
		spark.Setup( color, intense ? 1.3f : 1f );
	}

	private static void SpawnGlow( Vector3 pos, Color color, float scale, GameObject parent )
	{
		var go = new GameObject( true, "VFX_Glow" );
		go.Parent = parent;
		go.WorldPosition = pos + new Vector3( -2f, 0f, 0f ); // Behind other effects

		var glow = go.Components.Create<GlowEffect>();
		glow.Setup( color, scale );
	}
}
