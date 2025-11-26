using System;
using System.Text.Json.Serialization;
using Clover.Components;
using Clover.Data;
using Clover.Items;
using Clover.Persistence;

namespace Clover;

/// <summary>
///  This class represents a link between a world and an item in the world.
///  All items placed on the grid in a world have one of these associated with them.
/// </summary>
/*
public class WorldNodeLink : IValid
{
	[JsonIgnore] public World World { get; set; }
	[JsonIgnore] public GameObject Node { get; set; }

	public Vector2Int GridPosition => World.WorldToItemGrid( Node.WorldPosition );

	public World.ItemRotation GridRotation =>
		World.GetItemRotationFromDirection( World.Get4Direction( Node.WorldRotation ) );

	public Vector3 WorldPosition => Node.WorldPosition;
	public Rotation WorldRotation => Node.WorldRotation;

	// public World.ItemPlacement GridPlacement;
	public World.ItemPlacementType PlacementType { get; set; }

	public Vector2Int Size { get; set; }

	public string ItemId { get; set; }
	// public string PrefabPath;

	public PlaceableNode PlacedOn { get; set; }

	[Icon( "save" )] private PersistentItem Persistence { get; set; }

	public ItemData ItemData => ItemData.Get( ItemId );

	public bool IsBeingPickedUp { get; set; }

	// public bool IsDroppedItem => PrefabPath == "items/misc/dropped_item/dropped_item.prefab";

	public bool IsValid => World != null && World.HasNodeLink( this );

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

		if ( PlacementType == World.ItemPlacementType.Dropped )
		{
			return new List<Vector2Int> { global ? GridPosition : Vector2Int.Zero };
		}

		return itemData.GetGridPositions( GridRotation, GridPosition );
	}

	public bool ShouldBeSaved()
	{
		/#1#/ return true;
		if ( Node is IWorldItem worldItem )
		{
			return worldItem.ShouldBeSaved();
		}#1#

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
		// return GetPlaceableNodes().FirstOrDefault( n => GridPosition == World.WorldToItemGrid( n.WorldPosition ) );
		return GetPlaceableNodes().MinBy( n => (World.WorldToItemGrid( n.WorldPosition ) - position).LengthSquared );
	}

	/*public PersistentItem GetPersistentItem()
	{
		var persistentItem = new PersistentItem
		{
			ItemId = ItemId,
		};

		return persistentItem;
	}#1#

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

		/*foreach ( var saveable in Node.Components.GetAll<ISaveData>( FindMode.EverythingInSelfAndDescendants ) )
		{
			saveable.OnSave( this );
		}

		foreach ( var persistent in Node.Components.GetAll<IPersistent>( FindMode.EverythingInSelfAndDescendants ) )
		{
			persistent.OnSave( Persistence );
		}#1#

		RunSavePersistence( Persistence );

		return new PersistentWorldItem
		{
			Position = GridPosition,
			Rotation = GridRotation,
			WPosition = WorldPosition.SnapToGrid( 1 ),
			WAngles = WorldRotation.Angles().SnapToGrid( 1 ),
			// Placement = GridPlacement,
			PlacementType = PlacementType,
			// PrefabPath = PrefabPath,
			ItemId = ItemId,
			Item = Persistence,
		};
	}

	public void RunSavePersistence( PersistentItem item )
	{
		var components = Node.GetComponents<Component>();

		var keys = new List<string>();

		foreach ( var component in components )
		{
			var properties = TypeLibrary.GetPropertyDescriptions( component );

			foreach ( var property in properties )
			{
				var saveDataAttribute = property.GetCustomAttribute<SaveDataAttribute>();
				if ( saveDataAttribute == null )
				{
					// XLog.Debug( this, $"No save data attribute on {property.Name} on {component}" );
					continue;
				}

				var keyName = !string.IsNullOrEmpty( saveDataAttribute.Key ) ? saveDataAttribute.Key : property.Name;

				if ( keys.Contains( keyName ) )
				{
					Log.Error( $"Duplicate arbitrary data key {keyName} on {component}" );
					continue;
				}

				var type = property.PropertyType;

				var value = property.GetValue( component );

				// XLog.Info( this,
				// 	$"Saving arbitrary data {keyName} = {value}, type {type}/{value?.GetType()}" );

				// Log.Info( $"Saving '{keyName}' = '{value}' on {component}" );

				item.SetSaveData( keyName, value, type );
				// XLog.Info( this, $"Saving arbitrary data {keyName} = {value}" );
				keys.Add( keyName );

				// Log.Info( $"Saved '{keyName}' = '{value}' on {component}" );
			}
		}

		foreach ( var persistent in Node.Components.GetAll<IPersistent>( FindMode.EverythingInSelfAndDescendants ) )
		{
			persistent.OnSave( Persistence );
		}

		if ( Node.Components.TryGet<WorldItem>( out var worldItem ) )
		{
			worldItem.OnItemSave?.Invoke( this );
		}
	}

	public void RunLoadPersistence( PersistentItem item )
	{
		var components = Node.GetComponents<Component>();

		foreach ( var component in components )
		{
			var properties = TypeLibrary.GetPropertyDescriptions( component );

			foreach ( var property in properties )
			{
				var saveDataAttribute = property.GetCustomAttribute<SaveDataAttribute>();
				if ( saveDataAttribute == null )
				{
					continue;
				}

				if ( !string.IsNullOrEmpty( saveDataAttribute.Key ) )
				{
					// first try using the key from the attribute
					var keyData = item.GetSaveData( property.PropertyType, saveDataAttribute.Key );
					if ( keyData != null )
					{
						property.SetValue( component, keyData );
						continue;
					}
				}

				// if that fails, try using the property name
				// we do this to maintain backwards compatibility with old saves that don't have the key set
				var propertyData = item.GetSaveData( property.PropertyType, property.Name );
				if ( propertyData != null )
				{
					property.SetValue( component, propertyData );
				}
			}
		}

		foreach ( var persistent in Node.Components.GetAll<IPersistent>( FindMode.EverythingInSelfAndDescendants ) )
		{
			persistent.OnLoad( Persistence );
		}

		if ( Node.Components.TryGet<WorldItem>( out var worldItem ) )
		{
			worldItem.OnItemLoad?.Invoke( this );
		}
	}

	public void OnNodeLoad( PersistentWorldItem persistentItem )
	{
		// PrefabPath = persistentItem.PrefabPath;
		ItemId = persistentItem.ItemId;

		Persistence = persistentItem.Item;

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

	/// <summary>
	///  Removes this item from the world.
	/// </summary>
	public void Remove()
	{
		World.RemoveItem( this );
	}
}
*/
