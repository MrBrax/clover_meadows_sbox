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

	private CameraNode _currentCameraNode;
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

		if ( !_currentCameraNode.IsValid() )
		{
			_currentCameraNode = mainCameraNode;
			_currentCameraNode.OnActivated();
		}

		if ( _currentCameraNode.IsValid() && _currentCameraNode != mainCameraNode )
		{
			/*_positionLerp = mainCameraNode.WorldPosition;
			_rotationLerp = mainCameraNode.WorldRotation;
			_fovLerp = mainCameraNode.FieldOfView;
			_previousCameraNode = mainCameraNode;*/
			_currentCameraNode.OnDeactivated();
			mainCameraNode.OnActivated();
			_currentCameraNode = mainCameraNode;
		}

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

			// offset the camera when the player is moving to keep the player in the center of the screen
			if ( !mainCameraNode.DollyNode.IsValid() && !mainCameraNode.Static && PlayerCharacter.Local.IsValid() )
			{
				wishedPos += PlayerCharacter.Local.CharacterController.Velocity * 0.3f;
			}
		}

		// idle mode
		if ( MainUi.Instance.IsValid() && MainUi.Instance.LastInput > MainUi.HideUiDelay * 3 )
		{
			var p = Sandbox.Utility.Noise.Perlin( Time.Now * 5f, Time.Now * 7f, Time.Now * 9f ) - 0.5f;
			wishedRot *= Rotation.From( p * 2, p * 3, p );
			_lerpSpeed = 0.1f;
		}

		/*if ( mainCameraNode.Static && mainCameraNode.HasPivotRotation )
		{
			/*var basePosition = mainCameraNode.CameraPivot.WorldPosition;
			var cameraPosition = wishedPos;
			var distance = mainCameraNode.CameraPivot.WorldPosition.Distance( wishedPos );
			
			var direction = (cameraPosition - basePosition).Normal;
			var position = basePosition + direction * distance;
			
			_positionLerp = Vector3.Lerp( _positionLerp, position, Time.Delta * _lerpSpeed );
			// _rotationLerp = Rotation.Slerp( _rotationLerp, wishedRot, Time.Delta * _lerpSpeed );#1#
			
			var dir1 = (_positionLerp - mainCameraNode.CameraPivot.WorldPosition).Normal;
			var dir2 = (wishedPos - mainCameraNode.CameraPivot.WorldPosition).Normal;
			
			var rot1 = Rotation.LookAt( dir1 );
			var rot2 = Rotation.LookAt( dir2 );
			
			var newRot = Rotation.Slerp( rot1, rot2, Time.Delta * _lerpSpeed );
			var newPos = newRot.Forward * mainCameraNode.CameraPivot.WorldPosition.Distance( wishedPos );

			var cameraRotation = Rotation.LookAt( -newRot.Forward );
			
			_positionLerp = newPos + mainCameraNode.CameraPivot.WorldPosition;
			_rotationLerp = cameraRotation;
			
			
		}
		else
		{
			_positionLerp = Vector3.Lerp( _positionLerp, wishedPos, Time.Delta * _lerpSpeed );
			_rotationLerp = Rotation.Slerp( _rotationLerp, wishedRot, Time.Delta * _lerpSpeed );
		}*/
		
		_positionLerp = Vector3.Lerp( _positionLerp, wishedPos, Time.Delta * _lerpSpeed );
		_rotationLerp = Rotation.Slerp( _rotationLerp, wishedRot, Time.Delta * _lerpSpeed );
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
