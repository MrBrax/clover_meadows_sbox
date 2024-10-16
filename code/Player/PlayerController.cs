using System;
using Sandbox;
using Sandbox.Citizen;
using System.Drawing;
using System.Runtime;
using Clover.Carriable;
using Clover.Components;
using Clover.Player;

public class PlayerController : Component, IEquipChanged
{
	[RequireComponent] public PlayerCharacter Player { get; set; }
	[RequireComponent] public CharacterController CharacterController { get; set; }

	[Property] public SkinnedModelRenderer Model { get; set; }

	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }

	[Property] public Vector3 Gravity { get; set; } = new Vector3( 0, 0, 800 );

	[Property] public float WalkSpeed { get; set; } = 110.0f;
	[Property] public float RunSpeed { get; set; } = 180.0f;
	[Property] public float SneakSpeed { get; set; } = 60.0f;

	public Vector3 WishVelocity { get; private set; }

	[Sync] public float Yaw { get; set; }


	[Sync] public bool IsRunning { get; set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();

		if ( IsProxy )
			return;

		/*var cam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		if ( cam.IsValid() )
		{
			var ee = cam.WorldRotation.Angles();
			ee.roll = 0;
			EyeAngles = ee;
		}*/
	}

	protected override void OnUpdate()
	{
		if ( Player.Model.IsValid() )
		{
			Player.Model.WorldRotation = Rotation.Lerp( Player.Model.WorldRotation, Rotation.From( 0, Yaw, 0 ),
				Time.Delta * 10.0f );
		}

		// Eye input
		if ( !IsProxy )
		{
			/*var ee = EyeAngles;
			ee += Input.AnalogLook * 0.5f;
			ee.roll = 0;
			EyeAngles = ee;

			var cam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();

			var lookDir = EyeAngles.ToRotation();

			if ( FirstPerson )
			{
				cam.Transform.Position = Eye.Transform.Position;
				cam.WorldRotation = lookDir;
			}
			else
			{
				cam.Transform.Position = Transform.Position + lookDir.Backward * 300 + Vector3.Up * 75.0f;
				cam.WorldRotation = lookDir;
			}*/

			IsRunning = Input.Down( "Run" );
		}

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

			float rotateDifference = Model.WorldRotation.Distance( targetAngle );

			if ( rotateDifference > 50.0f || cc.Velocity.Length > 10.0f )
			{
				var newRotation = Rotation.Lerp( Model.WorldRotation, targetAngle, Time.Delta * 2.0f );

				// We won't end up actually moving to the targetAngle, so calculate how much we're actually moving
				var angleDiff = Model.WorldRotation.Angles() - newRotation.Angles(); // Rotation.Distance is unsigned
				moveRotationSpeed = angleDiff.yaw / Time.Delta;

				Model.WorldRotation = newRotation;
			}
		}*/

		var speed = CharacterController.Velocity.Length * 2.54f;
		Model.Set( "move_speed", speed );
		Model.Set( "running", IsRunning );
		// Model.Set( "holding", true );
		// Gizmo.Draw.Text( $"Speed: {speed}", new Transform( Player.WorldPosition + Vector3.Up * 32 ) );

		if ( AnimationHelper.IsValid() )
		{
			AnimationHelper.WithVelocity( CharacterController.Velocity );
			AnimationHelper.WithWishVelocity( WishVelocity );
			AnimationHelper.IsGrounded = CharacterController.IsOnGround;
			AnimationHelper.MoveRotationSpeed = 1f;
			// AnimationHelper.WithLook( EyeAngles.Forward, 1, 1, 1.0f );
			AnimationHelper.MoveStyle =
				IsRunning ? CitizenAnimationHelper.MoveStyles.Run : CitizenAnimationHelper.MoveStyles.Walk;
		}
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
		{
			Model.Set( "holding",
				Player.Equips.EquippedSlots.TryGetValue( Equips.EquipSlot.Tool, out var state ) && state );
			return;
		}

		if ( Player.InCutscene )
		{
			if ( Player.CutsceneTarget.HasValue )
			{
				var target = Player.CutsceneTarget.Value;
				var direction = (target - Player.WorldPosition).Normal;

				CharacterController.Velocity = direction * 100.0f;

				Player.ModelLookAt( target );

				// Gizmo.Draw.LineSphere( target, 10.0f );

				// Gizmo.Draw.Arrow( Player.WorldPosition, target );

				if ( (target - Player.WorldPosition).Length < 10.0f )
				{
					// Player.CutsceneTarget = null;
					CharacterController.Velocity = Vector3.Zero;
					// Log.Info( "Cutscene target reached" );
				}

				CharacterController.Move();
			}

			return;
		}

		if ( !Player.ShouldMove() )
		{
			return;
		}

		BuildWishVelocity();

		var cc = GameObject.Components.Get<CharacterController>();

		if ( !cc.IsValid() )
		{
			// Log.Error( "CharacterController is not valid" );
			return;
		}

		if ( cc.IsOnGround && Input.Down( "Jump" ) )
		{
			float flGroundFactor = 1.0f;
			float flMul = 268.3281572999747f * 1.2f;
			//if ( Duck.IsActive )
			//	flMul *= 0.8f;

			cc.Punch( Vector3.Up * flMul * flGroundFactor );
			//	cc.IsOnGround = false;

			// OnJump( fJumps, "Hello", new object[] { Time.Now.ToString(), 43.0f }, Vector3.Random );

			fJumps += 1.0f;
		}

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
		var input = Input.AnalogMove;

		if ( input.Length > 0 /*&& !Player.ShouldDisableMovement()*/ )
		{
			// Player.Model.WorldRotation = Rotation.Lerp( Player.Model.WorldRotation, Rotation.LookAt( input, Vector3.Up ),
			// 	Time.Delta * 10.0f );

			if ( !Player.Model.IsValid() )
			{
				Log.Error( "Player.Model is not valid" );
				return;
			}

			/*var inputYaw = MathF.Atan2( input.y, input.x ).RadianToDegree();
			// Player.Model.WorldRotation = Rotation.Lerp( Player.Model.WorldRotation, Rotation.From( 0, yaw + 180, 0 ), Time.Delta * 10.0f );
			// Yaw = yaw + 180;
			Yaw = Yaw.LerpDegreesTo( inputYaw + 180, Time.Delta * 10.0f );

			// WishedRotation = Rotation.LookAt( input, Vector3.Up );
			var forward = Player.Model.WorldRotation.Forward;
			WishVelocity = forward.Normal;*/

			WishVelocity = (input * -1).ClampLength( 1f );

			Yaw = Yaw.LerpDegreesTo( MathF.Atan2( input.y * -1, input.x * -1 ).RadianToDegree(), Time.Delta * 7.0f );
		}
		else
		{
			WishVelocity = Vector3.Zero;
		}

		if ( Input.Down( "Run" ) )
		{
			WishVelocity *= RunSpeed;
		}
		else if ( Input.Down( "Walk" ) )
		{
			WishVelocity *= SneakSpeed;
		}
		else
		{
			WishVelocity *= WalkSpeed;
		}

		if ( Player.Equips.TryGetEquippedItem<BaseCarriable>( Equips.EquipSlot.Tool, out var tool ) )
		{
			WishVelocity *= tool.CustomPlayerSpeed();
		}
	}

	public void OnEquippedItemChanged( GameObject owner, Equips.EquipSlot slot, GameObject item )
	{
		Log.Info( $"OnEquippedItemChanged: {owner} {slot} {item}" );
		if ( owner != GameObject ) return;
		Model.Set( "holding", item.IsValid() );
	}

	public void OnEquippedItemRemoved( GameObject owner, Equips.EquipSlot slot )
	{
		Log.Info( $"OnEquippedItemRemoved: {owner} {slot}" );
		if ( owner != GameObject ) return;
		Model.Set( "holding", false );
	}
}
