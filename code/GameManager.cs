using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Clover.Player;
using Clover.Ui;

namespace Clover;

public class GameManager : Component, Component.INetworkListener, ISceneStartup
{
	public static GameManager Instance;

	[Property] public GameObject PlayerPrefab { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();
		Instance = this;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		Instance = null;
	}

	protected override void OnStart()
	{
		_ = Bootstrap();
	}

	private async Task Bootstrap()
	{
		if ( IsProxy ) return;

		Log.Info( "GameManager is booting up" );

		await WorldManager.Instance.LoadWorld( WorldManager.Instance.DefaultWorldData );

		/*if ( !_spawnQueue.Contains( Connection.Local ) )
		{
			OnConnected( Connection.Local );
		}*/

		Log.Info( "GameManager has booted up" );
	}

	public static JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		IncludeFields = true,
		Converters = { new JsonStringEnumConverter() },
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private TimeSince _lastSave;

	public void SaveTimer()
	{
		if ( _lastSave < 60f )
		{
			return;
		}

		_lastSave = 0f;

		if ( !AutoSave ) return;

		if ( Networking.IsHost )
		{
			foreach ( var world in WorldManager.Instance.Worlds )
			{
				world.Value.Save();
			}
		}

		PlayerCharacter.Local?.Save();
	}

	[ConVar( "clover_autosave" )] public static bool AutoSave { get; set; } = true;

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		SaveTimer();
		SpawnPlayers();
	}

	private void SpawnPlayers()
	{
		if ( IsProxy ) return;

		foreach ( var channel in _spawnQueue.ToList() )
		{
			SpawnPlayer( channel );
		}
	}

	private readonly List<Connection> _spawnQueue = new();

	public void OnConnected( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' has joined the game" );
		// _spawnQueue.Add( channel );
	}

	public void OnDisconnected( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' has left the game" );
	}

	public void OnBecameHost( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' has become the host" );
	}

	public static void LoadRealm()
	{
		Game.ActiveScene.LoadFromFile( "scenes/clover.scene" );
	}

	public void SpawnPlayer( Connection channel )
	{
		if ( Scene.GetAllComponents<PlayerCharacter>().Any( x => x.Network.Owner == channel ) )
		{
			Log.Warning( $"Player '{channel.DisplayName}' already spawned" );
			_spawnQueue.Remove( channel );
			return;
		}

		if ( !PlayerPrefab.IsValid() )
			return;

		// Spawn this object and make the client the owner
		var player = PlayerPrefab.Clone( new Transform(), name: $"Player - {channel.DisplayName}" );
		player.NetworkSpawn( channel );

		// Notify any listeners that a player has spawned
		Scene.RunEvent<IPlayerSpawned>( x => x.OnPlayerSpawned( player.GetComponent<PlayerCharacter>() ) );

		var island = WorldManager.Island;

		if ( island.IsValid() )
		{
			var spawnPoint = island.GetEntrance( "spawn" );
			if ( spawnPoint.IsValid() )
			{
				player.GetComponent<PlayerCharacter>().SetLayer( island.Layer );
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

		_spawnQueue.Remove( channel );
	}

	[Authority]
	public void RequestSpawn( string playerId )
	{
		var caller = Rpc.Caller;
		Log.Info( $"Player '{caller.DisplayName}' has requested to spawn player '{playerId}'" );
		_spawnQueue.Add( caller );
	}

	public void OnHostPreInitialize( SceneFile scene )
	{
		Log.Info( "BOOT" );
		PlayerCharacter.SpawnPlayerId = null;
	}

	public void OnHostInitialize()
	{
		Log.Info( "BOOT" );
		PlayerCharacter.SpawnPlayerId = null;
	}

	public void OnClientInitialize()
	{
		Log.Info( "BOOT" );
		PlayerCharacter.SpawnPlayerId = null;
	}
}
