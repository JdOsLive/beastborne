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

		// Save original camera state
		savedCameraPos = mainCamera.WorldPosition;
		savedCameraRot = mainCamera.WorldRotation;
		savedFov = mainCamera.FieldOfView;

		// Move the main camera to our battle position
		mainCamera.FieldOfView = FieldOfView;
		mainCamera.BackgroundColor = new Color( 0.08f, 0.06f, 0.12f, 1f );

		ResetToDefault();
		isEnabled = true;

		Log.Info( $"Battle3D: Took over main camera '{mainCamera.GameObject.Name}', moved from {savedCameraPos} to {mainCamera.WorldPosition}, FOV={mainCamera.FieldOfView}" );
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
		UpdatePosition();
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

		// Continuously update target from properties so we can tweak live via MCP
		targetPosition = DefaultPosition;
		targetRotation = Rotation.LookAt( DefaultLookTarget - DefaultPosition, Vector3.Up );

		zoomPunchAmount = Math.Max( 0f, zoomPunchAmount - zoomPunchDecay * RealTime.Delta * zoomPunchAmount );

		var zoomDir = (targetPosition - DefaultLookTarget).Normal;
		var effectiveTarget = targetPosition - zoomDir * zoomPunchAmount;

		mainCamera.WorldPosition = Vector3.Lerp( mainCamera.WorldPosition, effectiveTarget, dt * 5f );
		mainCamera.WorldRotation = Rotation.Slerp( mainCamera.WorldRotation, targetRotation, dt * 5f );
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

	public CameraComponent GetCamera() => mainCamera;
}
