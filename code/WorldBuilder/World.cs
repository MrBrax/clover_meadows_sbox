using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Clover.Data;
using Clover.Player;

namespace Clover;

[Category( "Clover/World" )]
[Icon( "world" )]
public sealed partial class World : Component
{
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

	public HashSet<WorldNodeLink> Items { get; set; } = new();


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

	public void RemoveItem( GameObject node )
	{
		// RemoveItem( item.GridPosition, item.Placement );
		var nodeLink = GetNodeLink( node );
		if ( nodeLink == null )
		{
			throw new Exception( $"Failed to find node link for {node}" );
		}

		// RemoveItem( nodeLink.GridPosition, nodeLink.GridPlacement );

		nodeLink.DestroyNode();

		Items.Remove( nodeLink );

		OnItemRemoved?.Invoke( nodeLink );
	}

	public void RemoveItem( WorldNodeLink nodeLink )
	{
		RemoveItem( nodeLink.Node );
	}

	public void Setup()
	{
		var layerObjects = GetComponentsInChildren<WorldLayerObject>( true );
		foreach ( var layerObject in layerObjects )
		{
			layerObject.SetLayer( Layer );
		}

		Scene.NavMesh.Generate( Scene.PhysicsWorld );
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

		if ( ShowGrid )
		{
			Gizmo.Draw.Grid( Gizmo.GridAxis.XY, 32f );
		}

		if ( !ShowGridInfo ) return;

		if ( !Game.IsEditor || Gizmo.Camera == null || Layer != WorldManager.Instance.ActiveWorldIndex ) return;

		Gizmo.Transform = new Transform( WorldPosition );
		// Log.Info( WorldId + ": " + WorldPosition  );

		/*foreach ( var pos in _blockedTiles )
		{
			Gizmo.Draw.Text( pos.ToString(), new Transform( ItemGridToWorld( pos ) ) );
		}

		foreach ( var item in Items.Values.SelectMany( x => x.Values ) )
		{
			Gizmo.Draw.Text( item.GridPosition.ToString(), new Transform( ItemGridToWorld( item.GridPosition ) ) );
		}*/

		/*var i = 0;
		foreach ( var item in _nodeLinkGridMap )
		{
			// var pos = item.Key.Split( ':' )[0].Split( ',' ).Select( int.Parse ).ToArray();
			var offset = item.Key.Placement == ItemPlacement.OnTop ? Vector3.Up * 32f : Vector3.Zero;
			Gizmo.Draw.Text( $"{item.Key.Position} {item.Key.Placement} | {item.Value.GetName()}\n{item.Value.GridRotation}",
				new Transform( ItemGridToWorld( item.Key.Position ) + offset ) );

			Gizmo.Draw.ScreenText( $"[{item.Key.Position}:{item.Key.Placement}] {item.Value.GetName()}",
				new Vector2( 20f, 20f + ((i++) * 20f) ) );
		}*/
	}

	[ConVar( "clover_show_grid_info" )] public static bool ShowGridInfo { get; set; }

	[ConVar( "clover_show_grid" )] public static bool ShowGrid { get; set; }

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
		// return _nodeLinkGridMap.ContainsValue( node );
		return Items.Contains( node );
	}
}
