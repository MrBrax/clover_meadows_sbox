using System;
using Clover.Data;
using Clover.Inventory;
using Clover.Items;
using Clover.Persistence;
using Clover.Ui;

namespace Clover.Player;

[Title( "Item Placer" )]
[Icon( "inventory" )]
[Category( "Clover/Player" )]
public class ItemPlacer : Component, IWorldEvent
{
	private PlayerCharacter Player => GetComponent<PlayerCharacter>();

	[Property] public Model CursorModel { get; set; }

	public bool IsPlacing;
	public int InventorySlotIndex;

	public InventorySlot<PersistentItem> InventorySlot =>
		Player.Inventory.Container.GetSlotByIndex( InventorySlotIndex );

	private ItemData ItemData => InventorySlot.GetItem().ItemData;

	private GameObject _ghost;
	private ItemData _selectedItem;
	private bool _isPlacingFromInventory;

	protected override void OnStart()
	{
		Mouse.Visible = false;
	}

	public void StartMovingPlacedItem( WorldItem selectedGameObject )
	{
		if ( selectedGameObject == null )
		{
			return; 
		}
		Log.Info( selectedGameObject );
		if ( IsPlacing )
		{
			StopPlacing();
		}
		
		_isPlacingFromInventory = false;
		IsPlacing = true;
		_selectedItem = selectedGameObject.ItemData;
		selectedGameObject.DestroyGameObject();
		PlaceGhostInternal(_selectedItem.PlaceScene.Clone());
		Mouse.Visible = true;

	}
	
	void IWorldEvent.OnWorldChanged( World world )
	{
		StopPlacing();
	}

	public void StartPlacing( int inventorySlotIndex )
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

		_isPlacingFromInventory = true;
		InventorySlotIndex = inventorySlotIndex;
		IsPlacing = true;
		CreateGhostFromInventory();
		Mouse.Visible = true;
	}

	public void StopPlacing()
	{
		IsPlacing = false;
		DestroyGhost();
		_selectedItem = null;
		Mouse.Visible = false;
	}
	
	public void CreateGhostFromInventory()
	{
		var item = InventorySlot.GetItem();
		_selectedItem = item.ItemData;
		var gameObject = item.ItemData.PlaceScene.Clone();
		PlaceGhostInternal( gameObject );
		
	}

	private void PlaceGhostInternal(GameObject selectedGameObject)
	{
		
		selectedGameObject.NetworkMode = NetworkMode.Never;

		// kill all colliders
		foreach ( var collider in selectedGameObject.Components.GetAll<Collider>( FindMode.EverythingInSelfAndDescendants )
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
		foreach ( var worldItem in selectedGameObject.Components.GetAll<WorldItem>( FindMode.EverythingInSelfAndDescendants )
			         .ToList() )
		{
			worldItem.Destroy();
		}

		// tint the ghost
		foreach ( var renderable in selectedGameObject.Components
			         .GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants ).ToList() )
		{
			renderable.Tint = renderable.Tint.WithAlpha( 0.5f );
		}

		selectedGameObject.WorldPosition = Player.WorldPosition;

		_ghost = selectedGameObject;

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
		if ( !IsPlacing ) return;
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
				PlaceItem();
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
			StopPlacing();
		}

		if ( Input.Pressed( "RotateClockwise" ) )
		{
			_ghost.WorldRotation *= Rotation.FromYaw( -15 );
		}
		else if ( Input.Pressed( "RotateCounterClockwise" ) )
		{
			_ghost.WorldRotation *= Rotation.FromYaw( 15 );
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
			Player.World.SpawnPlacedNode( _selectedItem, _ghost.WorldPosition, _ghost.WorldRotation );
		}
		catch ( Exception e )
		{
			// Log.Error( e );
			Player.Notify( Notifications.NotificationType.Error, e.Message );
			return;
		}

		if ( _isPlacingFromInventory )
		{
			InventorySlot.TakeOneOrDelete();
		}
		
		StopPlacing();
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
			.WithoutTags( "player", "invisiblewall" )
			.Run();

		if ( !trace.Hit )
		{
			_isValidPlacement = false;
			return;
		}

		// Gizmo.Transform = new Transform( trace.EndPosition );
		// Gizmo.Draw.LineBBox( box );

		var endPosition = trace.EndPosition;

		if ( SnapEnabled && !Input.Down( "Snap" ) )
		{
			endPosition = endPosition.SnapToGrid( SnapDistance );
		}

		endPosition += _selectedItem.PlaceModeOffset;

		// var gridPosition = Player.World.WorldToItemGrid( endPosition );

		// TODO: Check if the item can be placed here
		_isValidPlacement = !Player.World.IsPositionOccupied( endPosition, 4f ) &&
		                    !Player.World.IsNearPlayer( endPosition );

		_ghost.WorldPosition = endPosition;
	}

	[ConVar( "clover_itemplacer_snap_enabled", Saved = true )]
	public static bool SnapEnabled { get; set; } = false;

	[ConVar( "clover_itemplacer_snap_distance", Saved = true )]
	public static float SnapDistance { get; set; } = 16f;

	[ConVar( "clover_itemplacer_show_grid", Saved = true )]
	public static bool ShowGrid { get; set; } = true;


	protected override void OnUpdate()
	{
		if ( IsPlacing && ShowGrid )
		{
			// TODO: re-add grid when it can be positioned relative to the player
			// Gizmo.Transform = new Transform( Player.WorldPosition );
			// Gizmo.Draw.Grid( Gizmo.GridAxis.XY, new Vector2( 32f, 32f ) );
		}

		if ( IsPlacing && _ghost.IsValid() )
		{
			Gizmo.Draw.Line( _ghost.WorldPosition, _ghost.WorldPosition + _ghost.WorldRotation.Down * 256f );
			Gizmo.Draw.LineBBox( BBox.FromPositionAndSize( _colliderCenter, _colliderSize )
				.Rotate( _ghost.WorldRotation ).Translate( _ghost.WorldPosition ) );
		}
	}
}
