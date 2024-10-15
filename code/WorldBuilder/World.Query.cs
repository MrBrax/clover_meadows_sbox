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

		/*if ( Items == null )
		{
			throw new Exception( "Items is null" );
		}*/

		// HashSet<WorldNodeLink> foundItems = new();

		// Log.Info( $"Getting items at {gridPos}" );

		// get items at exact grid position
		/*if ( Items.TryGetValue( gridPos, out var dict ) )
		{
			foreach ( var item in dict.Values )
			{
				// Log.Info( $"Found item {item.GetName()} at {gridPos} with exact placement" );
				foundItems.Add( item );
				yield return item;
			}
		}*/

		/*foreach ( var entry in _nodeLinkGridMap.Where( x => x.Key.Position == gridPos ) )
		{
			// Log.Info( $"Found item {entry.Value.GetName()} at {gridPos} in grid map" );
			// foundItems.Add( entry.Value );
			yield return entry.Value;
		}*/

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

		// TODO: rework for new system
		var w = ItemGridToWorld( gridPos );
		return Items.Where( x => x.WorldPosition.Distance( w ) < GridSize );
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
		// return _nodeLinkGridMap.Values.FirstOrDefault( x => x.Node == node );
		return Items.FirstOrDefault( x => x.Node == node );
	}
	
}
