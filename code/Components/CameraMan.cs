using System;
using Sandbox;

public sealed class CameraMan : Component
{
	// [RequireComponent] public CameraComponent CameraComponent { get; set; }
	[Property] public GameObject CameraPrefab { get; set; }

	private IEnumerable<CameraNode> _cameraNodes => Scene.GetAllComponents<CameraNode>().Where( x => !x.IsProxy );

	private CameraNode MainCameraNode => _cameraNodes.OrderBy( x => x.Priority ).Reverse().FirstOrDefault();

	private Vector3 _positionLerp;
	private Rotation _rotationLerp;
	private float _fovLerp;

	public float LerpSpeed = 5;

	private CameraComponent CameraComponent;

	protected override void OnStart()
	{
		base.OnStart();
		
		if ( !CameraPrefab.IsValid() ) throw new Exception( "CameraPrefab is not valid" );

		var camera = CameraPrefab.Clone();
		camera.NetworkMode = NetworkMode.Never;
		CameraComponent = camera.GetComponent<CameraComponent>();

		if ( MainCameraNode.IsValid() )
		{
			_positionLerp = MainCameraNode.WorldPosition;
			_rotationLerp = MainCameraNode.WorldRotation;
			_fovLerp = MainCameraNode.FieldOfView;
		}
	}

	protected override void OnUpdate()
	{
		if ( !MainCameraNode.IsValid() ) return;
		if ( !CameraComponent.IsValid() ) return;

		_positionLerp = Vector3.Lerp( _positionLerp, MainCameraNode.WorldPosition, Time.Delta * LerpSpeed );
		_rotationLerp = Rotation.Lerp( _rotationLerp, MainCameraNode.WorldRotation, Time.Delta * LerpSpeed );
		_fovLerp = _fovLerp.LerpTo( MainCameraNode.FieldOfView, Time.Delta * LerpSpeed );

		CameraComponent.WorldPosition = _positionLerp;
		CameraComponent.WorldRotation = _rotationLerp;
		CameraComponent.FieldOfView = _fovLerp;
	}

	public void SnapTo( Vector3 transformPosition, Rotation transformRotation )
	{
		_positionLerp = transformPosition;
		_rotationLerp = transformRotation;
	}
}
