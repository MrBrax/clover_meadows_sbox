using System;
using Clover.Data;
using Clover.Inventory;
using Clover.Items;
using Clover.Persistence;

namespace Clover.Player;

public class ItemPlacer : Component
{
	// [RequireComponent] public PlayerCharacter Player { get; set; }
	private PlayerCharacter Player => GetComponent<PlayerCharacter>();

	[Property] public Model CursorModel { get; set; }

	public bool IsPlacing;
	public int InventorySlotIndex;

	public InventorySlot<PersistentItem> InventorySlot =>
		Player.Inventory.Container.GetSlotByIndex( InventorySlotIndex );

	private ItemData ItemData => InventorySlot.GetItem().ItemData;

	private GameObject ghost;
	private GameObject cursor;

	public void StartPlacing( int inventorySlotIndex )
	{
		if ( !Player.IsValid() ) throw new System.Exception( "Player is not valid" );
		if ( !Player.Inventory.IsValid() ) throw new System.Exception( "Player Inventory is not valid" );
		if ( Player.Inventory.Container == null )
			throw new System.Exception( "Player Inventory Container is not valid" );

		if ( IsPlacing )
		{
			StopPlacing();
		}

		InventorySlotIndex = inventorySlotIndex;
		IsPlacing = true;
		CreateGhost();
	}

	public void StopPlacing()
	{
		IsPlacing = false;
		DestroyGhost();
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

		ghost = gameObject;

		// create cursor
		cursor = Scene.CreateObject();
		cursor.NetworkMode = NetworkMode.Never;

		var model = cursor.AddComponent<ModelRenderer>();
		model.Tint = Color.Parse( "#FFFFFF2B" ) ?? Color.White;
		model.Model = CursorModel;
		model.RenderType = ModelRenderer.ShadowRenderType.Off;

		cursor.WorldScale = new Vector3( 1, 1, 0.3f );
	}

	public void DestroyGhost()
	{
		if ( ghost.IsValid() )
		{
			ghost.Destroy();
		}

		ghost = null;

		if ( cursor.IsValid() )
		{
			cursor.Destroy();
		}

		cursor = null;
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
		if ( !ghost.IsValid() ) return;
		CheckInput();
		UpdateGhostTransform();
		UpdateVisuals();
	}

	private void CheckInput()
	{
		if ( Input.Pressed( "use" ) )
		{
			if ( _isValidPlacement )
			{
				PlaceItem();
			} else
			{
				Log.Warning( "Invalid placement" );
			}
			Input.Clear( "use" );
		}
		
		if ( Input.MouseWheel.y != 0 )
		{
			ghost.WorldRotation *= Rotation.FromYaw( Input.MouseWheel.y * 15 );
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
			Player.World.SpawnPlacedNode( InventorySlot.GetItem(), _lastGridPosition, _lastGridRotation,
				_lastItemPlacement );
		}
		catch ( Exception e )
		{
			Log.Error( e );
			return;
		}

		InventorySlot.TakeOneOrDelete();
		
		StopPlacing();
	}

	private void SetGhostTint( Color color )
	{
		foreach ( var renderable in ghost.Components.GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants )
			         .ToList() )
		{
			renderable.Tint = color;
		}
	}

	private void SetCursorTintColor( Color color )
	{
		var renderable = cursor.GetComponent<ModelRenderer>();
		renderable.SceneObject.Attributes.Set( "tint", color.WithAlpha( 1 ) );
		renderable.SceneObject.Attributes.Set( "opacity", color.a );
	}

	private void UpdateVisuals()
	{
		var s = MathF.Sin( Time.Now * 5 ) * 0.1f;
		SetGhostTint( _isValidPlacement ? Color.White.WithAlpha( 0.5f + s ) : Color.Red.WithAlpha( 0.5f + s ) );
		SetCursorTintColor( _isValidPlacement ? Color.White.WithAlpha( 0.2f + s ) : Color.Red.WithAlpha( 0.2f + s ) );
	}

	private bool _isValidPlacement;

	private Vector2Int _lastGridPosition;
	private World.ItemRotation _lastGridRotation;
	private World.ItemPlacement _lastItemPlacement = World.ItemPlacement.Floor;
	private Vector3 _colliderSize;
	private Vector3 _colliderCenter;

	private void UpdateGhostTransform()
	{
		/*var gridPosition = Player.GetAimingGridPosition();
		var gridRotation =
			World.GetItemRotationFromDirection(
				World.Get4Direction( Player.PlayerController.Yaw ) );

		if ( gridPosition == _lastGridPosition && gridRotation == _lastGridRotation ) return;

		_lastGridPosition = gridPosition;
		_lastGridRotation = gridRotation;

		_lastItemPlacement = GetItemPlacement( gridPosition ) ?? World.ItemPlacement.Floor;

		var canPlace = Player.World.CanPlaceItem(
			InventorySlot.GetItem().ItemData.GetGridPositions( _lastGridRotation, _lastGridPosition ),
			_lastItemPlacement );
		
		var isValidPlacementType = InventorySlot.GetItem().ItemData.Placements.HasFlag( _lastItemPlacement );

		_isValidPlacement = canPlace && isValidPlacementType;

		TransformChange();*/
		
		var ray = Scene.Camera.ScreenPixelToRay( Mouse.Position );
		
		var box = BBox.FromPositionAndSize( _colliderCenter, _colliderSize );
		
		box = box.Rotate( ghost.WorldRotation );

		var trace = Scene.Trace.Box( box, ray, 10000f )
			.WithoutTags( "player" )
			.Run();
		
		if ( !trace.Hit ) return;

		// Gizmo.Transform = new Transform( trace.EndPosition );
		// Gizmo.Draw.LineBBox( box );
		
		var endPosition = trace.EndPosition;
		
		var gridPosition = Player.World.WorldToItemGrid( endPosition );
		
		// endPosition = Player.World.ItemGridToWorld( gridPosition );
		// endPosition.z = trace.EndPosition.z;
		

		ghost.WorldPosition = endPosition;
		cursor.WorldPosition = endPosition;

	}

	private World.ItemPlacement? GetItemPlacement( Vector2Int gridPosition )
	{
		var floorItem = Player.World.GetItem( gridPosition, World.ItemPlacement.Floor );

		if ( floorItem != null )
		{
			var placeableNodes = floorItem.GetPlaceableNodes();
			if ( placeableNodes.Any() )
			{
				var onTopItem =
					Player.World.GetItem( gridPosition, World.ItemPlacement.OnTop );
				if ( onTopItem != null )
				{
					Log.Warning( "On top item already exists." );
					return null;
				}

				if ( ItemData.Width > 1 || ItemData.Height > 1 )
				{
					Log.Warning( "Can't place a large item on top of another item." );
					return null;
				}

				return World.ItemPlacement.OnTop;
			}

			return null;
		}

		return World.ItemPlacement.Floor;
	}

	private void TransformChange()
	{
		var transform = Player.World.GetTransform( _lastGridPosition, _lastGridRotation, _lastItemPlacement,
			InventorySlot.GetItem().ItemData );

		ghost.WorldPosition = transform.position;
		ghost.WorldRotation = transform.rotation;

		Log.Info( $"Ghost position: {ghost.WorldPosition}" );

		var gridPositions = InventorySlot.GetItem().ItemData.GetGridPositions( _lastGridRotation );
		var bounds = InventorySlot.GetItem().ItemData.GetBounds( _lastGridRotation );


		cursor.WorldPosition = transform.position;

		cursor.WorldScale = new Vector3( bounds.x, bounds.y, 0.3f );
	}
}
