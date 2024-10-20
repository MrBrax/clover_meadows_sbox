using System;
using Clover.Items;

namespace Clover;

public sealed partial class World
{
	public static Rotation GetRotation( ItemRotation rotation )
	{
		return rotation switch
		{
			ItemRotation.North => Rotation.FromYaw( 180 ),
			ItemRotation.East => Rotation.FromYaw( 90 ),
			ItemRotation.South => Rotation.FromYaw( 0 ),
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

	public static ItemRotation GetItemRotationFromDirection( Direction direction )
	{
		return direction switch
		{
			Direction.North => ItemRotation.North,
			Direction.South => ItemRotation.South,
			Direction.West => ItemRotation.West,
			Direction.East => ItemRotation.East,
			Direction.NorthWest => ItemRotation.North,
			Direction.NorthEast => ItemRotation.East,
			Direction.SouthWest => ItemRotation.West,
			Direction.SouthEast => ItemRotation.South,
			_ => ItemRotation.North
		};
	}

	public static Vector2Int GetPositionInDirection( Vector2Int gridPos, Direction gridDirection )
	{
		return gridDirection switch
		{
			Direction.North => new Vector2Int( gridPos.x, gridPos.y - 1 ),
			Direction.South => new Vector2Int( gridPos.x, gridPos.y + 1 ),
			Direction.West => new Vector2Int( gridPos.x - 1, gridPos.y ),
			Direction.East => new Vector2Int( gridPos.x + 1, gridPos.y ),
			Direction.NorthWest => new Vector2Int( gridPos.x - 1, gridPos.y - 1 ),
			Direction.NorthEast => new Vector2Int( gridPos.x + 1, gridPos.y - 1 ),
			Direction.SouthWest => new Vector2Int( gridPos.x - 1, gridPos.y + 1 ),
			Direction.SouthEast => new Vector2Int( gridPos.x + 1, gridPos.y + 1 ),
			_ => gridPos
		};
	}

	public static Direction Get4Direction( float angle )
	{
		var snapAngle = MathF.Round( angle / 90 ) * 90;
		// Log.Info( $"Snap angle: {snapAngle}" );
		switch ( snapAngle )
		{
			case 0:
				return Direction.South;
			case 90:
				return Direction.East;
			case 180:
			case -180:
				return Direction.North;
			case -90:
				return Direction.West;
		}

		return Direction.North;
	}

	public IList<Vector2Int> GetNeighbors( Vector2Int gridPosition )
	{
		var neighbors = new List<Vector2Int>
		{
			new(gridPosition.x, gridPosition.y - 1),
			new(gridPosition.x, gridPosition.y + 1),
			new(gridPosition.x - 1, gridPosition.y),
			new(gridPosition.x + 1, gridPosition.y),
			new(gridPosition.x - 1, gridPosition.y - 1),
			new(gridPosition.x + 1, gridPosition.y - 1),
			new(gridPosition.x - 1, gridPosition.y + 1),
			new(gridPosition.x + 1, gridPosition.y + 1)
		};

		return neighbors;
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

	/*public void ValidateNodeLinks()
	{
		foreach ( var nodeLink in _nodeLinkGridMap.Values )
		{
			if ( !nodeLink.Node.IsValid() )
			{
				Log.Error( $"Node link {nodeLink} is invalid (node is null)" );
			}
		}

		var uniqueNodeLinks = _nodeLinkGridMap.Values.Select( x => x ).Distinct().ToList();
		foreach ( var nodeLink in uniqueNodeLinks )
		{
			var gridPositions = nodeLink.GetGridPositions( true );

			foreach ( var gridPos in gridPositions )
			{
				if ( !_nodeLinkGridMap.ContainsKey(
					    new NodeLinkMapKey( nodeLink.GridPosition, nodeLink.GridPlacement ) ) )
				{
					Log.Error( $"Node link {nodeLink} at {gridPos} is not in the grid map" );
				}
			}

		}

		foreach ( var gridMapEntry in _nodeLinkGridMap )
		{
			if ( gridMapEntry.Key.Position != gridMapEntry.Value.GridPosition )
			{
				Log.Error( $"Grid map entry {gridMapEntry.Value} at {gridMapEntry.Key.Position} has a different position than the node link" );
			}

			if ( gridMapEntry.Key.Placement != gridMapEntry.Value.GridPlacement )
			{
				Log.Error( $"Grid map entry {gridMapEntry.Value} at {gridMapEntry.Key.Position} has a different placement than the node link" );
			}

		}

		var worldItems = Scene.GetAllComponents<WorldItem>().Where( x => x.WorldLayerObject.Layer == Layer ).ToList();
		foreach ( var worldItem in worldItems )
		{
			if ( !worldItem.NodeLink.IsValid() )
			{
				Log.Error( $"World item {worldItem} has an invalid node link" );
			}

			// if ( worldItem.WorldLayerObject.World
		}

		Log.Info( "If you didn't see any errors just now, you're good." );
	}*/

	/*[ConCmd( "clover_world_validate_node_links")]
	public static void ValidateNodeLinksCmd()
	{
		foreach ( var world in WorldManager.Instance.Worlds.Values )
		{
			world.ValidateNodeLinks();
		}
	}*/

	public Direction Get4Direction( Rotation nodeWorldRotation )
	{
		return Get4Direction( nodeWorldRotation.Yaw() );
	}
}
