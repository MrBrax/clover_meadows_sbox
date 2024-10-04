using System;
using System.Text.Json;
using Clover.Persistence;

namespace Clover;

public sealed partial class World
{
	private string SaveFileName => $"worlds/{Data.ResourceName}.json";

	private WorldSaveData _saveData = new();

	public void Save()
	{
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

				/*var persistentItem = new PersistentWorldItem
				{
					Position = position,
					Placement = placement,
					Rotation = nodeLink.GridRotation,
					PlacementType = nodeLink.PlacementType,
					PrefabPath = prefabPath,
					ItemId = nodeLink.ItemId,
					Item = nodeLink.GetPersistentItem()
				};*/

				var persistentItem = nodeLink.OnNodeSave();

				savedItems.Add( persistentItem );
			}
		}


		// var saveData = new WorldSaveData { Name = Data.ResourceName, Items = savedItems, LastSave = DateTime.Now };

		_saveData.Items = savedItems;

		FileSystem.Data.CreateDirectory( "worlds" );

		// FileSystem.Data.WriteJson( $"worlds/{Data.ResourceName}.json", saveData );
		var json = JsonSerializer.Serialize( _saveData, GameManager.JsonOptions );
		FileSystem.Data.WriteAllText( SaveFileName, json );
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

			// nodeLink.LoadItemData();
			UpdateTransform( nodeLink );
		}

		Log.Info( $"Loaded {saveData.Items.Count} items" );

		_saveData = saveData;
	}
}
