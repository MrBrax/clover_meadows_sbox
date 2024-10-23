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
	[ConVar( "clover_itemplacer_position_snap_enabled", Saved = true )]
	public static bool PositionSnapEnabled { get; set; } = false;

	[ConVar( "clover_itemplacer_position_snap_distance", Saved = true )]
	public static float PositionSnapDistance { get; set; } = 16f;

	public readonly float[] PositionSnapDistances = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };

	[ConVar( "clover_itemplacer_show_grid", Saved = true )]
	public static bool ShowGrid { get; set; } = true;

	[ConVar( "clover_itemplacer_rotation_snap_distance", Saved = true )]
	public static float RotationSnapDistance { get; set; } = 15f;

	public readonly float[] RotationSnapDistances = { 1, 5, 15, 30, 45, 90 };

	private PlayerCharacter Player => GetComponent<PlayerCharacter>();

	[Property] public Model CursorModel { get; set; }

	[Property] public SoundEvent StartPlacingSound { get; set; }
	[Property] public SoundEvent StopPlacingSound { get; set; }
	[Property] public SoundEvent RotateSound { get; set; }

	public bool IsPlacing;
	public bool IsMoving;
	public int InventorySlotIndex;

	public InventorySlot<PersistentItem> InventorySlot =>
		Player.Inventory.Container.GetSlotByIndex( InventorySlotIndex );

	private ItemData ItemData => InventorySlot.GetItem().ItemData;

	private GameObject _ghost;
	private ItemData _selectedItemData;
	// private bool _isPlacingFromInventory;

	private bool _isAdjustingCamera;
	private Vector2 _cameraAdjustmentStart;

	public WorldItem CurrentPlacedItem { get; set; }

	private TimeSince _lastAction;

	private const float MaxStartMoveDistance = 150f;
	private const float MaxMoveDistance = 200f;

	protected override void OnStart()
	{
		Mouse.Visible = true;
		_lastAction = 0;
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

		var clone = selectedGameObject.GameObject.Clone( selectedGameObject.WorldPosition,
			selectedGameObject.WorldRotation );
		PlaceGhostInternal( clone );

		// TODO: hide worlditem
		CurrentPlacedItem.Hide();

		Mouse.Visible = true;
		Mouse.Position = Scene.Camera.PointToScreenPixels( selectedGameObject.WorldPosition );

		Sound.Play( StartPlacingSound );
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

		Sound.Play( StartPlacingSound );
	}

	public void StopPlacing()
	{
		if ( IsMoving || IsPlacing )
		{
			Sound.Play( StopPlacingSound );
		}

		IsMoving = false;
		IsPlacing = false;
		DestroyGhost();
		_selectedItemData = null;
		_isAdjustingCamera = false;
		_heightMode = false;
		// Mouse.Visible = false;
	}

	public void StopMoving()
	{
		if ( IsMoving || IsPlacing )
		{
			Sound.Play( StopPlacingSound );
		}

		IsMoving = false;
		IsPlacing = false;
		DestroyGhost();
		_selectedItemData = null;
		if ( CurrentPlacedItem.IsValid() )
		{
			CurrentPlacedItem.Show();
		}

		CurrentPlacedItem = null;
		_isAdjustingCamera = false;
		_heightMode = false;
		// Mouse.Visible = false;
	}

	public void CreateGhostFromInventory()
	{
		var item = InventorySlot.GetItem();
		_selectedItemData = item.ItemData;
		var gameObject = item.ItemData.PlaceScene.Clone( Player.WorldPosition,
			Rotation.FromYaw( Player.PlayerController.Yaw ).Angles().SnapToGrid( RotationSnapDistance ) );

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

	/*protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;
		if ( !IsPlacing && !IsMoving ) return;
		if ( !_ghost.IsValid() ) return;
		// CheckInput();
		// UpdateGhostTransform();
		// UpdateVisuals();
	}*/

	private bool _heightMode;
	private Vector3 _heightModeStart;

	private void CheckInput()
	{
		if ( Input.Pressed( "ItemPlacerConfirm" ) )
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

			Input.ReleaseAction( "ItemPlacerConfirm" );
			Input.Clear( "ItemPlacerConfirm" );
			_lastAction = 0;
			return;
		}

		if ( Input.Pressed( "ItemPlacerCancel" ) )
		{
			if ( IsPlacing )
			{
				StopPlacing();
			}
			else if ( IsMoving )
			{
				StopMoving();
			}

			return;
		}

		if ( Input.Pressed( "PickUp" ) && IsMoving )
		{
			PickUpMovingItem();
			return;
		}

		if ( Input.Pressed( "ItemPlacerHeight" ) )
		{
			Log.Info( "Height mode on" );
			_heightMode = true;
			_heightModeStart = _ghost.WorldPosition;
			_cameraAdjustmentStart = Mouse.Position;
		}

		if ( Input.Released( "ItemPlacerHeight" ) )
		{
			Log.Info( "Height mode off" );
			_heightMode = false;
		}

		if ( Input.Pressed( "RotateClockwise" ) )
		{
			_ghost.WorldRotation *= Rotation.FromYaw( -RotationSnapDistance );
			_ghost.WorldRotation = _ghost.WorldRotation.Angles().SnapToGrid( RotationSnapDistance );
			Sound.Play( RotateSound );
		}
		else if ( Input.Pressed( "RotateCounterClockwise" ) )
		{
			_ghost.WorldRotation *= Rotation.FromYaw( RotationSnapDistance );
			_ghost.WorldRotation = _ghost.WorldRotation.Angles().SnapToGrid( RotationSnapDistance );
			Sound.Play( RotateSound );
		}

		if ( Input.Pressed( "CameraAdjust" ) )
		{
			_isAdjustingCamera = true;
			_cameraAdjustmentStart = Mouse.Position;
		}

		if ( Input.Released( "CameraAdjust" ) )
		{
			_isAdjustingCamera = false;
		}

		if ( _isAdjustingCamera )
		{
			if ( Mouse.Position.x > _cameraAdjustmentStart.x + 30f )
			{
				Player.CameraController.RotateCamera( Rotation.FromYaw( -CameraController.CameraRotateSnapDistance ) );
				_cameraAdjustmentStart = Mouse.Position;
			}
			else if ( Mouse.Position.x < _cameraAdjustmentStart.x - 30f )
			{
				Player.CameraController.RotateCamera( Rotation.FromYaw( CameraController.CameraRotateSnapDistance ) );
				_cameraAdjustmentStart = Mouse.Position;
			}
		}

		if ( Input.Pressed( "ItemPlacerPositionSnapDecrease" ) )
		{
			var index = Array.IndexOf( PositionSnapDistances, PositionSnapDistance );

			if ( index > 0 )
			{
				PositionSnapDistance = PositionSnapDistances[index - 1];
				Sound.Play( RotateSound );
			}
		}
		else if ( Input.Pressed( "ItemPlacerPositionSnapIncrease" ) )
		{
			var index = Array.IndexOf( PositionSnapDistances, PositionSnapDistance );

			if ( index < 0 )
			{
				PositionSnapDistance = PositionSnapDistances[0];
				Sound.Play( RotateSound );
			}
			else if ( index < PositionSnapDistances.Length - 1 )
			{
				PositionSnapDistance = PositionSnapDistances[index + 1];
				Sound.Play( RotateSound );
			}
		}
		else if ( Input.Pressed( "ItemPlacerRotationSnapDecrease" ) )
		{
			var index = Array.IndexOf( RotationSnapDistances, RotationSnapDistance );

			if ( index > 0 )
			{
				RotationSnapDistance = RotationSnapDistances[index - 1];
				Sound.Play( RotateSound );
			}
		}
		else if ( Input.Pressed( "ItemPlacerRotationSnapIncrease" ) )
		{
			var index = Array.IndexOf( RotationSnapDistances, RotationSnapDistance );

			if ( index < RotationSnapDistances.Length - 1 )
			{
				RotationSnapDistance = RotationSnapDistances[index + 1];
				Sound.Play( RotateSound );
			}
		}
	}

	private void PickUpMovingItem()
	{
		if ( !CurrentPlacedItem.CanPickup( Player ) ) return;
		CurrentPlacedItem.OnPickup( Player );
		StopMoving();
	}

	public WorldItem CurrentHoveredItem { get; set; }

	private void CheckStartMove()
	{
		CurrentHoveredItem = null;

		var trace = Scene.Trace.Ray( Scene.Camera.ScreenPixelToRay( Mouse.Position ), 1000f )
			.WithoutTags( "player", "invisiblewall", "doorway", "stairs", "room_invisible" )
			.Run();

		if ( !trace.Hit ) return;

		var worldItem = trace.GameObject.GetComponent<WorldItem>();

		if ( !worldItem.IsValid() ) return;

		if ( !worldItem.CanPickup( Player ) ) return;

		var tooFarAway = worldItem.WorldPosition.Distance( Player.WorldPosition ) > MaxStartMoveDistance;

		if ( !tooFarAway )
		{
			worldItem.ItemHighlight.Enabled = true;
			CurrentHoveredItem = worldItem;
		}

		if ( Input.Released( "ItemPlacerConfirm" ) && _lastAction > 0.2f )
		{
			if ( tooFarAway )
			{
				Player.Notify( Notifications.NotificationType.Warning, $"{worldItem.ItemData.Name} is too far away" );
				return;
			}

			StartMovingPlacedItem( worldItem );

			/*_clickedWorldItem = worldItem;
			_clickedWorldItemScreenPosition = Mouse.Position;*/
		}
		/*else if ( Input.Released( "ItemPlacerConfirm" ) )
		{
			_clickedWorldItem = null;
		}

		if ( Input.Down( "ItemPlacerConfirm" ) && _clickedWorldItem.IsValid() )
		{
			if ( tooFarAway )
			{
				Player.Notify( Notifications.NotificationType.Warning, "Too far away" );
				return;
			}

			var distance = Mouse.Position.Distance( _clickedWorldItemScreenPosition );

			if ( distance > 10f )
			{
				_clickedWorldItem.ItemHighlight.Enabled = false;
				StartMovingPlacedItem( _clickedWorldItem );
				_clickedWorldItem = null;
			}
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

		if ( _ghost.WorldPosition.Distance( Player.WorldPosition ) > MaxMoveDistance )
		{
			Player.Notify( Notifications.NotificationType.Warning, "Item placed too far away" );
			// StopMoving();
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

		if ( PositionSnapEnabled && !Input.Down( "Snap" ) )
		{
			endPosition = endPosition.SnapToGrid( PositionSnapDistance );
		}
		else if ( !PositionSnapEnabled && Input.Down( "Snap" ) )
		{
			endPosition = endPosition.SnapToGrid( PositionSnapDistance );
		}

		endPosition += _selectedItemData.PlaceModeOffset;

		if ( _heightMode )
		{
			var delta = Mouse.Position - _cameraAdjustmentStart;
			endPosition = (_heightModeStart + new Vector3( 0, 0, -(delta.y / 5f) )).SnapToGrid( PositionSnapDistance );
		}

		// var gridPosition = Player.World.WorldToItemGrid( endPosition );

		// TODO: Check if the item can be placed here
		_isValidPlacement = !Player.World.IsPositionOccupied( endPosition, CurrentPlacedItem?.GameObject, 4f ) &&
		                    !Player.World.IsNearPlayer( endPosition );

		_ghost.WorldPosition = endPosition;
	}


	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( !IsPlacing && !IsMoving )
		{
			CheckStartMove();
			return;
		}

		if ( !Player.IsValid() ) return;
		if ( !_ghost.IsValid() ) return;
		if ( !Scene.IsValid() ) return;

		/*if ( IsPlacing && ShowGrid )
		{
			// TODO: re-add grid when it can be positioned relative to the player
			// Gizmo.Transform = new Transform( Player.WorldPosition );
			// Gizmo.Draw.Grid( Gizmo.GridAxis.XY, new Vector2( 32f, 32f ) );
		}
		*/

		CheckInput();
		UpdateGhostTransform();
		UpdateVisuals();

		if ( !_ghost.IsValid() ) return;

		var trace = Scene.Trace.Ray( _ghost.WorldPosition, _ghost.WorldPosition + Vector3.Down * 300f )
			.WithoutTags( "player", "invisiblewall", "doorway", "stairs", "room_invisible" )
			.Run();

		if ( trace.Hit )
		{
			var endPos = trace.EndPosition;
			Gizmo.Draw.Arrow( _ghost.WorldPosition, endPos );
		}

		var bbox1 = BBox.FromPositionAndSize( _colliderCenter, _colliderSize );
		bbox1 = bbox1.Rotate( _ghost.WorldRotation );
		bbox1 = bbox1.Translate( _ghost.WorldPosition );

		Gizmo.Draw.LineBBox( bbox1 );

		foreach ( var worldItem in Scene.GetAllComponents<WorldItem>() )
		{
			if ( worldItem == CurrentPlacedItem ) continue;
			if ( worldItem == CurrentHoveredItem ) continue;

			var collider = worldItem.GameObject.GetComponent<BoxCollider>();

			if ( collider == null ) continue;

			var bbox = BBox.FromPositionAndSize( collider.Center, collider.Scale )
				.Rotate( worldItem.WorldRotation )
				.Translate( worldItem.WorldPosition );

			Gizmo.Draw.LineBBox( bbox );
		}
	}
}
