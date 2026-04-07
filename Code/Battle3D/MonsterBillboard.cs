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

	// Target selection state (3D mouse picking)
	public bool IsTargetHovered { get; set; }
	public bool IsTargetSelected { get; set; }

	// Swap preview state
	public bool IsSwapVisible { get; set; }
	public bool IsSwapHighlighted { get; set; }
	public float SwapFade { get; set; } = 1f; // 0=invisible, 1=full opacity
	private float highlightPulseTime;
	private float hoverPulseTime;

	// Contract capture fade
	public Color? ContractFadeColor { get; set; }

	// Swap carousel lerp
	private bool isSwapLerping;
	private Vector3 swapLerpStart;
	private Vector3 swapLerpTarget;
	private float swapLerpTimer;
	private float swapLerpTargetFade;
	private float swapLerpStartFade;
	private const float SwapLerpDuration = 0.25f;

	private SpriteRenderer renderer;

	// Faint animation state
	public bool IsFainting => isFainting;
	public bool FaintComplete => faintComplete;
	private bool isFainting;
	private bool faintComplete;
	private float faintTimer;
	[Property] public float FaintDuration { get; set; } = 0.45f;
	private Vector3 faintStartPos;

	// Attack animation state
	private bool isAttacking;
	private float attackTimer;
	private const float AttackLungeDuration = 0.15f;
	private const float AttackReturnDuration = 0.2f;
	private Vector3 attackStartPos;
	private Vector3 attackLungeTarget;

	// Hit flash state
	private bool isFlashing;
	private float flashTimer;
	private const float FlashDuration = 0.4f;
	private int flashCount = 3;

	// Entrance animation (for wave transitions)
	public Vector3 EntranceStartPos { get; set; }
	public Vector3 EntranceTargetPos { get; set; }
	public bool IsEntering => isEntering;
	private bool isEntering;

	/// <summary>
	/// Size of the sprite in world units.
	/// Player side is smaller to compensate for being closer to camera.
	/// </summary>
	private float spriteSize;
	private const float PlayerSpriteSize = 28f;
	private const float EnemySpriteSize = 45f;

	public void Setup( Monster monster, MonsterSpecies species, bool isPlayerSide )
	{
		Monster = monster;
		Species = species;
		IsPlayerSide = isPlayerSide;
		GameObject.Tags.Add( "monster" );
		GameObject.Tags.Add( "solid" );
		spriteSize = isPlayerSide ? PlayerSpriteSize : EnemySpriteSize;

		CreateSprite();
	}

	protected override void OnUpdate()
	{
		UpdateAttackAnimation();
		UpdateHitFlash();
		UpdateSwapLerp();
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

		// Add a box collider for 3D mouse picking
		var collider = GameObject.Components.GetOrCreate<BoxCollider>();
		collider.Scale = new Vector3( spriteSize, spriteSize, 2f );
		collider.Center = new Vector3( 0f, 0f, spriteSize * 0.5f );
		collider.Static = true;

		Log.Info( $"Battle3D: SpriteRenderer created for {Species?.Name}" );
	}

	/// <summary>
	/// Update visibility based on active/benched/KO state
	/// </summary>
	private void UpdateVisualState()
	{
		if ( renderer == null ) return;

		// Don't override state during entrance animation
		if ( isEntering ) return;

		// Handle faint animation (only start once, never restart)
		if ( IsKO && !isFainting && !faintComplete )
		{
			isFainting = true;
			faintTimer = 0f;
			faintStartPos = WorldPosition;
		}

		if ( isFainting )
		{
			faintTimer += Time.Delta;
			var progress = MathF.Min( faintTimer / FaintDuration, 1f );

			var alpha = 1f - progress;
			renderer.Color = new Color( 1f, 1f, 1f, alpha );
			WorldPosition = faintStartPos + Vector3.Down * (progress * 15f);

			if ( progress >= 1f )
			{
				renderer.Enabled = false;
				isFainting = false;
				faintComplete = true; // Prevents restart
			}
			return;
		}

		// Already fainted — stay invisible
		if ( faintComplete )
		{
			renderer.Enabled = false;
			return;
		}

		if ( !IsActive && IsPlayerSide && !IsSwapVisible )
		{
			renderer.Enabled = false;
		}
		else
		{
			renderer.Enabled = true;

			if ( ContractFadeColor.HasValue )
			{
				renderer.Color = ContractFadeColor.Value;
			}
			else if ( IsTargetHovered && !IsSwapVisible && !IsSwapHighlighted )
			{
				hoverPulseTime += Time.Delta * 3f;
				var pulse = 0.85f + MathF.Sin( hoverPulseTime ) * 0.15f;
				renderer.Color = new Color( pulse, pulse, pulse, 1f );
			}
			else if ( IsSwapHighlighted )
			{
				highlightPulseTime += Time.Delta * 3f;
				var pulse = 0.7f + MathF.Sin( highlightPulseTime ) * 0.3f;
				renderer.Color = new Color( pulse, pulse, 1f, SwapFade );
			}
			else if ( IsSwapVisible && SwapFade < 1f )
			{
				renderer.Color = new Color( 0.6f, 0.6f, 0.7f, SwapFade );
			}
			else
			{
				hoverPulseTime = 0f;
				renderer.Color = Color.White;
			}
		}
	}

	/// <summary>
	/// Play attack lunge animation toward a target position
	/// </summary>
	public void PlayAttack( Vector3 targetWorldPos )
	{
		if ( isAttacking || isFainting ) return;
		isAttacking = true;
		attackTimer = 0f;
		attackStartPos = WorldPosition;

		// Lunge partway toward the target
		var direction = (targetWorldPos - WorldPosition).Normal;
		attackLungeTarget = WorldPosition + direction * 20f;
	}

	/// <summary>
	/// Play hit flash animation (white/red flicker)
	/// </summary>
	public void PlayHitFlash()
	{
		if ( isFainting ) return;
		isFlashing = true;
		flashTimer = 0f;
	}

	/// <summary>
	/// Smoothly lerp to a new carousel position and fade level
	/// </summary>
	public void LerpToSwapPosition( Vector3 targetPos, float targetFade )
	{
		swapLerpStart = WorldPosition;
		swapLerpTarget = targetPos;
		swapLerpStartFade = SwapFade;
		swapLerpTargetFade = targetFade;
		swapLerpTimer = 0f;
		isSwapLerping = true;
	}

	/// <summary>
	/// Snap to position but fade in opacity (for initial carousel entry)
	/// </summary>
	public void FadeInToSwapPosition( Vector3 targetPos, float targetFade )
	{
		WorldPosition = targetPos;
		swapLerpStart = targetPos;
		swapLerpTarget = targetPos;
		swapLerpStartFade = 0f;
		swapLerpTargetFade = targetFade;
		SwapFade = 0f;
		swapLerpTimer = 0f;
		isSwapLerping = true;
	}

	/// <summary>
	/// Fade out opacity, then call ClearSwapState when done
	/// </summary>
	public void FadeOutSwap( System.Action onComplete = null )
	{
		swapLerpStart = WorldPosition;
		swapLerpTarget = WorldPosition;
		swapLerpStartFade = SwapFade;
		swapLerpTargetFade = 0f;
		swapLerpTimer = 0f;
		isSwapLerping = true;
		swapFadeOutCallback = onComplete;
	}

	private System.Action swapFadeOutCallback;

	private void UpdateSwapLerp()
	{
		if ( !isSwapLerping ) return;

		swapLerpTimer += Time.Delta;
		var t = MathF.Min( swapLerpTimer / SwapLerpDuration, 1f );
		// Ease-out cubic for smooth deceleration
		var eased = 1f - MathF.Pow( 1f - t, 3f );

		WorldPosition = Vector3.Lerp( swapLerpStart, swapLerpTarget, eased );
		SwapFade = MathX.Lerp( swapLerpStartFade, swapLerpTargetFade, eased );

		if ( t >= 1f )
		{
			isSwapLerping = false;
			if ( swapFadeOutCallback != null )
			{
				swapFadeOutCallback.Invoke();
				swapFadeOutCallback = null;
			}
		}
	}

	private void UpdateAttackAnimation()
	{
		if ( !isAttacking ) return;

		attackTimer += Time.Delta;
		var totalDuration = AttackLungeDuration + AttackReturnDuration;

		if ( attackTimer <= AttackLungeDuration )
		{
			// Lunge forward
			var t = attackTimer / AttackLungeDuration;
			WorldPosition = Vector3.Lerp( attackStartPos, attackLungeTarget, t * t );
		}
		else if ( attackTimer <= totalDuration )
		{
			// Return to start
			var t = (attackTimer - AttackLungeDuration) / AttackReturnDuration;
			WorldPosition = Vector3.Lerp( attackLungeTarget, attackStartPos, t );
		}
		else
		{
			WorldPosition = attackStartPos;
			isAttacking = false;
		}
	}

	private void UpdateHitFlash()
	{
		if ( !isFlashing || renderer == null ) return;

		flashTimer += Time.Delta;

		if ( flashTimer >= FlashDuration )
		{
			isFlashing = false;
			renderer.Color = Color.White;
			return;
		}

		// Rapid flash between white and red
		var flashPhase = (int)(flashTimer / (FlashDuration / (flashCount * 2)));
		if ( flashPhase % 2 == 0 )
			renderer.Color = new Color( 1f, 0.3f, 0.3f, 1f );
		else
			renderer.Color = Color.White;
	}

	/// <summary>
	/// Animate entrance from off-screen to target position (0-1 progress)
	/// </summary>
	public void AnimateEntrance( float progress )
	{
		float t = 1f - MathF.Pow( 1f - progress, 3f );
		WorldPosition = Vector3.Lerp( EntranceStartPos, EntranceTargetPos, t );

		if ( renderer != null )
			renderer.Color = new Color( 1f, 1f, 1f, MathF.Min( progress * 2f, 1f ) );

		if ( progress >= 1f )
		{
			isEntering = false;
			if ( renderer != null )
				renderer.Color = Color.White;
		}
	}

	/// <summary>
	/// Set sprite alpha and mark as entering (prevents UpdateVisualState from overriding)
	/// </summary>
	public void SetEntranceAlpha( float alpha )
	{
		isEntering = true;
		if ( renderer != null )
		{
			renderer.Enabled = true;
			renderer.Color = new Color( 1f, 1f, 1f, alpha );
		}
	}

	/// <summary>
	/// Reset from faint state (for wave transitions — player beasts get healed)
	/// </summary>
	public void ResetFromFaint()
	{
		IsKO = false;
		isFainting = false;
		faintComplete = false;
		isEntering = false;
		if ( renderer != null )
		{
			renderer.Enabled = true;
			renderer.Color = Color.White;
		}
	}

	public void ClearSwapState()
	{
		IsSwapVisible = false;
		IsSwapHighlighted = false;
		SwapFade = 1f;
		highlightPulseTime = 0f;
	}

	/// <summary>
	/// Get the top of the sprite in world space (for HP bar projection)
	/// </summary>
	public Vector3 GetTopPosition()
	{
		return WorldPosition + Vector3.Up * (spriteSize * 0.6f);
	}
}
