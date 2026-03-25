using Sandbox;

namespace Beastborne.Battle3D;

/// <summary>
/// Manages the 3D battle arena environment.
/// Phase 1: Simple ground plane with lighting.
/// Phase 3: Per-location diorama models.
/// </summary>
public sealed class BattleArena : Component
{
	private GameObject groundPlane;
	private GameObject battleLight;

	/// <summary>
	/// Set up the arena for a battle
	/// </summary>
	public void Setup( string locationId = null )
	{
		CreateGroundPlane();
		CreateLighting();
	}

	/// <summary>
	/// Tear down the arena
	/// </summary>
	public void Teardown()
	{
		groundPlane?.Destroy();
		groundPlane = null;

		battleLight?.Destroy();
		battleLight = null;
	}

	private void CreateGroundPlane()
	{
		groundPlane = new GameObject( true, "BattleGround" );
		groundPlane.Parent = GameObject;

		var renderer = groundPlane.Components.Create<ModelRenderer>();
		renderer.Model = Model.Load( "models/dev/plane.vmdl" );
		renderer.Tint = new Color( 0.15f, 0.12f, 0.2f, 1f );

		groundPlane.WorldPosition = new Vector3( 0f, 0f, -1f );
		groundPlane.WorldScale = new Vector3( 4f, 4f, 1f );
	}

	private void CreateLighting()
	{
		battleLight = new GameObject( true, "BattleLight" );
		battleLight.Parent = GameObject;

		var light = battleLight.Components.Create<DirectionalLight>();
		light.LightColor = new Color( 0.45f, 0.4f, 0.5f, 1f );
		light.SkyColor = new Color( 0.1f, 0.08f, 0.15f, 1f );
		light.Shadows = true;

		battleLight.WorldPosition = new Vector3( 0f, 0f, 200f );
		battleLight.WorldRotation = Rotation.From( 55f, -30f, 0f );
	}
}
