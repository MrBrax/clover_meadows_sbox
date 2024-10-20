namespace Clover.WorldBuilder;

public class InteriorPiece : Component
{
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }

	[Property] public List<string> BelongsToRooms { get; set; } = new();

	public void Hide()
	{
		GetComponent<ModelRenderer>( true ).Enabled = false;
	}

	public void Show()
	{
		GetComponent<ModelRenderer>( true ).Enabled = true;
	}

	public bool BelongsToRoom( string room )
	{
		return BelongsToRooms.Contains( room );
	}

	protected override void OnUpdate()
	{
		var camera = Scene.Camera;

		var cameraPosition = camera.WorldPosition;
		var cameraRotation = camera.WorldRotation;
		var cameraForward = cameraRotation.Forward;

		// check if we're looking from the back
		var toCamera = (cameraPosition - WorldPosition).Normal;
		var dot = toCamera.Dot( cameraForward );

		Gizmo.Draw.Text( dot.ToString(), new Transform( WorldPosition ) );
	}
}
