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

	private Rotation _wishedPivotRotation = Rotation.Identity;
	public GameObject CameraPivot => HasPivotRotation ? GameObject.Parent : null;

	protected override void OnUpdate()
	{
		if ( !IsCameraActive ) return;
		
		if ( CameraPivot.IsValid() )
		{
			// Gizmo.Draw.Arrow( CameraPivot.WorldPosition, CameraPivot.WorldPosition + ( CameraPivot.WorldRotation.Forward * 32f ) );
			
			// CameraPivot.WorldRotation = Rotation.Slerp( CameraPivot.WorldRotation, _wishedPivotRotation, Time.Delta * 5f );
			CameraPivot.WorldRotation = _wishedPivotRotation;
		}
		
		if ( DollyNode.IsValid() && CameraPivot.IsValid() )
		{
			// move the camera between dolly nodes to follow player
			var playerPosition = PlayerCharacter.Local.WorldPosition;
			var pathNodes = DollyNode.GetPath();

			if ( pathNodes.Count > 1 )
			{
				var closestNode = pathNodes.OrderBy( x => x.WorldPosition.Distance( playerPosition ) ).First();
				var nextNode = pathNodes.FirstOrDefault( x => x != closestNode );
				if ( nextNode.IsValid() )
				{
					var direction = (nextNode.WorldPosition - closestNode.WorldPosition).Normal;
					var distance = closestNode.WorldPosition.Distance( nextNode.WorldPosition );
					var t = (playerPosition - closestNode.WorldPosition).Dot( direction ) / distance;
					var position = Vector3.Lerp( closestNode.WorldPosition, nextNode.WorldPosition, t );
					CameraPivot.WorldPosition = position;
				}
			}
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
				_wishedPivotRotation = PlayerCharacter.Local.CameraController.CameraPivot.WorldRotation;
				CameraPivot.WorldRotation = _wishedPivotRotation;
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

		// CameraPivot.WorldRotation *= rotation;
		_wishedPivotRotation *= rotation;
		
		Log.Info( $"Rotating pivot by {rotation}" );
		
		if ( ShouldSyncWithPlayerCameraRotation )
		{
			PlayerCharacter.Local.CameraController.CameraPivot.WorldRotation = CameraPivot.WorldRotation;
		}
	}
}
