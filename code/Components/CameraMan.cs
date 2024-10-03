using Sandbox;

public sealed class CameraMan : Component
{
	[RequireComponent] public CameraComponent CameraComponent { get; set; }

	private IEnumerable<CameraNode> _cameraNodes => Scene.GetAllComponents<CameraNode>();

	private CameraNode MainCameraNode => _cameraNodes.OrderBy( x => x.Priority ).Reverse().FirstOrDefault();

	private Vector3 _positionLerp;
	private Rotation _rotationLerp;
	private float _fovLerp;

	public float LerpSpeed = 5;

	protected override void OnStart()
	{
		base.OnStart();

		if ( MainCameraNode.IsValid() )
		{
			_positionLerp = MainCameraNode.Transform.Position;
			_rotationLerp = MainCameraNode.Transform.Rotation;
			_fovLerp = MainCameraNode.FieldOfView;
		}
	}

	protected override void OnUpdate()
	{
		if ( !MainCameraNode.IsValid() ) return;

		_positionLerp = Vector3.Lerp( _positionLerp, MainCameraNode.Transform.Position, Time.Delta * LerpSpeed );
		_rotationLerp = Rotation.Lerp( _rotationLerp, MainCameraNode.Transform.Rotation, Time.Delta * LerpSpeed );
		_fovLerp = _fovLerp.LerpTo( MainCameraNode.FieldOfView, Time.Delta * LerpSpeed );

		Transform.Position = _positionLerp;
		Transform.Rotation = _rotationLerp;
		CameraComponent.FieldOfView = _fovLerp;
	}
}
