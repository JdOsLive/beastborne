using Sandbox;

namespace Beastborne.Battle3D;

/// <summary>
/// Placeholder impact effect — simple flash ring that expands and fades.
/// TODO: Replace with proper particle shader effect.
/// </summary>
public sealed class ImpactEffect : Component
{
	private float lifetime;
	private float maxLifetime = 0.3f;

	public void Setup( bool critical, bool superEffective )
	{
		maxLifetime = critical ? 0.4f : 0.3f;
		// For now, the visual impact is handled by MonsterBillboard.PlayHitFlash()
		// and BattleCameraController shake/hitstop.
		// This component just manages cleanup timing.
	}

	protected override void OnUpdate()
	{
		lifetime += Time.Delta;
		if ( lifetime >= maxLifetime )
		{
			GameObject.Destroy();
		}
	}
}
