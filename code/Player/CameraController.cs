using Sandbox;

[Category( "Clover/Player" )]
[Icon( "camera" )]
public sealed class CameraController : Component
{
	[Property] public CameraNode MainCameraNode { get; set; }
	[Property] public CameraNode SkyCameraNode { get; set; }
	
	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		if ( !SkyCameraNode.IsValid() )
		{
			Log.Error( "SkyCameraNode is not valid" );
			return;
		}
		
		SkyCameraNode.Priority = Input.Down("View") ? 10 : 0;
	}

	public void SnapCamera()
	{
		MainCameraNode.SnapTo();
	}
	
}
