using Clover.Components;
using Clover.Player;

namespace Clover;

[Category( "Clover/World" )]
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
		Gizmo.Hitbox.BBox( BBox.FromPositionAndSize( Collider.Center, Collider.Scale ) );
		Gizmo.Draw.LineBBox( BBox.FromPositionAndSize( Collider.Center, Collider.Scale ) );
		Gizmo.Draw.Arrow( Vector3.Zero, Vector3.Forward * 64f );
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

		Enter( player );
	}

	private async void Enter( PlayerCharacter player )
	{
		var w = await WorldManager.Instance.GetWorldOrLoad( DestinationWorldData );

		if ( player.InCutscene ) return;

		var entrance = w.GetEntrance( DestinationEntranceId );

		if ( entrance.IsValid() )
		{
			var currentWorld = WorldManager.Instance.GetWorld( WorldLayerObject.Layer );

			currentWorld.Save();

			player.CutsceneTarget = WorldPosition + WorldRotation.Forward * 64f;
			player.InCutscene = true;

			await Fader.Instance.FadeToBlack( true );

			player.SetLayer( entrance.WorldLayerObject.Layer );
			player.TeleportTo( entrance.EntranceId );
			// player.ModelLookAt( entrance.WorldPosition + entrance.WorldRotation.Forward );

			player.OnWorldChanged?.Invoke( w );

			if ( UnloadPreviousWorld && currentWorld.ShouldUnloadOnExit )
			{
				WorldManager.Instance.UnloadWorld( currentWorld );
			}
			
			player.CutsceneTarget = entrance.WorldPosition + entrance.WorldRotation.Forward * 64f;

			
			await Fader.Instance.FadeFromBlack( true );
			
			// await GameTask.DelayRealtimeSeconds( 0.25f );
			
			player.InCutscene = false;
			
			Log.Info( $"areatrigger finished" );
			
			
		}
		else
		{
			Log.Warning( $"AreaTrigger: OnTriggerEnter: No entrance found with id: {DestinationEntranceId}" );
		}
	}
}
