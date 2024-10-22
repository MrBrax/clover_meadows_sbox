using Clover.Camera;
using Clover.Player;
using Sandbox;

[Category( "Clover/Camera" )]
[Icon( "camera" )]
public sealed class CameraNode : Component
{
	public bool IsCameraActive => CameraMan.Instance.MainCameraNode == this;

	[Property] public int Priority { get; set; } = 0;

	[Property] public float FieldOfView { get; set; } = 90;

	[Property] public bool FollowTargets { get; set; } = true;

	[Property] public bool Static { get; set; } = false;

	[Property] public bool Lerping { get; set; } = true;

	[Property] public bool HasPivotRotation { get; set; } = false;

	[Property] public bool ShouldSyncWithPlayerCameraRotation { get; set; } = false;

	[Property] public CameraDollyNode DollyNode { get; set; }

	public GameObject CameraPivot => HasPivotRotation ? GameObject.Parent : null;

	protected override void OnUpdate()
	{
		if ( IsCameraActive && DollyNode.IsValid() && CameraPivot.IsValid() )
		{
			// move the camera between dolly nodes to follow player
			var playerPosition = PlayerCharacter.Local.WorldPosition;
			var path = DollyNode.GetPath();

			CameraPivot.WorldPosition = playerPosition;

			var node1 = path.MinBy( x => x.WorldPosition.Distance( playerPosition ) );
			var node2 = path.Where( x => x != node1 ).MinBy( x => x.WorldPosition.Distance( playerPosition ) );

			var t = (playerPosition - node1.WorldPosition).Length / (node2.WorldPosition - node1.WorldPosition).Length;

			CameraPivot.WorldPosition = Vector3.Lerp( node1.WorldPosition, node2.WorldPosition, t );

			Gizmo.Draw.LineSphere( node1.WorldPosition, 16f );
			Gizmo.Draw.Text( "NODE1", new Transform( node1.WorldPosition + Vector3.Up * 32f ) );
			Gizmo.Draw.LineSphere( node2.WorldPosition, 16f );
			Gizmo.Draw.Text( "NODE2", new Transform( node2.WorldPosition + Vector3.Up * 32f ) );

			Gizmo.Draw.LineSphere( CameraPivot.WorldPosition, 8f );
			Gizmo.Draw.Text( "PIVOT", new Transform( CameraPivot.WorldPosition + Vector3.Up * 32f ) );
			Gizmo.Draw.Arrow( CameraPivot.WorldPosition, node1.WorldPosition );
			Gizmo.Draw.Arrow( CameraPivot.WorldPosition, node2.WorldPosition );
		}
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
