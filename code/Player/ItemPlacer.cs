using System;
using Clover.Data;
using Clover.Inventory;
using Clover.Items;
using Clover.Persistence;
using Clover.Ui;
using Clover.WorldBuilder;

namespace Clover.Player;

[Title( "Item Placer" )]
[Icon( "inventory" )]
[Category( "Clover/Player" )]
public class ItemPlacer : Component, IWorldEvent
{
	private PlayerCharacter Player => GetComponent<PlayerCharacter>();

	[Property] public Model CursorModel { get; set; }

	public bool IsPlacing;
	public bool IsMoving;
	public int InventorySlotIndex;

	public InventorySlot<PersistentItem> InventorySlot =>
		Player.Inventory.Container.GetSlotByIndex( InventorySlotIndex );

	private ItemData ItemData => InventorySlot.GetItem().ItemData;

	private GameObject _ghost;
	private ItemData _selectedItemData;
	// private bool _isPlacingFromInventory;

	public WorldItem CurrentPlacedItem { get; set; }

	protected override void OnStart()
	{
		Mouse.Visible = false;
	}

	public void StartMovingPlacedItem( WorldItem selectedGameObject )
	{
		if ( !selectedGameObject.IsValid() )
		{
			return;
		}

		if ( IsPlacing )
		{
			StopPlacing();
		}

		// _isPlacingFromInventory = false;
		IsMoving = true;
		_selectedItemData = selectedGameObject.ItemData;
		// selectedGameObject.DestroyGameObject();

		CurrentPlacedItem = selectedGameObject;

		var clone = selectedGameObject.GameObject.Clone( selectedGameObject.WorldPosition, selectedGameObject.WorldRotation );
		PlaceGhostInternal( clone );

		// TODO: hide worlditem
		CurrentPlacedItem.Hide();

		Mouse.Visible = true;
		Mouse.Position = Scene.Camera.PointToScreenPixels( selectedGameObject.WorldPosition );
	}

	void IWorldEvent.OnWorldChanged( World world )
	{
		StopPlacing();
		StopMoving();
	}

	public void StartPlacingInventorySlot( int inventorySlotIndex )
	{
		if ( !Player.IsValid() ) throw new System.Exception( "Player is not valid" );
		if ( !Player.Inventory.IsValid() ) throw new System.Exception( "Player Inventory is not valid" );
		if ( Player.Inventory.Container == null )
			throw new System.Exception( "Player Inventory Container is not valid" );

		if ( Player.World.Data.DisableItemPlacement )
		{
			Player.Notify( Notifications.NotificationType.Error, "Item placement is disabled in this area" );
			return;
		}

		if ( IsPlacing )
		{
			StopPlacing();
		}

		// _isPlacingFromInventory = true;
		InventorySlotIndex = inventorySlotIndex;
		IsPlacing = true;
		CreateGhostFromInventory();
		Mouse.Visible = true;
	}

	public void StopPlacing()
	{
		IsMoving = false;
		IsPlacing = false;
		DestroyGhost();
		_selectedItemData = null;
		Mouse.Visible = false;
	}

	public void StopMoving()
	{
		IsMoving = false;
		IsPlacing = false;
		DestroyGhost();
		_selectedItemData = null;
		if ( CurrentPlacedItem.IsValid() )
		{
			CurrentPlacedItem.Show();
		}

		CurrentPlacedItem = null;
		Mouse.Visible = false;
	}

	public void CreateGhostFromInventory()
	{
		var item = InventorySlot.GetItem();
		_selectedItemData = item.ItemData;
		var gameObject = item.ItemData.PlaceScene.Clone( Player.WorldPosition,
			Rotation.FromYaw( Player.PlayerController.Yaw ).Angles().SnapToGrid( RotationDistance ) );

		PlaceGhostInternal( gameObject );
	}

	private void PlaceGhostInternal( GameObject clonedGameObject )
	{
		clonedGameObject.NetworkMode = NetworkMode.Never;

		// kill all colliders
		foreach ( var collider in clonedGameObject.Components
			         .GetAll<Collider>( FindMode.EverythingInSelfAndDescendants )
			         .ToList() )
		{
			if ( collider is BoxCollider boxCollider )
			{
				_colliderSize = boxCollider.Scale;
				_colliderCenter = boxCollider.Center;
			}

			collider.Destroy();
		}

		// kill all worlditems
		foreach ( var worldItem in clonedGameObject.Components
			         .GetAll<WorldItem>( FindMode.EverythingInSelfAndDescendants )
			         .ToList() )
		{
			worldItem.Destroy();
		}

		// tint the ghost
		foreach ( var renderable in clonedGameObject.Components
			         .GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants ).ToList() )
		{
			renderable.Tint = renderable.Tint.WithAlpha( 0.5f );
		}

		_ghost = clonedGameObject;

		// create cursor
		/*cursor = Scene.CreateObject();
		cursor.NetworkMode = NetworkMode.Never;

		var model = cursor.AddComponent<ModelRenderer>();
		model.Tint = Color.Parse( "#FFFFFF2B" ) ?? Color.White;
		model.Model = CursorModel;
		model.RenderType = ModelRenderer.ShadowRenderType.Off;

		cursor.WorldScale = new Vector3( 1, 1, 0.3f );*/
	}

	public void DestroyGhost()
	{
		if ( _ghost.IsValid() )
		{
			_ghost.Destroy();
		}

		_ghost = null;

		/*if ( cursor.IsValid() )
		{
			cursor.Destroy();
		}

		cursor = null;*/
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		DestroyGhost();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;
		if ( !IsPlacing && !IsMoving ) return;
		if ( !_ghost.IsValid() ) return;
		CheckInput();
		UpdateGhostTransform();
		UpdateVisuals();
	}

	private void CheckInput()
	{
		if ( Input.Pressed( "attack1" ) )
		{
			if ( _isValidPlacement )
			{
				if ( IsPlacing )
				{
					PlaceItem();
				}
				else if ( IsMoving )
				{
					MoveItem();
				}
			}
			else
			{
				Log.Info( "Bad" );
				Player.Notify( Notifications.NotificationType.Warning, "Invalid placement" );
			}

			Input.Clear( "use" );
		}

		if ( Input.Pressed( "attack2" ) )
		{
			if ( IsPlacing )
			{
				StopPlacing();
			}
			else if ( IsMoving )
			{
				StopMoving();
			}
		}

		if ( Input.Pressed( "RotateClockwise" ) )
		{
			_ghost.WorldRotation *= Rotation.FromYaw( -RotationDistance );
		}
		else if ( Input.Pressed( "RotateCounterClockwise" ) )
		{
			_ghost.WorldRotation *= Rotation.FromYaw( RotationDistance );
		}

		/*else if ( Input.Pressed( "cancel" ) )
		{
			StopPlacing();
		}*/
	}

	private void PlaceItem()
	{
		try
		{
			Player.World.SpawnPlacedNode( _selectedItemData, _ghost.WorldPosition, _ghost.WorldRotation );
		}
		catch ( Exception e )
		{
			// Log.Error( e );
			Player.Notify( Notifications.NotificationType.Error, e.Message );
			return;
		}

		InventorySlot.TakeOneOrDelete();

		StopPlacing();
	}


	private void MoveItem()
	{
		if ( !CurrentPlacedItem.IsValid() )
		{
			Log.Error( "CurrentPlacedItem is not valid" );
			StopMoving();
			return;
		}

		CurrentPlacedItem.WorldPosition = _ghost.WorldPosition;
		CurrentPlacedItem.WorldRotation = _ghost.WorldRotation;
		CurrentPlacedItem.Transform.ClearInterpolation();

		StopMoving();

		Log.Info( "Moved item" );
	}


	private Material _invalidMaterial = Material.Load( "materials/ghost_invalid.vmat" );

	private void SetGhostTint()
	{
		if ( !_ghost.IsValid() ) return;

		var s = MathF.Sin( Time.Now * 5 ) * 0.4f;

		foreach ( var renderable in _ghost.Components.GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants )
			         .ToList() )
		{
			renderable.Tint = _isValidPlacement ? Color.White.WithAlpha( 0.5f + s ) : Color.Red.WithAlpha( 0.5f + s );

			renderable.MaterialOverride = _isValidPlacement ? null : _invalidMaterial;
		}
	}

	/*private void SetCursorTintColor( Color color )
	{
		var renderable = cursor.GetComponent<ModelRenderer>();
		renderable.SceneObject.Attributes.Set( "tint", color.WithAlpha( 1 ) );
		renderable.SceneObject.Attributes.Set( "opacity", color.a );
	}*/

	private void UpdateVisuals()
	{
		SetGhostTint();
		// SetCursorTintColor( _isValidPlacement ? Color.White.WithAlpha( 0.2f + s ) : Color.Red.WithAlpha( 0.2f + s ) );
	}

	private bool _isValidPlacement;

	// private Vector2Int _lastGridPosition;
	// private World.ItemRotation _lastGridRotation;
	// private World.ItemPlacement _lastItemPlacement = World.ItemPlacement.Floor;
	private Vector3 _colliderSize;
	private Vector3 _colliderCenter;

	private void UpdateGhostTransform()
	{
		if ( !_ghost.IsValid() ) return;

		if ( _colliderSize == Vector3.Zero )
		{
			Log.Error( "Collider size is zero" );
			return;
		}

		var ray = Scene.Camera.ScreenPixelToRay( Mouse.Position );

		var box = BBox.FromPositionAndSize( _colliderCenter, _colliderSize );

		box = box.Rotate( _ghost.WorldRotation );

		var trace = Scene.Trace.Box( box, ray, 1000f )
			.WithoutTags( "player", "invisiblewall", "doorway", "stairs", "room_invisible", "invisible" )
			.Run();

		if ( !trace.Hit )
		{
			_isValidPlacement = false;
			return;
		}

		if ( trace.GameObject.Tags.Has( "noplace" ) )
		{
			_isValidPlacement = false;
			return;
		}

		var interiorManager = Player.World.Components.Get<InteriorManager>( FindMode.InDescendants );
		if ( interiorManager != null && !interiorManager.IsInCurrentRoom( trace.EndPosition ) )
		{
			_isValidPlacement = false;
			return;
		}

		var endPosition = trace.EndPosition;

		if ( SnapEnabled && !Input.Down( "Snap" ) )
		{
			endPosition = endPosition.SnapToGrid( SnapDistance );
		}
		else if ( !SnapEnabled && Input.Down( "Snap" ) )
		{
			endPosition = endPosition.SnapToGrid( SnapDistance );
		}

		endPosition += _selectedItemData.PlaceModeOffset;

		// var gridPosition = Player.World.WorldToItemGrid( endPosition );

		// TODO: Check if the item can be placed here
		_isValidPlacement = !Player.World.IsPositionOccupied( endPosition, CurrentPlacedItem?.GameObject, 4f ) &&
		                    !Player.World.IsNearPlayer( endPosition );

		_ghost.WorldPosition = endPosition;
	}

	[ConVar( "clover_itemplacer_snap_enabled", Saved = true )]
	public static bool SnapEnabled { get; set; } = false;

	[ConVar( "clover_itemplacer_snap_distance", Saved = true )]
	public static float SnapDistance { get; set; } = 16f;

	[ConVar( "clover_itemplacer_show_grid", Saved = true )]
	public static bool ShowGrid { get; set; } = true;

	[ConVar( "clover_itemplacer_rotation_distance", Saved = true )]
	public static float RotationDistance { get; set; } = 15f;


	protected override void OnUpdate()
	{
		if ( IsPlacing && ShowGrid )
		{
			// TODO: re-add grid when it can be positioned relative to the player
			// Gizmo.Transform = new Transform( Player.WorldPosition );
			// Gizmo.Draw.Grid( Gizmo.GridAxis.XY, new Vector2( 32f, 32f ) );
		}

		if ( (IsPlacing || IsMoving) && _ghost.IsValid() )
		{
			var trace = Scene.Trace.Ray( _ghost.WorldPosition, _ghost.WorldPosition + Vector3.Down * 300f )
				.WithoutTags( "player", "invisiblewall", "doorway", "stairs", "room_invisible" )
				.Run();

			if ( trace.Hit )
			{
				var endPos = trace.EndPosition;
				Gizmo.Draw.Arrow( _ghost.WorldPosition, endPos );
			}

			Gizmo.Draw.LineBBox( BBox.FromPositionAndSize( _colliderCenter, _colliderSize )
				.Rotate( _ghost.WorldRotation ).Translate( _ghost.WorldPosition ) );
		}
	}
}
