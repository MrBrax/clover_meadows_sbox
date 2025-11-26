using System;
using Clover.Items;

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
	public IEnumerable<WorldItem> GetItems( Vector2Int gridPos, float radius = 16f )
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

		return WorldItems.Where( x => x.WorldPosition.Distance( w ) < radius );
	}

	public IEnumerable<WorldItem> GetItems( Vector3 worldPos, float radius = 16f )
	{
		if ( !Networking.IsHost )
		{
			throw new Exception( "Only the host can query the world" );
		}

		return WorldItems.Where( x => x.WorldPosition.Distance( worldPos ) < radius );
	}

	public T GetItem<T>( Vector2Int gridPos, float radius = 16f ) where T : Component
	{
		return GetItems( gridPos, radius ).Select( x => x.GetComponent<T>() ).FirstOrDefault();
	}

	public bool IsPositionOccupied( Vector2Int gridPos )
	{
		return GetItems( gridPos ).Any();
	}

	public bool IsPositionOccupied( Vector3 worldPos, float radius = 16f )
	{
		return GetItems( worldPos, radius ).Any();
	}

	public bool IsPositionOccupied( Vector3 endPosition, GameObject ignoreMe, float f )
	{
		return WorldItems.Any( x => x.GameObject != ignoreMe && x.WorldPosition.Distance( endPosition ) < f );
	}

	public bool IsNearPlayer( Vector3 worldPos, float radius = 16f )
	{
		return PlayersInWorld.Any( x => x.WorldPosition.Distance( worldPos ) < radius );
	}

	[Obsolete]
	public WorldItem GetWorldItem( GameObject node )
	{
		return WorldItems.FirstOrDefault( x => x.GameObject == node );
	}

	public WorldItem GetWorldItem<T>( Vector2Int gridPos ) where T : Component
	{
		return WorldItems.FirstOrDefault( x =>
			x.WorldPosition == ItemGridToWorld( gridPos ) && x.GameObject.GetComponent<T>() != null );
	}
}
