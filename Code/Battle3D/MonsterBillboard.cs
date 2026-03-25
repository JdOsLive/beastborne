using Sandbox;
using Beastborne.Core;
using Beastborne.Data;

namespace Beastborne.Battle3D;

/// <summary>
/// Renders a 2D monster sprite in 3D space using SpriteRenderer.
/// Uses .sprite resources for proper alpha and animation support.
/// </summary>
public sealed class MonsterBillboard : Component
{
	public Monster Monster { get; private set; }
	public MonsterSpecies Species { get; private set; }
	public bool IsPlayerSide { get; private set; }
	public bool IsActive { get; set; } = true;
	public bool IsKO { get; set; }

	private SpriteRenderer renderer;

	/// <summary>
	/// Size of the sprite in world units
	/// </summary>
	private float spriteSize = 45f;

	public void Setup( Monster monster, MonsterSpecies species, bool isPlayerSide )
	{
		Monster = monster;
		Species = species;
		IsPlayerSide = isPlayerSide;

		CreateSprite();
	}

	protected override void OnUpdate()
	{
		UpdateVisualState();
	}

	private void CreateSprite()
	{
		renderer = GameObject.Components.GetOrCreate<SpriteRenderer>();

		// Load the .sprite resource file
		var speciesName = (Species?.Id ?? Species?.Name ?? "").ToLowerInvariant().Replace( " ", "" );
		var spritePath = $"ui/monsters/{speciesName}/{speciesName}_static.sprite";
		var sprite = ResourceLibrary.Get<Sprite>( spritePath );

		if ( sprite != null )
		{
			renderer.Sprite = sprite;

			// Play the idle animation if it exists
			var idleIdx = sprite.GetAnimationIndex( "idle" );
			if ( idleIdx >= 0 )
			{
				renderer.PlayAnimation( idleIdx );
			}

			Log.Info( $"Battle3D: Loaded sprite resource '{spritePath}' for {Species?.Name}" );
		}
		else
		{
			// Fallback: create sprite from icon texture
			var tex = Texture.Load( FileSystem.Mounted, Species?.IconPath ?? "" );
			if ( tex != null )
			{
				renderer.Sprite = Sprite.FromTexture( tex );
			}
			Log.Warning( $"Battle3D: Sprite resource not found at '{spritePath}', using icon fallback for {Species?.Name}" );
		}

		renderer.Size = new Vector2( spriteSize, spriteSize );
		renderer.Shadows = false;
		renderer.FlipHorizontal = IsPlayerSide; // Player sprites face right
		renderer.Lighting = true;
		renderer.AlphaCutoff = 0f;
		renderer.TextureFilter = Sandbox.Rendering.FilterMode.Point; // Pixel-art crispness

		Log.Info( $"Battle3D: SpriteRenderer created for {Species?.Name}" );
	}

	/// <summary>
	/// Update visibility based on active/benched/KO state
	/// </summary>
	private void UpdateVisualState()
	{
		if ( renderer == null ) return;

		if ( !IsActive || IsKO )
		{
			renderer.Enabled = false;
		}
		else
		{
			renderer.Enabled = true;
			renderer.Color = Color.White;
		}
	}

	/// <summary>
	/// Get the top of the sprite in world space (for HP bar projection)
	/// </summary>
	public Vector3 GetTopPosition()
	{
		return WorldPosition + Vector3.Up * (spriteSize * 0.6f);
	}
}
