using System;
using Clover.Components;
using Clover.Interactable;
using Clover.Player;
using Light = Sandbox.Light;

namespace Clover.Vehicles;

public class BaseVehicle : Component, IInteract
{
	[RequireComponent] public CharacterController CharacterController { get; set; }

	[Property] public GameObject Model { get; set; }

	[Property] public List<Light> Headlights { get; set; }

	[Property] public List<SittableNode> Seats { get; set; }

	[Property] public float Steering { get; set; } = 0.5f;

	[Property] public SoundEvent StartEngineSound { get; set; }
	[Property] public SoundEvent StopEngineSound { get; set; }
	[Property] public SoundEvent IdleSound { get; set; }
	[Property] public SoundEvent EngineSound { get; set; }
	[Property] public SoundEvent HornSound { get; set; }
	
	[Property] public GameObject ExhaustParticles { get; set; }

	private bool IsOn;

	public Dictionary<SittableNode, GameObject> Occupants { get; set; } = new();

	public bool HasDriver => Occupants.Keys.Contains( Seats[0] );

	private SoundHandle _idleSoundHandle;
	private SoundHandle _engineSoundHandle;
	private TimeSince _startEngineTime;

	protected override void OnStart()
	{
		base.OnStart();

		foreach ( var light in Headlights )
		{
			light.Enabled = false;
		}
		
		ExhaustParticles.Enabled = false;
	}

	public void StartInteract( PlayerCharacter player )
	{
		if ( !HasDriver )
		{
			AddOccupant( 0, player.GameObject );
		}
	}

	public void FinishInteract( PlayerCharacter player )
	{
	}

	private void StartEngine()
	{
		IsOn = true;

		foreach ( var light in Headlights )
		{
			light.Enabled = true;
		}

		GameObject.PlaySound( StartEngineSound );

		_idleSoundHandle = GameObject.PlaySound( IdleSound );
		_idleSoundHandle.Volume = 0;

		_engineSoundHandle = GameObject.PlaySound( EngineSound );
		_engineSoundHandle.Volume = 0;

		_startEngineTime = 0;

		if ( ExhaustParticles != null )
		{
			ExhaustParticles.Enabled = true;
		}
	}

	private void StopEngine()
	{
		IsOn = false;

		foreach ( var light in Headlights )
		{
			light.Enabled = false;
		}

		GameObject?.PlaySound( StopEngineSound );

		_idleSoundHandle?.Stop();
		_engineSoundHandle?.Stop();

		if ( ExhaustParticles != null )
		{
			ExhaustParticles.Enabled = false;
		}
	}

	public void AddOccupant( int seatIndex, GameObject occupant )
	{
		var seat = Seats[seatIndex];

		if ( Occupants.ContainsKey( seat ) ) throw new InvalidOperationException( "Seat is already occupied" );

		Occupants[seat] = occupant;
		Log.Info( $"Added occupant to seat {seatIndex}" );

		// occupant.WorldPosition = seat.WorldPosition;
		occupant.SetParent( GameObject );
		Log.Info( $"Occupant {occupant.Name} position: {seat.WorldPosition}, vehicle position: {WorldPosition}" );
		if ( occupant.Components.TryGet<PlayerCharacter>( out var player ) )
		{
			// player.Seat = seat;
			player.TeleportTo( seat.WorldPosition, seat.WorldRotation );
			player.PlayerController.Yaw = Model.WorldRotation.Yaw();
			player.SetCollisionEnabled( false );
			player.SetCarriableVisibility( false );
		}

		if ( occupant.Components.TryGet<VehicleRider>( out var rider ) )
		{
			rider.OnEnterVehicle( this, seat, seatIndex );
		}

		if ( seatIndex == 0 )
		{
			StartEngine();
		}
	}

	public void RemoveOccupant( GameObject occupant )
	{
		var seat = Occupants.FirstOrDefault( x => x.Value == occupant ).Key;

		if ( seat == null ) throw new InvalidOperationException( "Occupant not found" );

		Occupants.Remove( seat );
		Log.Info( $"Removed occupant from seat {Seats.IndexOf( seat )}" );

		occupant.SetParent( null );

		if ( occupant.Components.TryGet<PlayerCharacter>( out var player ) )
		{
			player.Seat = null;
			player.SetCollisionEnabled( true );
			player.SetCarriableVisibility( true );
		}

		if ( occupant.Components.TryGet<VehicleRider>( out var rider ) )
		{
			rider.OnExitVehicle();
		}

		if ( seat == Seats[0] )
		{
			StopEngine();
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		foreach ( var occupant in Occupants.Values )
		{
			if ( occupant.Components.TryGet<PlayerCharacter>( out var player ) )
			{
				player.Seat = null;
				player.SetCollisionEnabled( true );
				player.SetCarriableVisibility( true );
			}

			if ( occupant.Components.TryGet<VehicleRider>( out var rider ) )
			{
				rider.OnExitVehicle();
			}
		}

		StopEngine();
	}

	public void TryToEjectOccupant( GameObject occupant )
	{
		var seat = Occupants.FirstOrDefault( x => x.Value == occupant ).Key;

		if ( seat == null ) throw new InvalidOperationException( "Occupant not found" );

		var exitPosition = FindExitPosition( occupant );

		RemoveOccupant( occupant );

		if ( occupant.Components.TryGet<PlayerCharacter>( out var player ) )
		{
			player.TeleportTo( exitPosition, Model.WorldRotation );
		}
		else
		{
			occupant.WorldPosition = exitPosition;
		}
	}

	private Vector3 FindExitPosition( GameObject occupant )
	{
		return WorldPosition + Model.WorldRotation.Backward * 100;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		// HandleOccupants();
		HandleSounds();
		Handling();

		if ( HasDriver && Input.Pressed( "Use" ) )
		{
			TryToEjectOccupant( Occupants[Seats[0]] );
			Input.Clear( "Use" );
		}
	}

	private void HandleSounds()
	{
		if ( !IsOn ) return;

		if ( _startEngineTime < 1f ) return;

		var velocity = CharacterController.Velocity.Length;

		if ( velocity < 2f )
		{
			_idleSoundHandle.Volume = _idleSoundHandle.Volume.LerpTo( IdleSound.Volume.FixedValue, Time.Delta * 2.0f );
			_engineSoundHandle.Volume = _engineSoundHandle.Volume.LerpTo( 0, Time.Delta * 2.0f );
		}
		else
		{
			_idleSoundHandle.Volume = _idleSoundHandle.Volume.LerpTo( 0, Time.Delta * 2.0f );
			_engineSoundHandle.Volume =
				_engineSoundHandle.Volume.LerpTo( EngineSound.Volume.FixedValue, Time.Delta * 2.0f );
		}

		_engineSoundHandle.Pitch = MathX.Lerp( 0.5f, 1.5f, MathF.Abs( velocity / 500 ) );
	}

	public Vector3 WishVelocity { get; private set; }

	[Property] public Vector3 Gravity { get; set; } = new Vector3( 0, 0, 800 );

	private void Handling()
	{
		if ( IsProxy )
			return;

		BuildWishVelocity();

		if ( CharacterController.IsOnGround )
		{
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );
			CharacterController.Accelerate( WishVelocity );
			CharacterController.ApplyFriction( 1.0f );
		}
		else
		{
			CharacterController.Velocity -= Gravity * Time.Delta * 0.5f;
			CharacterController.Accelerate( WishVelocity.ClampLength( 50 ) );
			CharacterController.ApplyFriction( 0.1f );
		}

		CharacterController.Move();

		if ( !CharacterController.IsOnGround )
		{
			CharacterController.Velocity -= Gravity * Time.Delta * 0.5f;
		}
		else
		{
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );
		}
	}

	public void BuildWishVelocity()
	{
		if ( !HasDriver ) return;

		var input = Input.AnalogMove;

		if ( input.Length > 0 /*&& !Player.ShouldDisableMovement()*/ )
		{
			// Player.Model.WorldRotation = Rotation.Lerp( Player.Model.WorldRotation, Rotation.LookAt( input, Vector3.Up ),
			// 	Time.Delta * 10.0f );

			if ( !Model.IsValid() )
			{
				Log.Error( "Model is not valid" );
				return;
			}

			// var inputYaw = MathF.Atan2( input.y, input.x ).RadianToDegree();
			// Player.Model.WorldRotation = Rotation.Lerp( Player.Model.WorldRotation, Rotation.From( 0, yaw + 180, 0 ), Time.Delta * 10.0f );
			// Yaw = yaw + 180;
			// Yaw = Yaw.LerpDegreesTo( inputYaw + 180, Time.Delta * 10.0f );

			Model.WorldRotation *= Rotation.From( 0, Steering * input.y, 0 );
			// Model.WorldRotation = Rotation.Lerp( Model.WorldRotation, Model.WorldRotation * Rotation.From( 0, Steering * input.y, 0 ), Time.Delta * 10.0f );

			var forward = Model.WorldRotation.Forward;
			WishVelocity = forward * ( input.x * 300f );
			WishVelocity = WishVelocity.ClampLength( 200f );

			Gizmo.Draw.ScreenText( WishVelocity.Length.ToString(), new Vector2( 20, 20 ) );

		}
		else
		{
			WishVelocity = Vector3.Zero;
		}

		// WishVelocity *= 200f;

		/*if ( Input.Down( "Run" ) )
		{
			WishVelocity *= 180.0f;
		}
		else
		{
			WishVelocity *= 110.0f;
		}*/
	}
}
