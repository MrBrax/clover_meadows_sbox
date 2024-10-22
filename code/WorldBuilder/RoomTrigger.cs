using Clover.Player;

namespace Clover.WorldBuilder;

[Category( "Clover/World" )]
public class RoomTrigger : Component, Component.ITriggerListener
{
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }

	[Property] public InteriorManager InteriorManager { get; set; }

	[Property] public string RoomId { get; set; }

	[Property] public List<string> BodyGroupsToHide { get; set; } = new();

	[Property] public List<GameObject> PiecesToShow { get; set; } = new();

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( other.GameObject.Components.TryGet<PlayerCharacter>( out var player,
			    FindMode.EverythingInSelfAndAncestors ) )
		{
			Log.Info( $"Player {player.PlayerName} entered room {RoomId}" );
			InteriorManager?.EnterRoom( player, this );
			/*foreach ( var bodyGroup in BodyGroupsToHide )
			{
				InteriorManager?.SetInteriorModelBodyGroup( bodyGroup, false );
			}*/

			/*foreach ( var piece in PiecesToShow )
			{
				piece.Enabled = true;
			}*/
		}
	}

	void ITriggerListener.OnTriggerExit( Collider other )
	{
		if ( other.GameObject.Components.TryGet<PlayerCharacter>( out var player,
			    FindMode.EverythingInSelfAndAncestors ) )
		{
			Log.Info( $"Player {player.PlayerName} exited room {RoomId}" );
			InteriorManager?.ExitRoom( player, this );
			/*foreach ( var bodyGroup in BodyGroupsToHide )
			{
				InteriorManager?.SetInteriorModelBodyGroup( bodyGroup, true );
			}*/

			/*foreach ( var piece in PiecesToShow )
			{
				piece.Enabled = false;
			}*/
		}
	}

	protected override void DrawGizmos()
	{
		foreach ( var piece in PiecesToShow )
		{
			if ( piece.IsValid() )
			{
				Gizmo.Transform = global::Transform.Zero;
				Gizmo.Draw.Arrow( WorldPosition, piece.WorldPosition );
			}
		}
	}
}
