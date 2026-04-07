using Sandbox;

namespace Beastborne.Battle3D;

/// <summary>
/// Ambient glow that appears behind the impact point.
/// Scales up quickly and fades out slowly.
/// HDR color triggers bloom for a soft light effect.
/// </summary>
public sealed class GlowEffect : Component
{
	private SpriteRenderer renderer;
	private float lifetime;
	private float maxLifetime = 0.5f;
	private float scale;
	private Color baseColor;

	public void Setup( Color color, float effectScale )
	{
		baseColor = new Color( color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, 0.4f );
		scale = effectScale;

		renderer = GameObject.Components.GetOrCreate<SpriteRenderer>();

		var tex = Texture.Load( FileSystem.Mounted, "ui/effects/glow.png" );
		if ( tex != null )
		{
			renderer.Sprite = Sprite.FromTexture( tex );
		}

		renderer.Size = new Vector2( 30f * scale, 30f * scale );
		renderer.Color = baseColor;
		renderer.Shadows = false;
		renderer.Lighting = false;
		renderer.AlphaCutoff = 0f;
		renderer.TextureFilter = Sandbox.Rendering.FilterMode.Anisotropic;

		GameObject.WorldScale = new Vector3( 0.5f, 0.5f, 0.5f );
	}

	protected override void OnUpdate()
	{
		if ( renderer == null ) return;

		lifetime += Time.Delta;
		var progress = lifetime / maxLifetime;

		if ( progress >= 1f )
		{
			GameObject.Destroy();
			return;
		}

		// Quick expand then hold
		var expandCurve = MathF.Min( 1f, progress * 4f ); // reaches 1.0 at 25%
		var currentScale = 0.5f + expandCurve * 0.8f;
		GameObject.WorldScale = new Vector3( currentScale, currentScale, currentScale );

		// Slow fade out
		var alpha = baseColor.a * (1f - progress * progress);
		renderer.Color = new Color( baseColor.r, baseColor.g, baseColor.b, alpha );
	}
}
