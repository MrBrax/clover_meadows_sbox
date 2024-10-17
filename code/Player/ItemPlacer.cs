using System;
using Clover.Data;
using Clover.Inventory;
using Clover.Items;
using Clover.Persistence;
using Clover.Ui;

namespace Clover.Player;

public class ItemPlacer : Component
{
	private PlayerCharacter Player => GetComponent<PlayerCharacter>();

	[Property] public Model CursorModel { get; set; }

	public bool IsPlacing;
	public int InventorySlotIndex;

	public InventorySlot<PersistentItem> InventorySlot =>
		Player.Inventory.Container.GetSlotByIndex( InventorySlotIndex );

	private ItemData ItemData => InventorySlot.GetItem().ItemData;

	private GameObject _ghost;

	protected override void OnStart()
	{
		Mouse.Visible = true;
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

		InventorySlotIndex = inventorySlotIndex;
		IsPlacing = true;
		CreateGhost();
		Mouse.Visible = true;
	}

	public void StopPlacing()
	{
		IsPlacing = false;
		DestroyGhost();
		// Mouse.Visible = false;
	}

	public void CreateGhost()
	{
		var item = InventorySlot.GetItem();

		var gameObject = item.ItemData.PlaceScene.Clone();
		gameObject.NetworkMode = NetworkMode.Never;

		// kill all colliders
		foreach ( var collider in gameObject.Components.GetAll<Collider>( FindMode.EverythingInSelfAndDescendants )
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
		foreach ( var worldItem in gameObject.Components.GetAll<WorldItem>( FindMode.EverythingInSelfAndDescendants )
			         .ToList() )
		{
			worldItem.Destroy();
		}

		// tint the ghost
		foreach ( var renderable in gameObject.Components
			         .GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants ).ToList() )
		{
			renderable.Tint = renderable.Tint.WithAlpha( 0.5f );
		}

		gameObject.WorldPosition = Player.WorldPosition;

		_ghost = gameObject;

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
				Player.Notify( Notifications.NotificationType.Warning, "Invalid placement" );
			}

			Input.Clear( "use" );
		}

		if ( Input.Pressed( "attack2" ) )
		{
			StopPlacing();
		}

		if ( Input.MouseWheel.y != 0 )
		{
			_ghost.WorldRotation *= Rotation.FromYaw( Input.MouseWheel.y * 15 );
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
			Player.World.SpawnPlacedNode( InventorySlot.GetItem(), _ghost.WorldPosition, _ghost.WorldRotation,
				_lastItemPlacement );
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

	private void SetGhostTint( Color color )
	{
		if ( !_ghost.IsValid() ) return;
		foreach ( var renderable in _ghost.Components.GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants )
			         .ToList() )
		{
			renderable.Tint = color;
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
		var s = MathF.Sin( Time.Now * 5 ) * 0.1f;
		SetGhostTint( _isValidPlacement ? Color.White.WithAlpha( 0.5f + s ) : Color.Red.WithAlpha( 0.5f + s ) );
		// SetCursorTintColor( _isValidPlacement ? Color.White.WithAlpha( 0.2f + s ) : Color.Red.WithAlpha( 0.2f + s ) );
	}

	private bool _isValidPlacement;

	private Vector2Int _lastGridPosition;
	private World.ItemRotation _lastGridRotation;
	private World.ItemPlacement _lastItemPlacement = World.ItemPlacement.Floor;
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

		var trace = Scene.Trace.Box( box, ray, 10000f )
			.WithoutTags( "player" )
			.Run();

		if ( !trace.Hit ) return;

		// Gizmo.Transform = new Transform( trace.EndPosition );
		// Gizmo.Draw.LineBBox( box );

		var endPosition = trace.EndPosition;

		if ( SnapEnabled )
		{
			endPosition = endPosition.SnapToGrid( SnapDistance );
		}

		endPosition += ItemData.PlaceModeOffset;

		// var gridPosition = Player.World.WorldToItemGrid( endPosition );

		// TODO: Check if the item can be placed here
		_isValidPlacement = true;

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
			Gizmo.Draw.Grid( Gizmo.GridAxis.XY, 32f );
		}
	}
}
