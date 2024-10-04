using System;
using System.Text.Json;
using Clover.Data;
using Clover.Items;
using Clover.Persistence;
using Clover.Player;
using Sandbox;

namespace Clover;

public sealed partial class World : Component
{
	[Flags]
	public enum ItemPlacement
	{
		[Icon( "wallpaper" )] Wall = 1 << 0,

		[Icon( "inventory" )] OnTop = 1 << 1,

		[Icon( "waves" )] Floor = 1 << 2,


		Underground = 1 << 3,

		[Icon( "dashboard" )] FloorDecal = 1 << 4,
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

	[Property] public Data.WorldData Data { get; set; }


	public delegate void OnItemAddedEvent( WorldNodeLink nodeLink );

	public event OnItemAddedEvent OnItemAdded;

	public delegate void OnItemRemovedEvent( WorldNodeLink nodeLink );

	public event OnItemRemovedEvent OnItemRemoved;

	public Dictionary<Vector2Int, Dictionary<ItemPlacement, WorldNodeLink>> Items { get; set; } = new();

	// TODO: should this be synced?
	private HashSet<Vector2Int> _blockedTiles { get; set; } = new();

	[Sync] private Dictionary<Vector2Int, float> _tileHeights { get; set; } = new();

	private Dictionary<GameObject, WorldNodeLink> _nodeLinkMap = new();

	[Sync] public int Layer { get; set; }

	public string WorldId => Data.ResourceName;

	public bool ShouldUnloadOnExit
	{
		get
		{
			var playersInWorld = Scene.GetAllComponents<PlayerCharacter>()
				.Count( p => p.WorldLayerObject.Layer == Layer );
			return playersInWorld == 0;
		}
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
		if ( _blockedTiles.Contains( position ) ) return true;

		CheckTerrainAt( position );

		return _blockedTiles.Contains( position );
	}

	public float GetHeightAt( Vector2Int position )
	{
		if ( _tileHeights.TryGetValue( position, out var height ) )
		{
			return height;
		}

		CheckTerrainAt( position );

		if ( _tileHeights.TryGetValue( position, out height ) )
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
			_tileHeights[position] = worldPos.z;
			// Logger.Info( $"Adding grid position height {gridPos} = {worldPos.Y}" );
		}

		if ( !check )
		{
			_blockedTiles.Add( position );
			// Logger.Info( $"Blocking grid position from terrain check: {gridPos} (height: {worldPos.Y})" );
			// GetTree().CallGroup( "debugdraw", "add_line", ItemGridToWorld( gridPos ), ItemGridToWorld( gridPos ) + new Vector3( 0, 10, 0 ), new Color( 1, 0, 0 ), 15 );
		}
		else
		{
			// GetTree().CallGroup( "debugdraw", "add_line", ItemGridToWorld( gridPos ), ItemGridToWorld( gridPos ) + new Vector3( 0, 10, 0 ), new Color( 0, 1, 0 ), 15 );
		}
	}

	public bool IsOutsideGrid( Vector2Int position )
	{
		return position.x < 0 || position.y < 0 || position.x >= Data.Width || position.y >= Data.Height;
	}

	public static Rotation GetRotation( ItemRotation rotation )
	{
		return rotation switch
		{
			ItemRotation.North => Rotation.FromYaw( 0 ),
			ItemRotation.East => Rotation.FromYaw( 90 ),
			ItemRotation.South => Rotation.FromYaw( 180 ),
			ItemRotation.West => Rotation.FromYaw( 270 ),
			_ => Rotation.FromYaw( 0 )
		};
	}

	public static Rotation GetRotation( Direction direction )
	{
		return direction switch
		{
			Direction.North => Rotation.FromYaw( 0 ),
			Direction.East => Rotation.FromYaw( 90 ),
			Direction.South => Rotation.FromYaw( 180 ),
			Direction.West => Rotation.FromYaw( 270 ),
			Direction.NorthWest => Rotation.FromYaw( 315 ),
			Direction.NorthEast => Rotation.FromYaw( 45 ),
			Direction.SouthWest => Rotation.FromYaw( 225 ),
			Direction.SouthEast => Rotation.FromYaw( 135 ),
			_ => Rotation.FromYaw( 0 )
		};
	}

	public static float GetRotationAngle( ItemRotation rotation )
	{
		return rotation switch
		{
			ItemRotation.North => 0,
			ItemRotation.East => 90,
			ItemRotation.South => 180,
			ItemRotation.West => 270,
			_ => 0
		};
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

		// var gridPosString = Vector2IToString( gridPos );

		// GetTree().CallGroup( "debugdraw", "add_line", ItemGridToWorld( gridPos ), ItemGridToWorld( gridPos ) + Vector3.Up * 5f, new Color( 1, 1, 1 ), 3f );

		// get items at exact grid position
		if ( Items.TryGetValue( gridPos, out var dict ) )
		{
			foreach ( var item in dict.Values )
			{
				yield return item;
				foundItems.Add( item );
			}
		}

		// get items that are intersecting this grid position
		foreach ( var item in Items.Values.SelectMany( d => d.Values ) )
		{
			/*if ( item.GridSize.X == 1 && item.GridSize.Y == 1 )
			{
				// Logger.Info( "GetItems", $"Item {item} is 1x1" );
				continue;
			}*/

			var itemGridPositions = item.GetGridPositions( true );

			/* foreach ( var pos in itemGridPositions )
			{
				Logger.Info( "GetItems", $" - Item {item} has grid position {pos}" );
			} */

			if ( itemGridPositions.Contains( gridPos ) )
			{
				if ( foundItems.Contains( item ) )
				{
					// Logger.Info( "GetItems", $"Item {item} is already found" );
					continue;
				}

				yield return item;
				foundItems.Add( item );
			}

			/*var positions = item.GetGridPositions( true );
			if ( positions.Contains( gridPos ) )
			{
				yield return item;
			}*/
		}
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
	/// <param name="item"></param>
	/// <param name="position"></param>
	/// <param name="rotation"></param>
	/// <param name="placement"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public bool CanPlaceItem( ItemData itemData, Vector2Int position, ItemRotation rotation, ItemPlacement placement )
	{
		if ( IsOutsideGrid( position ) )
		{
			// throw new Exception( $"Position {position} is outside the grid" );
			Log.Warning( $"Position {position} is outside the grid" );
			return false;
		}

		var positions = itemData.GetGridPositions( rotation, position );

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
		}

		return true;
	}

	public bool CheckGridPositionEligibility( Vector2Int position, out Vector3 worldPosition )
	{
		if ( IsOutsideGrid( position ) )
		{
			Log.Warning( $"Position {position} is outside the grid" );
			worldPosition = Vector3.Zero;
			return false;
		}

		if ( _blockedTiles.Contains( position ) )
		{
			// Log.Info( $"Position {position} is already blocked" );
			worldPosition = Vector3.Zero;
			return false;
		}

		// Log.Info( $"Checking eligibility of {position}" );

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

		/*var spaceState = GetWorld3D().DirectSpaceState;

		// uint collisionMask = 1010; // terrain is on layer 10
		// Logger.Info( "EligibilityCheck", $"Collision mask: {collisionMask}" );

		var traceTopLeft =
			new Trace( spaceState ).CastRay(
				PhysicsRayQueryParameters3D.Create( topLeft, new Vector3( topLeft.X, -50, topLeft.Z ),
					TerrainLayer ) );
		var traceTopRight =
			new Trace( spaceState ).CastRay(
				PhysicsRayQueryParameters3D.Create( topRight, new Vector3( topRight.X, -50, topRight.Z ),
					TerrainLayer ) );
		var traceBottomLeft = new Trace( spaceState ).CastRay(
			PhysicsRayQueryParameters3D.Create( bottomLeft, new Vector3( bottomLeft.X, -50, bottomLeft.Z ),
				TerrainLayer ) );
		var traceBottomRight = new Trace( spaceState ).CastRay(
			PhysicsRayQueryParameters3D.Create( bottomRight, new Vector3( bottomRight.X, -50, bottomRight.Z ),
				TerrainLayer ) );

		if ( traceTopLeft == null || traceTopRight == null || traceBottomLeft == null || traceBottomRight == null )
		{
			Logger.Warn( "ElegibilityCheck", $"Failed to trace rays at {position}" );
			worldPosition = Vector3.Zero;
			return false;
		}*/

		var traceTopLeft = Scene.Trace.Ray( topLeft, topLeft + (Vector3.Down * traceDistance) ).WithTag( "terrain" )
			.Run();
		var traceTopRight = Scene.Trace.Ray( topRight, topRight + (Vector3.Down * traceDistance) ).WithTag( "terrain" )
			.Run();
		var traceBottomLeft = Scene.Trace.Ray( bottomLeft, bottomLeft + (Vector3.Down * traceDistance) ).WithTag( "terrain" )
			.Run();
		var traceBottomRight = Scene.Trace.Ray( bottomRight, bottomRight + (Vector3.Down * traceDistance) ).WithTag( "terrain" )
			.Run();
		
		// Gizmo.Draw.Line( topLeft, topLeft + (Vector3.Down * traceDistance) );
		// Gizmo.Draw.Line( topRight, topRight + (Vector3.Down * traceDistance) );
		// Gizmo.Draw.Line( bottomLeft, bottomLeft + (Vector3.Down * traceDistance) );
		// Gizmo.Draw.Line( bottomRight, bottomRight + (Vector3.Down * traceDistance) );

		if ( !traceTopLeft.Hit || !traceTopRight.Hit || !traceBottomLeft.Hit || !traceBottomRight.Hit )
		{
			Log.Warning( $"Ray trace failed at {position} ({basePosition}), start height is {baseHeight}" );
			worldPosition = Vector3.Zero;
			return false;
		}

		var heightTopLeft = traceTopLeft.HitPosition.z;
		var heightTopRight = traceTopRight.HitPosition.z;
		var heightBottomLeft = traceBottomLeft.HitPosition.z;
		var heightBottomRight = traceBottomRight.HitPosition.z;

		/*if ( heightTopLeft != heightTopRight || heightTopLeft != heightBottomLeft ||
			 heightTopLeft != heightBottomRight )
		{
			worldPosition = Vector3.Zero;
			return false;
		}*/

		if ( heightTopLeft <= -50 )
		{
			Log.Warning( $"Height at {position} is below -50" );
		}

		// var averageHeight = (heightTopLeft + heightTopRight + heightBottomLeft + heightBottomRight) / 4;

		if ( Math.Abs( heightTopLeft - heightTopRight ) > heightTolerance ||
		     Math.Abs( heightTopLeft - heightBottomLeft ) > heightTolerance ||
		     Math.Abs( heightTopLeft - heightBottomRight ) > heightTolerance )
		{
			Log.Info(
				$"Height difference at {position} is too high ({heightTopLeft}, {heightTopRight}, {heightBottomLeft}, {heightBottomRight})" );
			worldPosition = Vector3.Zero;
			return false;
		}
		
		if ( heightTopLeft.AlmostEqual( 0f ) ) heightTopLeft = 0f;

		worldPosition = new Vector3( basePosition.x, basePosition.y, heightTopLeft );

		// Log.Info( $"Position {position} is eligible" );

		return true;
	}


	public WorldNodeLink SpawnPlacedNode( ItemData itemData, Vector2Int position, ItemRotation rotation,
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

		if ( !CanPlaceItem( itemData, position, rotation, placement ) )
		{
			throw new Exception( $"Cannot place item {itemData.Name} at {position} with placement {placement}" );
		}

		if ( itemData.PlaceScene == null )
		{
			throw new Exception( $"Item {itemData.Name} has no place scene" );
		}

		var gameObject = itemData.PlaceScene.Clone();

		var nodeLink = AddItem( position, rotation, placement, gameObject );

		nodeLink.ItemId = itemData.ResourceName;
		nodeLink.PlacementType = ItemPlacementType.Placed;
		
		nodeLink.CalculateSize();
		
		UpdateTransform( nodeLink );

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

		Vector3 offset = Vector3.Zero;

		var itemData = nodeLink.ItemData;
		if ( itemData != null )
		{
			var itemWidth = itemData.Width - 1;
			var itemHeight = itemData.Height - 1;

			// "rotate" the offset based on the item's rotation
			if ( nodeLink.GridRotation == ItemRotation.North )
			{
				offset = new Vector3( itemWidth * GridSizeCenter, 0, itemHeight * GridSizeCenter );
			}
			else if ( nodeLink.GridRotation == ItemRotation.East )
			{
				offset = new Vector3( itemHeight * GridSizeCenter, 0, itemWidth * GridSizeCenter );
			}
			else if ( nodeLink.GridRotation == ItemRotation.South )
			{
				offset = new Vector3( itemWidth * GridSizeCenter, 0, -itemHeight * GridSizeCenter );
			}
			else if ( nodeLink.GridRotation == ItemRotation.West )
			{
				offset = new Vector3( -itemHeight * GridSizeCenter, 0, itemWidth * GridSizeCenter );
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
			height != 0 ? height : WorldPosition.z
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
		
		// Gizmo.Draw.Grid( Gizmo.GridAxis.XY, 32f );
	}
}
