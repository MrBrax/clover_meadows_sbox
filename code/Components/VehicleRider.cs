using Clover.Vehicles;

namespace Clover.Components;

public class VehicleRider : Component
{
	
	public BaseVehicle Vehicle { get; set; }

	public void OnEnterVehicle( BaseVehicle baseVehicle, int seatIndex )
	{
		Vehicle = baseVehicle;
	}
	
	public void OnExitVehicle()
	{
		Vehicle = null;
	}
}
