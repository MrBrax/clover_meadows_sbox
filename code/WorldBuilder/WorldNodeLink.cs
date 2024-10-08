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
	private PersistentItem Persistence { get; set; }

	public ItemData ItemData
	{
		get => ResourceLibrary.GetAll<ItemData>().FirstOrDefault( x => x.ResourceName == ItemId );
	}

	public bool IsBeingPickedUp { get; set; }

	public bool IsDroppedItem => PrefabPath == "items/misc/dropped_item/dropped_item.prefab";

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
			throw new Exception( $"Item data not found on {this} ({ItemId})" );
		}

		if ( IsDroppedItem )
		{
			return new List<Vector2Int> { global ? GridPosition : Vector2Int.Zero };
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
	
	public void SetPersistence( PersistentItem persistentItem )
	{
		Persistence = persistentItem;
	}
	
	[Pure, Icon( "save" )]
	public PersistentItem GetPersistence()
	{
		return Persistence ??= new PersistentItem();
	}
	
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
			Log.Trace( $"Running OnItemSave on {Node}" );
			worldItem.OnItemSave?.Invoke( this );
		}
		else
		{
			Log.Warning( $"No WorldItem component found on {Node}" );
		}
		
		Persistence.ItemId ??= ItemId;
		
		foreach ( var saveable in Node.Components.GetAll<ISaveData>( FindMode.EverythingInSelfAndDescendants ) )
		{
			saveable.OnSave( this );
		}
		
		foreach ( var persistent in Node.Components.GetAll<IPersistent>( FindMode.EverythingInSelfAndDescendants ) )
		{
			persistent.OnSave( Persistence );
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
	
	public void RefreshPersistence()
	{
		foreach ( var saveable in Node.Components.GetAll<ISaveData>( FindMode.EverythingInSelfAndDescendants ) )
		{
			saveable.OnSave( this );
		}
		
		foreach ( var persistent in Node.Components.GetAll<IPersistent>( FindMode.EverythingInSelfAndDescendants ) )
		{
			persistent.OnSave( Persistence );
		}
	}

	public void OnNodeLoad( PersistentWorldItem persistentItem )
	{
		PrefabPath = persistentItem.PrefabPath;
		ItemId = persistentItem.ItemId;

		Persistence = persistentItem.Item;
		
		foreach ( var saveable in Node.Components.GetAll<ISaveData>( FindMode.EverythingInSelfAndAncestors ) )
		{
			saveable.OnLoad( this );
		}
		
		foreach ( var persistent in Node.Components.GetAll<IPersistent>( FindMode.EverythingInSelfAndAncestors ) )
		{
			persistent.OnLoad( Persistence );
		}

		if ( Node.Components.TryGet<WorldItem>( out var worldItem ) )
		{
			// Log.Info( $"Running OnItemLoad on {Node}" );
			worldItem.WorldLayerObject.SetLayer( World.Layer );
			worldItem.OnItemLoad?.Invoke( this );
		}
		else
		{
			Log.Warning( $"No WorldItem component found on {Node}" );
		}
		
		Node.Name = GetName();
	}

	public void OnNodeAdded()
	{
		if ( Node.Components.TryGet<WorldItem>( out var worldItem ) )
		{
			Log.Trace( $"Running OnItemAdded on {Node}" );
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

	public void CalculateSize()
	{
		var gridPositions = GetGridPositions();
		var minX = gridPositions.Min( p => p.x );
		var minY = gridPositions.Min( p => p.y );
		var maxX = gridPositions.Max( p => p.x );
		var maxY = gridPositions.Max( p => p.y );
		
		Size = new Vector2Int( maxX - minX + 1, maxY - minY + 1 );
		
		// Log.Info( $"Calculated size for {this}: {Size}" );
		
	}

	public void Remove()
	{
		World.RemoveItem( this );
	}
}
