using System;
using Clover;
using Clover.Player;
using Sandbox;

[Category( "Clover/Camera" )]
[Icon( "camera" )]
public sealed class CameraMan : Component
{
	public static CameraMan Instance { get; private set; }

	[Property] public GameObject CameraPrefab { get; set; }

	public HashSet<GameObject> Targets { get; set; } = new();

	private IEnumerable<CameraNode> _cameraNodes => Scene.GetAllComponents<CameraNode>().Where( x => !x.IsProxy );

	public CameraNode MainCameraNode => _cameraNodes.OrderBy( x => x.Priority ).Reverse().FirstOrDefault();

	private Vector3 _positionLerp;
	private Rotation _rotationLerp;
	private float _fovLerp;

	public const float LerpSpeed = 5;

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
		CameraComponent.AddComponent<Highlight>();

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
		var mainCameraNode = MainCameraNode;

		if ( !mainCameraNode.IsValid() ) return;
		if ( !CameraComponent.IsValid() ) return;

		Rotation wishedRot;
		Vector3 wishedPos;
		var _lerpSpeed = LerpSpeed;

		wishedPos = mainCameraNode.WorldPosition;

		if ( Targets.Count > 1 && mainCameraNode.FollowTargets )
		{
			var midpoint = GetTargetsMidpoint();
			wishedRot = Rotation.LookAt( midpoint - mainCameraNode.WorldPosition, Vector3.Up );
		}
		else
		{
			wishedRot = mainCameraNode.WorldRotation;
			if ( !mainCameraNode.Static && PlayerCharacter.Local.IsValid() )
			{
				wishedPos += PlayerCharacter.Local.CharacterController.Velocity * 0.3f;
			}
		}

		if ( MainUi.Instance.IsValid() && MainUi.Instance.LastInput > MainUi.HideUiDelay * 3 )
		{
			var p = Sandbox.Utility.Noise.Perlin( Time.Now * 5f, Time.Now * 7f, Time.Now * 9f ) - 0.5f;
			wishedRot *= Rotation.From( p * 2, p * 3, p );
			_lerpSpeed = 0.1f;
		}

		_positionLerp = Vector3.Lerp( _positionLerp, wishedPos, Time.Delta * _lerpSpeed );
		_rotationLerp = Rotation.Lerp( _rotationLerp, wishedRot, Time.Delta * _lerpSpeed );
		_fovLerp = _fovLerp.LerpTo( mainCameraNode.FieldOfView, Time.Delta * _lerpSpeed );

		// var midpoint = GetTargetsMidpoint();
		// _rotationLerp = Rotation.Lerp( _rotationLerp, Rotation.LookAt( midpoint - _positionLerp, Vector3.Up ), Time.Delta * LerpSpeed );

		if ( mainCameraNode.Lerping )
		{
			CameraComponent.WorldPosition = _positionLerp;
			CameraComponent.WorldRotation = _rotationLerp;
		}
		else
		{
			CameraComponent.WorldPosition = wishedPos;
			CameraComponent.WorldRotation = wishedRot;
		}

		CameraComponent.FieldOfView = _fovLerp;
	}

	public Vector3 GetTargetsMidpoint()
	{
		if ( Targets.Count == 0 ) return Vector3.Zero;

		var midpoint = Vector3.Zero;
		foreach ( var target in Targets.ToList() )
		{
			if ( !target.IsValid() )
			{
				Targets.Remove( target );
				continue;
			}

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
