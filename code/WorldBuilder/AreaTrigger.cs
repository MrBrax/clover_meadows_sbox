using Clover.Player;

namespace Clover;

public sealed class AreaTrigger : Component, Component.ITriggerListener
{
	
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }
	[RequireComponent] public BoxCollider Collider { get; set; }

	[Property] public Data.WorldData DestinationWorldData { get; set; }
	[Property] public string DestinationEntranceId { get; set; }
	
	[Property] public bool UnloadPreviousWorld { get; set; } = true;


	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Text( DestinationWorldData?.Title + "\n" + DestinationEntranceId, new Transform() );
		Gizmo.Hitbox.BBox( BBox.FromPositionAndSize( Collider.Center, Collider.Scale ));
		Gizmo.Draw.LineBBox( BBox.FromPositionAndSize( Collider.Center, Collider.Scale ) );
	}

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( IsProxy ) return;
		
		var player = other.GetComponent<PlayerCharacter>();
		if ( !player.IsValid() )
		{
			Log.Warning( "AreaTrigger: OnTriggerEnter: Not a player" );
			return;
		}

		var w = WorldManager.Instance.GetWorldOrLoad( DestinationWorldData );

		var entrance = w.GetEntrance( DestinationEntranceId );

		if ( entrance.IsValid() )
		{
			/*player.WorldPosition = entrance.WorldPosition;
			player.ModelLookAt( entrance.WorldPosition + entrance.WorldRotation.Forward );
			player.Transform.ClearInterpolation();
			player.WorldLayerObject.SetLayer( entrance.WorldLayerObject.Layer );
			player.GetComponent<CameraController>().SnapCamera();
			WorldManager.Instance.SetActiveWorld( w );
			
			if ( UnloadPreviousWorld )
			{
				WorldManager.Instance.UnloadWorld( WorldManager.Instance.GetWorld( WorldLayerObject.Layer ) );
			}*/
			
			player.TeleportTo( entrance.WorldPosition, entrance.WorldRotation );
			player.SetLayer( entrance.WorldLayerObject.Layer );
			
			if ( UnloadPreviousWorld && WorldManager.Instance.GetWorld( WorldLayerObject.Layer ).ShouldUnloadOnExit )
			{
				WorldManager.Instance.UnloadWorld( WorldManager.Instance.GetWorld( WorldLayerObject.Layer ) );
			}
			
		}
		else
		{
			Log.Warning( $"AreaTrigger: OnTriggerEnter: No entrance found with id: {DestinationEntranceId}" );
		}
	}
}
