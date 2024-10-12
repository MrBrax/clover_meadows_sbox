using System;
using System.Text.Json.Serialization;
using Clover.Data;
using Clover.Items;
using Clover.Persistence;
using Clover.Player;
using Sandbox.Diagnostics;

namespace Clover;

[Category( "Clover/World" )]
[Icon( "world" )]
public sealed partial class World : Component
{
	[Flags]
	public enum ItemPlacement
	{
		[Icon( "wallpaper" )] Wall = 1 << 0,

		[Icon( "inventory" )] OnTop = 1 << 1,

		[Icon( "waves" )] Floor = 1 << 2,

		[Description( "Items dug into the ground" )]
		Underground = 1 << 3,


		[Icon( "dashboard" ), Description( "Special case for decals" )]
		FloorDecal = 1 << 4,

		Rug = 1 << 5,
	}

	public enum ItemPlacementType
	{
		Placed = 1,
		Dropped = 2,
	}

	public enum ItemRotation
	{
		North = 1,
		East = 2,
		South = 3,
		West = 4
	}

	public enum Direction
	{
		North = 1,
		South = 2,
		West = 3,
		East = 4,
		NorthWest = 5,
		NorthEast = 6,
		SouthWest = 7,
		SouthEast = 8
	}

	public static float GridSize = 32f;
	public static float GridSizeCenter = GridSize / 2f;

	[Property] public WorldData Data { get; set; }


	public delegate void OnItemAddedEvent( WorldNodeLink nodeLink );

	public event OnItemAddedEvent OnItemAdded;

	public delegate void OnItemRemovedEvent( WorldNodeLink nodeLink );

	public event OnItemRemovedEvent OnItemRemoved;

	public Dictionary<Vector2Int, Dictionary<ItemPlacement, WorldNodeLink>> Items { get; set; } = new();

	// TODO: should this be synced?
	private HashSet<Vector2Int> BlockedTiles { get; set; } = new();

	[Sync] private Dictionary<Vector2Int, float> TileHeights { get; set; } = new();

	private readonly Dictionary<GameObject, WorldNodeLink> _nodeLinkMap = new();

	public record struct NodeLinkMapKey
	{
		public Vector2Int Position;
		public ItemPlacement Placement;
	}

	private readonly Dictionary<NodeLinkMapKey, WorldNodeLink> _nodeLinkGridMap = new();

	[Sync] public int Layer { get; set; }

	public string WorldId => Data.ResourceName;
	
	[JsonIgnore] public IEnumerable<PlayerCharacter> PlayersInWorld => Scene.GetAllComponents<PlayerCharacter>().Where( p => p.WorldLayerObject.Layer == Layer );

	public bool ShouldUnloadOnExit
	{
		get => !PlayersInWorld.Any();
	}

	protected override void OnAwake()
	{
		base.OnAwake();

		Log.Info( $"World {WorldId} awake" );
		foreach ( var item in Items )
		{
			Log.Info( $"Item at {item.Key}: {item.Value}" );
		}
	}

	public bool IsBlockedGridPosition( Vector2Int position )
	{
		if ( BlockedTiles.Contains( position ) ) return true;

		CheckTerrainAt( position );

		return BlockedTiles.Contains( position );
	}

	public float GetHeightAt( Vector2Int position )
	{
		if ( TileHeights.TryGetValue( position, out var height ) )
		{
			return height;
		}

		CheckTerrainAt( position );

		if ( TileHeights.TryGetValue( position, out height ) )
		{
			return height;
		}

		return 0;
	}

	public void CheckTerrainAt( Vector2Int position )
	{
		var check = CheckGridPositionEligibility( position, out var worldPos );

		if ( worldPos.z != 0 )
		{
			TileHeights[position] = worldPos.z;
		}

		if ( !check )
		{
			BlockedTiles.Add( position );
		}
		
	}

	public bool IsOutsideGrid( Vector2Int position )
	{
		return position.x < 0 || position.y < 0 || position.x >= Data.Width || position.y >= Data.Height;
	}

	/// <summary>
	/// Retrieves the WorldNodeLinks at the specified grid position.
	/// This method will return items that are intersecting the grid position as well, if they are larger than 1x1.
	/// <br />Use <see cref="WorldNodeLink.Node"/> to get the actual node.
	/// </summary>
	/// <param name="gridPos">The grid position to retrieve items from.</param>
	/// <returns>An enumerable collection of WorldNodeLink items at the specified grid position.</returns>
	public IEnumerable<WorldNodeLink> GetItems( Vector2Int gridPos )
	{
		if ( IsOutsideGrid( gridPos ) )
		{
			throw new Exception( $"Position {gridPos} is outside the grid" );
		}

		if ( Items == null )
		{
			throw new Exception( "Items is null" );
		}

		HashSet<WorldNodeLink> foundItems = new();

		Log.Info( $"Getting items at {gridPos}" );

		// get items at exact grid position
		if ( Items.TryGetValue( gridPos, out var dict ) )
		{
			foreach ( var item in dict.Values )
			{
				// Log.Info( $"Found item {item.GetName()} at {gridPos} with exact placement" );
				foundItems.Add( item );
				yield return item;
			}
		}

		foreach ( var entry in _nodeLinkGridMap.Where( x => x.Key.Position == gridPos ) )
		{
			// Log.Info( $"Found item {entry.Value.GetName()} at {gridPos} in grid map" );
			foundItems.Add( entry.Value );
			yield return entry.Value;
		}

		// get items that are intersecting this grid position
		/*foreach ( var item in Items.Values.SelectMany( d => d.Values ) )
		{
			if ( item.Size is { x: 1, y: 1 } )
			{
				Log.Info( $"Item {item.GetName()} is 1x1 so it won't intersect" );
				continue;
			}

			var itemGridPositions = item.GetGridPositions( true );

			foreach ( var pos in itemGridPositions )
			{
				Log.Info( $" - Item {item.GetName()} has grid position {pos}" );
			}

			if ( itemGridPositions.Contains( gridPos ) )
			{
				if ( foundItems.Contains( item ) )
				{
					Log.Info( $"Item {item.GetName()} is already found" );
					continue;
				}

				Log.Info( $"Found intersecting item {item} at {gridPos}" );
				foundItems.Add( item );
				yield return item;
			}

			/*var positions = item.GetGridPositions( true );
			if ( positions.Contains( gridPos ) )
			{
				yield return item;
			}#1#

		}

		Log.Info( $"Found {foundItems.Count} items at {gridPos}" );*/
	}

	/// <summary>
	///  Get a node link at a specific grid position and placement.
	///  Use <see cref="WorldNodeLink.Node"/> to get the node.
	/// </summary>
	/// <param name="gridPos"></param>
	/// <param name="placement"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public WorldNodeLink GetItem( Vector2Int gridPos, ItemPlacement placement )
	{
		foreach ( var item in GetItems( gridPos ) )
		{
			if ( item.GridPlacement == placement )
			{
				return item;
			}
		}

		return null;
	}

	public WorldNodeLink GetItem( GameObject node )
	{
		return CollectionExtensions.GetValueOrDefault( _nodeLinkMap, node );
	}

	/// <summary>
	///  Checks if an item can be placed at the specified position and rotation.
	///  It will check if the position is outside the grid, if there are any items at the position, and if there are any items nearby that would block the placement.
	///  An item can be larger than 1x1, in which case it will check all positions that the item would occupy.
	/// </summary>
	/// <param name="positions"></param>
	/// <param name="placement"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public bool CanPlaceItem( List<Vector2Int> positions, ItemPlacement placement )
	{
		if ( positions.Any( IsOutsideGrid ) )
		{
			Log.Warning( $"One or more positions are outside the grid" );
			return false;
		}

		// check any nearby items
		foreach ( var pos in positions )
		{
			if ( IsBlockedGridPosition( pos ) )
			{
				Log.Warning( $"Found blocked grid position at {pos}" );
				return false;
			}

			/*if ( GetItems( pos ).Any() )
			{
				Log.Warning("CanPlaceItem", $"Found item at {pos}" );
				return false;
			}*/

			if ( Items.TryGetValue( pos, out var dict ) )
			{
				if ( dict.ContainsKey( placement ) )
				{
					Log.Warning( $"Found item at {pos} with placement {placement}" );
					return false;
				}
			}

			/*if ( _nodeLinkGridMap.TryGetValue( $"{pos.x},{pos.y}:{placement}", out var nodeLink ) )
			{
				Log.Warning( $"Found item at {pos} with placement {placement} in grid map, but not in items" );
				return false;
			}*/

			if ( _nodeLinkGridMap.TryGetValue( new NodeLinkMapKey { Position = pos, Placement = placement },
				    out var nodeLink ) )
			{
				Log.Warning( $"Found item at {pos} with placement {placement} in grid map, but not in items" );
				return false;
			}
		}
		
		return true;
	}

	public bool CheckGridPositionEligibility( Vector2Int position, out Vector3 worldPosition )
	{
		if ( IsOutsideGrid( position ) )
		{
			worldPosition = Vector3.Zero;
			return false;
		}

		if ( BlockedTiles.Contains( position ) )
		{
			worldPosition = Vector3.Zero;
			return false;
		}
		
		// trace a ray from the sky straight down in each corner, if height is the same on all corners then it's a valid position

		var basePosition = ItemGridToWorld( position, true );

		var margin = GridSizeCenter * 0.8f;
		var heightTolerance = 0.1f;

		var traceDistance = 2000f;
		var baseHeight = basePosition.z + 1000;

		var topLeft = new Vector3( basePosition.x - margin, basePosition.y - margin, baseHeight );
		var topRight = new Vector3( basePosition.x + margin, basePosition.y - margin, baseHeight );
		var bottomLeft = new Vector3( basePosition.x - margin, basePosition.y + margin, baseHeight );
		var bottomRight = new Vector3( basePosition.x + margin, basePosition.y + margin, baseHeight );

		var traceTopLeft = Scene.Trace.Ray( topLeft, topLeft + (Vector3.Down * traceDistance) )
			.WithTag( "terrain" )
			.Run();
		var traceTopRight = Scene.Trace.Ray( topRight, topRight + (Vector3.Down * traceDistance) )
			.WithTag( "terrain" )
			.Run();
		var traceBottomLeft = Scene.Trace.Ray( bottomLeft, bottomLeft + (Vector3.Down * traceDistance) )
			.WithTag( "terrain" )
			.Run();
		var traceBottomRight = Scene.Trace.Ray( bottomRight, bottomRight + (Vector3.Down * traceDistance) )
			.WithTag( "terrain" )
			.Run();

		if ( !traceTopLeft.Hit || !traceTopRight.Hit || !traceBottomLeft.Hit || !traceBottomRight.Hit )
		{
			Log.Warning( $"Ray trace failed at {position} ({basePosition}), start height is {baseHeight}" );
			worldPosition = Vector3.Zero;
			return false;
		}

		var heightTopLeft = traceTopLeft.HitPosition.z - WorldPosition.z;
		var heightTopRight = traceTopRight.HitPosition.z - WorldPosition.z;
		var heightBottomLeft = traceBottomLeft.HitPosition.z - WorldPosition.z;
		var heightBottomRight = traceBottomRight.HitPosition.z - WorldPosition.z;

		if ( heightTopLeft <= -50 )
		{
			Log.Warning( $"Height at {position} is below -50" );
		}

		if ( Math.Abs( heightTopLeft - heightTopRight ) > heightTolerance ||
		     Math.Abs( heightTopLeft - heightBottomLeft ) > heightTolerance ||
		     Math.Abs( heightTopLeft - heightBottomRight ) > heightTolerance )
		{
			Log.Trace(
				$"Height difference at {position} is too high ({heightTopLeft}, {heightTopRight}, {heightBottomLeft}, {heightBottomRight})" );
			worldPosition = Vector3.Zero;
			return false;
		}

		if ( heightTopLeft.AlmostEqual( 0f ) ) heightTopLeft = 0f;

		if ( Data.MaxItemAltitude > 0 && heightTopLeft > Data.MaxItemAltitude )
		{
			Log.Warning( $"Height at {position} is too high: {heightTopLeft}" );
			worldPosition = Vector3.Zero;
			return false;
		}

		worldPosition = new Vector3( basePosition.x, basePosition.y, heightTopLeft );

		return true;
	}


	public WorldNodeLink SpawnPlacedNode( ItemData itemData, Vector2Int position, ItemRotation rotation,
		ItemPlacement placement )
	{
		return SpawnNode( itemData, ItemPlacementType.Placed, position, rotation, placement );
	}

	public WorldNodeLink SpawnDroppedNode( ItemData itemData, Vector2Int position, ItemRotation rotation,
		ItemPlacement placement )
	{
		return SpawnNode( itemData, ItemPlacementType.Dropped, position, rotation, placement );
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

		var nodeLink = AddItem( position, rotation, placement, gameObject );

		nodeLink.ItemId = itemData.ResourceName;
		nodeLink.PlacementType = ItemPlacementType.Placed;

		nodeLink.CalculateSize();

		UpdateTransform( nodeLink );

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

		var nodeLink = AddItem( position, rotation, placement, gameObject );

		nodeLink.ItemId = itemData.ResourceName;
		nodeLink.PlacementType = ItemPlacementType.Placed;

		// replace itemdata with the one from the item, mainly for dropped items
		if ( nodeLink.Node.Components.TryGet<WorldItem>( out var worldItem ) )
		{
			worldItem.ItemData = itemData;
		}

		nodeLink.CalculateSize();

		UpdateTransform( nodeLink );

		gameObject.Name = nodeLink.GetName();

		// add node link to grid map
		foreach ( var pos in nodeLink.GetGridPositions( true ) )
		{
			// _nodeLinkGridMap[$"{pos.x},{pos.y}:{placement}"] = nodeLink;
			_nodeLinkGridMap[new NodeLinkMapKey { Position = pos, Placement = placement }] = nodeLink;
		}

		// nodeLink.OnNodeAdded();

		gameObject.NetworkSpawn();

		return nodeLink;
	}


	public WorldNodeLink SpawnPlacedNode( PersistentItem persistentItem, Vector2Int position, ItemRotation rotation,
		ItemPlacement placement )
	{
		var itemData = persistentItem.ItemData;
		var nodeLink = SpawnPlacedNode( itemData, position, rotation, placement );
		nodeLink.SetPersistence( persistentItem );
		return nodeLink;
	}

	public WorldNodeLink SpawnDroppedNode( PersistentItem persistentItem, Vector2Int position, ItemRotation rotation,
		ItemPlacement placement )
	{
		var itemData = persistentItem.ItemData;
		var nodeLink = SpawnDroppedNode( itemData, position, rotation, placement );
		nodeLink.SetPersistence( persistentItem );
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

		if ( Items.TryGetValue( position, out var dict ) )
		{
			dict[placement] = nodeLink;
		}
		else
		{
			Items[position] = new Dictionary<ItemPlacement, WorldNodeLink>() { { placement, nodeLink } };
		}

		nodeLink.GridPosition = position;
		nodeLink.GridPlacement = placement;
		nodeLink.GridRotation = rotation;
		nodeLink.PrefabPath = nodeLink.GetPrefabPath();

		_nodeLinkMap[item] = nodeLink;

		item.SetParent( GameObject ); // TODO: should items be parented to the world?

		OnItemAdded?.Invoke( nodeLink );

		nodeLink.OnNodeAdded();

		// UpdateTransform( nodeLink );

		return nodeLink;
	}

	private void UpdateTransform( WorldNodeLink nodeLink )
	{
		var position = nodeLink.GridPosition;
		var placement = nodeLink.GridPlacement;

		var newPosition = ItemGridToWorld( position );
		var newRotation = GetRotation( nodeLink.GridRotation );

		var offset = Vector3.Zero;

		var itemData = nodeLink.ItemData;
		if ( itemData != null )
		{
			var itemWidth = itemData.Width - 1;
			var itemHeight = itemData.Height - 1;

			// Log.Info( nodeLink.PrefabPath );
			if ( nodeLink.IsDroppedItem )
			{
				itemWidth = 0;
				itemHeight = 0;
				Log.Info( $"Forcing item size to 1x1 for {nodeLink.GetName()} - dropped item" );
			}

			// "rotate" the offset based on the item's rotation
			if ( nodeLink.GridRotation == ItemRotation.North )
			{
				// offset = new Vector3( itemWidth * GridSizeCenter, 0, itemHeight * GridSizeCenter );
				offset = new Vector3( itemWidth * GridSizeCenter, itemHeight * GridSizeCenter, 0 );
			}
			else if ( nodeLink.GridRotation == ItemRotation.East )
			{
				// offset = new Vector3( itemHeight * GridSizeCenter, 0, itemWidth * GridSizeCenter );
				offset = new Vector3( itemHeight * GridSizeCenter, itemWidth * GridSizeCenter, 0 );
			}
			else if ( nodeLink.GridRotation == ItemRotation.South )
			{
				// offset = new Vector3( itemWidth * GridSizeCenter, 0, -itemHeight * GridSizeCenter );
				offset = new Vector3( -itemWidth * GridSizeCenter, -itemHeight * GridSizeCenter, 0 );
			}
			else if ( nodeLink.GridRotation == ItemRotation.West )
			{
				// offset = new Vector3( -itemHeight * GridSizeCenter, 0, itemWidth * GridSizeCenter );
				offset = new Vector3( -itemHeight * GridSizeCenter, -itemWidth * GridSizeCenter, 0 );
			}
		}
		else
		{
			Log.Warning( $"No item data for {nodeLink.GetName()}" );
		}

		if ( placement == ItemPlacement.Underground )
		{
			// newPosition = new Vector3( newPosition.X, -50, newPosition.Z );
			newPosition = new Vector3( newPosition.x, newPosition.y, -50 );
		}
		else if ( placement == ItemPlacement.OnTop )
		{
			var floorNodeLink = GetItem( position, ItemPlacement.Floor );
			if ( floorNodeLink == null )
			{
				Log.Warning( $"No floor item at {position}" );
				return;
			}

			var onTopNode = floorNodeLink.GetPlaceableNodeAtGridPosition( position );
			if ( onTopNode == null )
			{
				Log.Warning( $"No on top node at {position}" );
				return;
			}

			Log.Info( $"Updating transform of {nodeLink.GetName()} to be on top of {onTopNode}" );
			// newPosition = onTopNode.GlobalTransform.Origin;
			newPosition = onTopNode.WorldPosition;
		}

		newPosition += offset;

		// nodeLink.Node.GlobalTransform = new Transform3D( new Basis( newRotation ), newPosition );
		nodeLink.Node.WorldPosition = newPosition;
		nodeLink.Node.WorldRotation = newRotation;

		// Log.Info( $"Updated transform of {nodeLink.GetName()} to {newPosition} with rotation {newRotation}" );
	}

	/// <summary>
	/// Removes an item from the world at the specified position and placement.
	/// </summary>
	/// <param name="position">The position of the item to remove.</param>
	/// <param name="placement">The placement of the item to remove.</param>
	/// <remarks>
	/// Do NOT remove nodes directly from the world, use this method instead.
	/// </remarks>
	public void RemoveItem( Vector2Int position, ItemPlacement placement )
	{
		if ( Items.TryGetValue( position, out var itemsDict ) )
		{
			if ( itemsDict.ContainsKey( placement ) )
			{
				var nodeLink = itemsDict[placement];
				_nodeLinkMap.Remove( nodeLink.Node );
				nodeLink.DestroyNode();
				itemsDict.Remove( placement );

				// remove all entries in grid map containing this node
				foreach ( var entry in _nodeLinkGridMap.Where( x => x.Value == nodeLink ).ToList() )
				{
					_nodeLinkGridMap.Remove( entry.Key );
				}

				if ( itemsDict.Count == 0 )
				{
					Log.Info( $"Removed last item at {position}" );
					Items.Remove( position );
					// EmitSignal( SignalName.OnItemRemoved, nodeLink );
					OnItemRemoved?.Invoke( nodeLink );
				}

				Log.Info( $"Removed item {nodeLink} at {position} with placement {placement}" );
				// DebugPrint();
			}
			else
			{
				Log.Warning( $"No item at {position} with placement {placement}" );
			}
		}
		else
		{
			Log.Warning( $"No items at {position}" );
		}
	}

	/// <inheritdoc cref="RemoveItem(Vector2Int,ItemPlacement)"/>
	public void RemoveItem( GameObject node )
	{
		// RemoveItem( item.GridPosition, item.Placement );
		var nodeLink = Items.Values.SelectMany( x => x.Values ).FirstOrDefault( x => x.Node == node );
		if ( nodeLink == null )
		{
			throw new Exception( $"Failed to find node link for {node}" );
		}

		RemoveItem( nodeLink.GridPosition, nodeLink.GridPlacement );
	}

	/// <inheritdoc cref="RemoveItem(Vector2Int,ItemPlacement)"/>
	public void RemoveItem( WorldNodeLink nodeLink )
	{
		RemoveItem( nodeLink.GridPosition, nodeLink.GridPlacement );
	}

	public Vector3 ItemGridToWorld( Vector2Int gridPosition, bool noRecursion = false )
	{
		if ( GridSize == 0 ) throw new Exception( "Grid size is 0" );
		if ( GridSizeCenter == 0 ) throw new Exception( "Grid size center is 0" );

		var height = !noRecursion ? GetHeightAt( gridPosition ) : 0;

		return new Vector3(
			(gridPosition.x * GridSize) + GridSizeCenter + WorldPosition.x,
			(gridPosition.y * GridSize) + GridSizeCenter + WorldPosition.y,
			height != 0 ? WorldPosition.z + height : WorldPosition.z
		);
	}

	public Vector2Int WorldToItemGrid( Vector3 worldPosition )
	{
		var x = (int)Math.Floor( (worldPosition.x - WorldPosition.x) / GridSize );
		var y = (int)Math.Floor( (worldPosition.y - WorldPosition.y) / GridSize );

		return new Vector2Int( x, y );
	}

	public void Setup()
	{
		var layerObjects = GetComponentsInChildren<WorldLayerObject>( true );
		foreach ( var layerObject in layerObjects )
		{
			layerObject.SetLayer( Layer );
		}

		/*var modelPhysics = GetComponentsInChildren<ModelPhysics>( true );
		foreach ( var model in modelPhysics )
		{
			model.Enabled = false;
			Invoke( 0.01f, () => model.Enabled = true );
		}*/

		/*var modelPhysics = GetComponentsInChildren<ModelPhysics>( true ).ToList();

		foreach ( var model in modelPhysics )
		{
			model.Enabled = false;
		}

		await Task.Frame();

		foreach ( var model in modelPhysics )
		{
			model.Enabled = true;
		}*/
	}

	public WorldEntrance GetEntrance( string entranceId )
	{
		var entrances = GetComponentsInChildren<WorldEntrance>( true );

		foreach ( var entrance in entrances )
		{
			if ( entrance.EntranceId == entranceId )
			{
				return entrance;
			}
		}

		return null;
	}

	public void OnWorldLoaded()
	{
		if ( IsProxy ) return;

		Log.Info( $"World {WorldId} loaded" );

		/*// test add some items
		var nodeLink = new WorldNodeLink();
		nodeLink.Node = new GameObject();

		Items[Vector2Int.Zero] = new Dictionary<ItemPlacement, WorldNodeLink>();
		Items[Vector2Int.Zero][ItemPlacement.Floor] = nodeLink;*/
	}

	public void OnWorldUnloaded()
	{
		if ( IsProxy ) return;
		Log.Info( $"World {WorldId} unloaded" );
		Save();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !ShowGridInfo ) return;

		if ( !Game.IsEditor || Gizmo.Camera == null || Layer != WorldManager.Instance.ActiveWorldIndex ) return;

		Gizmo.Transform = new Transform( WorldPosition );
		// Log.Info( WorldId + ": " + WorldPosition  );

		Gizmo.Draw.Grid( Gizmo.GridAxis.XY, 32f );

		/*foreach ( var pos in _blockedTiles )
		{
			Gizmo.Draw.Text( pos.ToString(), new Transform( ItemGridToWorld( pos ) ) );
		}

		foreach ( var item in Items.Values.SelectMany( x => x.Values ) )
		{
			Gizmo.Draw.Text( item.GridPosition.ToString(), new Transform( ItemGridToWorld( item.GridPosition ) ) );
		}*/

		foreach ( var item in _nodeLinkGridMap )
		{
			// var pos = item.Key.Split( ':' )[0].Split( ',' ).Select( int.Parse ).ToArray();
			var offset = item.Key.Placement == ItemPlacement.OnTop ? Vector3.Up * 32f : Vector3.Zero;
			Gizmo.Draw.Text( $"{item.Key.Position} {item.Key.Placement} | {item.Value.GetName()}",
				new Transform( ItemGridToWorld( item.Key.Position ) + offset ) );
		}

		foreach ( var entry in Items )
		{
			foreach ( var item in entry.Value.Values )
			{
				Gizmo.Draw.Text( item.GridPlacement + "\n" + item.GetName(),
					new Transform( ItemGridToWorld( item.GridPosition ) ) );
			}
		}
	}

	[ConVar( "clover_show_grid_info" )] public static bool ShowGridInfo { get; set; }

	[ConCmd( "clover_dump_items" )]
	public static void CmdDumpItems()
	{
		var world = WorldManager.Instance.ActiveWorld;

		foreach ( var entry in world.Items )
		{
			Log.Info( $"Items at {entry.Key}:" );

			foreach ( var item in entry.Value )
			{
				Log.Info( $" - {item.Key}: {item.Value.GetName()}" );
			}
		}
	}

	public bool HasNodeLink( WorldNodeLink node )
	{
		return _nodeLinkMap.ContainsKey( node.Node );
	}
}
