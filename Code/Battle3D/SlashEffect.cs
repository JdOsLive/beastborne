using Sandbox;

namespace Beastborne.Battle3D;

/// <summary>
/// HD slash line effect — a smooth white texture stretched into a line,
/// tinted with element color, animated to sweep across the target.
/// Uses bilinear filtering for smooth HD look on pixel art.
/// HDR colors trigger bloom for natural glow.
/// </summary>
public sealed class SlashEffect : Component
{
	private SpriteRenderer renderer;
	private float lifetime;
	private float maxLifetime = 0.2f;
	private float angle;
	private float scale;
	private Color baseColor;
	private Vector3 startPos;

	public void Setup( Color color, float angleDeg, float effectScale )
	{
		baseColor = color;
		angle = angleDeg;
		scale = effectScale;
		startPos = WorldPosition;

		renderer = GameObject.Components.GetOrCreate<SpriteRenderer>();

		// Load the slash texture
		var tex = Texture.Load( FileSystem.Mounted, "ui/effects/slash.png" );
		if ( tex != null )
		{
			renderer.Sprite = Sprite.FromTexture( tex );
		}

		renderer.Size = new Vector2( 50f * scale, 4f * scale );
		renderer.Color = color;
		renderer.Shadows = false;
		renderer.Lighting = false;
		renderer.AlphaCutoff = 0f;

		// Bilinear filtering for smooth HD look
		renderer.TextureFilter = Sandbox.Rendering.FilterMode.Anisotropic;

		// Rotate the slash
		var currentRot = WorldRotation;
		WorldRotation = currentRot * Rotation.FromAxis( Vector3.Forward, angle );

		// Start small, will scale up
		GameObject.WorldScale = new Vector3( 0.1f, 1f, 1f );
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

		if ( progress < 0.3f )
		{
			// Sweep in — scale X from 0.1 to 1
			var t = progress / 0.3f;
			var scaleX = 0.1f + t * 0.9f;
			GameObject.WorldScale = new Vector3( scaleX, 1f, 1f );
		}
		else if ( progress < 0.6f )
		{
			// Hold at full
			GameObject.WorldScale = new Vector3( 1f, 1f, 1f );
		}
		else
		{
			// Fade out and widen slightly
			var t = (progress - 0.6f) / 0.4f;
			var alpha = 1f - t;
			renderer.Color = new Color( baseColor.r, baseColor.g, baseColor.b, alpha );

			var widthScale = 1f + t * 0.3f;
			GameObject.WorldScale = new Vector3( widthScale, 1f - t * 0.5f, 1f );
		}
	}
}
