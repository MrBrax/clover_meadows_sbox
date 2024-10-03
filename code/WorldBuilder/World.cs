using System;
using Clover.Data;
using Clover.Player;
using Sandbox;

namespace Clover;

public sealed class World : Component
{
	[Flags]
	public enum ItemPlacement
	{
		Wall = 1 << 0,
		OnTop = 1 << 1,
		Floor = 1 << 2,
		Underground = 1 << 3,
		FloorDecal = 1 << 4,
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

	public static float GridSize = 64f;
	public static float GridSizeCenter = GridSize / 2f;

	[Property] public Data.WorldData Data { get; set; }


	public delegate void OnItemAddedEvent( WorldNodeLink nodeLink );

	public event OnItemAddedEvent OnItemAdded;

	public delegate void OnItemRemovedEvent( WorldNodeLink nodeLink );

	public event OnItemRemovedEvent OnItemRemoved;

	[Sync] public NetDictionary<Vector2Int, Dictionary<ItemPlacement, WorldNodeLink>> Items { get; set; } = new();

	[Sync] private HashSet<Vector2Int> _blockedTiles { get; set; } = new();

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

		if ( worldPos.y != 0 )
		{
			_tileHeights[position] = worldPos.y;
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
			Log.Trace( $"Position {position} is outside the grid" );
			worldPosition = Vector3.Zero;
			return false;
		}

		if ( _blockedTiles.Contains( position ) )
		{
			Log.Trace(  $"Position {position} is already blocked" );
			worldPosition = Vector3.Zero;
			return false;
		}

		// trace a ray from the sky straight down in each corner, if height is the same on all corners then it's a valid position

		var basePosition = ItemGridToWorld( position, true );

		var margin = GridSizeCenter * 0.8f;
		var heightTolerance = 0.1f;

		var topLeft = new Vector3( basePosition.x - margin, basePosition.y - margin, 50  );
		var topRight = new Vector3( basePosition.x + margin, basePosition.y - margin, 50 );
		var bottomLeft = new Vector3( basePosition.x - margin, basePosition.y + margin, 50 );
		var bottomRight = new Vector3( basePosition.x + margin, basePosition.y + margin, 50 );

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

		var traceTopLeft = Scene.Trace.Ray( topLeft, topLeft + Vector3.Down * 100 ) /*.WithTag("terrain")*/.Run();
		var traceTopRight = Scene.Trace.Ray( topRight, topRight + Vector3.Down * 100 ) /*.WithTag("terrain")*/.Run();
		var traceBottomLeft = Scene.Trace.Ray( bottomLeft, bottomLeft + Vector3.Down * 100 ) /*.WithTag("terrain")*/.Run();
		var traceBottomRight = Scene.Trace.Ray( bottomRight, bottomRight + Vector3.Down * 100 ) /*.WithTag("terrain")*/.Run();
		
		if ( !traceTopLeft.Hit || !traceTopRight.Hit || !traceBottomLeft.Hit || !traceBottomRight.Hit )
		{
			Log.Warning( $"Failed to trace rays at {position}" );
			worldPosition = Vector3.Zero;
			return false;
		}
		

		var heightTopLeft = traceTopLeft.HitPosition.y;
		var heightTopRight = traceTopRight.HitPosition.y;
		var heightBottomLeft = traceBottomLeft.HitPosition.y;
		var heightBottomRight = traceBottomRight.HitPosition.y;

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
			Log.Trace( $"Height difference at {position} is too high ({heightTopLeft}, {heightTopRight}, {heightBottomLeft}, {heightBottomRight})" );
			worldPosition = Vector3.Zero;
			return false;
		}

		worldPosition = new Vector3( basePosition.x, basePosition.y, heightTopLeft );

		return true;

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

		var modelPhysics = GetComponentsInChildren<ModelPhysics>( true );
		foreach ( var model in modelPhysics )
		{
			model.Enabled = false;
			Invoke( 0.01f, () => model.Enabled = true );
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

		// test add some items
		var nodeLink = new WorldNodeLink();
		nodeLink.Node = new GameObject();

		Items[Vector2Int.Zero] = new Dictionary<ItemPlacement, WorldNodeLink>();
		Items[Vector2Int.Zero][ItemPlacement.Floor] = nodeLink;
	}

	public void OnWorldUnloaded()
	{
		if ( IsProxy ) return;
		Log.Info( $"World {WorldId} unloaded" );
	}
}
