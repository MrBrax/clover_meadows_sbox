using Clover.Player;

namespace Clover.Components;

/// <summary>
///  A trigger that adds/removes a tag to the GameObject that enters/exits the trigger.
///  Used for bridges to walk over the collision blocker while also walking on top of the bridge.
/// </summary>
[Title( "Collision Layer Trigger" )]
[Category( "Clover/Components" )]
public class CollisionLayerTrigger : Component, Component.ITriggerListener
{
	[Property] public string Layer { get; set; }

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		// Log.Info( $"OnTriggerEnter {other.GameObject}" );
		if ( !other.GameObject.IsValid() )
		{
			Log.Warning( "Invalid GameObject" );
			return;
		}

		if ( other.GameObject.Components.Get<PlayerCharacter>() == null ) return;

		Log.Info( $"Adding {Layer} to {other.GameObject}" );
		other.GameObject.Tags.Add( Layer );
	}

	void ITriggerListener.OnTriggerExit( Collider other )
	{
		// Log.Info( $"OnTriggerExit {other.GameObject}" );
		if ( !other.GameObject.IsValid() )
		{
			Log.Warning( "Invalid GameObject" );
			return;
		}

		if ( other.GameObject.Components.Get<PlayerCharacter>() == null ) return;

		Log.Info( $"Removing {Layer} from {other.GameObject}" );
		other.GameObject.Tags.Remove( Layer );
	}
}
