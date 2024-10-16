using System;
using Clover.Data;
using Clover.Items;
using Clover.Persistence;
using Sandbox.Diagnostics;

namespace Clover;

public sealed partial class World
{
	public WorldNodeLink SpawnPlacedNode( ItemData itemData, Vector2Int position, ItemRotation rotation,
		ItemPlacement placement )
	{
		return SpawnNode( itemData, ItemPlacementType.Placed, position, rotation, placement );
	}

	public WorldNodeLink SpawnPlacedNode( PersistentItem persistentItem, Vector2Int position, ItemRotation rotation,
		ItemPlacement placement )
	{
		var itemData = persistentItem.ItemData;
		var nodeLink = SpawnPlacedNode( itemData, position, rotation, placement );
		nodeLink.SetPersistence( persistentItem );
		nodeLink.RunLoadPersistence();
		return nodeLink;
	}

	public WorldNodeLink SpawnPlacedNode( ItemData itemData, Vector3 position, Rotation rotation,
		ItemPlacement placement )
	{
		return SpawnNode( itemData, ItemPlacementType.Placed, position, rotation, placement );
	}

	public WorldNodeLink SpawnPlacedNode( PersistentItem persistentItem, Vector3 position, Rotation rotation,
		ItemPlacement placement )
	{
		var itemData = persistentItem.ItemData;
		var nodeLink = SpawnPlacedNode( itemData, position, rotation, placement );
		nodeLink.SetPersistence( persistentItem );
		nodeLink.RunLoadPersistence();
		return nodeLink;
	}

	public WorldNodeLink SpawnDroppedNode( ItemData itemData, Vector2Int position, ItemRotation rotation,
		ItemPlacement placement )
	{
		return SpawnNode( itemData, ItemPlacementType.Dropped, position, rotation, placement );
	}

	public WorldNodeLink SpawnDroppedNode( PersistentItem persistentItem, Vector2Int position, ItemRotation rotation,
		ItemPlacement placement )
	{
		var itemData = persistentItem.ItemData;
		var nodeLink = SpawnDroppedNode( itemData, position, rotation, placement );
		nodeLink.SetPersistence( persistentItem );
		nodeLink.RunLoadPersistence();
		return nodeLink;
	}

	public WorldNodeLink SpawnCustomNode( ItemData itemData, GameObject scene, Vector2Int position,
		ItemRotation rotation,
		ItemPlacement placement )
	{
		if ( IsOutsideGrid( position ) )
		{
			throw new Exception( $"Position {position} is outside the grid" );
		}

		if ( !itemData.Placements.HasFlag( placement ) )
		{
			throw new Exception( $"Item {itemData.Name} does not support placement {placement}" );
		}

		var positions = itemData.GetGridPositions( rotation, position );

		if ( !CanPlaceItem( positions, placement ) )
		{
			throw new Exception( $"Cannot place item {itemData.Name} at {position} with placement {placement}" );
		}

		if ( scene == null )
		{
			throw new Exception( $"Item {itemData.Name} has no scene" );
		}

		var gameObject = scene.Clone();
		if ( !gameObject.IsValid() )
		{
			throw new Exception( $"Failed to clone scene for {itemData.Name}" );
		}

		gameObject.WorldPosition = ItemGridToWorld( position );
		gameObject.WorldRotation = GetRotation( rotation );

		var nodeLink = AddItem( position, rotation, placement, gameObject );

		nodeLink.ItemId = itemData.ResourceName;
		nodeLink.PlacementType = ItemPlacementType.Placed;

		nodeLink.CalculateSize();

		// UpdateTransform( nodeLink );

		// nodeLink.OnNodeAdded();

		gameObject.NetworkSpawn();

		return nodeLink;
	}

	private WorldNodeLink SpawnNode( ItemData itemData, ItemPlacementType placementType, Vector2Int position,
		ItemRotation rotation, ItemPlacement placement )
	{
		Assert.NotNull( itemData, "Item data is null" );

		if ( IsOutsideGrid( position ) )
		{
			throw new Exception( $"Position {position} is outside the grid" );
		}

		if ( !itemData.Placements.HasFlag( placement ) )
		{
			throw new Exception( $"Item {itemData.Name} does not support placement {placement}" );
		}


		var defaultDropScene =
			SceneUtility.GetPrefabScene(
				ResourceLibrary.Get<PrefabFile>( "items/misc/dropped_item/dropped_item.prefab" ) );

		var scene = placementType switch
		{
			ItemPlacementType.Placed => itemData.PlaceScene,
			ItemPlacementType.Dropped => itemData.DropScene ?? defaultDropScene,
			_ => throw new ArgumentOutOfRangeException( nameof(placementType), placementType, null )
		};

		if ( scene == null )
		{
			throw new Exception( $"Item {(itemData.Name ?? itemData.ResourceName)} has no {placementType} scene" );
		}

		// dropped items are always 1x1
		var positions = placementType == ItemPlacementType.Dropped
			? new List<Vector2Int> { position }
			: itemData.GetGridPositions( rotation, position );

		if ( !CanPlaceItem( positions, placement ) )
		{
			throw new Exception( $"Cannot place item {itemData.Name} at {position} with placement {placement}" );
		}

		var gameObject = scene.Clone();

		gameObject.WorldPosition = ItemGridToWorld( position );
		gameObject.WorldRotation = GetRotation( rotation );

		var nodeLink = AddItem( position, rotation, placement, gameObject );

		nodeLink.ItemId = itemData.ResourceName;
		nodeLink.PlacementType = ItemPlacementType.Placed;

		// replace itemdata with the one from the item, mainly for dropped items
		if ( nodeLink.Node.Components.TryGet<WorldItem>( out var worldItem ) )
		{
			worldItem.ItemData = itemData;
		}

		// nodeLink.CalculateSize();

		// UpdateTransform( nodeLink );

		gameObject.Name = nodeLink.GetName();

		// AddNodeLinkToGridMap( nodeLink );

		Items.Add( nodeLink );

		// nodeLink.OnNodeAdded();

		gameObject.NetworkSpawn();

		return nodeLink;
	}

	private WorldNodeLink SpawnNode( ItemData itemData, ItemPlacementType placementType, Vector3 position,
		Rotation rotation, ItemPlacement placement )
	{
		Assert.NotNull( itemData, "Item data is null" );

		if ( IsOutsideGrid( WorldToItemGrid( position ) ) )
		{
			throw new Exception( $"Position {position} is outside the grid" );
		}

		if ( !itemData.Placements.HasFlag( placement ) )
		{
			throw new Exception( $"Item {itemData.Name} does not support placement {placement}" );
		}

		var defaultDropScene =
			SceneUtility.GetPrefabScene(
				ResourceLibrary.Get<PrefabFile>( "items/misc/dropped_item/dropped_item.prefab" ) );

		var scene = placementType switch
		{
			ItemPlacementType.Placed => itemData.PlaceScene,
			ItemPlacementType.Dropped => itemData.DropScene ?? defaultDropScene,
			_ => throw new ArgumentOutOfRangeException( nameof(placementType), placementType, null )
		};

		if ( scene == null )
		{
			throw new Exception( $"Item {(itemData.Name ?? itemData.ResourceName)} has no {placementType} scene" );
		}

		// dropped items are always 1x1
		/*var positions = placementType == ItemPlacementType.Dropped
			? new List<Vector2Int> { position }
			: itemData.GetGridPositions( rotation, position );

		if ( !CanPlaceItem( positions, placement ) )
		{
			throw new Exception( $"Cannot place item {itemData.Name} at {position} with placement {placement}" );
		}*/

		var gameObject = scene.Clone();

		gameObject.WorldPosition = position;
		gameObject.WorldRotation = rotation;

		var nodeLink = AddItem( placement, gameObject );

		nodeLink.ItemId = itemData.ResourceName;
		nodeLink.PlacementType = ItemPlacementType.Placed;

		// replace itemdata with the one from the item, mainly for dropped items
		if ( nodeLink.Node.Components.TryGet<WorldItem>( out var worldItem ) )
		{
			worldItem.ItemData = itemData;
		}

		// nodeLink.CalculateSize();

		// UpdateTransform( nodeLink );

		gameObject.Name = nodeLink.GetName();

		// AddNodeLinkToGridMap( nodeLink );

		// nodeLink.OnNodeAdded();

		gameObject.NetworkSpawn();

		return nodeLink;
	}

	/// <summary>
	///  Adds an item to the world at the specified position and placement. It does not check if the item can be placed at the specified position.
	/// </summary>
	/// <param name="position"></param>
	/// <param name="rotation"></param>
	/// <param name="placement"></param>
	/// <param name="item"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public WorldNodeLink AddItem( Vector2Int position, ItemRotation rotation, ItemPlacement placement, GameObject item )
	{
		if ( IsOutsideGrid( position ) )
		{
			throw new Exception( $"Position {position} is outside the grid" );
		}

		var nodeLink = new WorldNodeLink( this, item );

		nodeLink.GridPosition = position;
		nodeLink.GridRotation = rotation;

		nodeLink.GridPlacement = placement;
		nodeLink.PrefabPath = nodeLink.GetPrefabPath();

		// NODE LINK IS NOT ADDED TO WORLD YET, CAN'T DO IT HERE BECAUSE WE NEED TO CALCULATE SIZE FIRST
		// AddNodeLinkToGridMap( nodeLink );

		item.SetParent( GameObject ); // TODO: should items be parented to the world?

		OnItemAdded?.Invoke( nodeLink );

		nodeLink.OnNodeAdded();

		// UpdateTransform( nodeLink );

		Log.Info( $"Added item {item.Name} at {position} with placement {placement} ({item.Id})" );

		return nodeLink;
	}

	public WorldNodeLink AddItem( ItemPlacement placement, GameObject item )
	{
		/*if ( IsOutsideGrid( position ) )
		{
			throw new Exception( $"Position {position} is outside the grid" );
		}*/

		var nodeLink = new WorldNodeLink( this, item );

		nodeLink.GridPlacement = placement;
		nodeLink.PrefabPath = nodeLink.GetPrefabPath();

		// NODE LINK IS NOT ADDED TO WORLD YET, CAN'T DO IT HERE BECAUSE WE NEED TO CALCULATE SIZE FIRST
		// AddNodeLinkToGridMap( nodeLink );

		item.SetParent( GameObject ); // TODO: should items be parented to the world?

		Items.Add( nodeLink );

		OnItemAdded?.Invoke( nodeLink );

		nodeLink.OnNodeAdded();

		// UpdateTransform( nodeLink );

		Log.Info( $"Added item {item.Name} at {item.WorldPosition} with placement {placement} ({item.Id})" );

		return nodeLink;
	}
}
