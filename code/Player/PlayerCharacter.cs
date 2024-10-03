namespace Clover.Player;

public sealed class PlayerCharacter : Component
{
	
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }
	
	[Property] public GameObject Model { get; set; }
	

	protected override void OnStart()
	{
		base.OnStart();
		GameObject.BreakFromPrefab();
	}

	public void ModelLookAt( Vector3 position )
	{
		var dir = ( position - WorldPosition ).Normal;
		dir.y = 0;
		Model.WorldRotation = Rotation.LookAt( dir, Vector3.Up );
	}

	/*protected override void OnUpdate()
	{
		Gizmo.Draw.Arrow( WorldPosition + Vector3.Up * 16f, WorldPosition + Vector3.Up * 16 + Model.WorldRotation.Forward * 32f );
	}*/
	
	[Authority]
	public void TeleportTo( Vector3 pos, Rotation rot )
	{
		WorldPosition = pos;
		// WorldRotation = rot;
		Transform.ClearInterpolation();
		ModelLookAt( pos + rot.Forward );
		GetComponent<CameraController>().SnapCamera();
	}

	[Authority]
	public void SetLayer( int layer )
	{
		WorldLayerObject.SetLayer( layer, true );
		WorldManager.Instance.SetActiveWorld( layer );
	}
}
