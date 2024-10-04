using System;
using System.Text.Json.Serialization;
using Clover.Components;
using Clover.Data;
using Clover.Items;
using Clover.Persistence;

namespace Clover;

public class WorldNodeLink
{
	[JsonIgnore] public World World;
	[JsonIgnore] public GameObject Node;

	public Vector2Int GridPosition;

	public World.ItemRotation GridRotation;
	public World.ItemPlacement GridPlacement;
	public World.ItemPlacementType PlacementType;

	public Vector2Int Size;

	public string ItemId;
	public string PrefabPath;

	[Icon( "save" )]
	public PersistentItem Persistence;

	public ItemData ItemData
	{
		get => ResourceLibrary.GetAll<ItemData>().FirstOrDefault( x => x.ResourceName == ItemId );
	}

	public WorldNodeLink( World world, GameObject item )
	{
		World = world;
		Node = item;
		// GetData( node );
		// LoadItemData();
	}

	public IList<Vector2Int> GetGridPositions( bool global = false )
	{
		var itemData = ItemData;

		if ( itemData == null )
		{
			throw new Exception( $"Item data not found on {this}" );
		}

		return itemData.GetGridPositions( GridRotation, GridPosition );
	}

	public bool ShouldBeSaved()
	{
		/*// return true;
		if ( Node is IWorldItem worldItem )
		{
			return worldItem.ShouldBeSaved();
		}*/

		return true;
	}

	public void DestroyNode()
	{
		Node.Destroy();
	}


	public string GetName()
	{
		return ItemData != null ? ItemData.Name : Node.Name;
	}

	// public IList<PlaceableNode> GetPlaceableNodes() => Node.GetNodesOfType<PlaceableNode>();
	public IEnumerable<PlaceableNode> GetPlaceableNodes() =>
		Node.Components.GetAll<PlaceableNode>( FindMode.EverythingInDescendants );

	public PlaceableNode GetPlaceableNodeAtGridPosition( Vector2Int position )
	{
		return GetPlaceableNodes().FirstOrDefault( n => GridPosition == World.WorldToItemGrid( n.WorldPosition ) );
	}

	/*public PersistentItem GetPersistentItem()
	{
		var persistentItem = new PersistentItem
		{
			ItemId = ItemId,
		};

		return persistentItem;
	}*/
	
	public T GetPersistence<T>() where T : PersistentItem
	{
		if ( Persistence is T t )
		{
			return t;
		}
		
		return null;
	}

	public static WorldNodeLink Get( GameObject gameObject )
	{
		return WorldManager.Instance.GetWorldNodeLink( gameObject );
	}

	public PersistentWorldItem OnNodeSave()
	{
		if ( Node.Components.TryGet<WorldItem>( out var worldItem ) )
		{
			Log.Info( $"Running OnItemSave on {Node}" );
			worldItem.OnItemSave?.Invoke( this );
		}
		else
		{
			Log.Warning( $"No WorldItem component found on {Node}" );
		}

		return new PersistentWorldItem
		{
			Position = GridPosition,
			Placement = GridPlacement,
			Rotation = GridRotation,
			PlacementType = PlacementType,
			PrefabPath = PrefabPath,
			ItemId = ItemId,
			Item = Persistence,
		};
	}

	public void OnNodeLoad( PersistentWorldItem persistentItem )
	{
		PrefabPath = persistentItem.PrefabPath;
		ItemId = persistentItem.ItemId;

		Persistence = persistentItem.Item;

		if ( Node.Components.TryGet<WorldItem>( out var worldItem ) )
		{
			Log.Info( $"Running OnItemLoad on {Node}" );
			worldItem.WorldLayerObject.SetLayer( World.Layer );
			worldItem.OnItemLoad?.Invoke( this );
		}
		else
		{
			Log.Warning( $"No WorldItem component found on {Node}" );
		}
	}

	public void OnNodeAdded()
	{
		if ( Node.Components.TryGet<WorldItem>( out var worldItem ) )
		{
			Log.Info( $"Running OnItemAdded on {Node}" );
			// worldItem.OnItemAdded?.Invoke( this );
		}

		if ( Persistence == null )
		{
			Persistence = new PersistentItem();
		}
	}

	public string GetPrefabPath()
	{
		var prefabPath = Node.PrefabInstanceSource;
		if ( string.IsNullOrEmpty( prefabPath ) )
		{
			if ( Node.Components.TryGet<WorldItem>( out var worldObject ) )
			{
				prefabPath = worldObject.Prefab;
				if ( string.IsNullOrEmpty( prefabPath ) )
				{
					Log.Warning( $"NodeLink {this} has no prefab path" );
					return null;
				}
			}
		}

		return prefabPath;
	}
}
