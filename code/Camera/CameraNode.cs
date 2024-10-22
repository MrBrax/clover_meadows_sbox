using Clover.Player;
using Sandbox;

[Category( "Clover/Camera" )]
[Icon( "camera" )]
public sealed class CameraNode : Component
{
	[Property] public int Priority { get; set; } = 0;

	[Property] public float FieldOfView { get; set; } = 90;

	[Property] public bool FollowTargets { get; set; } = true;

	[Property] public bool Static { get; set; } = false;

	[Property] public bool Lerping { get; set; } = true;

	[Property] public bool HasPivotRotation { get; set; } = false;

	[Property] public bool ShouldSyncWithPlayerCameraRotation { get; set; } = false;

	public GameObject CameraPivot => HasPivotRotation ? GameObject.Parent : null;

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

	public void OnActivated()
	{
		Log.Info( $"CameraNode {GameObject.Name} activated" );
		if ( ShouldSyncWithPlayerCameraRotation )
		{
			if ( HasPivotRotation )
			{
				CameraPivot.WorldRotation = PlayerCharacter.Local.CameraController.CameraPivot.WorldRotation;
			}
			else
			{
				Log.Error( "ShouldSyncWithPlayerCameraRotation is true but HasPivotRotation is false" );
			}
		}
	}

	public void OnDeactivated()
	{
		Log.Info( $"CameraNode {GameObject.Name} deactivated" );
	}

	public void RotatePivot( Rotation rotation )
	{
		if ( !HasPivotRotation )
		{
			Log.Error( "RotatePivot called but HasPivotRotation is false" );
			return;
		}

		CameraPivot.WorldRotation *= rotation;
		if ( ShouldSyncWithPlayerCameraRotation )
		{
			PlayerCharacter.Local.CameraController.CameraPivot.WorldRotation = CameraPivot.WorldRotation;
		}
	}
}
