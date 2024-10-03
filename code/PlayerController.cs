using Sandbox;
using Sandbox.Citizen;
using System.Drawing;
using System.Runtime;

public class PlayerController : Component
{
	[Property] public Vector3 Gravity { get; set; } = new Vector3( 0, 0, 800 );

	public Vector3 WishVelocity { get; private set; }

	[Property] public GameObject Model { get; set; }
	

	[Sync]
	public bool IsRunning { get; set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();

		if ( IsProxy )
			return;

		/*var cam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		if ( cam.IsValid() )
		{
			var ee = cam.Transform.Rotation.Angles();
			ee.roll = 0;
			EyeAngles = ee;
		}*/
	}

	protected override void OnUpdate()
	{
		// Eye input
		/*if ( !IsProxy )
		{
			var ee = EyeAngles;
			ee += Input.AnalogLook * 0.5f;
			ee.roll = 0;
			EyeAngles = ee;

			var cam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();

			var lookDir = EyeAngles.ToRotation();

			if ( FirstPerson )
			{
				cam.Transform.Position = Eye.Transform.Position;
				cam.Transform.Rotation = lookDir;
			}
			else
			{
				cam.Transform.Position = Transform.Position + lookDir.Backward * 300 + Vector3.Up * 75.0f;
				cam.Transform.Rotation = lookDir;
			}



			IsRunning = Input.Down( "Run" );
		}*/

		/*var cc = GameObject.Components.Get<CharacterController>();
		if ( !cc.IsValid() ) return;

		float moveRotationSpeed = 0;

		// rotate body to look angles
		if ( Model.IsValid() )
		{
			var targetAngle = new Angles( 0, 0, 0 ).ToRotation();

			var v = cc.Velocity.WithZ( 0 );

			if ( v.Length > 10.0f )
			{
				targetAngle = Rotation.LookAt( v, Vector3.Up );
			}

			float rotateDifference = Model.Transform.Rotation.Distance( targetAngle );

			if ( rotateDifference > 50.0f || cc.Velocity.Length > 10.0f )
			{
				var newRotation = Rotation.Lerp( Model.Transform.Rotation, targetAngle, Time.Delta * 2.0f );

				// We won't end up actually moving to the targetAngle, so calculate how much we're actually moving
				var angleDiff = Model.Transform.Rotation.Angles() - newRotation.Angles(); // Rotation.Distance is unsigned
				moveRotationSpeed = angleDiff.yaw / Time.Delta;

				Model.Transform.Rotation = newRotation;
			}
		}*/


		/*if ( AnimationHelper.IsValid() )
		{
			AnimationHelper.WithVelocity( cc.Velocity );
			AnimationHelper.WithWishVelocity( WishVelocity );
			AnimationHelper.IsGrounded = cc.IsOnGround;
			AnimationHelper.MoveRotationSpeed = moveRotationSpeed;
			AnimationHelper.WithLook( EyeAngles.Forward, 1, 1, 1.0f );
			AnimationHelper.MoveStyle = IsRunning ? CitizenAnimationHelper.MoveStyles.Run : CitizenAnimationHelper.MoveStyles.Walk;
		}*/
	}

	/*[Broadcast]
	public void OnJump( float floatValue, string dataString, object[] objects, Vector3 position )
	{
		AnimationHelper?.TriggerJump();
	}*/

	float fJumps;

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		BuildWishVelocity();

		var cc = GameObject.Components.Get<CharacterController>();

		/*if ( cc.IsOnGround && Input.Down( "Jump" ) )
		{
			float flGroundFactor = 1.0f;
			float flMul = 268.3281572999747f * 1.2f;
			//if ( Duck.IsActive )
			//	flMul *= 0.8f;

			cc.Punch( Vector3.Up * flMul * flGroundFactor );
			//	cc.IsOnGround = false;

			// OnJump( fJumps, "Hello", new object[] { Time.Now.ToString(), 43.0f }, Vector3.Random );

			fJumps += 1.0f;

		}*/

		if ( cc.IsOnGround )
		{
			cc.Velocity = cc.Velocity.WithZ( 0 );
			cc.Accelerate( WishVelocity );
			cc.ApplyFriction( 4.0f );
		}
		else
		{
			cc.Velocity -= Gravity * Time.Delta * 0.5f;
			cc.Accelerate( WishVelocity.ClampLength( 50 ) );
			cc.ApplyFriction( 0.1f );
		}

		cc.Move();

		if ( !cc.IsOnGround )
		{
			cc.Velocity -= Gravity * Time.Delta * 0.5f;
		}
		else
		{
			cc.Velocity = cc.Velocity.WithZ( 0 );
		}
	}

	public void BuildWishVelocity()
	{
		var rot = Model.LocalRotation;

		WishVelocity = rot * Input.AnalogMove;
		WishVelocity = WishVelocity.WithZ( 0 );

		if ( !WishVelocity.IsNearZeroLength )
		{
			WishVelocity = WishVelocity.Normal;
		}

		if ( Input.Down( "Run" ) )
		{
			WishVelocity *= 320.0f;
		}
		else
		{
			WishVelocity *= 110.0f;
		}
	}
}
