using System;
using System.Threading.Tasks;
using Clover.Data;
using Sandbox.Diagnostics;

namespace Clover;

public class WorldManager : Component
{
	public static WorldManager Instance { get; private set; }
	
	public static World Island => Instance.GetWorld( "island" );

	// [Property] public List<World> Worlds { get; set; } = new();
	[Property, Sync, Change] public NetDictionary<int, World> Worlds { get; set; } = new();

	[Property] public int ActiveWorldIndex { get; set; }
	[Property] public World ActiveWorld => GetWorld( ActiveWorldIndex );
	
	[Property] public WorldData DefaultWorldData { get; set; }

	public delegate void WorldUnloadEventHandler( World world );

	public delegate void WorldLoadedEventHandler( World world );

	public delegate void ActiveWorldChangedEventHandler( World world );


	[Property] public WorldLoadedEventHandler WorldLoaded { get; set; }
	[Property] public WorldUnloadEventHandler WorldUnload { get; set; }
	[Property] public ActiveWorldChangedEventHandler ActiveWorldChanged { get; set; }


	public string CurrentWorldDataPath { get; set; }

	public bool IsLoading;

	public const float WorldOffset = 1000;

	// public Array LoadingProgress { get; set; } = new Array();

	protected override void OnAwake()
	{
		base.OnAwake();
		Instance = this;
	}
	
	public void OnWorldsChanged()
	{
		Log.Info( "Worlds changed." );
		RebuildVisibility();
	}

	public World GetWorld( string id )
	{
		var val = Worlds.Values.FirstOrDefault( w => w.Data.ResourceName == id );
		if ( !val.IsValid() )
		{
			Log.Warning( $"World not found: {id}, searching scene..." );
			val = Scene.GetAllComponents<World>().FirstOrDefault( w => w.Data.ResourceName == id );
		}

		return val;
	}

	public World GetWorld( int index )
	{
		// return Worlds.TryGetValue( index, out var world ) ? world : null;
		var val = Worlds.Values.FirstOrDefault( w => w.Layer == index );
		if ( !val.IsValid() )
		{
			Log.Warning( $"World not found at index: {index}, searching scene..." );
			val = Scene.GetAllComponents<World>().FirstOrDefault( w => w.Layer == index );
		}
		
		return val;
	}

	public void SetActiveWorld( int index )
	{
		Log.Info( $"Setting active world to index: {index}" );
		ActiveWorldIndex = index;
		RebuildVisibility();
		ActiveWorldChanged?.Invoke( ActiveWorld );
		Scene.RunEvent<IWorldEvent>( x => x.OnWorldChanged( ActiveWorld ) );
	}

	private void RebuildVisibility()
	{
		if ( Worlds.Count == 0 )
		{
			Log.Warning( "No worlds to rebuild visibility for." );
			return;
		}
		
		Log.Info( $"Rebuilding world visibility for {Worlds.Count} worlds..." );
		
		// rebuild world visibility
		for ( var i = 0; i < Worlds.Count; i++ )
		{
			var isVisible = i == ActiveWorldIndex;
			var world = Worlds.TryGetValue( i, out var w ) ? w : null;
			if ( world == null )
			{
				Log.Warning( $"World not found at index: {i}" );
				continue;
			}

			world.Tags.Remove( "worldlayer_invisible" );
			world.Tags.Remove( "worldlayer_visible" );

			if ( isVisible )
			{
				world.Tags.Add( "worldlayer_visible" );
			}
			else
			{
				world.Tags.Add( "worldlayer_invisible" );
			}
		}

		// rebuild object visibility
		foreach ( var layerObject in Scene.GetAllComponents<WorldLayerObject>() )
		{
			layerObject.RebuildVisibility( layerObject.Layer);
		}
	}

	public void SetActiveWorld( World world )
	{
		ActiveWorldIndex = world.Layer;
		RebuildVisibility();
	}

	protected override void OnStart()
	{
		/*if ( Worlds.Count == 0 )
		{
			// await LoadWorldAsync( "res://world/worlds/island.tres" );
			// await NodeManager.UserInterface.GetNode<Fader>( "Fade" ).FadeOutAsync();

			_ = LoadWorld( DefaultWorldData );
		}*/

		// WorldLoaded += ( World world ) => NodeManager.SettingsSaveData.ApplyWorldSettings();

		/*WorldLoaded += ( world ) =>
		{
			SetupNewWorld();
		};*/
	}

	public bool HasWorld( string id )
	{
		return Worlds.Values.Any( w => w.Data.ResourceName == id );
	}

	public bool HasWorld( WorldData data )
	{
		return Worlds.Values.Any( w => w.Data == data );
	}

	public async Task<World> LoadWorld( WorldData data )
	{
		Log.Info( $"Loading world: {data.ResourceName}" );

		// use the first available index
		var index = 0;
		while ( Worlds.ContainsKey( index ) )
		{
			index++;
		}

		var gameObject = data.Prefab.Clone();

		// gameObject.BreakFromPrefab();

		var world = gameObject.GetComponent<World>();
		world.Data = data; // already set
		world.Layer = index;

		gameObject.WorldPosition = new Vector3( new Vector3( 0, 0, index * WorldOffset ) );
		gameObject.Transform.ClearInterpolation();
		gameObject.SetParent( GameObject );

		gameObject.Tags.Add( "dworld" );
		gameObject.Tags.Add( $"dworldlayer_{index}" );

		gameObject.NetworkMode = NetworkMode.Object;
		gameObject.NetworkSpawn();
		
		Worlds[index] = world;

		world.Setup();

		world.Load();

		Log.Info( $"Loaded world: {data.ResourceName}, now has {Worlds.Count} worlds." );

		RebuildVisibility();

		// ActiveWorldChanged?.Invoke( world );
		
		OnWorldLoadedRpc( data.ResourceName );

		// return dummy task to shut up the compiler

		await Task.Frame();
		
		return world;

	}

	public async Task<World> GetWorldOrLoad( WorldData data )
	{
		Assert.NotNull( data, "World data is null." );
		var world = GetWorld( data.ResourceName );
		return world.IsValid() ? world : await LoadWorld( data );
	}
	
	[Authority]
	public void RequestLoadWorld( string id )
	{
		var worldData = ResourceLibrary.GetAll<WorldData>().FirstOrDefault( w => w.ResourceName == id );
		if ( worldData != null )
		{
			_ = LoadWorld( worldData );
		}
		else
		{
			Log.Warning( $"Could not find world with id: {id}" );
		}
	}

	[Broadcast( NetPermission.HostOnly )]
	public void OnWorldLoadedRpc( string id )
	{
		Log.Info( $"World loaded: {id}" );
		var world = GetWorld( id );
		
		if ( !world.IsValid() )
		{
			Log.Error( $"World not found: {id}" );
			return;
		}
		
		WorldLoaded?.Invoke( world );
		Scene.RunEvent<IWorldEvent>( x => x.OnWorldLoaded( world ) );
		world.OnWorldLoaded();

		foreach ( var world2 in Worlds )
		{
			Log.Info( $"World #{world2.Key}: {world2.Value.Data.ResourceName}" );
		}
		
	}


	public void UnloadWorld( string id )
	{
		var world = GetWorld( id );
		if ( world.IsValid() )
		{
			UnloadWorld( world );
		}
	}

	public void UnloadWorld( World world )
	{
		Log.Info( $"Unloading world: {world.Data.ResourceName}" );
		world.OnWorldUnloaded();
		world.DestroyGameObject();
		Worlds.Remove( world.Layer );
		RebuildVisibility();
		WorldUnload?.Invoke( world );
		Scene.RunEvent<IWorldEvent>( x => x.OnWorldUnloaded( world ) );
	}

	public void UnloadWorld( int index )
	{
		var world = GetWorld( index );
		if ( world.IsValid() )
		{
			UnloadWorld( world );
		}
	}


	[ConCmd( "world_load" )]
	public static void LoadWorldCmd( string id )
	{
		var worldManager = NodeManager.WorldManager;
		var worldData = ResourceLibrary.GetAll<WorldData>().FirstOrDefault( w => w.ResourceName == id );
		if ( worldData != null )
		{
			_ = worldManager.LoadWorld( worldData );
		}
		else
		{
			Log.Warning( $"Could not find world with id: {id}" );
		}
	}

	[ConCmd( "world_set_active" )]
	public static void SetActiveWorldCmd( int index )
	{
		NodeManager.WorldManager.SetActiveWorld( index );
	}

	[ConCmd( "world_move_to_entrance" )]
	public static void MoveToEntranceCmd( int worldIndex, string entranceId )
	{
		var world = Instance.GetWorld( worldIndex );
		if ( !world.IsValid() ) throw new Exception( $"Invalid world index: {worldIndex}" );

		var entrance = world.GetEntrance( entranceId );
		if ( entrance == null ) throw new Exception( $"Invalid entrance id: {entranceId}" );

		Instance.SetActiveWorld( worldIndex );

		var player = NodeManager.Player;

		player.WorldLayerObject.SetLayer( worldIndex, true );

		player.WorldPosition = entrance.WorldPosition;
		player.GetComponent<CameraController>().SnapCamera();
	}
	
	[ConCmd( "world_save_all" )]
	public static void SaveAllCmd()
	{
		foreach ( var world in Instance.Worlds.Values )
		{
			world.Save();
		}
	}
	
	public WorldNodeLink GetWorldNodeLink( GameObject gameObject )
	{
		foreach ( var world in Worlds.Values )
		{
			var link = world.GetItem( gameObject );
			if ( link != null )
			{
				return link;
			}
		}
		
		return null;
		
	}
}

public interface IWorldEvent
{
	void OnWorldLoaded( World world ) { }
	void OnWorldUnloaded( World world ) { }
	void OnWorldChanged( World world ) { }
}
