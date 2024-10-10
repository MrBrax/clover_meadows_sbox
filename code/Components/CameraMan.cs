using System;
using Sandbox;

[Category( "Clover/Camera" )]
[Icon( "camera" )]
public sealed class CameraMan : Component
{
	public static CameraMan Instance { get; private set; }

	[Property] public GameObject CameraPrefab { get; set; }

	public List<GameObject> Targets { get; set; } = new();

	private IEnumerable<CameraNode> _cameraNodes => Scene.GetAllComponents<CameraNode>().Where( x => !x.IsProxy );

	private CameraNode MainCameraNode => _cameraNodes.OrderBy( x => x.Priority ).Reverse().FirstOrDefault();

	private Vector3 _positionLerp;
	private Rotation _rotationLerp;
	private float _fovLerp;

	public float LerpSpeed = 5;

	private CameraComponent CameraComponent;

	protected override void OnAwake()
	{
		base.OnAwake();
		// if ( IsProxy ) return;
		Instance = this;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		Instance = null;
	}

	protected override void OnStart()
	{
		base.OnStart();

		if ( !CameraPrefab.IsValid() ) throw new Exception( "CameraPrefab is not valid" );

		var camera = CameraPrefab.Clone();
		camera.NetworkMode = NetworkMode.Never;
		CameraComponent = camera.GetComponent<CameraComponent>();
		
		camera.BreakFromPrefab();

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

		var wishedRot = Rotation.Identity;

		if ( Targets.Count > 0 )
		{
			var midpoint = GetTargetsMidpoint();
			wishedRot = Rotation.LookAt( midpoint - MainCameraNode.WorldPosition, Vector3.Up );
		}
		else
		{
			wishedRot = MainCameraNode.WorldRotation;
		}

		_positionLerp = Vector3.Lerp( _positionLerp, MainCameraNode.WorldPosition, Time.Delta * LerpSpeed );
		_rotationLerp = Rotation.Lerp( _rotationLerp, wishedRot, Time.Delta * LerpSpeed );
		_fovLerp = _fovLerp.LerpTo( MainCameraNode.FieldOfView, Time.Delta * LerpSpeed );

		// var midpoint = GetTargetsMidpoint();
		// _rotationLerp = Rotation.Lerp( _rotationLerp, Rotation.LookAt( midpoint - _positionLerp, Vector3.Up ), Time.Delta * LerpSpeed );

		CameraComponent.WorldPosition = _positionLerp;
		CameraComponent.WorldRotation = _rotationLerp;
		CameraComponent.FieldOfView = _fovLerp;
	}

	public Vector3 GetTargetsMidpoint()
	{
		if ( Targets.Count == 0 ) return Vector3.Zero;

		var midpoint = Vector3.Zero;
		foreach ( var target in Targets )
		{
			midpoint += target.WorldPosition;
		}

		return midpoint / Targets.Count;
	}

	public void SnapTo( Vector3 transformPosition, Rotation transformRotation )
	{
		_positionLerp = transformPosition;
		_rotationLerp = transformRotation;
	}
}
