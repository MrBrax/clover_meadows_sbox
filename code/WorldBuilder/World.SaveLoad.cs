using System;
using System.Text.Json;
using Clover.Items;
using Clover.Persistence;

namespace Clover;

public sealed partial class World
{
	private string SaveFileName => $"{GameManager.Instance.SaveProfile}/worlds/{Data.ResourceName}.json";

	private WorldSaveData _saveData = new();

	public Action OnSave;

	public void Save()
	{
		Log.Info( $"Saving world {Data.ResourceName}" );
		
		Scene.RunEvent<IWorldSaved>( x => x.PreWorldSaved( this ) );

		var savedItems = new List<PersistentWorldItem>();

		foreach ( var item in Items )
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
		}
		
		var savedObjects = new List<PersistentWorldObject>();

		foreach ( var worldObject in Scene.GetAllComponents<WorldObject>().Where( x => x.WorldLayerObject.Layer == Layer ) )
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

		FileSystem.Data.CreateDirectory( $"{GameManager.Instance?.SaveProfile}/worlds" );

		// FileSystem.Data.WriteJson( $"worlds/{Data.ResourceName}.json", saveData );
		var json = JsonSerializer.Serialize( _saveData, GameManager.JsonOptions );
		FileSystem.Data.WriteAllText( SaveFileName, json );

		OnSave?.Invoke();
		Scene.RunEvent<IWorldSaved>( x => x.PostWorldSaved( this ) );
	}

	public void Load()
	{
		if ( !FileSystem.Data.FileExists( SaveFileName ) )
		{
			Log.Warning( $"File {SaveFileName} does not exist" );
			return;
		}

		var json = FileSystem.Data.ReadAllText( SaveFileName );
		var saveData = JsonSerializer.Deserialize<WorldSaveData>( json, GameManager.JsonOptions );

		Log.Info( $"Loaded save data from {SaveFileName}" );

		foreach ( var item in saveData.Items )
		{
			var position = item.Position;
			var placement = item.Placement;
			var rotation = item.Rotation;
			var prefabPath = item.PrefabPath;

			if ( string.IsNullOrEmpty( prefabPath ) )
			{
				Log.Warning( $"Item {item} has no prefab path" );
				continue;
			}

			var gameObject = Scene.CreateObject();
			gameObject.SetPrefabSource( prefabPath );
			gameObject.UpdateFromPrefab();

			var nodeLink = new WorldNodeLink( this, gameObject );

			nodeLink.GridPosition = position;
			nodeLink.GridPlacement = placement;
			nodeLink.GridRotation = rotation;
			nodeLink.ItemId = item.ItemId;
			nodeLink.PlacementType = item.PlacementType;

			if ( !Items.ContainsKey( position ) )
			{
				Items[position] = new Dictionary<ItemPlacement, WorldNodeLink>();
			}

			Items[position][placement] = nodeLink;

			_nodeLinkMap[nodeLink.Node] = nodeLink;

			nodeLink.OnNodeLoad( item );

			nodeLink.CalculateSize();

			// nodeLink.LoadItemData();
			UpdateTransform( nodeLink );

			foreach ( var pos in nodeLink.GetGridPositions( true ) )
			{
				_nodeLinkGridMap[$"{pos.x},{pos.y}:{nodeLink.GridPlacement}"] = nodeLink;
			}

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
