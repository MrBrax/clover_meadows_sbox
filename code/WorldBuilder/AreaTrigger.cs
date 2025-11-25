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

	[Property] public bool NoWalk { get; set; }

	private HashSet<GameObject> _triggerQueue = new();


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
		if ( IsProxy || !Networking.IsHost ) return;

		var player = other.GetComponent<PlayerCharacter>();
		if ( !player.IsValid() )
		{
			// Log.Warning( $"AreaTrigger: OnTriggerEnter: Not a player: {other}" );
			return;
		}

		// Enter( player );
		_triggerQueue.Add( other.GameObject );
	}

	void ITriggerListener.OnTriggerExit( Collider other )
	{
		_triggerQueue.Remove( other.GameObject );
	}

	private async void Enter( PlayerCharacter player )
	{
		if ( !DestinationWorldData.IsValid() )
		{
			Log.Error( $"AreaTrigger: OnTriggerEnter: Invalid destination world data" );
			return;
		}

		var w = await WorldManager.Instance.GetWorldOrLoad( DestinationWorldData );

		if ( !w.IsValid() )
		{
			Log.Error( $"AreaTrigger: OnTriggerEnter: Failed to load world {DestinationWorldData.ResourceName}" );
			return;
		}

		if ( player.InCutscene )
		{
			Log.Warning( "AreaTrigger: OnTriggerEnter: Player is in cutscene" );
			return;
		}

		var entrance = w.GetEntrance( DestinationEntranceId );

		if ( entrance.IsValid() )
		{
			var currentWorld = WorldManager.Instance.GetWorld( WorldLayerObject.Layer );

			currentWorld.Save();

			player.StartCutscene( WorldPosition + WorldRotation.Forward * 64f );

			if ( !player.Network.Owner.IsHost )
			{
				Log.Info( $"areatrigger waiting for host" );
				await Task.DelayRealtimeSeconds( 1f ); // Wait for the host to load the world
			}

			// await Fader.Instance.FadeToBlack( true );
			using ( Rpc.FilterInclude( player.Network.Owner ) )
			{
				Fader.Instance.FadeToBlackRpc( true );
			}

			await Task.DelayRealtimeSeconds( Fader.Instance.FadeTime );

			player.SetLayer( entrance.WorldLayerObject.Layer );
			player.TeleportTo( entrance.EntranceId );
			// player.ModelLookAt( entrance.WorldPosition + entrance.WorldRotation.Forward );

			player.OnWorldChanged?.Invoke( w );

			if ( UnloadPreviousWorld && currentWorld.ShouldUnloadOnExit )
			{
				WorldManager.Instance.UnloadWorld( currentWorld );
			}

			await GameTask.DelayRealtimeSeconds( 0.25f );

			player.StartCutscene( entrance.WorldPosition + entrance.WorldRotation.Forward * 64f );

			// await Fader.Instance.FadeFromBlack( true );
			using ( Rpc.FilterInclude( player.Network.Owner ) )
			{
				Fader.Instance.FadeFromBlackRpc( true );
			}

			await GameTask.DelayRealtimeSeconds( Fader.Instance.FadeTime );

			// await GameTask.DelayRealtimeSeconds( 0.25f );

			player.EndCutscene();

			Log.Info( $"areatrigger finished" );
		}
		else
		{
			Log.Warning( $"AreaTrigger: OnTriggerEnter: No entrance found with id: {DestinationEntranceId}" );
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;

		// hack to continually check for players in the trigger
		foreach ( var go in _triggerQueue )
		{
			var player = go.GetComponent<PlayerCharacter>();
			if ( player.IsValid() && !player.InCutscene )
			{
				Enter( player );
				_triggerQueue.Remove( go );
			}
		}
	}
}
