using System;

namespace Clover;

public partial class WorldManager : Component
{
	
	public static WorldManager Instance { get; private set; }
	
	[Property] public List<World> Worlds { get; set; } = new();

	[Property] public int ActiveWorldIndex { get; set; }
	[Property] public World ActiveWorld => Worlds.ElementAtOrDefault( ActiveWorldIndex );
	
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

	public World GetWorld( string id )
	{
		return Worlds.FirstOrDefault( w => w.WorldId == id );
	}
	
	public void SetActiveWorld( int index )
	{
		Log.Info( $"Setting active world to index: {index}" );
		ActiveWorldIndex = index;
		RebuildVisibility();
		ActiveWorldChanged?.Invoke( ActiveWorld );
	}

	private void RebuildVisibility()
	{
		// rebuild world visibility
		for ( var i = 0; i < Worlds.Count; i++ )
		{
			var isVisible = i == ActiveWorldIndex;
			var world = Worlds[i];
			
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
			layerObject.RebuildVisibility();
		}
	}

	public void SetActiveWorld( World world )
	{
		ActiveWorldIndex = Worlds.IndexOf( world );
		RebuildVisibility();
	}

	protected override void OnStart()
	{
		if ( Worlds.Count == 0 )
		{
			// await LoadWorldAsync( "res://world/worlds/island.tres" );
			// await NodeManager.UserInterface.GetNode<Fader>( "Fade" ).FadeOutAsync();

			LoadWorld( ResourceLibrary.GetAll<Data.World>().First() );
		}

		// WorldLoaded += ( World world ) => NodeManager.SettingsSaveData.ApplyWorldSettings();

		/*WorldLoaded += ( world ) =>
		{
			SetupNewWorld();
		};*/
	}

	public World LoadWorld( Data.World data )
	{
		Log.Info( $"Loading world: {data.ResourceName}" );

		var index = Worlds.Count;

		var gameObject = data.Prefab.Clone();
		
		// gameObject.BreakFromPrefab();

		var world = gameObject.GetComponent<World>();
		world.Data = data; // already set

		gameObject.WorldPosition = new Vector3( new Vector3( 0, 0, index * WorldOffset ) );
		gameObject.Transform.ClearInterpolation();
		gameObject.SetParent( GameObject );

		gameObject.Tags.Add( "dworld" );
		gameObject.Tags.Add( $"dworldlayer_{index}" );

		Worlds.Add( world );
		
		world.Layer = index;
		world.Setup();
		
		Log.Info( $"Loaded world: {data.ResourceName}, now has {Worlds.Count} worlds." );
		
		RebuildVisibility();
		
		// ActiveWorldChanged?.Invoke( world );
		
		return world;
		
		// SetActiveWorld( index );
	}

	[ConCmd( "world_load" )]
	public static void LoadWorldCmd( string id )
	{
		var worldManager = NodeManager.WorldManager;
		var worldData = ResourceLibrary.GetAll<Data.World>().FirstOrDefault( w => w.ResourceName == id );
		if ( worldData != null )
		{
			worldManager.LoadWorld( worldData );
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
	
	[ConCmd("world_move_to_entrance")]
	public static void MoveToEntranceCmd( int worldIndex, string entranceId )
	{
		var world = WorldManager.Instance.Worlds.ElementAtOrDefault( worldIndex );
		if ( !world.IsValid() ) throw new Exception( $"Invalid world index: {worldIndex}" );
		
		var entrance = world.GetEntrance( entranceId );
		if ( entrance == null ) throw new Exception( $"Invalid entrance id: {entranceId}" );
		
		WorldManager.Instance.SetActiveWorld( worldIndex );
		
		var player = NodeManager.Player;
			
		player.WorldLayerObject.SetLayer( worldIndex, true );
		
		player.Transform.Position = entrance.Transform.Position;
		player.GetComponent<CameraController>().SnapCamera();
		
	}

	/*private void SetLoadingScreen( bool visible, string text = "" )
	{
		// TODO: make loading screen a class
		// NodeManager.UserInterface.GetNode<PanelContainer>( "LoadingScreen" ).Visible = visible;
		// NodeManager.UserInterface.GetNode<Label>( "LoadingScreen/MarginContainer/LoadingLabel" ).Text = text;
	}


	/// <summary>
	/// Loads a world from the specified world data path asynchronously.
	/// TODO: load persistent data asynchronously
	/// </summary>
	/// <param name="worldDataPath">The path to the world data.</param>
	/// <returns>A task representing the asynchronous operation. The task result is true if the world was loaded successfully, false otherwise.</returns>
	public async Task<bool> LoadWorldAsync( string worldDataPath )
	{
		if ( IsLoading )
		{
			Logger.LogError( "WorldManager", "Already loading a world." );
			return false;
		}

		SetLoadingScreen( true, $"Loading {worldDataPath}..." );

		// wait for loading screen to show
		await ToSignal( GetTree(), SceneTree.SignalName.ProcessFrame );

		if ( ActiveWorld != null )
		{
			EmitSignal( SignalName.WorldUnload, ActiveWorld );
			ActiveWorld.Unload();
			ActiveWorld.QueueFree();
			ActiveWorld = null;
		}

		// WorldChanged?.Invoke();

		Logger.Info( "WorldManager", "Waiting for old world to be freed." );
		await ToSignal( GetTree(), SceneTree.SignalName.ProcessFrame );

		Logger.Info( "WorldManager", "Waited for old world to be freed, hopefully it's gone now." );

		// clear loaded resources
		Loader.ClearLoadedResources();

		CurrentWorldDataPath = worldDataPath;

		if ( ResourceLoader.HasCached( CurrentWorldDataPath ) )
		{
			Logger.Info( "WorldManager", "Loading world data from cache." );
			var resource = Loader.LoadResource<WorldData>( CurrentWorldDataPath );
			if ( resource is WorldData worldData )
			{
				SetupNewWorld( worldData );
			}
			else
			{
				Logger.LogError( "WorldManager", $"Failed to load world data: {CurrentWorldDataPath}" );
				IsLoading = false;
				SetLoadingScreen( false );
			}

			return true;
		}

		Logger.Info( "WorldManager", "Loading world data threaded..." );
		var error = ResourceLoader.LoadThreadedRequest( CurrentWorldDataPath );
		if ( error != Error.Ok )
		{
			Logger.LogError( "WorldManager", $"Failed to load world data: {CurrentWorldDataPath} ({error})" );
			IsLoading = false;
			SetLoadingScreen( false );
			return false;
		}

		Logger.Info( "WorldManager", $"World data loading response: {error}" );
		IsLoading = true;

		// wait for the world to load
		await ToSignal( this, SignalName.WorldLoaded );

		Logger.Info( "WorldManager", "World loaded." );

		SetLoadingScreen( false );

		return true;

	}

	public override void _Process( double delta )
	{
		base._Process( delta );

		if ( !IsLoading )
		{
			return;
		}

		// check if world data is loaded
		if ( !string.IsNullOrWhiteSpace( CurrentWorldDataPath ) && !IsInstanceValid( ActiveWorld ) )
		{
			var status = ResourceLoader.LoadThreadedGetStatus( CurrentWorldDataPath, LoadingProgress );
			if ( status == ResourceLoader.ThreadLoadStatus.Loaded )
			{
				var resource = ResourceLoader.LoadThreadedGet( CurrentWorldDataPath );
				if ( resource is WorldData worldData )
				{
					SetupNewWorld( worldData );
				}
			}
			else if ( status == ResourceLoader.ThreadLoadStatus.Failed )
			{
				Logger.LogError( "WorldManager", $"Failed to load world data: {CurrentWorldDataPath}" );
				IsLoading = false;
				SetLoadingScreen( true, "Failed to load world data." );
			}
			else if ( status == ResourceLoader.ThreadLoadStatus.InvalidResource )
			{
				Logger.LogError( "WorldManager", $"Invalid resource: {CurrentWorldDataPath}" );
				IsLoading = false;
				SetLoadingScreen( true, "Invalid resource." );
			}
			else
			{
				// Logger.Info( "World data not loaded yet." );
				if ( LoadingProgress.Count > 0 )
				{
					var progress = (float)LoadingProgress[0] * 100f;
					SetLoadingScreen( true, $"Loading {CurrentWorldDataPath} ({progress:0.0}%)" );
				}
			}
		}
	}



	private async void SetupNewWorld( WorldData worldData )
	{
		/*if ( worldData == null )
		{
			throw new System.Exception( "World data is null." );
			return;
		}

		if ( worldData.WorldScene == null )
		{
			throw new System.Exception( "World scene is null." );
			return;
		}#1#

		Logger.Info( "WorldManager", "Loading new world." );

		ActiveWorld = worldData.WorldScene.Instantiate<World>();
		ActiveWorld.WorldId = worldData.WorldId;
		ActiveWorld.WorldName = worldData.WorldName;
		ActiveWorld.WorldPath = worldData.ResourcePath;
		ActiveWorld.GridWidth = worldData.Width;
		ActiveWorld.GridHeight = worldData.Height;
		ActiveWorld.UseAcres = worldData.UseAcres;

		Logger.Debug( "WorldManager", "Adding new world to scene." );
		AddChild( ActiveWorld );

		// Logger.Info( "WorldManager", "Checking terrain." );
		// ActiveWorld.CheckTerrain();

		Logger.Debug( "WorldManager", "Setup interior collisions." );
		ActiveWorld.SetupInteriorCollisions();

		Logger.Debug( "WorldManager", "Loading editor placed items." );
		ActiveWorld.LoadEditorPlacedItems();

		Logger.Debug( "WorldManager", "Loading world data." );
		await ActiveWorld.LoadAsync();

		Logger.Debug( "WorldManager", "Load interiors." );
		ActiveWorld.LoadInteriors();

		Logger.Debug( "WorldManager", "Activate classes." );
		ActiveWorld.ActivateClasses();

		Logger.Info( "WorldManager", "World loaded." );
		IsLoading = false;
		NodeManager.UserInterface.GetNode<PanelContainer>( "LoadingScreen" ).Hide();
		// WorldLoaded?.Invoke( ActiveWorld );
		// WorldLoaded
		EmitSignal( SignalName.WorldLoaded, ActiveWorld );
	}*/
}