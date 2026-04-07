using Sandbox;

namespace Beastborne.Battle3D;

/// <summary>
/// Expanding burst effect — ring or circle that scales up and fades out.
/// HDR colors trigger bloom glow.
/// </summary>
public sealed class ImpactBurstEffect : Component
{
	private SpriteRenderer renderer;
	private float lifetime;
	private float maxLifetime = 0.3f;
	private float scale;
	private Color baseColor;
	private string shapeType; // "ring" or "circle"

	public void Setup( Color color, string shape, float effectScale )
	{
		baseColor = color;
		shapeType = shape;
		scale = effectScale;

		renderer = GameObject.Components.GetOrCreate<SpriteRenderer>();

		var texPath = shape == "ring" ? "ui/effects/ring.png" : "ui/effects/circle.png";
		var tex = Texture.Load( FileSystem.Mounted, texPath );
		if ( tex != null )
		{
			renderer.Sprite = Sprite.FromTexture( tex );
		}

		var baseSize = shape == "ring" ? 20f : 12f;
		renderer.Size = new Vector2( baseSize * scale, baseSize * scale );
		renderer.Color = color;
		renderer.Shadows = false;
		renderer.Lighting = false;
		renderer.AlphaCutoff = 0f;
		renderer.TextureFilter = Sandbox.Rendering.FilterMode.Anisotropic;

		// Start small
		GameObject.WorldScale = new Vector3( 0.2f, 0.2f, 0.2f );
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

		// Expand rapidly then slow
		var expandCurve = 1f - MathF.Pow( 1f - progress, 3f ); // ease-out cubic
		var currentScale = 0.2f + expandCurve * 1.5f;
		GameObject.WorldScale = new Vector3( currentScale, currentScale, currentScale );

		// Fade out
		var alpha = 1f - progress;
		renderer.Color = new Color( baseColor.r, baseColor.g, baseColor.b, alpha );
	}
}
