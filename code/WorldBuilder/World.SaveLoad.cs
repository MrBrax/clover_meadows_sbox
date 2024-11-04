using System;
using System.Text.Json;
using System.Threading.Tasks;
using Clover.Items;
using Clover.Persistence;

namespace Clover;

public sealed partial class World
{
	private string SaveFileName => $"{RealmManager.CurrentRealm.Path}/worlds/{Data.ResourceName}.json";

	private WorldSaveData _saveData = new();

	public Action OnSave;

	public void Save()
	{
		if ( IsProxy )
		{
			Log.Error( "Cannot save proxy world. Fix this call." );
			return;
		}

		Log.Info( $"Saving world {Data.ResourceName}" );

		Scene.RunEvent<IWorldSaved>( x => x.PreWorldSaved( this ) );

		var savedItems = new List<PersistentWorldItem>();

		/*foreach ( var item in Items )
		{
			var position = item.Key;
			foreach ( var itemEntry in item.Value )
			{
				var placement = itemEntry.Key;
				var nodeLink = itemEntry.Value;

				if ( !nodeLink.ShouldBeSaved() )
				{
					Log.Info( $"Skipping {nodeLink} at {position}" );
					continue;
				}

				var prefabPath = nodeLink.GetPrefabPath();
				nodeLink.PrefabPath = prefabPath;

				var persistentItem = nodeLink.OnNodeSave();

				savedItems.Add( persistentItem );
			}
		}*/

		var items = Items.ToList();

		foreach ( var nodeLink in items )
		{
			if ( !nodeLink.ShouldBeSaved() )
			{
				Log.Info( $"Skipping {nodeLink}" );
				continue;
			}

			var prefabPath = nodeLink.GetPrefabPath();
			nodeLink.PrefabPath = prefabPath;

			var persistentItem = nodeLink.OnNodeSave();

			savedItems.Add( persistentItem );
		}


		var savedObjects = new List<PersistentWorldObject>();

		foreach ( var worldObject in Scene.GetAllComponents<WorldObject>()
			         .Where( x => x.WorldLayerObject.Layer == Layer ) )
		{
			Log.Info( $"Saving object {worldObject}" );
			var persistentObject = worldObject.OnObjectSave();
			savedObjects.Add( persistentObject );
		}

		// var saveData = new WorldSaveData { Name = Data.ResourceName, Items = savedItems, LastSave = DateTime.Now };

		if ( _saveData == null )
		{
			Log.Warning( "Save data is null, creating new instance. Should probably do this when starting the game" );
			_saveData = new WorldSaveData();
		}

		_saveData.LastSave = DateTime.Now;

		Log.Info( $"Saving {savedItems.Count} items" );
		_saveData.Items = savedItems;

		Log.Info( $"Saving {savedObjects.Count} objects" );
		_saveData.Objects = savedObjects;

		FileSystem.Data.CreateDirectory( $"{RealmManager.CurrentRealm.Path}/worlds" );

		Log.Info( $"Writing save data to {SaveFileName}" );

		// FileSystem.Data.WriteJson( $"worlds/{Data.ResourceName}.json", saveData );
		var json = JsonSerializer.Serialize( _saveData, GameManager.JsonOptions );
		FileSystem.Data.WriteAllText( SaveFileName, json );

		OnSave?.Invoke();
		Scene.RunEvent<IWorldSaved>( x => x.PostWorldSaved( this ) );
	}

	public async Task Load()
	{
		Log.Info( $"Loading world {Data.ResourceName} async..." );

		if ( RealmManager.CurrentRealm == null )
		{
			Log.Error( "Current realm is null" );
			return;
		}

		if ( string.IsNullOrEmpty( RealmManager.CurrentRealm.Path ) )
		{
			Log.Error( "Current realm path is null or empty" );
			return;
		}

		if ( string.IsNullOrEmpty( SaveFileName ) )
		{
			Log.Error( "Save file name is null or empty" );
			return;
		}

		if ( !FileSystem.Data.FileExists( SaveFileName ) )
		{
			Log.Warning( $"File {SaveFileName} does not exist" );
			return;
		}

		var json = await FileSystem.Data.ReadAllTextAsync( SaveFileName );
		var saveData = JsonSerializer.Deserialize<WorldSaveData>( json, GameManager.JsonOptions );

		Log.Info( $"Loaded save data from {SaveFileName}" );

		foreach ( var item in saveData.Items )
		{
			var position = item.Position;
			var rotation = item.Rotation;
			var prefabPath = item.PrefabPath;

			if ( string.IsNullOrEmpty( prefabPath ) )
			{
				Log.Warning( $"Item {item} has no prefab path" );
				continue;
			}

			Log.Info( $"Loading item {item.PrefabPath}" );

			if ( !string.IsNullOrEmpty( item.Item.PackageIdent ) )
			{
				var package = await Package.Fetch( item.Item.PackageIdent, false );
				if ( package == null )
				{
					Log.Warning( $"Could not fetch package {item.Item.PackageIdent}" );
					continue;
				}

				Log.Info( $"Fetched package {package.Title}" );
			}

			var gameObject = Scene.CreateObject();
			gameObject.SetPrefabSource( prefabPath );
			gameObject.UpdateFromPrefab();

			gameObject.WorldPosition = item.WPosition;
			gameObject.WorldRotation = item.WAngles;

			var nodeLink = new WorldNodeLink( this, gameObject )
			{
				ItemId = item.ItemId, PlacementType = item.PlacementType
			};

			nodeLink.OnNodeLoad( item );

			Items.Add( nodeLink );

			gameObject.SetParent( GameObject ); // TODO: should items be parented to the world?

			gameObject.NetworkSpawn();
		}

		Log.Info( $"Loaded {saveData.Items.Count} items" );

		foreach ( var worldObject in saveData.Objects )
		{
			Log.Info( $"Loading object {worldObject}" );
			var gameObject = Scene.CreateObject();
			gameObject.SetPrefabSource( worldObject.PrefabPath );
			gameObject.UpdateFromPrefab();

			if ( !gameObject.Components.TryGet<WorldObject>( out var worldObjectComponent ) )
			{
				Log.Warning( $"No WorldObject component found on {gameObject}" );
				gameObject.Destroy();
				continue;
			}

			worldObjectComponent.WorldLayerObject.SetLayer( Layer );
			worldObjectComponent.OnObjectLoad( worldObject );

			gameObject.SetParent( GameObject ); // TODO: should items be parented to the world?

			gameObject.NetworkSpawn();
		}

		Log.Info( $"Loaded {saveData.Objects.Count} objects" );

		_saveData = saveData;
	}
}

interface IWorldSaved
{
	void PreWorldSaved( World world );
	void PostWorldSaved( World world );
}
