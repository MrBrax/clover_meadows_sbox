using System;
using Clover.Carriable;
using Clover.Components;
using Clover.Data;
using Clover.Objects;

namespace Clover.Animals;

[Category( "Clover/Animals" )]
public class CatchableFish : Component, IFootstepEvent
{
	public enum FishState
	{
		Idle = 0,
		Swimming = 1,
		FoundBobber = 2,
		TryingToEat = 3,
		Fighting = 4
	}

	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }

	[Property] public SkinnedModelRenderer Renderer { get; set; }

	[Property] public FishData Data { get; set; }

	[Property] public SoundEvent SplashSound { get; set; }
	[Property] public SoundEvent NibbleSound { get; set; }
	[Property] public SoundEvent ChompSound { get; set; }
	[Property] public SoundEvent CatchSound { get; set; }

	private const float _bobberMaxDistance = 40f;
	private const float _bobberDiscoverAngle = 45f;

	private Vector3 _velocity = Vector3.Zero;
	private const float _maxSwimSpeed = 30f;
	private const float _maxSwimSpeedPanic = 40f;
	private const float _swimAcceleration = 1.5f;
	private const float _swimDeceleration = 0.5f;

	// private const float _catchMsecWindow = 1500f;

	private TimeUntil _nextSplashSound = 0;

	public FishState State { get; set; } = FishState.Idle;

	public FishingBobber Bobber { get; set; }

	public Rotation WishedRotation { get; set; }


	public RangedFloat NibbleTime = new(0.5f, 2f);

	public float CurrentNibbleTime;

	public TimeUntil NextNibble;

	private TimeSince _lastPanic = 999;

	public bool IsNibbling => CurrentNibbleTime > NextNibble.Passed;

	public float Stamina;

	public void SetState( FishState state )
	{
		// Log.Info( $"Fish state changed to {state}." );
		State = state;
		/*_lastAction = Time.Now;
		_actionDuration = 0;
		_panicMaxIdles = 0;
		_swimTarget = Vector3.Zero;
		_swimProgress = 0;
		_nibbles = 0;
		_isNibbleDeep = false;*/
	}

	protected override void OnFixedUpdate()
	{
		// don't simulate fish if there are no players in the world
		if ( WorldLayerObject.World == null || !WorldLayerObject.World.PlayersInWorld.Any() )
		{
			// Log.Info( "No players in the world." );
			return;
		}

		switch ( State )
		{
			case FishState.Idle:
				Idle();
				break;
			case FishState.Swimming:
				Swim();
				break;
			case FishState.FoundBobber:
				FoundBobber();
				break;
			// case FishState.TryingToEat:
			// 	TryToEat( delta );
			// 	break;
			case FishState.Fighting:
				Fight();
				break;
		}

		// Animate();

		WorldRotation = Rotation.Lerp( WorldRotation, WishedRotation, Time.Delta * 2f );
	}

	private void Fight()
	{
		if ( _nextSplashSound )
		{
			// GetNode<AudioStreamPlayer3D>( "Splash" ).Play();
			GameObject.PlaySound( SplashSound );
			_nextSplashSound = (TimeUntil)Math.Clamp( Random.Shared.Float( 0.2f, 0.5f ) + (Stamina / 50f), 0.1, 10f );

			Bobber.Rod.SplashParticle.Clone( WorldPosition, Rotation.FromPitch( -90f ) );
		}

		if ( !Bobber.IsValid() )
		{
			Log.Warning( "Bobber is not valid." );
			SetState( FishState.Idle );
			return;
		}

		var fleePosition = Bobber.WorldPosition + Bobber.WorldRotation.Forward * 32f;
		// fleePosition += Bobber.WorldRotation.Right * Random.Shared.Float( -32f, 32f );
		fleePosition += Bobber.WorldRotation.Right * ((Sandbox.Utility.Noise.Perlin( Time.Now * 20f ) * 64f) - 32f);

		// Gizmo.Draw.Arrow( fleePosition + Vector3.Up * 16f, fleePosition + Vector3.Down * 16f );
		// Gizmo.Draw.LineSphere( fleePosition, 8f );

		if ( !FishingRod.CheckForWater( fleePosition + Vector3.Up * 8f ) )
		{
			return;
		}

		var distance = WorldPosition.Distance( Bobber.Rod.WorldPosition );

		if ( distance > Bobber.Rod.LineLength )
		{
			fleePosition = Bobber.WorldPosition;
			Bobber.Rod.LineStrength -= Time.Delta * 0.3f;
		}

		var swimSpeed = _maxSwimSpeedPanic;

		// TODO: properly adjust this based on distance and stamina
		if ( distance < 96f )
		{
			swimSpeed = _maxSwimSpeedPanic * (distance / 40f);
		}

		if ( Stamina <= 0 )
		{
			swimSpeed *= 0.1f;
		}

		Bobber.WorldPosition += (fleePosition - Bobber.WorldPosition).Normal * swimSpeed * Time.Delta;

		WorldPosition = Bobber.WorldPosition;

		Stamina -= Time.Delta * 0.1f;

		Bobber.Rod.LineStrength -= Time.Delta * 0.1f;

		// Gizmo.Draw.Text( Stamina.ToString(), new Transform( WorldPosition + Vector3.Up * 32f ) );

		/*if ( Stamina <= 0 )
		{
			CatchFish();
		}*/

		// Log.Info( "Fighting the fish." );
	}

	private void Animate()
	{
		/*if ( State == FishState.Swimming )
		{
			AnimationPlayer.Play( "swimming" );
			// AnimationPlayer.SpeedScale =
		}
		else
		{
			AnimationPlayer.Play( "idle" );
		}*/

		// Renderer.Set( "swimming", State == FishState.Swimming );
	}

	public void TryHook()
	{
		// if last nibble is within the catch window, catch the fish
		if ( IsNibbling )
		{
			if ( Random.Shared.Float() <= 0.7f )
			{
				Bobber.OnFight();
				HookFish();
			}
			else
			{
				Scare( Bobber.WorldPosition );
			}
		}
		else
		{
			Log.Info( "Nibble was not deep enough." );
			Scare( Bobber.WorldPosition );
		}
	}

	private void Scare( Vector3 source = default )
	{
		if ( _lastPanic < 5f ) return;
		Log.Info( "Scared the fish." );
		// Gizmo.Draw.Line( WorldPosition, source );
		// Gizmo.Draw.LineSphere( source, 8f );
		// Gizmo.Draw.LineSphere( WorldPosition, 8f );
		_lastPanic = 0;
		// _swimProgress = 0;
		_lastAction = Time.Now;
		_actionDuration = 0;
		FindSwimAwayFromTarget( source );
		SetState( FishState.Swimming );
		GameObject.PlaySound( SplashSound );
	}

	private void FoundBobber()
	{
		if ( !ActionDone ) return;

		if ( !Bobber.IsValid() )
		{
			Log.Warning( "Bobber is not valid." );
			SetState( FishState.Idle );
			Bobber = null;
			return;
		}

		/*if ( _isNibbleDeep )
		{
			FailCatch();
			return;
		}*/

		var bobberPosition = Bobber.WorldPosition.WithZ( 0 );
		var fishPosition = WorldPosition.WithZ( 0 );

		// rotate towards the bobber
		var moveDirection = (bobberPosition - fishPosition).Normal;

		var newRotation = Rotation.LookAt( (bobberPosition - fishPosition), Vector3.Up );

		WishedRotation = newRotation;

		var distance = fishPosition.Distance( bobberPosition );

		if ( distance > 3f )
		{
			var speed = _maxSwimSpeed * distance / 8f;

			// move towards the bobber
			WorldPosition += moveDirection * speed * Time.Delta;
			return;
		}

		if ( !NextNibble ) return;

		CurrentNibbleTime = NibbleTime.GetValue();

		NextNibble = Random.Shared.Float( 3f, 8f );

		Sound.Play( NibbleSound, WorldPosition );
		Bobber.OnNibble();
	}

	private void FailCatch()
	{
		Log.Info( "Failed to catch the fish." );
		Bobber.Rod.FishGotAway();
		DestroyGameObject();
		// QueueFree();
	}

	private void HookFish()
	{
		Log.Info( "Hooked the fish." );
		SetState( FishState.Fighting );

		switch ( Data?.Size )
		{
			case FishData.FishSize.Tiny:
				// _stamina = GD.RandRange( 2, 7 );
				Stamina = Random.Shared.Float( 2, 7 );
				break;
			case FishData.FishSize.Small:
				// _stamina = GD.RandRange( 5, 10 );
				Stamina = Random.Shared.Float( 5, 10 );
				break;
			case FishData.FishSize.Medium:
				// _stamina = GD.RandRange( 8, 15 );
				Stamina = Random.Shared.Float( 8, 15 );
				break;
			case FishData.FishSize.Large:
				// _stamina = GD.RandRange( 10, 20 );
				Stamina = Random.Shared.Float( 10, 20 );
				break;
			default:
				Stamina = 10;
				break;
		}

		_lastAction = Time.Now;
		// _actionDuration = GD.RandRange( 4000, 8000 );
		_actionDuration = Random.Shared.Float( 4, 8 );
	}

	private void CatchFish()
	{
		// SetState( FishState.Caught );
		// GetNode<AudioStreamPlayer3D>( "Catch" ).Play();
		GameObject.PlaySound( CatchSound );
		Bobber.Rod.CatchFish( this );
		// SetState( FishState.Caught );
	}

	private float _swimRandomRadius = 128f;

	private Vector3 _swimTarget;
	private const int _swimTargetTries = 10;
	// private float _swimProgress;

	private void Swim()
	{
		if ( !ActionDone ) return;

		// find a new swim target
		if ( _swimTarget == Vector3.Zero )
		{
			var randomTries = 0;

			do
			{
				GetNewSwimTarget();
				randomTries++;
			} while ( _swimTarget == Vector3.Zero && randomTries < _swimTargetTries );

			if ( _swimTarget == Vector3.Zero )
			{
				Log.Warning( "Failed to find a swim target." );
				SetState( FishState.Idle );
				return;
			}

			_lastAction = Time.Now;
			// _swimStartPos = WorldPosition;
			// _swimProgress = 0;

			Log.Trace( $"New swim target: {_swimTarget}." );
		}

		var swimSpeed = _maxSwimSpeed;
		if ( _lastPanic < 5f )
		{
			// swimSpeed = 15f;
		}

		// move towards the target smoothly
		// var moveDirection = (_swimTarget - WorldPosition).Normal;
		// var swimDistance = _swimTarget.Distance( _swimStartPos );

		// _swimProgress += Time.Delta * (swimSpeed / swimDistance);

		// Vector3 preA = _swimStartPos;
		// Vector3 postB = _swimTarget;

		// Gizmo.Draw.LineSphere( preA, 8f );
		// Gizmo.Draw.LineSphere( postB, 8f );
		// Gizmo.Draw.Arrow( preA + Vector3.Up * 8f, postB + Vector3.Up * 8f );

		var distance = WorldPosition.Distance( _swimTarget );

		var acceleration = _swimAcceleration;

		if ( distance < 16f )
		{
			acceleration = _swimDeceleration;
		}

		var swimDirection = (_swimTarget - WorldPosition).Normal;

		_velocity += swimDirection * acceleration;

		if ( distance < 8f )
		{
			_velocity = _velocity.ClampLength( 8f );
		}

		_velocity = _velocity.ClampLength( swimSpeed );

		var newRotation = Rotation.LookAt( _velocity.Normal, Vector3.Up );

		WorldPosition += _velocity * Time.Delta;
		WishedRotation = newRotation;

		// check if the fish has reached the target


		/**/

		if ( distance < 4f )
		{
			Log.Trace( "Reached swim target." );
			_swimTarget = Vector3.Zero;
			SetState( FishState.Idle );
			return;
		}

		CheckForBobber();
	}

	private void FindSwimAwayFromTarget( Vector3 source )
	{
		var currentPos = WorldPosition;
		var direction = (currentPos - source).Normal.WithZ( 0 );

		var basePoint = currentPos + direction * 128f;

		for ( var i = 0; i < 10; i++ )
		{
			var randomPoint = basePoint + new Vector3(
				Random.Shared.Float( -_swimRandomRadius, _swimRandomRadius ),
				Random.Shared.Float( -_swimRandomRadius, _swimRandomRadius ),
				0 );

			var traceWater = Scene.Trace.Ray( randomPoint + Vector3.Up * 16f, randomPoint + Vector3.Down * 32f )
				.WithTag( "water" )
				.Run();

			// Gizmo.Draw.Line( randomPoint + Vector3.Up * 16f, randomPoint + Vector3.Down * 32f );

			if ( !traceWater.Hit )
			{
				// Log.Warning( $"No water found at {randomPoint}." );
				// this will just try again
				continue;
			}

			var traceTerrain = Scene.Trace.Ray( randomPoint + Vector3.Up * 16f, randomPoint + Vector3.Down * 32f )
				.WithTag( "terrain" )
				.Run();

			if ( traceTerrain.Hit )
			{
				// Log.Warning( $"Terrain found at {randomPoint}." );
				// this will just try again
				continue;
			}

			var trace = Scene.Trace.Ray( currentPos, randomPoint )
				.WithTag( "terrain" )
				.Run();

			if ( trace.Hit )
			{
				// Log.Warning( $"Terrain found between {currentPos} and {randomPoint}." );
				// this will just try again
				continue;
			}

			Log.Info( $"Found a flee swim target: {randomPoint}." );
			// Gizmo.Draw.LineSphere( randomPoint, 32f );
			_swimTarget = randomPoint;
			return;
		}

		Log.Warning( "Failed to find a flee swim target." );
	}

	private void GetNewSwimTarget()
	{
		var randomPoint = WorldPosition + new Vector3(
			Random.Shared.Float( -_swimRandomRadius, _swimRandomRadius ),
			Random.Shared.Float( -_swimRandomRadius, _swimRandomRadius ),
			0 );

		var traceWater = Scene.Trace.Ray( randomPoint + Vector3.Up * 16f, randomPoint + Vector3.Down * 32f )
			.WithTag( "water" )
			.Run();

		// Gizmo.Draw.Line( randomPoint + Vector3.Up * 16f, randomPoint + Vector3.Down * 32f );

		if ( !traceWater.Hit )
		{
			// Log.Warning( $"No water found at {randomPoint}." );
			// this will just try again
			return;
		}

		var traceTerrain = Scene.Trace.Ray( randomPoint + Vector3.Up * 16f, randomPoint + Vector3.Down * 32f )
			.WithTag( "terrain" )
			.Run();

		if ( traceTerrain.Hit )
		{
			// Log.Warning( $"Terrain found at {randomPoint}." );
			// this will just try again
			return;
		}

		var trace = Scene.Trace.Ray( WorldPosition, randomPoint )
			.WithTag( "terrain" )
			.Run();

		if ( trace.Hit )
		{
			// Log.Warning( $"Terrain found between {WorldPosition} and {randomPoint}." );
			// this will just try again
			return;
		}

		_swimTarget = randomPoint;
	}

	private float _lastAction;
	private float _actionDuration;
	private float _panicMaxIdles;

	private bool ActionDone => Time.Now - _lastAction > _actionDuration;

	public float Weight { get; internal set; }


	private void Idle()
	{
		CheckForBobber();

		if ( !ActionDone ) return;

		// randomly start swimming, 20% chance
		if ( Random.Shared.Float() <= 0.2f || _panicMaxIdles > 5 )
		{
			Log.Trace( "Starting to swim after being idle." );
			SetState( FishState.Swimming );
			_panicMaxIdles = 0;
			return;
		}

		// else, stay idle
		// _actionDuration = (float)GD.RandRange( 1000, 5000 );
		_actionDuration = Random.Shared.Float( 1, 5 );
		_lastAction = Time.Now;
		_panicMaxIdles++;

		Log.Trace( $"Idle for {_actionDuration} msec." );
	}

	private void CheckForBobber()
	{
		if ( Bobber.IsValid() )
		{
			return;
		}

		// var bobber = GetTree().GetNodesInGroup<FishingBobber>( "fishing_bobber" ).FirstOrDefault();
		var bobber = Scene.GetAllComponents<FishingBobber>().FirstOrDefault( x => !x.IsProxy && !x.Fish.IsValid() );

		if ( !bobber.IsValid() )
		{
			return;
		}

		if ( !bobber.IsInWater ) return;

		var bobberPosition = bobber.WorldPosition.WithZ( 0 );
		var fishPosition = WorldPosition.WithZ( 0 );

		// check if the bobber is near the fish
		var distance = fishPosition.Distance( bobberPosition );
		if ( distance > _bobberMaxDistance )
		{
			// Log.Info($"Bobber is too far away ({distance})." );
			return;
		}

		// check if the bobber is within the fish's view
		var direction = (bobberPosition - WorldPosition).Normal;
		var angle = MathX.RadianToDegree( MathF.Acos( direction.Dot( WorldRotation.Forward ) ) );

		/*if ( angle > _bobberDiscoverAngle )
		{
			Log.Info($"Bobber is not in view ({angle})." );
			return;
		} */

		Bobber = bobber;
		Bobber.Fish = this;

		Log.Info( "Found the bobber." );

		SetState( FishState.FoundBobber );
	}

	public void OnLinePull()
	{
		/*if ( State != FishState.Fighting ) return;
		_stamina -= 1;
		Log.Info( $"Pulled the fish, stamina left: {_stamina}." );
		if ( _stamina <= 0 )
		{
			CatchFish();
		}*/

		if ( State == FishState.FoundBobber )
		{
			if ( IsNibbling )
			{
				TryHook();
			}
			else
			{
				Scare( Bobber.WorldPosition );
			}

			return;
		}
	}

	internal void SetSize( FishData.FishSize size )
	{
		var scale = 1f;
		switch ( size )
		{
			case FishData.FishSize.Tiny:
				scale = 0.5f;
				break;
			case FishData.FishSize.Small:
				scale = 0.75f;
				break;
			case FishData.FishSize.Medium:
				scale = 1f;
				break;
			case FishData.FishSize.Large:
				scale = 1.25f;
				break;
		}

		WorldScale = new Vector3( scale, scale, scale );
	}

	/*protected override void OnUpdate()
	{
		base.OnUpdate();

		Gizmo.Draw.LineSphere( WorldPosition, 8f );
		Gizmo.Draw.Arrow( WorldPosition, WorldPosition + WorldRotation.Forward * 32f );

		Gizmo.Draw.Arrow( _swimTarget + Vector3.Up * 64f, _swimTarget );
	}*/

	public void OnFootstepEvent( SceneModel.FootstepEvent e )
	{
		if ( e.Transform.Position.Distance( WorldPosition ) < 128f )
		{
			Log.Info( $"Fish heard a footstep: {e.Volume}." );
			// Scare( e.Transform.Position );
		}
	}
}
