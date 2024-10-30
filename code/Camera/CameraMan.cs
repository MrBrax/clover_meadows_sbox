using System;
using Clover;
using Clover.Player;
using Clover.Utilities;
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
	public const float DecaySpeed = 2.5f;

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
		var _decaySpeed = DecaySpeed;

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
				wishedPos += PlayerCharacter.Local.CharacterController.Velocity * 0.4f;

				if ( PlayerCharacter.Local.PlayerController.WishVelocity.Length == 0 )
				{
					_lerpSpeed /= 2;
				}
			}
		}

		// idle mode
		if ( MainUi.Instance.IsValid() && MainUi.Instance.LastInput > MainUi.HideUiDelay * 3 )
		{
			var p = Sandbox.Utility.Noise.Perlin( Time.Now * 5f, Time.Now * 7f, Time.Now * 9f ) - 0.5f;
			wishedRot *= Rotation.From( p * 2, p * 3, p );
			_lerpSpeed = 0.1f;
			_decaySpeed = 0.2f;
		}


		_positionLerp = _positionLerp.ExpDecayTo( wishedPos, _decaySpeed );
		_rotationLerp = Rotation.Slerp( _rotationLerp, wishedRot, Time.Delta * _lerpSpeed );
		_fovLerp = _fovLerp.LerpTo( mainCameraNode.FieldOfView, Time.Delta * _lerpSpeed );

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
