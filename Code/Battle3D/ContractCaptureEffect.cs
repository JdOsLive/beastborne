using Sandbox;

namespace Beastborne.Battle3D;

/// <summary>
/// Purple ink swirl effect when a beast is successfully contracted.
/// Spawns multiple rotating particles that converge on the beast, then it fades out.
/// </summary>
public sealed class ContractCaptureEffect : Component
{
	private SpriteRenderer[] particles;
	private Vector3[] orbitOffsets;
	private float lifetime;
	private const float TotalDuration = 1.2f;
	private const float ConvergeDuration = 0.8f; // Particles converge over this time
	private const float FadeStart = 0.6f; // Beast starts fading at this point
	private const int ParticleCount = 6;
	private float orbitSpeed = 4f;
	private MonsterBillboard targetBillboard;

	public void Setup( MonsterBillboard billboard )
	{
		targetBillboard = billboard;

		particles = new SpriteRenderer[ParticleCount];
		orbitOffsets = new Vector3[ParticleCount];

		var tex = Texture.Load( FileSystem.Mounted, "ui/effects/glow.png" );

		for ( int i = 0; i < ParticleCount; i++ )
		{
			var particleGo = new GameObject( true, $"ContractParticle_{i}" );
			particleGo.Parent = GameObject;

			var renderer = particleGo.Components.Create<SpriteRenderer>();
			if ( tex != null )
				renderer.Sprite = Sprite.FromTexture( tex );

			renderer.Size = new Vector2( 8f, 8f );
			// Purple/violet HDR color for bloom
			float hue = 0.7f + (i * 0.05f); // Slight variation
			renderer.Color = new Color( 0.6f, 0.2f, 1.0f, 0.8f );
			renderer.Shadows = false;
			renderer.Lighting = false;
			renderer.AlphaCutoff = 0f;

			particles[i] = renderer;

			// Distribute evenly around a circle
			float angle = (float)i / ParticleCount * MathF.PI * 2f;
			orbitOffsets[i] = new Vector3( 0f, MathF.Cos( angle ) * 25f, MathF.Sin( angle ) * 25f );
		}
	}

	protected override void OnUpdate()
	{
		lifetime += Time.Delta;
		float progress = lifetime / TotalDuration;

		if ( progress >= 1f )
		{
			// Done — destroy the effect and the billboard
			if ( targetBillboard != null )
			{
				targetBillboard.ClearSwapState();
				targetBillboard.GameObject.Destroy();
			}
			GameObject.Destroy();
			return;
		}

		// Orbit and converge
		float convergeProgress = MathF.Min( 1f, lifetime / ConvergeDuration );
		// Ease-in-out for smooth convergence
		float convergeEased = convergeProgress * convergeProgress * (3f - 2f * convergeProgress);
		float orbitRadius = 1f - convergeEased; // 1.0 → 0.0

		float rotationAngle = lifetime * orbitSpeed;

		for ( int i = 0; i < ParticleCount; i++ )
		{
			if ( particles[i] == null ) continue;

			// Rotate around the target
			float baseAngle = (float)i / ParticleCount * MathF.PI * 2f + rotationAngle;
			var offset = new Vector3(
				0f,
				MathF.Cos( baseAngle ) * 25f * orbitRadius,
				MathF.Sin( baseAngle ) * 25f * orbitRadius + 10f // Center on beast middle
			);

			particles[i].GameObject.WorldPosition = GameObject.WorldPosition + offset;

			// Scale up then shrink as they converge
			float particleScale = orbitRadius * 1.5f + 0.3f;
			particles[i].Size = new Vector2( 8f * particleScale, 8f * particleScale );

			// Fade particles as they converge
			float alpha = 0.8f * (convergeProgress < 0.9f ? 1f : (1f - convergeProgress) * 10f);
			particles[i].Color = new Color( 0.6f, 0.2f, 1.0f, alpha );
		}

		// Fade the target billboard after convergence starts
		if ( targetBillboard != null && progress > FadeStart )
		{
			float fadeProgress = (progress - FadeStart) / (1f - FadeStart);
			targetBillboard.SwapFade = 1f - fadeProgress;
			targetBillboard.IsSwapVisible = true;
			// Tint purple as it fades
			targetBillboard.ContractFadeColor = new Color( 0.6f, 0.3f, 1f, 1f - fadeProgress );
		}
	}
}
