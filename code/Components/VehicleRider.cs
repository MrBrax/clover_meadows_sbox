using Clover.Player;
using Clover.Vehicles;

namespace Clover.Components;

public class VehicleRider : Component
{
	
	public BaseVehicle Vehicle { get; set; }
	public SittableNode Seat { get; set; }

	public void OnEnterVehicle( BaseVehicle baseVehicle, SittableNode seat, int seatIndex )
	{
		Vehicle = baseVehicle;
		Seat = seat;
	}
	
	public void OnExitVehicle()
	{
		Vehicle = null;
		Seat = null;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();


		if ( Seat.IsValid() )
		{
			if ( Components.TryGet<PlayerCharacter>( out var player ) )
			{
				player.ModelLook( Seat.WorldRotation );
			}
		}
		
	}
}
