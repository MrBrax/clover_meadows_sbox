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
}
