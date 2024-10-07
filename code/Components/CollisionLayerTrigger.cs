namespace Clover.Components;

public class CollisionLayerTrigger : Component, Component.ITriggerListener
{
	[Property] public string Layer { get; set; }

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		Log.Info( $"OnTriggerEnter {other.GameObject}" );
		if ( !other.GameObject.IsValid() )
		{
			Log.Warning( "Invalid GameObject" );
			return;
		}

		Log.Info($"Adding {Layer} to {other.GameObject}");
		other.GameObject.Tags.Add( Layer );
	}

	void ITriggerListener.OnTriggerExit( Collider other )
	{
		Log.Info( $"OnTriggerExit {other.GameObject}" );
		if ( !other.GameObject.IsValid() )
		{
			Log.Warning( "Invalid GameObject" );
			return;
		}

		Log.Info($"Removing {Layer} from {other.GameObject}");
		other.GameObject.Tags.Remove( Layer );
	}
}
