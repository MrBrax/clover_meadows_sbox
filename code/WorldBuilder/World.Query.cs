using System;

namespace Clover;

public sealed partial class World
{
	/// <summary>
	/// Retrieves the WorldNodeLinks at the specified grid position.
	/// This method will return items that are intersecting the grid position as well, if they are larger than 1x1.
	/// <br />Use <see cref="WorldNodeLink.Node"/> to get the actual node.
	/// </summary>
	/// <param name="gridPos">The grid position to retrieve items from.</param>
	/// <returns>An enumerable collection of WorldNodeLink items at the specified grid position.</returns>
	public IEnumerable<WorldNodeLink> GetItems( Vector2Int gridPos )
	{
		if ( !Networking.IsHost )
		{
			throw new Exception( "Only the host can query the world" );
		}

		if ( IsOutsideGrid( gridPos ) )
		{
			throw new Exception( $"Position {gridPos} is outside the grid" );
		}

		// TODO: rework for new system
		var w = ItemGridToWorld( gridPos );
		return Items.Where( x => x.WorldPosition.Distance( w ) < GridSize );
	}

	public IEnumerable<WorldNodeLink> GetItems( Vector3 worldPos, float radius = 16f )
	{
		if ( !Networking.IsHost )
		{
			throw new Exception( "Only the host can query the world" );
		}

		return Items.Where( x => x.WorldPosition.Distance( worldPos ) < radius );
	}

	public bool IsPositionOccupied( Vector2Int gridPos )
	{
		return GetItems( gridPos ).Any();
	}

	public bool IsPositionOccupied( Vector3 worldPos, float radius = 16f )
	{
		return GetItems( worldPos, radius ).Any();
	}

	public bool IsNearPlayer( Vector3 worldPos, float radius = 16f )
	{
		return PlayersInWorld.Any( x => x.WorldPosition.Distance( worldPos ) < radius );
	}

	/*/// <summary>
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
	}*/

	public WorldNodeLink GetNodeLink( GameObject node )
	{
		// return _nodeLinkGridMap.Values.FirstOrDefault( x => x.Node == node );
		return Items.FirstOrDefault( x => x.Node == node );
	}

	public WorldNodeLink GetNodeLink<T>( Vector2Int gridPos ) where T : Component
	{
		return Items.FirstOrDefault( x =>
			x.WorldPosition == ItemGridToWorld( gridPos ) && x.Node.GetComponent<T>() != null );
	}
}
