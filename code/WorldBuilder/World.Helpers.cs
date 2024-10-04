using System;

namespace Clover;

public sealed partial class World
{
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
	
}
