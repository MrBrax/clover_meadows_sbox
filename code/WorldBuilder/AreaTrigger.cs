using Clover.Player;

namespace Clover;

public sealed class AreaTrigger : Component, Component.ITriggerListener
{
	[RequireComponent] public BoxCollider Collider { get; set; }

	[Property] public Data.World DestinationWorld { get; set; }
	[Property] public string DestinationEntranceId { get; set; }


	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Text( DestinationWorld?.Title + "\n" + DestinationEntranceId, new Transform() );
	}

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		var player = other.GetComponent<PlayerCharacter>();
		if ( !player.IsValid() )
		{
			Log.Warning( "AreaTrigger: OnTriggerEnter: Not a player" );
			return;
		}

		var w = WorldManager.Instance.LoadWorld( DestinationWorld );

		var entrance = w.GetEntrance( DestinationEntranceId );

		if ( entrance.IsValid() )
		{
			player.WorldPosition = entrance.WorldPosition;
			
			WorldManager.Instance.SetActiveWorld( w );
		}
		else
		{
			Log.Warning( $"AreaTrigger: OnTriggerEnter: No entrance found with id: {DestinationEntranceId}" );
		}
	}
}
