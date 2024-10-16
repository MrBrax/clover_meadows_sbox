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

	[Sync] private bool IsOn { get; set; }

	public bool LocalPlayerInside => Occupants.Values.Contains( PlayerCharacter.Local?.GameObject );

	public bool LocalPlayerIsDriver =>
		Occupants.Keys.Contains( Seats[0] ) && Occupants[Seats[0]] == PlayerCharacter.Local?.GameObject;

	[Property, Sync] public NetDictionary<SittableNode, GameObject> Occupants { get; set; } = new();

	public bool HasDriver => Occupants.Keys.Contains( Seats[0] );

	private SoundHandle _idleSoundHandle;
	private SoundHandle _engineSoundHandle;
	[Sync] private TimeSince _startEngineTime { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		foreach ( var light in Headlights )
		{
			light.Enabled = IsOn;
		}

		ExhaustParticles.Enabled = IsOn;

		if ( IsOn )
		{
			_idleSoundHandle = GameObject.PlaySound( IdleSound );
			_idleSoundHandle.Volume = 0;

			_engineSoundHandle = GameObject.PlaySound( EngineSound );
			_engineSoundHandle.Volume = 0;
		}
	}

	public void StartInteract( PlayerCharacter player )
	{
		EnterVehicle();
	}

	public void FinishInteract( PlayerCharacter player )
	{
	}

	public string GetInteractName()
	{
		return "Enter vehicle";
	}

	[Broadcast]
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

	[Broadcast]
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


	[Authority]
	private void EnterVehicle()
	{
		var caller = Rpc.Caller;
		var player = PlayerCharacter.Get( caller );

		if ( !player.IsValid() )
		{
			Log.Error( "Player not found" );
			return;
		}

		// check if player is already inside
		if ( Occupants.Values.Contains( player.GameObject ) )
		{
			Log.Error( "Player is already inside" );
			return;
		}

		if ( !HasDriver )
		{
			var seat = Seats[0];

			Log.Info( "Player is entering as driver" );

			Occupants[seat] = player.GameObject;

			seat.Occupant = player.GameObject;

			using ( Rpc.FilterInclude( caller ) )
			{
				SitDown( seat );
			}

			// player.VehicleRider.OnEnterVehicle( this, Seats[0], 0 );

			StartEngine();
		}
		else
		{
			Log.Info( "Player is entering as passenger" );

			var seat = Seats.FirstOrDefault( x => !Occupants.ContainsKey( x ) );

			if ( seat == null )
			{
				Log.Error( "No available seats" );
				return;
			}

			Occupants[seat] = player.GameObject;

			seat.Occupant = player.GameObject;

			using ( Rpc.FilterInclude( caller ) )
			{
				SitDown( seat );
			}

			// player.VehicleRider.OnEnterVehicle( this, seat, Seats.IndexOf( seat ) );
		}
	}

	[Broadcast]
	private void SitDown( SittableNode seat )
	{
		var player = PlayerCharacter.Local;

		if ( player.IsValid() )
		{
			player.GameObject.SetParent( GameObject );
			// player.Seat = seat;
			player.TeleportTo( seat.WorldPosition, seat.WorldRotation );
			player.PlayerController.Yaw = Model.WorldRotation.Yaw();
			player.SetCollisionEnabled( false );
			player.SetCarriableVisibility( false );

			if ( player.Components.TryGet<VehicleRider>( out var rider ) )
			{
				rider.OnEnterVehicle( this, seat, Seats.IndexOf( seat ) );
			}
		}
	}

	/*public void AddOccupant( GameObject occupant )
	{
		var seat = Seats.FirstOrDefault( x => !Occupants.ContainsKey( x ) );

		if ( seat == null ) throw new InvalidOperationException( "No available seats" );

		AddOccupant( Seats.IndexOf( seat ), occupant );
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
	}*/

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

		// RemoveOccupant( occupant );

		Occupants.Remove( seat );
		seat.Occupant = null;

		if ( occupant.Components.TryGet<PlayerCharacter>( out var player ) )
		{
			player.TeleportTo( exitPosition, Model.WorldRotation );
		}
		else
		{
			occupant.WorldPosition = exitPosition;
		}

		occupant.SetParent( null );
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

		if ( LocalPlayerInside && Input.Pressed( "Use" ) )
		{
			// TryToEjectOccupant( Occupants[Seats[0]] );
			ExitVehicle();
			Input.Clear( "Use" );
		}
	}

	[Authority]
	private void ExitVehicle()
	{
		var caller = Rpc.Caller;
		var player = PlayerCharacter.Get( caller );

		if ( !player.IsValid() )
		{
			Log.Error( "Player not found" );
			return;
		}

		if ( !Occupants.Values.Contains( player.GameObject ) )
		{
			Log.Error( "Player is not inside" );
			return;
		}

		var seat = Occupants.FirstOrDefault( x => x.Value == player.GameObject ).Key;

		if ( seat == null )
		{
			Log.Error( "Seat not found" );
			return;
		}

		Occupants.Remove( seat );
		seat.Occupant = null;

		using ( Rpc.FilterInclude( caller ) )
		{
			ExitVehicleRpc();
		}

		// player.VehicleRider.OnExitVehicle();

		if ( seat == Seats[0] )
		{
			StopEngine();
		}
	}

	[Broadcast]
	private void ExitVehicleRpc()
	{
		var player = PlayerCharacter.Local;

		if ( player.IsValid() )
		{
			player.GameObject.SetParent( null );
			player.TeleportTo( FindExitPosition( player.GameObject ), Model.WorldRotation );
			// player.PlayerController.Yaw = Model.WorldRotation.Yaw();
			player.SetCollisionEnabled( true );
			player.SetCarriableVisibility( true );

			if ( player.Components.TryGet<VehicleRider>( out var rider ) )
			{
				rider.OnExitVehicle();
			}
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
		BuildInput();

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

	public Vector3 ProxyInput;

	private void BuildInput()
	{
		if ( !LocalPlayerIsDriver ) return;

		if ( !IsProxy )
		{
			ProxyInput = Input.AnalogMove;
		}
		else
		{
			InputRpc( Input.AnalogMove );
		}
	}

	[Authority]
	private void InputRpc( Vector3 input )
	{
		ProxyInput = input;
	}

	public void BuildWishVelocity()
	{
		if ( !HasDriver ) return;

		var input = ProxyInput;

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
			WishVelocity = forward * (input.x * 300f);
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
