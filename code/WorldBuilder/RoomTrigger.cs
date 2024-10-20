using Clover.Player;

namespace Clover.WorldBuilder;

public class RoomTrigger : Component, Component.ITriggerListener
{
	[Property] public InteriorManager InteriorManager { get; set; }

	[Property] public string RoomId { get; set; }

	[Property] public List<string> BodyGroupsToHide { get; set; } = new();

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( other.GameObject.Components.TryGet<PlayerCharacter>( out var player,
			    FindMode.EverythingInSelfAndAncestors ) )
		{
			Log.Info( $"Player {player.PlayerName} entered room {RoomId}" );
			InteriorManager?.EnterRoom( player, RoomId );
			foreach ( var bodyGroup in BodyGroupsToHide )
			{
				InteriorManager?.SetInteriorModelBodyGroup( bodyGroup, false );
			}
		}
	}

	void ITriggerListener.OnTriggerExit( Collider other )
	{
		if ( other.GameObject.Components.TryGet<PlayerCharacter>( out var player,
			    FindMode.EverythingInSelfAndAncestors ) )
		{
			Log.Info( $"Player {player.PlayerName} exited room {RoomId}" );
			InteriorManager?.ExitRoom( player, RoomId );
			foreach ( var bodyGroup in BodyGroupsToHide )
			{
				InteriorManager?.SetInteriorModelBodyGroup( bodyGroup, true );
			}
		}
	}
}
