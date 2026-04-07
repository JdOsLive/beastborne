using Sandbox;

namespace Beastborne.Battle3D;

/// <summary>
/// A floating damage number that rises and fades out.
/// Uses TextRenderer with high font size and small scale for crisp rendering.
/// </summary>
public sealed class FloatingDamageNumber : Component
{
	private TextRenderer textRenderer;
	private float lifetime;
	private float maxLifetime = 1.2f;
	private float riseSpeed = 25f;
	private Vector3 startPos;
	private bool isCritical;

	public void Setup( int damage, bool critical, bool superEffective )
	{
		isCritical = critical;
		startPos = WorldPosition;
		lifetime = 0f;

		textRenderer = GameObject.Components.GetOrCreate<TextRenderer>();
		textRenderer.Text = damage.ToString( "N0" );
		textRenderer.FontSize = critical ? 200f : 150f;

		if ( critical )
			textRenderer.Color = new Color( 1f, 0.85f, 0.1f, 1f );
		else if ( superEffective )
			textRenderer.Color = new Color( 1f, 0.45f, 0.2f, 1f );
		else
			textRenderer.Color = new Color( 1f, 1f, 1f, 1f );

		// Small world scale so the large font renders crisply
		GameObject.WorldScale = critical ? new Vector3( 0.15f, 0.15f, 0.15f ) : new Vector3( 0.12f, 0.12f, 0.12f );
	}

	public void SetupMiss()
	{
		startPos = WorldPosition;
		lifetime = 0f;
		maxLifetime = 0.8f;

		textRenderer = GameObject.Components.GetOrCreate<TextRenderer>();
		textRenderer.Text = "MISS";
		textRenderer.FontSize = 120f;
		textRenderer.Color = new Color( 0.5f, 0.5f, 0.6f, 1f );

		GameObject.WorldScale = new Vector3( 0.1f, 0.1f, 0.1f );
	}

	protected override void OnUpdate()
	{
		if ( textRenderer == null ) return;

		lifetime += Time.Delta;
		var progress = lifetime / maxLifetime;

		if ( progress >= 1f )
		{
			GameObject.Destroy();
			return;
		}

		// Rise upward
		WorldPosition = startPos + Vector3.Up * (lifetime * riseSpeed);

		// Scale pop on crits — start big, settle to normal
		if ( isCritical && lifetime < 0.15f )
		{
			var baseScale = 0.15f;
			var scale = baseScale * (1f + (1f - lifetime / 0.15f) * 0.5f);
			GameObject.WorldScale = new Vector3( scale, scale, scale );
		}

		// Fade out in the last 40%
		if ( progress > 0.6f )
		{
			var fadeProgress = (progress - 0.6f) / 0.4f;
			var alpha = 1f - fadeProgress;
			var c = textRenderer.Color;
			textRenderer.Color = new Color( c.r, c.g, c.b, alpha );
		}
	}
}
