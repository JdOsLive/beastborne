using Sandbox;

namespace Beastborne.Battle3D;

/// <summary>
/// Small bright spark that flies outward from the impact point.
/// HDR color triggers bloom for natural sparkle.
/// </summary>
public sealed class SparkEffect : Component
{
	private SpriteRenderer renderer;
	private float lifetime;
	private float maxLifetime;
	private Color baseColor;
	private Vector3 velocity;
	private float startSize;

	public void Setup( Color color, float intensity )
	{
		baseColor = color;
		maxLifetime = Random.Shared.Float( 0.2f, 0.4f );

		renderer = GameObject.Components.GetOrCreate<SpriteRenderer>();

		var tex = Texture.Load( FileSystem.Mounted, "ui/effects/spark.png" );
		if ( tex != null )
		{
			renderer.Sprite = Sprite.FromTexture( tex );
		}

		startSize = Random.Shared.Float( 2f, 5f ) * intensity;
		renderer.Size = new Vector2( startSize, startSize );
		renderer.Color = color;
		renderer.Shadows = false;
		renderer.Lighting = false;
		renderer.AlphaCutoff = 0f;
		renderer.TextureFilter = Sandbox.Rendering.FilterMode.Anisotropic;

		// Random outward direction (in YZ plane since camera looks along X)
		var angle = Random.Shared.Float( 0f, MathF.PI * 2f );
		var speed = Random.Shared.Float( 40f, 100f ) * intensity;
		velocity = new Vector3(
			Random.Shared.Float( -10f, 10f ),
			MathF.Cos( angle ) * speed,
			MathF.Sin( angle ) * speed
		);
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

		// Move outward with gravity
		velocity += Vector3.Down * 120f * Time.Delta;
		WorldPosition += velocity * Time.Delta;

		// Shrink and fade
		var sizeMult = 1f - progress * progress;
		renderer.Size = new Vector2( startSize * sizeMult, startSize * sizeMult );

		var alpha = 1f - progress;
		renderer.Color = new Color( baseColor.r, baseColor.g, baseColor.b, alpha );
	}
}
