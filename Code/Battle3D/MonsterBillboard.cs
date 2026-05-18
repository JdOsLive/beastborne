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
	// Faint timing — was 0.45s linear which felt rushed (sprite vanished
	// before the player registered the KO, especially during the KO money-
	// shot zoom). Bumped to 0.85s with ease-in (gravity-style accel) so the
	// drop reads as weight, not a teleport. Drop distance 15u → 22u for
	// more visible travel. The sprite also tilts ~25° during the fall so
	// the KO reads as "slumped over" rather than "rigidly translated down".
	[Property] public float FaintDuration { get; set; } = 0.85f;
	[Property] public float FaintDropDistance { get; set; } = 22f;
	[Property] public float FaintTiltDegrees { get; set; } = 25f;
	private Vector3 faintStartPos;
	private Rotation faintStartRot;
	private bool faintStartRotCaptured;

	// Attack animation state (legacy PlayAttack lunge — currently unused; kept
	// for compatibility. Real attack motion now flows through LungeForward).
	private bool isAttacking;
	private float attackTimer;
	private const float AttackLungeDuration = 0.15f;
	private const float AttackReturnDuration = 0.2f;
	private Vector3 attackStartPos;
	private Vector3 attackLungeTarget;

	// ── Billboard motion (lunge / knockback) ──────────────────────────
	// Layered offset on top of WorldPosition. Rest position is captured by
	// PositionTeam → SetRestPosition; per-frame Update applies offset on top.
	// Last call wins — a new lunge cancels any in-flight motion.
	//
	// Tuning constants (initial — easy to iterate). Distances in world units,
	// durations in seconds. See the table in CLAUDE-task plan for source.
	public const float LungeDistanceNormal = 18f;
	public const float LungeDistanceCrit = 28f;
	public const float LungeDistanceSuper = 22f;
	public const float LungeDistanceMiss = 18f;

	public const float KnockbackDistanceNormal = 8f;
	public const float KnockbackDistanceCrit = 14f;
	public const float KnockbackDistanceSuper = 10f;

	public const float LungeOutNormal = 0.15f;
	public const float LungeHoldNormal = 0.05f;
	public const float LungeBackNormal = 0.20f;

	public const float LungeOutCrit = 0.15f;
	public const float LungeHoldCrit = 0.08f;
	public const float LungeBackCrit = 0.20f;

	public const float LungeOutSuper = 0.15f;
	public const float LungeHoldSuper = 0.05f;
	public const float LungeBackSuper = 0.22f;

	public const float LungeOutMiss = 0.15f;
	public const float LungeHoldMiss = 0.0f;
	public const float LungeBackMiss = 0.10f;

	public const float KbOutNormal = 0.10f;
	public const float KbBackNormal = 0.25f;
	public const float KbOutCrit = 0.10f;
	public const float KbBackCrit = 0.30f;
	public const float KbOutSuper = 0.10f;
	public const float KbBackSuper = 0.27f;

	public const float KbDelayNormal = 0.15f;
	public const float KbDelayCrit = 0.18f;
	public const float KbDelaySuper = 0.15f;

	private Vector3 motionOffset = Vector3.Zero;
	private Vector3 restPosition;
	private bool restPositionSet;

	// Lunge state machine
	private enum MotionPhase { Idle, Out, Hold, Back }
	private MotionPhase lungePhase = MotionPhase.Idle;
	private Vector3 lungeTargetOffset;
	private float lungeOutDuration;
	private float lungeHoldDuration;
	private float lungeBackDuration;
	private float lungePhaseTimer;
	private Vector3 lungePhaseStart;

	// Knockback state machine (with optional delay before starting)
	private MotionPhase kbPhase = MotionPhase.Idle;
	private float kbDelay;
	private Vector3 kbTargetOffset;
	private float kbOutDuration;
	private float kbBackDuration;
	private float kbPhaseTimer;
	private Vector3 kbPhaseStart;

	// Hit flash state — single bright-white flash on damage taken.
	// Was 0.4s red-flicker (3 pulses) which dragged across the camera shake
	// and read more like "constant alarm" than "punch". 80ms white is the
	// fighting-game standard — one frame of "I got hit", then back to normal
	// so the next frame's animation reads cleanly.
	private bool isFlashing;
	private float flashTimer;
	private const float FlashDuration = 0.08f;

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

	/// <summary>
	/// Refresh the cached <see cref="Species"/> + sprite if the monster's
	/// species has changed (i.e., the player evolved it between battles
	/// while the 3D scene was still alive in wave-transition mode).
	/// Returns true if the sprite was recreated.
	/// </summary>
	public bool RefreshSpeciesIfChanged()
	{
		if ( Monster == null ) return false;
		var currentSpecies = MonsterManager.Instance?.GetSpecies( Monster.SpeciesId );
		if ( currentSpecies == null ) return false;
		if ( currentSpecies.Id == Species?.Id ) return false;

		Log.Info( $"Battle3D: Species changed for {Monster.Nickname} ({Species?.Id} → {currentSpecies.Id}) — refreshing sprite" );
		Species = currentSpecies;
		CreateSprite();
		return true;
	}

	protected override void OnUpdate()
	{
		UpdateAttackAnimation();
		UpdateSwapLerp();
		UpdateVisualState();
		// Hit flash runs AFTER UpdateVisualState — the normal-state branch of
		// UpdateVisualState writes renderer.Color every frame, so a flash set
		// before it would be overwritten and never seen.
		UpdateHitFlash();
		UpdateBillboardMotion();
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
			// Clear any in-flight billboard motion so the faint starts from
			// the rest pose, not whatever knockback frame we happened to be on.
			ClearMotion();
			isFainting = true;
			faintTimer = 0f;
			faintStartPos = restPositionSet ? restPosition : WorldPosition;
			faintStartRot = WorldRotation;
			faintStartRotCaptured = true;
			WorldPosition = faintStartPos;
		}

		if ( isFainting )
		{
			faintTimer += Time.Delta;
			var progress = MathF.Min( faintTimer / FaintDuration, 1f );

			// Ease-in (quadratic) on the drop so it accelerates like gravity
			// instead of falling at constant speed. Alpha stays linear so the
			// sprite is still readable mid-fall. Tilt eases the SAME curve so
			// it visually couples with the drop — sprite slumps as it falls.
			var dropProgress = progress * progress;

			var alpha = 1f - progress;
			renderer.Color = new Color( 1f, 1f, 1f, alpha );
			WorldPosition = faintStartPos + Vector3.Down * (dropProgress * FaintDropDistance);

			// Roll around world-X — for the angled side-view battle camera this
			// reads as the sprite slumping forward toward the viewer. Player side
			// tilts opposite direction from enemy side so both sides slump TOWARD
			// the centerline (visually "falling into the battlefield" rather than
			// "falling away from it"). Worst case if the billboard fully camera-
			// locks the rotation: it's a no-op and we still get the drop+fade.
			float tiltDir = IsPlayerSide ? 1f : -1f;
			float tilt = dropProgress * FaintTiltDegrees * tiltDir;
			WorldRotation = faintStartRot * Rotation.From( tilt, 0f, 0f );

			if ( progress >= 1f )
			{
				renderer.Enabled = false;
				isFainting = false;
				faintComplete = true; // Prevents restart
				WorldRotation = faintStartRot; // Restore so a future ResetFromFaint sees clean state
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

	// ── Billboard motion API ──────────────────────────────────────────────

	/// <summary>
	/// World-space rest position the controller placed this billboard at.
	/// Lunge/knockback offsets are layered on top of this. Defaults to current
	/// WorldPosition the first time it's read so callers don't get Vector3.Zero
	/// before SetRestPosition has fired.
	/// </summary>
	public Vector3 RestPosition
	{
		get
		{
			if ( !restPositionSet ) { restPosition = WorldPosition; restPositionSet = true; }
			return restPosition;
		}
	}

	/// <summary>
	/// Tell the billboard the position it should sit at when at rest. Called by
	/// PositionTeam / wave transitions / swap exits — anything that wants to
	/// (re)anchor the sprite. Clears any in-flight motion so the new rest
	/// position takes effect immediately.
	/// </summary>
	public void SetRestPosition( Vector3 pos )
	{
		restPosition = pos;
		restPositionSet = true;
		ClearMotion();
		WorldPosition = pos;
	}

	/// <summary>
	/// Cancel any in-flight lunge/knockback and snap motionOffset to zero.
	/// Used on KO and wave-transition cleanup.
	/// </summary>
	public void ClearMotion()
	{
		motionOffset = Vector3.Zero;
		lungePhase = MotionPhase.Idle;
		kbPhase = MotionPhase.Idle;
		kbDelay = 0f;
	}

	/// <summary>
	/// Attacker physical commit. Eases motionOffset from 0 → dirToTarget*distance
	/// over outDuration, holds, then eases back to 0 over backDuration. Last
	/// call wins — cancels any prior in-flight lunge.
	/// </summary>
	public void LungeForward( Vector3 dirToTarget, float distance, float outDuration, float holdDuration, float backDuration )
	{
		if ( IsKO || isFainting ) return;
		lungeTargetOffset = dirToTarget.Normal * distance;
		lungeOutDuration = MathF.Max( 0.001f, outDuration );
		lungeHoldDuration = MathF.Max( 0f, holdDuration );
		lungeBackDuration = MathF.Max( 0.001f, backDuration );
		lungePhaseStart = motionOffset;
		lungePhaseTimer = 0f;
		lungePhase = MotionPhase.Out;
	}

	/// <summary>
	/// Defender knockback. Eases motionOffset away from the attacker direction.
	/// No hold phase. Optional delayBeforeStart so it can lag the attacker's
	/// lunge for an "impact-then-recoil" beat.
	/// </summary>
	public void Knockback( Vector3 fromAttackerDir, float distance, float outDuration, float backDuration, float delayBeforeStart = 0f )
	{
		if ( IsKO || isFainting ) return;
		kbTargetOffset = fromAttackerDir.Normal * distance;
		kbOutDuration = MathF.Max( 0.001f, outDuration );
		kbBackDuration = MathF.Max( 0.001f, backDuration );
		kbDelay = MathF.Max( 0f, delayBeforeStart );
		kbPhaseStart = motionOffset;
		kbPhaseTimer = 0f;
		kbPhase = delayBeforeStart > 0f ? MotionPhase.Idle : MotionPhase.Out;
		// During delay we keep kbPhase = Idle and tick kbDelay down in the
		// motion update; once it hits zero we promote to Out. Sentinel pattern
		// so a single state machine handles both flows.
		_kbAwaitingDelay = delayBeforeStart > 0f;
	}

	private bool _kbAwaitingDelay;

	private static float SmoothStep( float t )
	{
		// Cubic in/out — heavier on start/end, snappier through the middle.
		t = MathX.Clamp( t, 0f, 1f );
		return t * t * (3f - 2f * t);
	}

	private void UpdateBillboardMotion()
	{
		// KO / faint / swap-lerp / entrance owns WorldPosition outright; bail
		// so we don't fight them.
		if ( IsKO || isFainting || faintComplete || isEntering || isSwapLerping )
		{
			if ( motionOffset != Vector3.Zero ) motionOffset = Vector3.Zero;
			return;
		}

		// Legacy PlayAttack also writes WorldPosition directly; defer to it
		// when active (it'll restore attackStartPos on completion, then we
		// resume here). Without this, the two systems would race per-frame.
		if ( isAttacking ) return;

		float dt = Time.Delta;

		// ── Lunge phases ─────
		switch ( lungePhase )
		{
			case MotionPhase.Out:
			{
				lungePhaseTimer += dt;
				float t = SmoothStep( lungePhaseTimer / lungeOutDuration );
				motionOffset = Vector3.Lerp( lungePhaseStart, lungeTargetOffset, t );
				if ( lungePhaseTimer >= lungeOutDuration )
				{
					motionOffset = lungeTargetOffset;
					if ( lungeHoldDuration > 0f )
					{
						lungePhase = MotionPhase.Hold;
						lungePhaseTimer = 0f;
					}
					else
					{
						lungePhase = MotionPhase.Back;
						lungePhaseTimer = 0f;
						lungePhaseStart = motionOffset;
					}
				}
				break;
			}
			case MotionPhase.Hold:
			{
				lungePhaseTimer += dt;
				motionOffset = lungeTargetOffset;
				if ( lungePhaseTimer >= lungeHoldDuration )
				{
					lungePhase = MotionPhase.Back;
					lungePhaseTimer = 0f;
					lungePhaseStart = motionOffset;
				}
				break;
			}
			case MotionPhase.Back:
			{
				lungePhaseTimer += dt;
				float t = SmoothStep( lungePhaseTimer / lungeBackDuration );
				motionOffset = Vector3.Lerp( lungePhaseStart, Vector3.Zero, t );
				if ( lungePhaseTimer >= lungeBackDuration )
				{
					motionOffset = Vector3.Zero;
					lungePhase = MotionPhase.Idle;
				}
				break;
			}
		}

		// ── Knockback phases (independent state machine — both can run simultaneously
		// in principle, but in practice attacker and defender are different billboards).
		// We keep both here so "self-knockback" or chain reactions remain trivial.
		if ( _kbAwaitingDelay )
		{
			kbDelay -= dt;
			if ( kbDelay <= 0f )
			{
				_kbAwaitingDelay = false;
				kbPhase = MotionPhase.Out;
				kbPhaseTimer = 0f;
				kbPhaseStart = motionOffset;
			}
		}

		switch ( kbPhase )
		{
			case MotionPhase.Out:
			{
				kbPhaseTimer += dt;
				float t = SmoothStep( kbPhaseTimer / kbOutDuration );
				// Combine with any active lunge offset by treating kb as additive
				// override during this frame — but since attacker billboards
				// don't get knockback (and vice-versa) in normal play, this is
				// effectively a direct write.
				motionOffset = Vector3.Lerp( kbPhaseStart, kbTargetOffset, t );
				if ( kbPhaseTimer >= kbOutDuration )
				{
					motionOffset = kbTargetOffset;
					kbPhase = MotionPhase.Back;
					kbPhaseTimer = 0f;
					kbPhaseStart = motionOffset;
				}
				break;
			}
			case MotionPhase.Back:
			{
				kbPhaseTimer += dt;
				float t = SmoothStep( kbPhaseTimer / kbBackDuration );
				motionOffset = Vector3.Lerp( kbPhaseStart, Vector3.Zero, t );
				if ( kbPhaseTimer >= kbBackDuration )
				{
					motionOffset = Vector3.Zero;
					kbPhase = MotionPhase.Idle;
				}
				break;
			}
		}

		// Apply motion offset on top of rest position.
		if ( restPositionSet && (lungePhase != MotionPhase.Idle || kbPhase != MotionPhase.Idle || _kbAwaitingDelay || motionOffset != Vector3.Zero) )
		{
			WorldPosition = restPosition + motionOffset;
		}
	}

	private void UpdateHitFlash()
	{
		if ( !isFlashing || renderer == null ) return;

		// A faint fade or entrance animation owns the sprite color — drop any
		// in-flight flash so it doesn't fight them. UpdateVisualState already
		// set the correct color this frame, so no need to restore it here.
		if ( isFainting || faintComplete || isEntering )
		{
			isFlashing = false;
			return;
		}

		flashTimer += Time.Delta;

		if ( flashTimer >= FlashDuration )
		{
			isFlashing = false;
			renderer.Color = Color.White;
			return;
		}

		// Single bright-white flash that ramps out over the duration. Color
		// stays above 1.0 at the start to wash the sprite toward pure white;
		// the per-channel value eases back to 1.0 across the 80ms window so
		// the tail of the flash blends back into the normal sprite color
		// without a hard pop. Renderer.Color uses RGB scaling > 1.0 to brighten.
		var t = flashTimer / FlashDuration;
		var intensity = 1f + (1f - t) * 1.5f; // 2.5 → 1.0
		renderer.Color = new Color( intensity, intensity, intensity, 1f );
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
		ClearMotion();
		// Restore upright rotation so a healed/wave-cleared beast doesn't come
		// back still tilted from the previous KO collapse. Gated on an explicit
		// capture flag rather than comparing against default(Rotation).
		if ( faintStartRotCaptured )
		{
			WorldRotation = faintStartRot;
			faintStartRotCaptured = false;
		}
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
