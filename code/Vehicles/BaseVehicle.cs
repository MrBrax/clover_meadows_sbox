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

	private float Momentum;

	private bool IsOn;
	
	public Dictionary<SittableNode, GameObject> Occupants { get; set; } = new();
	
	public bool HasDriver => Occupants.Keys.Contains( Seats[0] );

	protected override void OnStart()
	{
		base.OnStart();
		
		foreach ( var light in Headlights )
		{
			light.Enabled = false;
		}
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
	}
	
	private void StopEngine()
	{
		IsOn = false;
		
		foreach ( var light in Headlights )
		{
			light.Enabled = false;
		}
	}
	
	public void AddOccupant( int seatIndex, GameObject occupant )
	{
		var seat = Seats[seatIndex];
		
		if ( Occupants.ContainsKey( seat ) ) throw new InvalidOperationException( "Seat is already occupied" );
		
		Occupants[seat] = occupant;
		Log.Info( $"Added occupant to seat {seatIndex}" );
		
		occupant.WorldPosition = seat.WorldPosition;
		if ( occupant.Components.TryGet<PlayerCharacter>( out var player ) )
		{
			player.Seat = seat;
			player.PlayerController.Yaw = Model.WorldRotation.Yaw();
			player.SetCollisionEnabled( false );
			player.SetCarriableVisibility( false );
		}
		
		if ( occupant.Components.TryGet<VehicleRider>( out var rider ) )
		{
			rider.OnEnterVehicle( this, seatIndex );
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
	}
	
	public void TryToEjectOccupant( GameObject occupant )
	{
		var seat = Occupants.FirstOrDefault( x => x.Value == occupant ).Key;
		
		if ( seat == null ) throw new InvalidOperationException( "Occupant not found" );

		var exitPosition = FindExitPosition( occupant );
		
		RemoveOccupant( occupant );
		
		occupant.WorldPosition = exitPosition;
	}

	private Vector3 FindExitPosition( GameObject occupant )
	{
		return WorldPosition + Model.WorldRotation.Backward * 100;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		
		// HandleOccupants();
		// Handling();
		
		if ( HasDriver && Input.Pressed( "Use" ) )
		{
			TryToEjectOccupant( Occupants[Seats[0]] );
		}
		
		
	}
}
