using Clover.Player;
using Clover.Vehicles;

namespace Clover.Components;

public class VehicleRider : Component
{
	public BaseVehicle Vehicle { get; set; }
	public SittableNode Seat { get; set; }

	[Authority]
	public void OnEnterVehicle( BaseVehicle baseVehicle, SittableNode seat, int seatIndex )
	{
		Vehicle = baseVehicle;
		Seat = seat;
		Seat.Occupant = GameObject;
	}

	[Authority]
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
