using System;
using Clover.Objects;

namespace Clover.Carriable;

[Category( "Clover/Carriable" )]
public class PelletGun : BaseCarriable
{
	[Property] public float FireRate { get; set; } = 3f;
	[Property] public float PelletSpeed { get; set; } = 10f;
	[Property] public GameObject PelletScene { get; set; }

	[Property] public GameObject PelletGunFpsScene { get; set; }

	[Property] public SoundEvent FireSound { get; set; }

	private float _fireTimer;

	private bool _isAiming;

	private bool _isWaitingForHit;
	private bool _isLookingAtWhenShot;

	private Angles _aimDirection;

	/// <summary>
	///  A scene that contains a camera and a gun model. When aiming, this scene is instantiated and the player model is hidden.
	///  The entire thing rotates to aim.
	/// </summary>
	private GameObject _pelletGunFpsNode;

	public override void OnUseDown()
	{
		if ( _isWaitingForHit ) return;
		base.OnUseDown();
		StartAiming();
	}

	public override void OnUseUp()
	{
		if ( _isWaitingForHit ) return;
		base.OnUseUp();
		Fire();
		// StopAiming();
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		StopAiming();
	}

	public override string GetUseName()
	{
		return "Fire";
	}

	private void StartAiming()
	{
		_isAiming = true;

		_pelletGunFpsNode = PelletGunFpsScene.Clone();
		_pelletGunFpsNode.WorldPosition = Player.WorldPosition + Vector3.Up * 32f;

		_pelletGunFpsNode.WorldRotation = Rotation.FromYaw( Player.PlayerController.Yaw );

		// TODO: Hide the player model
		Player.SetVisible( false );

		Mouse.Visible = false;

		// Hide the player model
		// PlayerModel.Visible = false;
	}

	private void StopAiming()
	{
		_isAiming = false;
		Player.PlayerController.Yaw = _pelletGunFpsNode.WorldRotation.Yaw();
		_pelletGunFpsNode.Destroy();

		// TODO: Show the player model
		Player.SetVisible( true );

		Mouse.Visible = true;
	}

	public override bool ShouldDisableMovement()
	{
		return base.ShouldDisableMovement() || _isAiming;
	}

	private void Fire()
	{
		if ( !_pelletGunFpsNode.IsValid() ) return;

		var startPos = _pelletGunFpsNode.WorldPosition + _pelletGunFpsNode.WorldRotation.Forward * 16f;

		var pelletObject = PelletScene.Clone( startPos );
		var pellet = pelletObject.GetComponent<Pellet>();
		pellet.StartPosition = startPos;
		pellet.PelletGun = this;

		pellet.Speed = PelletSpeed;

		// var pelletDirection = _pelletGunFpsNode.Transform.Basis.Z;
		pelletObject.WorldPosition = startPos;
		pelletObject.WorldRotation = _pelletGunFpsNode.WorldRotation;

		// pelletGunFpsNode.GetNode<Control>( "Crosshair" ).Visible = false;

		pellet.OnTimeout += () =>
		{
			_isWaitingForHit = false;
			StopAiming();
		};

		pellet.OnHit += OnPelletHit;

		_fireTimer = 0;

		_isWaitingForHit = true;

		// GetNode<AudioStreamPlayer3D>( "Fire" ).Play();
		SoundEx.Play( FireSound, _pelletGunFpsNode.WorldPosition );

		// _pelletGunFpsNode.GetNode<AnimationPlayer>( "AnimationPlayer" ).Play( "fire" );*/
	}

	private void OnPelletHit( GameObject target, PelletGun gun )
	{
		_isWaitingForHit = false;
		_isLookingAtWhenShot = false;
		StopAiming();
	}

	protected override void OnUpdate()
	{
		if ( !_isAiming ) return;

		if ( !_isWaitingForHit && !_isLookingAtWhenShot )
		{
			var input = Input.AnalogLook;
			// _aimDirection += input.WithRoll( 0f );
			var angles = _aimDirection;
			angles += input;
			angles.pitch = Math.Clamp( angles.pitch, -89, 89 );
			angles.roll = 0;
			_aimDirection = angles;
		}

		if ( Input.Pressed( "attack2" ) )
		{
			StopAiming();
		}

		_pelletGunFpsNode.WorldRotation = _aimDirection;
	}
}
