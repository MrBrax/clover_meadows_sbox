using System;
using System.Threading.Tasks;
using Clover.Player;

namespace Clover;

[Title( "Network Helper2" )]
[Category( "Clover" )]
[Icon( "electrical_services" )]
public sealed class NetworkHelper : Component, Component.INetworkListener
{
	/// <summary>
	/// Create a server (if we're not joining one)
	/// </summary>
	[Property]
	public bool StartServer { get; set; } = true;

	/// <summary>
	/// The prefab to spawn for the player to control.
	/// </summary>
	[Property]
	public GameObject PlayerPrefab { get; set; }

	/// <summary>
	/// A list of points to choose from randomly to spawn the player in. If not set, we'll spawn at the
	/// location of the NetworkHelper object.
	/// </summary>
	[Property]
	public List<GameObject> SpawnPoints { get; set; }

	protected override async Task OnLoad()
	{
		if ( Scene.IsEditor )
			return;

		if ( StartServer && !Networking.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			Networking.CreateLobby();
		}
	}

	/// <summary>
	/// A client is fully connected to the server. This is called on the host.
	/// </summary>
	public void OnActive( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' has joined the game" );

		if ( !PlayerPrefab.IsValid() )
			return;

		//
		// Find a spawn location for this player
		//
		var startLocation = FindSpawnLocation().WithScale( 1 );

		// Spawn this object and make the client the owner
		var player = PlayerPrefab.Clone( startLocation, name: $"Player - {channel.DisplayName}" );
		player.NetworkSpawn( channel );

		// Notify any listeners that a player has spawned
		Scene.RunEvent<IPlayerSpawned>( x => x.OnPlayerSpawned( player.GetComponent<PlayerCharacter>() ) );

		if ( WorldManager.Instance.ActiveWorld != null )
		{
			var spawnPoint = WorldManager.Instance.ActiveWorld.GetEntrance( "spawn" );
			if ( spawnPoint.IsValid() )
			{
				player.GetComponent<PlayerCharacter>().TeleportTo( spawnPoint.EntranceId );
			}
			else
			{
				Log.Error( "No spawn point found in the world" );
			}
		}
		else
		{
			Log.Error( "No active world found" );
		}
	}

	/// <summary>
	/// Find the most appropriate place to respawn
	/// </summary>
	Transform FindSpawnLocation()
	{
		//
		// If they have spawn point set then use those
		//
		if ( SpawnPoints is not null && SpawnPoints.Count > 0 )
		{
			return Random.Shared.FromList( SpawnPoints, default ).Transform.World;
		}

		//
		// If we have any SpawnPoint components in the scene, then use those
		//
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		if ( spawnPoints.Length > 0 )
		{
			return Random.Shared.FromArray( spawnPoints ).Transform.World;
		}

		//
		// Failing that, spawn where we are
		//
		return Transform.World;
	}
}

public interface IPlayerSpawned
{
	/// <summary>
	///  Called when a player has spawned into the game
	/// </summary>
	/// <param name="player"></param>
	void OnPlayerSpawned( PlayerCharacter player );
}
