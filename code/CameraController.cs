using Sandbox;

public sealed class CameraController : Component
{
	[Property] public CameraNode MainCameraNode { get; set; }
	[Property] public CameraNode SkyCameraNode { get; set; }
	
	protected override void OnUpdate()
	{
		
		SkyCameraNode.Priority = Input.Down("Jump") ? 10 : 0;
	}

	public void SnapCamera()
	{
		MainCameraNode.SnapTo();
	}
	
}
