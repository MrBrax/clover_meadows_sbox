using System;
using System.Diagnostics;
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

	// public Dictionary<Vector2Int, Dictionary<ItemPlacement, WorldNodeLink>> Items { get; set; } = new();

	// TODO: should this be synced?
	private HashSet<Vector2Int> BlockedTiles { get; set; } = new();

	[Sync] private Dictionary<Vector2Int, float> TileHeights { get; set; } = new();

	public record struct NodeLinkMapKey( Vector2Int Position, ItemPlacement Placement )
	{
		public Vector2Int Position = Position;
		public ItemPlacement Placement = Placement;
	}

	/// <summary>
	///  This is now the main grid map for all items in the world. Node links appear multiple times in this map if they occupy multiple grid positions.
	///  Somehow, the record struct works as a non-reference type, so it can be used like a search query. Anyone know why?
	/// </summary>
	private readonly Dictionary<NodeLinkMapKey, WorldNodeLink> _nodeLinkGridMap = new();

	private void AddNodeLinkToGridMap( WorldNodeLink nodeLink )
	{
		foreach ( var pos in nodeLink.GetGridPositions( true ) )
		{
			if ( _nodeLinkGridMap.ContainsKey(
				    new NodeLinkMapKey { Position = pos, Placement = nodeLink.GridPlacement } ) )
			{
				throw new Exception( $"Node link already exists at {pos} with placement {nodeLink.GridPlacement}" );
			}

			_nodeLinkGridMap[new NodeLinkMapKey { Position = pos, Placement = nodeLink.GridPlacement }] = nodeLink;
		}
	}

	private void RemoveNodeLinkFromGridMap( WorldNodeLink nodeLink )
	{
		foreach ( var entry in _nodeLinkGridMap.Where( x => x.Value == nodeLink ).ToList() )
		{
			_nodeLinkGridMap.Remove( entry.Key );
		}
	}

	private void AddNodeLinkGridMapEntry( Vector2Int position, ItemPlacement placement, WorldNodeLink nodeLink )
	{
		/*if ( _nodeLinkGridMap.ContainsKey( new NodeLinkMapKey { Position = position, Placement = placement } ) )
		{
			throw new Exception( $"Node link already exists at {position} with placement {placement}" );
		}*/
		_nodeLinkGridMap[new NodeLinkMapKey { Position = position, Placement = placement }] = nodeLink;
	}

	[Sync] public int Layer { get; set; }

	public string WorldId => Data.ResourceName;

	[JsonIgnore]
	public IEnumerable<PlayerCharacter> PlayersInWorld =>
		Scene.GetAllComponents<PlayerCharacter>().Where( p => p.WorldLayerObject.Layer == Layer );

	public bool ShouldUnloadOnExit
	{
		get => !PlayersInWorld.Any();
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
	
	/// <summary>
	///  Checks if a player is in the way of an item placement.
	/// </summary>
	/// <param name="positions"></param>
	/// <returns></returns>
	public bool CheckPlayerObstruction( List<Vector2Int> positions )
	{
		foreach ( var player in PlayersInWorld )
		{
			var playerPos = WorldToItemGrid( player.WorldPosition );

			if ( positions.Any( x => ItemGridToWorld( x ).Distance( player.WorldPosition ) < 25 ) )
			{
				Log.Warning( $"Player {player.PlayerName} is in the way" );
				return true;
			}
		}

		return false;
		
	}

	public bool IsOutsideGrid( Vector2Int position )
	{
		return position.x < 0 || position.y < 0 || position.x >= Data.Width || position.y >= Data.Height;
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
		
		if ( CheckPlayerObstruction( positions ) )
		{
			Log.Warning( $"Player obstruction" );
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

		AddNodeLinkToGridMap( nodeLink );

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

		/*if ( Items.TryGetValue( position, out var dict ) )
		{
			dict[placement] = nodeLink;
		}
		else
		{
			Items[position] = new Dictionary<ItemPlacement, WorldNodeLink>() { { placement, nodeLink } };
		}*/

		nodeLink.GridPosition = position;
		nodeLink.GridPlacement = placement;
		nodeLink.GridRotation = rotation;
		nodeLink.PrefabPath = nodeLink.GetPrefabPath();

		// NODE LINK IS NOT ADDED TO WORLD YET, CAN'T DO IT HERE BECAUSE WE NEED TO CALCULATE SIZE FIRST
		// AddNodeLinkToGridMap( nodeLink );

		item.SetParent( GameObject ); // TODO: should items be parented to the world?

		OnItemAdded?.Invoke( nodeLink );

		nodeLink.OnNodeAdded();

		// UpdateTransform( nodeLink );

		Log.Info( $"Added item {nodeLink.GetName()} at {position} with placement {placement} ({item.Id})" );

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
			
			if ( nodeLink.IsDroppedItem )
			{
				itemWidth = 0;
				itemHeight = 0;
				Log.Info( $"Forcing item size to 1x1 for {nodeLink.GetName()} - dropped item" );
			}

			// "rotate" the offset based on the item's rotation
			if ( nodeLink.GridRotation == ItemRotation.North )
			{
				offset = new Vector3( itemWidth * GridSizeCenter, itemHeight * GridSizeCenter, 0 );
			}
			else if ( nodeLink.GridRotation == ItemRotation.East )
			{
				offset = new Vector3( itemHeight * GridSizeCenter, itemWidth * GridSizeCenter, 0 );
			}
			else if ( nodeLink.GridRotation == ItemRotation.South )
			{
				offset = new Vector3( -itemWidth * GridSizeCenter, -itemHeight * GridSizeCenter, 0 );
			}
			else if ( nodeLink.GridRotation == ItemRotation.West )
			{
				offset = new Vector3( -itemHeight * GridSizeCenter, -itemWidth * GridSizeCenter, 0 );
			}
		}
		else
		{
			Log.Warning( $"No item data for {nodeLink.GetName()}" );
		}

		if ( placement == ItemPlacement.Underground )
		{
			newPosition = new Vector3( newPosition.x, newPosition.y, -50 );
		}
		else if ( placement == ItemPlacement.OnTop )
		{
			var floorNodeLink = GetItem( position, ItemPlacement.Floor );
			if ( floorNodeLink == null )
			{
				Log.Warning( $"No floor item at {position}" );
				WorldPosition = newPosition;
				return;
			}

			var onTopNode = floorNodeLink.GetPlaceableNodeAtGridPosition( position );
			if ( onTopNode == null )
			{
				Log.Warning( $"No on top node at {position}" );
				WorldPosition = newPosition;
				return;
			}
			
			onTopNode.PlacedNodeLink = nodeLink;
			nodeLink.PlacedOn = onTopNode;

			Log.Info( $"Updating transform of {nodeLink.GetName()} to be on top of {onTopNode}" );
			
			newPosition = onTopNode.WorldPosition;
		}

		newPosition += offset;
		
		nodeLink.Node.WorldPosition = newPosition;
		nodeLink.Node.WorldRotation = newRotation;

		// Log.Info( $"Updated transform of {nodeLink.GetName()} to {newPosition} with rotation {newRotation}" );
	}
	
	public (Vector3 position, Rotation rotation) GetTransform( Vector2Int gridPosition, ItemRotation gridRotation, ItemPlacement placement, ItemData itemData, bool isDropped = false )
	{
		var position = gridPosition;

		var newPosition = ItemGridToWorld( position );
		var newRotation = GetRotation( gridRotation );

		var offset = Vector3.Zero;
		
		if ( itemData != null )
		{
			var itemWidth = itemData.Width - 1;
			var itemHeight = itemData.Height - 1;
			
			if ( isDropped )
			{
				itemWidth = 0;
				itemHeight = 0;
				Log.Info( $"Forcing item size to 1x1 - dropped item" );
			}

			// "rotate" the offset based on the item's rotation
			if ( gridRotation == ItemRotation.North )
			{
				offset = new Vector3( itemWidth * GridSizeCenter, itemHeight * GridSizeCenter, 0 );
			}
			else if ( gridRotation == ItemRotation.East )
			{
				offset = new Vector3( itemHeight * GridSizeCenter, itemWidth * GridSizeCenter, 0 );
			}
			else if ( gridRotation == ItemRotation.South )
			{
				offset = new Vector3( -itemWidth * GridSizeCenter, -itemHeight * GridSizeCenter, 0 );
			}
			else if ( gridRotation == ItemRotation.West )
			{
				offset = new Vector3( -itemHeight * GridSizeCenter, -itemWidth * GridSizeCenter, 0 );
			}
		}
		else
		{
			Log.Warning( $"No item data" );
		}

		if ( placement == ItemPlacement.Underground )
		{
			newPosition = new Vector3( newPosition.x, newPosition.y, -50 );
		}
		else if ( placement == ItemPlacement.OnTop )
		{
			var floorNodeLink = GetItem( position, ItemPlacement.Floor );
			if ( floorNodeLink == null )
			{
				Log.Warning( $"No floor item at {position}" );
				WorldPosition = newPosition;
				return (newPosition, newRotation);
			}

			var onTopNode = floorNodeLink.GetPlaceableNodeAtGridPosition( position );
			if ( onTopNode == null )
			{
				Log.Warning( $"No on top node at {position}" );
				WorldPosition = newPosition;
				return (newPosition, newRotation);
			}
			
			// onTopNode.PlacedNodeLink = nodeLink;
			// nodeLink.PlacedOn = onTopNode;

			Log.Info( $"Updating transform to be on top of {onTopNode}" );
			
			newPosition = onTopNode.WorldPosition;
		}

		newPosition += offset;
		
		// nodeLink.Node.WorldPosition = newPosition;
		// nodeLink.Node.WorldRotation = newRotation;
		
		return (newPosition, newRotation);

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
		var nodeLink = GetItem( position, placement );

		if ( nodeLink == null )
		{
			Log.Warning( $"No item at {position} with placement {placement}" );
			return;
		}

		nodeLink.DestroyNode();

		RemoveNodeLinkFromGridMap( nodeLink );

		if ( nodeLink.PlacedOn != null )
		{
			nodeLink.PlacedOn.PlacedNodeLink = null;
		}

		OnItemRemoved?.Invoke( nodeLink );
	}

	/// <inheritdoc cref="RemoveItem(Vector2Int,ItemPlacement)"/>
	public void RemoveItem( GameObject node )
	{
		// RemoveItem( item.GridPosition, item.Placement );
		var nodeLink = GetItem( node );
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

	public void Setup()
	{
		var layerObjects = GetComponentsInChildren<WorldLayerObject>( true );
		foreach ( var layerObject in layerObjects )
		{
			layerObject.SetLayer( Layer );
		}
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

		var i = 0;
		foreach ( var item in _nodeLinkGridMap )
		{
			// var pos = item.Key.Split( ':' )[0].Split( ',' ).Select( int.Parse ).ToArray();
			var offset = item.Key.Placement == ItemPlacement.OnTop ? Vector3.Up * 32f : Vector3.Zero;
			Gizmo.Draw.Text( $"{item.Key.Position} {item.Key.Placement} | {item.Value.GetName()}\n{item.Value.GridRotation}",
				new Transform( ItemGridToWorld( item.Key.Position ) + offset ) );

			Gizmo.Draw.ScreenText( $"[{item.Key.Position}:{item.Key.Placement}] {item.Value.GetName()}",
				new Vector2( 20f, 20f + ((i++) * 20f) ) );
		}
	}

	[ConVar( "clover_show_grid_info" )] public static bool ShowGridInfo { get; set; }

	/*[ConCmd( "clover_dump_items" )]
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
	}*/

	public bool HasNodeLink( WorldNodeLink node )
	{
		return _nodeLinkGridMap.ContainsValue( node );
	}

	public void NodeLinkBenchmark()
	{
		var sw = new Stopwatch();
		sw.Start();

		var pos = new Vector2Int( 47, 6 );

		for ( var i = 0; i < 10000; i++ )
		{
			var nodeLink = GetItems( pos ).FirstOrDefault();
		}

		sw.Stop();
		Log.Info( $"Took {sw.ElapsedMilliseconds}ms to find node link in GetItems" );


		/*sw.Restart();

		for ( var i = 0; i < 10000; i++ )
		{
			var nodeLink = _nodeLinkMap.FirstOrDefault( x => x.Value.GridPosition == pos );
		}

		sw.Stop();
		Log.Info( $"Took {sw.ElapsedMilliseconds}ms to find node link in _nodeLinkMap" );

		sw.Restart();

		for ( var i = 0; i < 10000; i++ )
		{
			var nodeLink = _nodeLinks.FirstOrDefault( x => x.GridPosition == pos );
		}

		sw.Stop();
		Log.Info( $"Took {sw.ElapsedMilliseconds}ms to find node link in _nodeLinks" );*/

		sw.Restart();

		for ( var i = 0; i < 10000; i++ )
		{
			var nodeLink = _nodeLinkGridMap.FirstOrDefault( x => x.Key.Position == pos );
		}

		sw.Stop();
		Log.Info( $"Took {sw.ElapsedMilliseconds}ms to find node link in _nodeLinkGridMap" );
	}

	[ConCmd( "world_node_link_benchmark" )]
	public static void CmdNodeLinkBenchmark()
	{
		WorldManager.Instance.ActiveWorld.NodeLinkBenchmark();
	}
}
