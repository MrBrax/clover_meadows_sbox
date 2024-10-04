using Clover.Components;
using Clover.Player;

namespace Clover.Interactable;

public class Sittable : Component, IInteract
{
	
	[Property] public List<SittableNode> Seats { get; set; } = new();

	protected override void OnAwake()
	{
		base.OnAwake();
		foreach ( var seat in Seats )
		{
			seat.Sittable = this;
		}
	}

	public void StartInteract( PlayerCharacter player )
	{
		SitDown( player );
	}

	public void FinishInteract( PlayerCharacter player )
	{
		
	}
	
	public void SitDown( PlayerCharacter player )
	{
		var seat = Seats.FirstOrDefault( x => !x.IsOccupied );
		if ( seat == null ) return;
		
		seat.Occupant = player.GameObject;
		player.Seat = seat;
		player.EnterPosition = player.WorldPosition;
		player.WorldPosition = seat.WorldPosition;
		player.Controller.Yaw = seat.WorldRotation.Yaw();
		
		Input.Clear( "use" );
	}
	
}
