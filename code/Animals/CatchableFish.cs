using System;
using Clover.Data;
using Clover.Objects;

namespace Clover.Animals;

public class CatchableFish : Component
{
	public enum FishState
	{
		Idle = 0,
		Swimming = 1,
		FoundBobber = 2,
		TryingToEat = 3,
		Fighting = 4
	}

	[Property] public FishData Data { get; set; }

	[Property] public SoundEvent SplashSound { get; set; }
	[Property] public SoundEvent NibbleSound { get; set; }
	[Property] public SoundEvent ChompSound { get; set; }
	[Property] public SoundEvent CatchSound { get; set; }

	private const float _bobberMaxDistance = 2f;
	private const float _bobberDiscoverAngle = 45f;

	private Vector3 _velocity = Vector3.Zero;
	private const float _maxSwimSpeed = 0.8f;
	private const float _swimAcceleration = 0.1f;
	private const float _swimDeceleration = 0.5f;

	private const float _catchMsecWindow = 1500f;
	private const float _maxNibbles = 6;

	private float _lastNibble;
	private bool _isNibbleDeep;
	private float _stamina;
	private TimeUntil _nextSplashSound = 0;
	private int _nibbles = 0;

	public FishState State { get; set; } = FishState.Idle;

	public FishingBobber Bobber { get; set; }

	public Rotation WishedRotation { get; set; }

	public void SetState( FishState state )
	{
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
		base.OnFixedUpdate();

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

		Animate();

		WorldRotation = Rotation.Lerp( WorldRotation, WishedRotation, Time.Delta * 2f );
	}

	private void Fight()
	{
		if ( _nextSplashSound )
		{
			// GetNode<AudioStreamPlayer3D>( "Splash" ).Play();
			GameObject.PlaySound( SplashSound );
			_nextSplashSound = Random.Shared.Float( 0.8f, 1.2f );
		}

		if ( !ActionDone ) return;

		Log.Info( "Time ran out, fish got away." );
		FailCatch();
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
	}

	public void TryHook()
	{
		// if last nibble is within the catch window, catch the fish
		if ( _isNibbleDeep )
		{
			if ( Time.Now - _lastNibble < _catchMsecWindow )
			{
				HookFish();
				return;
			}
			else
			{
				Log.Info( "Catch window missed." );
				FailCatch();
				return;
			}

			// TODO: remove fish?
			_isNibbleDeep = false;
			_lastNibble = 0;
		}
		else
		{
			Log.Info( "Nibble was not deep enough." );
			FailCatch();
		}
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

		if ( _isNibbleDeep )
		{
			FailCatch();
			return;
		}

		var bobberPosition = Bobber.WorldPosition.WithZ( 0 );
		var fishPosition = WorldPosition.WithZ( 0 );

		// rotate towards the bobber
		var moveDirection = (bobberPosition - fishPosition).Normal;
		var targetRotation = MathF.Atan2( moveDirection.x, moveDirection.y );
		// var currentRotation = Rotation.Y;
		// var newRotation = Mathf.LerpAngle( currentRotation, targetRotation, (float)delta * 2 );

		// WishedRotation = new Vector3( 0, float.IsNaN( targetRotation ) ? 0 : targetRotation, 0 );
		WishedRotation = Rotation.FromYaw( float.IsNaN( targetRotation ) ? 0 : targetRotation );

		var distance = fishPosition.Distance( bobberPosition );

		if ( distance > 0.3f )
		{
			// move towards the bobber
			WorldPosition += moveDirection * _maxSwimSpeed * Time.Delta;
			return;
		}

		// _actionDuration = (float)GD.RandRange( 2000, 5000 );
		_actionDuration = Random.Shared.Float( 2, 5 );
		_lastAction = Time.Now;

		// random chance to try to eat the bobber
		/* if ( GD.RandRange( 0, 100 ) < 10 )
		{
			Log.Info("Trying to eat the bobber." );
			_lastNibble = Time.Now;
			GetNode<AudioStreamPlayer3D>( "Chomp" ).Play();
			return;
		} */

		// _isNibbleDeep = GD.RandRange( 0, 100 ) < 30 || _nibbles >= _maxNibbles;
		_isNibbleDeep = Random.Shared.Float() > 0.7f || _nibbles >= _maxNibbles;
		_lastNibble = Time.Now;

		if ( _isNibbleDeep )
		{
			// GetNode<AudioStreamPlayer3D>( "Chomp" ).Play();
			GameObject.PlaySound( ChompSound );
		}
		else
		{
			// GetNode<AudioStreamPlayer3D>( "Nibble" ).Play();
			GameObject.PlaySound( NibbleSound );
		}

		_nibbles++;

		// Log.Info($"Found bobber, waiting for {_actionDuration} msec." );
	}

	private void FailCatch()
	{
		Log.Info( "Failed to catch the fish." );
		Bobber.Rod.FishGotAway();
		// QueueFree();
	}

	private void HookFish()
	{
		Log.Info( "Hooked the fish." );
		SetState( FishState.Fighting );
		Bobber.Fish = this;

		switch ( Data?.Size )
		{
			case FishData.FishSize.Tiny:
				// _stamina = GD.RandRange( 2, 7 );
				_stamina = Random.Shared.Float( 2, 7 );
				break;
			case FishData.FishSize.Small:
				// _stamina = GD.RandRange( 5, 10 );
				_stamina = Random.Shared.Float( 5, 10 );
				break;
			case FishData.FishSize.Medium:
				// _stamina = GD.RandRange( 8, 15 );
				_stamina = Random.Shared.Float( 8, 15 );
				break;
			case FishData.FishSize.Large:
				// _stamina = GD.RandRange( 10, 20 );
				_stamina = Random.Shared.Float( 10, 20 );
				break;
			default:
				_stamina = 10;
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

	private int _swimRandomRadius = 6;

	private Vector3 _swimStartPos;
	private Vector3 _swimTarget;
	private const int _swimTargetTries = 10;
	private float _swimProgress;

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
			_swimStartPos = WorldPosition;
			_swimProgress = 0;

			Log.Trace( $"New swim target: {_swimTarget}." );
		}


		// move towards the target smoothly
		var moveDirection = (_swimTarget - WorldPosition).Normal;
		var swimDistance = _swimTarget.Distance( _swimStartPos );
		_swimProgress += Time.Delta * (_maxSwimSpeed / swimDistance);

		Vector3 preA = _swimStartPos;
		Vector3 postB = _swimTarget;

		if ( preA.Distance( postB ) < 0.01f )
		{
			// Log.Info("preA is equal to postB." );
			SetState( FishState.Idle );
			return;
		}

		// TODO: find the cubic interpolation function
		// var interp = _swimStartPos.CubicInterpolate( _swimTarget, preA, postB, _swimProgress );
		var interp = _swimStartPos.LerpTo( _swimTarget, _swimProgress );

		WorldPosition = interp;

		// rotate towards the target
		var targetRotation = MathF.Atan2( moveDirection.x, moveDirection.y );

		// var newRotation = new Vector3( 0, float.IsNaN( targetRotation ) ? 0 : targetRotation, 0 );
		var newRotation = Rotation.FromYaw( float.IsNaN( targetRotation ) ? 0 : targetRotation );

		WishedRotation = newRotation;

		// check if the fish has reached the target
		var distance = WorldPosition.Distance( _swimTarget );
		if ( distance < 0.01f )
		{
			Log.Trace( "Reached swim target." );
			_swimTarget = Vector3.Zero;
			SetState( FishState.Idle );
			return;
		}

		CheckForBobber();
	}

	private void GetNewSwimTarget()
	{
		/*var randomPoint = WorldPosition + new Vector3( GD.RandRange( -_swimRandomRadius, _swimRandomRadius ), 0,
			GD.RandRange( -_swimRandomRadius, _swimRandomRadius ) );

		var spaceState = GetWorld3D().DirectSpaceState;

		// check if the random point is in water
		var traceWater =
			new Trace( spaceState ).CastRay(
				PhysicsRayQueryParameters3D.Create( randomPoint + Vector3.Up * 1f, randomPoint + Vector3.Down * 1f,
					World.WaterLayer ) );

		if ( traceWater == null )
		{
			// Log.Trace( $"No water found at {randomPoint}." );
			// this will just try again
			return;
		}

		// check if the random point is on terrain or at the edge of the water where there's a steep slant
		var traceTerrain =
			new Trace( spaceState ).CastRay(
				PhysicsRayQueryParameters3D.Create( randomPoint + Vector3.Up * 1f, randomPoint + Vector3.Down * 1f,
					World.TerrainLayer ) );

		if ( traceTerrain != null )
		{
			Log.Trace( $"Terrain found at {randomPoint}." );
			// this will just try again
			return;
		}

		// check if there is terrain between the fish and the random point
		var trace = new Trace( spaceState ).CastRay(
			PhysicsRayQueryParameters3D.Create( GlobalTransform.Origin, randomPoint, World.TerrainLayer ) );

		if ( trace != null )
		{
			Log.Trace( $"Terrain found between {GlobalTransform.Origin} and {randomPoint}." );
			// this will just try again
			return;
		}*/
		
		var randomPoint = WorldPosition + new Vector3( Random.Shared.Float( -_swimRandomRadius, _swimRandomRadius ), 0,
			Random.Shared.Float( -_swimRandomRadius, _swimRandomRadius ) );
		
		var traceWater = Scene.Trace.Ray( randomPoint + Vector3.Up * 16f, randomPoint + Vector3.Down * 32f )
			.WithTag( "water" )
			.Run();
		
		if ( !traceWater.Hit )
		{
			Log.Warning( $"No water found at {randomPoint}." );
			// this will just try again
			return;
		}
		
		var traceTerrain = Scene.Trace.Ray( randomPoint + Vector3.Up * 16f, randomPoint + Vector3.Down * 32f )
			.WithTag( "terrain" )
			.Run();
		
		if ( traceTerrain.Hit )
		{
			Log.Warning( $"Terrain found at {randomPoint}." );
			// this will just try again
			return;
		}
		
		var trace = Scene.Trace.Ray( WorldPosition, randomPoint )
			.WithTag( "terrain" )
			.Run();
		
		if ( trace.Hit )
		{
			Log.Warning( $"Terrain found between {WorldPosition} and {randomPoint}." );
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
		var bobber = Scene.GetAllComponents<FishingBobber>().FirstOrDefault();
		
		if ( !bobber.IsValid() )
		{
			return;
		}

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

		/* if ( angle > _bobberDiscoverAngle )
		{
			Log.Info($"Bobber is not in view ({angle})." );
			return;
		} */

		Bobber = bobber;
		Bobber.Fish = this;

		SetState( FishState.FoundBobber );
	}

	public void Pull()
	{
		if ( State != FishState.Fighting ) return;
		_stamina -= 1;
		Log.Info( $"Pulled the fish, stamina left: {_stamina}." );
		if ( _stamina <= 0 )
		{
			CatchFish();
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
}
