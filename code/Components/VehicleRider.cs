using Clover.Player;
using Clover.Vehicles;

namespace Clover.Components;

/// <summary>
///  Helper component for getting players/NPCs to ride vehicles.
/// </summary>
[Category( "Clover/Components" )]
[Icon( "car" )]
public class VehicleRider : Component
{
	public BaseVehicle Vehicle { get; set; }
	public SittableNode Seat { get; set; }

	[Rpc.Owner]
	public void OnEnterVehicle( BaseVehicle baseVehicle, SittableNode seat, int seatIndex )
	{
		Vehicle = baseVehicle;
		Seat = seat;
		Seat.Occupant = GameObject;
	}

	[Rpc.Owner]
	public void OnExitVehicle()
	{
		Vehicle = null;
		Seat.Occupant = null;
		Seat = null;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();


		if ( Seat.IsValid() )
		{
			WorldPosition = Seat.WorldPosition;
			if ( Components.TryGet<PlayerCharacter>( out var player ) )
			{
				player.ModelLook( Seat.WorldRotation );
			}
		}
	}
}
