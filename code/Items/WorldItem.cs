using System;
using System.Text.Json.Serialization;
using Clover.Data;
using Clover.Inventory;
using Clover.Player;

namespace Clover.Items;

[Category( "Clover/Items" )]
[Icon( "outlet" )]
public class WorldItem : Component, IPickupable
{
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }

	public WorldNodeLink NodeLink => WorldLayerObject?.World?.GetItem( GameObject );

	private Vector2Int _tilePosition { get; set; }

	/*[Property, ReadOnly, Sync]
	public Vector2Int TilePosition
	{
		get => _tilePosition;
		set
		{
			if ( Scene.IsEditor || value == Vector2Int.Zero ) // TODO: don't make zero a special case
			{
				_tilePosition = value;
				return;
			}

			var oldPosition = _tilePosition;

			// Log.Info( $"Setting tile position of {GameObject.Name} to {value}" );
			_tilePosition = value;
			// WorldPosition = new Vector3( value.x * 32, value.y * 32, 0 );
			WorldPosition = WorldManager.Instance.GetWorld( WorldLayerObject.Layer ).ItemGridToWorld( value );
			// LevelManager.Instance?.OnWorldObjectMoved( this, oldPosition, value );
			// OnTilePositionChanged?.Invoke( _tilePosition, value );
		}
	}

	[Property, ReadOnly, Sync]
	public Vector2Int Size { get; set; } = new(1, 1);*/

	/*public delegate void TilePositionChanged( Vector2Int oldPosition, Vector2Int newPosition );
	[JsonIgnore] public TilePositionChanged OnTilePositionChanged;*/

	public Vector2Int GridPosition => NodeLink?.GridPosition ?? Vector2Int.Zero;

	[Property, ReadOnly]
	public Vector2Int Size => NodeLink?.Size ?? new Vector2Int( ItemData?.Width ?? 1, ItemData?.Height ?? 1 );

	public World.ItemPlacement GridPlacement => NodeLink?.GridPlacement ?? World.ItemPlacement.Floor;
	public World.ItemPlacementType GridPlacementType => NodeLink?.PlacementType ?? World.ItemPlacementType.Placed;
	public World.ItemRotation GridRotation => NodeLink?.GridRotation ?? World.ItemRotation.North;


	private string _prefab;

	[Property, ResourceType( "prefab" )]
	public string Prefab
	{
		get { return !string.IsNullOrEmpty( _prefab ) ? _prefab : GameObject.PrefabInstanceSource; }
		set { _prefab = value; }
	}

	[Property] public ItemData ItemData { get; set; }

	protected override void OnAwake()
	{
		if ( Scene.IsEditor ) return;

		GameObject.Transform.OnTransformChanged += GameObjectTransformChanged;

		base.OnEnabled();

		BackupPrefabSource();
		/*
		if ( Transform.Position != Vector3.Zero )
		{
			// Log.Warning( $"WorldObject {GameObject.Name} has no position set" );
			SetPosition( Transform.Position );
		}*/
	}

	private void BackupPrefabSource()
	{
		// Log.Info( $"WorldItem {GameObject.Name} has prefab source {GameObject.PrefabInstanceSource}, backup is {Prefab}" );
		if ( !string.IsNullOrEmpty( GameObject.PrefabInstanceSource ) && GameObject.PrefabInstanceSource != Prefab )
		{
			Log.Error(
				$"WorldItem {GameObject.Name} has prefab source {GameObject.PrefabInstanceSource}, backup is {Prefab}" );
			Prefab = GameObject.PrefabInstanceSource;
		}
	}

	private void GameObjectTransformChanged()
	{
		// Log.Info( $"WorldItem {GameObject.Name} transform changed" );
	}

	public delegate void OnObjectSave( WorldNodeLink nodeLink );

	[Property] public OnObjectSave OnItemSave { get; set; }

	public delegate void OnObjectLoad( WorldNodeLink nodeLink );

	[Property] public OnObjectLoad OnItemLoad { get; set; }

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( Gizmo.Camera == null ) return;

		if ( ItemData == null ) return;

		Gizmo.Transform = global::Transform.Zero;

		if ( Size == Vector2Int.Zero )
		{
			Gizmo.Draw.Text( "No size", new Transform( WorldPosition + Vector3.Up * 32 ) );
			return;
		}

		var gridSize = World.GridSize;

		/*var mins = new Vector3( GridPosition.x * gridSize, GridPosition.y * gridSize, gridSize );
		var maxs = new Vector3( (GridPosition.x + Size.x) * gridSize, (GridPosition.y + Size.y) * gridSize, 0 );

		var bbox = new BBox( mins, maxs );*/

		var bbox = BBox.FromPositionAndSize( WorldPosition + (Vector3.Up * (gridSize / 2f)),
			new Vector3( gridSize * Size.x, gridSize * Size.y, 32 ) );

		Gizmo.Draw.LineBBox( bbox );
	}

	/*protected override void OnUpdate()
	{
		base.OnUpdate();
		DrawGizmos();
	}*/

	/*protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( Gizmo.Camera == null ) return;
		Gizmo.Draw.Text( WorldPosition.ToString(), new Transform( WorldPosition ) );
	}*/

	[Property] public bool CanPickupSimple { get; set; }

	public bool CanPickup( PlayerCharacter player )
	{
		/*if ( WorldLayerObject.World.GetItems( GridPosition ).ToList().FirstOrDefault()?.GridPlacement ==
		    World.ItemPlacement.OnTop && GridPlacement == World.ItemPlacement.Floor )
		{
			Log.Warning( $"Item {this} is on top of another item" );
			return false;
		}*/

		if ( HasItemOnTop() )
		{
			return false;
		}

		Log.Info( $"CanPickupSimple {GameObject}: {CanPickupSimple}" );

		return CanPickupSimple;
	}

	public bool HasItemOnTop()
	{
		var gridPositions = NodeLink.GetGridPositions( true );

		foreach ( var pos in gridPositions )
		{
			if ( WorldLayerObject.World.GetItems( pos ).ToList().FirstOrDefault()?.GridPlacement == World.ItemPlacement.OnTop &&
			     GridPlacement == World.ItemPlacement.Floor )
			{
				Log.Warning( $"An item is on top of this item" );
				return true;
			}
		}

		return false;
	}

	public void OnPickup( PlayerCharacter player )
	{
		player.Inventory.PickUpItem( NodeLink );
	}
}
