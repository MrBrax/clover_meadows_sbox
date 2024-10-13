using System;
using System.Text.Json.Serialization;
using Clover.Data;
using Clover.Inventory;
using Clover.Player;

namespace Clover.Items;

[Category( "Clover/Items" )]
[Icon( "outlet" )]
[Description( "Has to be added to items placed on the world grid, otherwise they will not be saved." )]
public class WorldItem : Component, IPickupable
{
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }

	public WorldNodeLink NodeLink => !Scene.IsEditor ? WorldLayerObject.World?.GetItem( GameObject ) : null;

	public Vector2Int GridPosition => NodeLink?.GridPosition ?? Vector2Int.Zero;
	
	public Vector2Int Size =>
		!Scene.IsEditor ? NodeLink.Size : new Vector2Int( ItemData?.Width ?? 1, ItemData?.Height ?? 1 );

	public World.ItemPlacement GridPlacement => !Scene.IsEditor ? NodeLink.GridPlacement : World.ItemPlacement.Floor;
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

		if ( Scene != GameObject )
		{
			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.Text( "NOT AT ROOT", new Transform( WorldPosition ), "Roboto", 24f );
			Gizmo.Draw.Color = Color.White;
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

	/*protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( Gizmo.Camera == null ) return;
		if ( NodeLink.IsValid() && NodeLink.IsDroppedItem )
		{
			Gizmo.Draw.Text( $"Dropped: {NodeLink.GetName()}", new Transform( WorldPosition + Vector3.Up * 20 ) );
		}
	}*/

	[Property] public bool CanPickupSimple { get; set; }

	public bool CanPickup( PlayerCharacter player )
	{
		return !HasItemOnTop() && CanPickupSimple;
	}

	public bool HasItemOnTop()
	{
		var gridPositions = NodeLink.GetGridPositions( true );

		foreach ( var pos in gridPositions )
		{
			// if this item has an item on top of it then it can't be picked up
			if ( WorldLayerObject.World.GetItems( pos ).Any( x => x.GridPlacement == World.ItemPlacement.OnTop ) &&
			     GridPlacement == World.ItemPlacement.Floor )
			{
				Log.Warning( $"An item is on top of this item" );
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///  Called when the player picks up this item, mostly so you don't have to add IPickupable to every item.
	///  This will just call the Inventory.PickUpItem method on the node link.
	///  ALL IPickupable items will be iterated through when trying to pick up something.
	/// </summary>
	/// <param name="player"></param>
	public void OnPickup( PlayerCharacter player )
	{
		player.Inventory.PickUpItem( NodeLink );
	}
}
