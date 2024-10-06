using Sandbox;

[Category( "Clover/Camera" )]
[Icon( "camera" )]
public sealed class CameraNode : Component
{
	
	[Property] public int Priority { get; set; } = 0;
	
	[Property] public float FieldOfView { get; set; } = 90;
	
	protected override void OnUpdate()
	{

	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Hitbox.BBox( BBox.FromPositionAndSize( Vector3.Zero, 16f ) );
		Gizmo.Draw.Model( "models/editor/camera.vmdl", new Transform() );
		Gizmo.Draw.Arrow( Vector3.Zero, Vector3.Forward * 64f );
	}

	public void SnapTo()
	{
		var camera = Scene.GetAllComponents<CameraMan>().FirstOrDefault();
		if ( !camera.IsValid() ) return;
		camera.SnapTo( WorldPosition, WorldRotation );
	}
}
