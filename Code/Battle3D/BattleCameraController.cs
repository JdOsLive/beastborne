using Sandbox;

namespace Beastborne.Battle3D;

/// <summary>
/// Controls the battle camera by taking over the EXISTING scene camera.
/// Phase 1: static overview angle with shake/hitstop.
/// Based on TennisCamera pattern from the tennis project.
/// </summary>
public sealed class BattleCameraController : Component
{
	[Property] public Vector3 DefaultPosition { get; set; } = new Vector3( -300f, 0f, 40f );
	[Property] public Vector3 DefaultLookTarget { get; set; } = new Vector3( 0f, 0f, 10f );
	[Property] public float FieldOfView { get; set; } = 50f;

	// ── KO money-shot tuning ───────────────────────────────────────────
	// Iterated down THREE times now from the original 80u/FOV35 framing.
	// User feedback consistent: any noticeable zoom feels too dramatic.
	//
	// Final philosophy: NO FOV change (FOV stays at default 50, which means
	// zero perspective warp), camera barely repositions (260u from target
	// = often a slight pullback, sometimes a slight push depending on which
	// side KO'd — both read as "camera lingers on the moment"). The drama
	// lives in the slow faint animation + the post-KO settle hold, not in
	// the camera move. The dolly is now mostly a "the camera noticed,"
	// not a "BAM cinematic close-up."
	public const float KOZoomDistance = 260f;
	public const float KOZoomFOV = 50f;
	public const float KOZoomTravelIn = 0.5f;
	public const float KOZoomHold = 0.35f;
	public const float KOZoomTravelOut = 0.45f;

	private CameraComponent mainCamera;
	private Vector3 savedCameraPos;
	private Rotation savedCameraRot;
	private float savedFov;

	// Shake
	private float shakeTimer;
	private float shakeIntensity;

	// Hitstop
	private float hitstopTimer;

	// Zoom punch
	private float zoomPunchAmount;
	private float zoomPunchDecay = 8f;

	private Vector3 targetPosition;
	private Rotation targetRotation;
	private bool isEnabled;

	// Idle sway
	private float swayTime;
	private float swayAmplitudeX = 8f;
	private float swayAmplitudeY = 5f;
	private float swayAmplitudeZ = 3f;
	private float swaySpeed = 0.4f;

	// Camera focus mode: "default" | "player" | "enemies"
	private string focusMode = "default";
	private float focusLerpSpeed = 3f;

	// ── KO money-shot state ────────────────────────────────────────────
	private enum KOZoomPhase { None, TravelIn, Hold, TravelOut }
	private KOZoomPhase koZoomPhase = KOZoomPhase.None;
	private float koZoomTimer;
	private float koZoomHoldOverride; // per-call hold duration override
	private Vector3 koZoomTargetWorld;
	private Vector3 koZoomFromPos;
	private Rotation koZoomFromRot;
	private float koZoomFromFov;
	// Captured at TriggerKOZoom time so the framing angle stays consistent
	// even while the idle sway would otherwise have drifted underneath us.
	private Vector3 koZoomViewDir;

	/// <summary>
	/// True while the KO money-shot (travel-in / hold / travel-out) is playing.
	/// BattleSceneController gates turn-progression off this so the next turn
	/// doesn't start mid-pan.
	/// </summary>
	public bool IsInKOZoom => koZoomPhase != KOZoomPhase.None;

	// Focus offsets (subtle shifts, not dramatic zooms)
	private Vector3 PlayerFocusOffset = new Vector3( 15f, 12f, -3f );
	private Vector3 EnemyFocusOffset = new Vector3( 60f, -40f, -5f );
	private Vector3 PlayerFocusLookOffset = new Vector3( -60f, 20f, 0f );
	private Vector3 EnemyFocusLookOffset = new Vector3( 40f, -40f, 0f );

	/// <summary>
	/// Enable — find the scene's main camera and take it over
	/// </summary>
	public void Enable()
	{
		// Find the existing main camera in the scene
		mainCamera = Scene.GetAllComponents<CameraComponent>()
			.FirstOrDefault( c => c.IsMainCamera || c.Priority >= 1 );

		if ( mainCamera == null )
		{
			Log.Warning( "Battle3D: No main camera found in scene!" );
			return;
		}

		// Save original camera state — but only if we're not already enabled.
		// Re-entering a battle on a fresh controller without this guard would
		// snapshot the PREVIOUS battle's position as the "saved" state, so
		// Disable() would restore the camera to a battle pose instead of the
		// true pre-battle pose. This is the controller's first run per
		// expedition so the guard is defensive — mostly matters if the same
		// controller gets reused across rounds.
		if ( !isEnabled )
		{
			savedCameraPos = mainCamera.WorldPosition;
			savedCameraRot = mainCamera.WorldRotation;
			savedFov = mainCamera.FieldOfView;
		}

		// Move the main camera to our battle position
		mainCamera.FieldOfView = FieldOfView;
		mainCamera.BackgroundColor = new Color( 0.08f, 0.06f, 0.12f, 1f );
		mainCamera.IsMainCamera = true; // Ensure nothing has demoted it.
		mainCamera.Enabled = true;       // Guard against it being toggled off.

		ResetToDefault();
		isEnabled = true;

		Log.Info( $"Battle3D: Took over main camera '{mainCamera.GameObject.Name}', moved from {savedCameraPos} to {mainCamera.WorldPosition}, FOV={mainCamera.FieldOfView}, Enabled={mainCamera.Enabled}" );
	}

	/// <summary>
	/// Disable — restore the main camera to its original state
	/// </summary>
	public void Disable()
	{
		if ( mainCamera != null )
		{
			mainCamera.WorldPosition = savedCameraPos;
			mainCamera.WorldRotation = savedCameraRot;
			mainCamera.FieldOfView = savedFov;
		}

		isEnabled = false;
	}

	public void ResetToDefault()
	{
		targetPosition = DefaultPosition;
		targetRotation = Rotation.LookAt( DefaultLookTarget - DefaultPosition, Vector3.Up );

		if ( mainCamera != null )
		{
			mainCamera.WorldPosition = targetPosition;
			mainCamera.WorldRotation = targetRotation;
		}
	}

	protected override void OnUpdate()
	{
		if ( !isEnabled || mainCamera == null ) return;

		UpdateHitstop();
		// KO zoom owns the camera while it's playing — it overrides default
		// position/sway/focus until travel-out completes.
		if ( koZoomPhase != KOZoomPhase.None )
		{
			UpdateKOZoom();
		}
		else
		{
			UpdatePosition();
		}
		UpdateShake();
	}

	private void UpdateHitstop()
	{
		if ( hitstopTimer <= 0f ) return;
		hitstopTimer -= RealTime.Delta;
		if ( hitstopTimer < 0f ) hitstopTimer = 0f;
	}

	private void UpdatePosition()
	{
		var dt = hitstopTimer > 0f ? Time.Delta * 0.05f : Time.Delta;

		// Idle sway — gentle orbiting motion
		swayTime += dt * swaySpeed;
		var swayOffset = new Vector3(
			MathF.Sin( swayTime ) * swayAmplitudeX,
			MathF.Cos( swayTime * 0.7f ) * swayAmplitudeY,
			MathF.Sin( swayTime * 0.5f + 1f ) * swayAmplitudeZ
		);

		// Focus mode position and look target offsets
		var posOffset = Vector3.Zero;
		var lookOffset = Vector3.Zero;

		if ( focusMode == "player" )
		{
			posOffset = PlayerFocusOffset;
			lookOffset = PlayerFocusLookOffset;
		}
		else if ( focusMode == "enemies" )
		{
			posOffset = EnemyFocusOffset;
			lookOffset = EnemyFocusLookOffset;
		}

		// Calculate target position with sway and focus
		var desiredPos = DefaultPosition + swayOffset + posOffset;
		var desiredLook = DefaultLookTarget + lookOffset;
		targetPosition = desiredPos;
		targetRotation = Rotation.LookAt( desiredLook - desiredPos, Vector3.Up );

		// Zoom punch
		zoomPunchAmount = Math.Max( 0f, zoomPunchAmount - zoomPunchDecay * RealTime.Delta * zoomPunchAmount );
		var zoomDir = (targetPosition - desiredLook).Normal;
		var effectiveTarget = targetPosition - zoomDir * zoomPunchAmount;

		// Smooth lerp — slower for focus transitions, faster for normal
		var lerpSpeed = focusMode == "default" ? 4f : focusLerpSpeed;
		mainCamera.WorldPosition = Vector3.Lerp( mainCamera.WorldPosition, effectiveTarget, dt * lerpSpeed );
		mainCamera.WorldRotation = Rotation.Slerp( mainCamera.WorldRotation, targetRotation, dt * lerpSpeed );
	}

	/// <summary>
	/// Cinematic KO money-shot: lerp the camera to a tight portrait of the
	/// fallen monster, hold, then lerp back. Total ~1.2s by default.
	/// Idle sway and focus offsets are suppressed for the duration so the
	/// framing stays locked. NO global Time.Scale change — purely camera.
	/// </summary>
	public void TriggerKOZoom( Vector3 targetWorldPos, float holdDuration = -1f )
	{
		if ( !isEnabled || mainCamera == null ) return;

		koZoomTargetWorld = targetWorldPos;
		koZoomHoldOverride = holdDuration < 0f ? KOZoomHold : holdDuration;
		koZoomFromPos = mainCamera.WorldPosition;
		koZoomFromRot = mainCamera.WorldRotation;
		koZoomFromFov = mainCamera.FieldOfView;

		// Capture the current view direction so the cinematic angle matches
		// whatever the player is currently looking at (keeps the framing
		// from "snapping" to a weird side angle if the sway has drifted).
		var lookFrom = koZoomFromPos;
		var lookAt = koZoomTargetWorld;
		var dir = (lookFrom - lookAt);
		if ( dir.LengthSquared < 0.01f )
			dir = (DefaultPosition - DefaultLookTarget);
		koZoomViewDir = dir.Normal;

		koZoomTimer = 0f;
		koZoomPhase = KOZoomPhase.TravelIn;
		Log.Info( $"Battle3D: KO zoom triggered, target={targetWorldPos}" );
	}

	private void UpdateKOZoom()
	{
		var dt = RealTime.Delta;
		koZoomTimer += dt;

		// Compute the cinematic target pose (recomputed each frame so if the
		// monster's faint translateY moves them slightly, framing follows).
		var koPos = koZoomTargetWorld + koZoomViewDir * KOZoomDistance;
		var koRot = Rotation.LookAt( koZoomTargetWorld - koPos, Vector3.Up );

		switch ( koZoomPhase )
		{
			case KOZoomPhase.TravelIn:
			{
				float t = MathX.Clamp( koZoomTimer / KOZoomTravelIn, 0f, 1f );
				// ease-in-out
				float eased = t * t * (3f - 2f * t);
				mainCamera.WorldPosition = Vector3.Lerp( koZoomFromPos, koPos, eased );
				mainCamera.WorldRotation = Rotation.Slerp( koZoomFromRot, koRot, eased );
				mainCamera.FieldOfView = MathX.Lerp( koZoomFromFov, KOZoomFOV, eased );
				if ( koZoomTimer >= KOZoomTravelIn )
				{
					koZoomPhase = KOZoomPhase.Hold;
					koZoomTimer = 0f;
				}
				break;
			}
			case KOZoomPhase.Hold:
			{
				mainCamera.WorldPosition = koPos;
				mainCamera.WorldRotation = koRot;
				mainCamera.FieldOfView = KOZoomFOV;
				if ( koZoomTimer >= koZoomHoldOverride )
				{
					koZoomPhase = KOZoomPhase.TravelOut;
					koZoomTimer = 0f;
					// Capture "from" again at the start of travel-out so we
					// lerp back from the actual KO pose, not the stale entry pose.
					koZoomFromPos = mainCamera.WorldPosition;
					koZoomFromRot = mainCamera.WorldRotation;
					koZoomFromFov = mainCamera.FieldOfView;
				}
				break;
			}
			case KOZoomPhase.TravelOut:
			{
				// Travel-out destination = whatever the default-pose update would
				// produce right now. Compute it inline so when control returns to
				// UpdatePosition, the handoff is seamless.
				var swayOffset = new Vector3(
					MathF.Sin( swayTime ) * swayAmplitudeX,
					MathF.Cos( swayTime * 0.7f ) * swayAmplitudeY,
					MathF.Sin( swayTime * 0.5f + 1f ) * swayAmplitudeZ
				);
				var posOffset = focusMode == "player" ? PlayerFocusOffset
					: focusMode == "enemies" ? EnemyFocusOffset : Vector3.Zero;
				var lookOffset = focusMode == "player" ? PlayerFocusLookOffset
					: focusMode == "enemies" ? EnemyFocusLookOffset : Vector3.Zero;
				var endPos = DefaultPosition + swayOffset + posOffset;
				var endLook = DefaultLookTarget + lookOffset;
				var endRot = Rotation.LookAt( endLook - endPos, Vector3.Up );

				float t = MathX.Clamp( koZoomTimer / KOZoomTravelOut, 0f, 1f );
				float eased = t * t * (3f - 2f * t);
				mainCamera.WorldPosition = Vector3.Lerp( koZoomFromPos, endPos, eased );
				mainCamera.WorldRotation = Rotation.Slerp( koZoomFromRot, endRot, eased );
				mainCamera.FieldOfView = MathX.Lerp( koZoomFromFov, FieldOfView, eased );

				// Tick sway during travel-out so when we hand back, the sway
				// time isn't frozen at the moment of trigger.
				swayTime += dt * swaySpeed;

				if ( koZoomTimer >= KOZoomTravelOut )
				{
					koZoomPhase = KOZoomPhase.None;
					koZoomTimer = 0f;
					Log.Info( "Battle3D: KO zoom complete" );
				}
				break;
			}
		}
	}

	private void UpdateShake()
	{
		if ( shakeTimer <= 0f ) return;
		shakeTimer -= Time.Delta;
		var shake = Random.Shared.Float( -1f, 1f ) * shakeIntensity;
		mainCamera.WorldPosition += new Vector3( shake, shake * 0.5f, shake * 0.3f );
	}

	public void TriggerShake( float intensity, float duration = 0.3f )
	{
		shakeIntensity = intensity;
		shakeTimer = duration;
	}

	public void TriggerHitstop( bool isCritical )
	{
		if ( isCritical )
		{
			hitstopTimer = 0.1f;
			zoomPunchAmount = 30f;
			TriggerShake( 4f, 0.2f );
		}
		else
		{
			hitstopTimer = 0.05f;
			zoomPunchAmount = 10f;
			TriggerShake( 1.5f, 0.1f );
		}
	}

	/// <summary>
	/// Focus camera on the player's beast (when picking moves)
	/// </summary>
	public void FocusPlayer()
	{
		focusMode = "player";
	}

	/// <summary>
	/// Focus camera on the enemy side (when picking targets)
	/// </summary>
	public void FocusEnemies()
	{
		focusMode = "enemies";
	}

	/// <summary>
	/// Return camera to default idle position
	/// </summary>
	public void FocusDefault()
	{
		focusMode = "default";
	}

	public CameraComponent GetCamera() => mainCamera;
}
