using System;
using System.Text.Json.Serialization;
using Clover.Components;
using Clover.Data;
using Clover.Interactable;
using Clover.Inventory;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Items;

[Category( "Clover/Items" )]
[Icon( "outlet" )]
[Description( "Has to be added to items placed on the world grid, otherwise they will not be saved." )]
public class WorldItem : Component, IPickupable
{
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }
	[RequireComponent] public ItemHighlight ItemHighlight { get; set; }

	// public WorldNodeLink NodeLink => !Scene.IsEditor ? WorldLayerObject.World?.GetNodeLink( GameObject ) : null;

	// public Vector2Int GridPosition => NodeLink?.GridPosition ?? Vector2Int.Zero;

	// public Vector2Int Size =>
	// 	!Scene.IsEditor ? NodeLink.Size : new Vector2Int( ItemData?.Width ?? 1, ItemData?.Height ?? 1 );

	// public World.ItemPlacement GridPlacement => !Scene.IsEditor ? NodeLink.GridPlacement : World.ItemPlacement.Floor;
	// public World.ItemPlacementType GridPlacementType => NodeLink?.PlacementType ?? World.ItemPlacementType.Placed;
	// public World.ItemRotation GridRotation => NodeLink?.GridRotation ?? World.ItemRotation.North;

	public Vector2Int GridPosition { get; set; }
	public Vector2Int Size { get; set; }
	public World.ItemPlacementType ItemPlacement { get; set; }
	public World.ItemRotation ItemRotation { get; set; }

	private string _prefab;

	[Property, ResourceType( "prefab" )]
	public string Prefab
	{
		get { return !string.IsNullOrEmpty( _prefab ) ? _prefab : GameObject.PrefabInstanceSource; }
		set { _prefab = value; }
	}

	[Property] public ItemData ItemData { get; set; }

	[Property] public Vector3 PlaceModeOffset { get; set; }

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

	/*public delegate void OnObjectSave( WorldNodeLink nodeLink );

	[Property] public OnObjectSave OnItemSave { get; set; }

	public delegate void OnObjectLoad( WorldNodeLink nodeLink );

	[Property] public OnObjectLoad OnItemLoad { get; set; }*/

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( !Gizmo.Settings.GizmosEnabled ) return;

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

		Gizmo.Draw.Arrow( WorldPosition + Vector3.Up * 64f, WorldPosition + Vector3.Forward * 64 + Vector3.Up * 64f,
			8f );
		Gizmo.Draw.Text( $"NORTH", new Transform( WorldPosition + Vector3.Up * 64f + Vector3.Forward * 72 ) );
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

	protected override void OnFixedUpdate()
	{
		// ItemHighlight.Enabled = false;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( Gizmo.Camera == null || !DebugWorldItem ) return;
		/*if ( NodeLink.IsValid() && NodeLink.PlacementType == World.ItemPlacementType.Dropped )
		{
			Gizmo.Draw.Text( $"Dropped: {NodeLink.GetName()}", new Transform( WorldPosition + Vector3.Up * 20 ) );
		}

		if ( NodeLink.IsValid() )
		{
			var placeableNodes = NodeLink.GetPlaceableNodes().ToList();
			foreach ( var placeableNode in placeableNodes )
			{
				Gizmo.Draw.Color = placeableNode.PlacedNodeLink == null ? Color.Green : Color.Red;
				Gizmo.Draw.Text( placeableNode.GameObject.Name, new Transform( placeableNode.WorldPosition ) );
				Gizmo.Draw.Color = Color.White;
			}
		}*/
	}

	[ConVar( "clover_debug_worlditem" )] public static bool DebugWorldItem { get; set; }

	[Property] public bool CanPickupSimple { get; set; }

	public bool IsBeingPickedUp { get; set; }

	[Property, TargetType( typeof(PersistentItem) )]
	public Type PersistentItemType { get; set; }

	public bool CanPickup( PlayerCharacter player )
	{
		return CanPickupSimple &&
		       !WorldLayerObject.World.Data.DisableItemPlacement; // TODO: make a bool for picking up on world
	}

	/*[Obsolete]
	public bool HasItemOnTop()
	{
		if ( NodeLink == null ) return false;

		var gridPositions = NodeLink.GetGridPositions( true );

		foreach ( var pos in gridPositions )
		{
			// if this item has an item on top of it then it can't be picked up
			if ( WorldLayerObject.World.GetItems( pos ).Any( x => x.GridPlacement == World.ItemPlacement.OnTop ) &&
			     GridPlacement == World.ItemPlacement.Floor )
			{
				// Log.Warning( $"An item is on top of this item" );
				return true;
			}
		}

		return false;
	}*/

	/// <summary>
	///  Called when the player picks up this item, mostly so you don't have to add IPickupable to every item.
	///  This will just call the Inventory.PickUpItem method on the node link.
	///  ALL IPickupable items will be iterated through when trying to pick up something.
	/// </summary>
	/// <param name="player"></param>
	public void OnPickup( PlayerCharacter player )
	{
		player.Inventory.PickUpItem( this );
	}

	public string GetPickupName()
	{
		return ItemData?.Name ?? "Item";
	}


	[Button( "Add simple collider (32)" )]
	private void AddSimpleCollider32()
	{
		var collider = GameObject.AddComponent<BoxCollider>();
		collider.Scale = new Vector3( 32, 32, 32 );
		collider.Center = new Vector3( 0, 0, 16 );
	}

	[Button( "Add simple collider (16)" )]
	private void AddSimpleCollider16()
	{
		var collider = GameObject.AddComponent<BoxCollider>();
		collider.Scale = new Vector3( 16, 16, 16 );
		collider.Center = new Vector3( 0, 0, 8 );
	}

	/*public void OnTriggerEnter( Collider other )
	{
		ToggleHighlight( other, true );
	}

	public void OnTriggerExit( Collider other )
	{
		ToggleHighlight( other, false );
	}

	private void ToggleHighlight( Collider otherCollider, bool shouldEnable )
	{
		if ( otherCollider.GetComponentInParent<PlayerCharacter>() == null )
		{
			return;
		}

		ItemHighlight.Enabled = shouldEnable;
	}*/
	public void Hide()
	{
		Tags.Add( "invisible" );
	}

	public void Show()
	{
		Tags.Remove( "invisible" );
	}

	public string GetName()
	{
		return ItemData.IsValid() ? ItemData.Name : GameObject.Name;
	}

	public bool ShouldBeSaved()
	{
		return true; // TODO: add option to not save
	}

	public void SavePersistence( PersistentItem item )
	{
		var components = GetComponents<Component>();

		var persistentInterfaceComponents = GetComponents<IPersistent>().ToList();

		// foreach ( var persistentInterfaceComponent in persistentInterfaceComponents )
		// {
		// 	// XLog.Debug( this, $"Calling OnPreSave on {persistentInterfaceComponent}" );
		// 	persistentInterfaceComponent.OnPreSave( item );
		// }

		var keys = new List<string>();

		foreach ( var component in components )
		{
			var properties = TypeLibrary.GetPropertyDescriptions( component );

			// XLog.Debug( this, $"Saving persistence for {component} on {this}" );

			foreach ( var property in properties )
			{
				var saveDataAttribute = property.GetCustomAttribute<SaveDataAttribute>();
				if ( saveDataAttribute == null )
				{
					// XLog.Debug( this, $"No save data attribute on {property.Name} on {component}" );
					continue;
				}

				var keyName = !string.IsNullOrEmpty( saveDataAttribute.Key ) ? saveDataAttribute.Key : property.Name;

				/*// clear the save data if the item is picked up and the attribute says to reset on pickup
				if ( pickedUp )
				{
					if ( saveDataAttribute.ResetOnPickup )
					{
						// XLog.Debug( this, $"Resetting {property.Name} on {component}" );
						item.ClearSaveData( keyName );
						continue;
					}
				}*/

				if ( keys.Contains( keyName ) )
				{
					Log.Error( $"Duplicate arbitrary data key {keyName} on {component}" );
					continue;
				}

				var type = property.PropertyType;

				var value = property.GetValue( component );

				// XLog.Info( this,
				// 	$"Saving arbitrary data {keyName} = {value}, type {type}/{value?.GetType()}" );

				// XLog.Debug( this, $"Saving '{keyName}' = '{value}' on {component}" );

				item.SetSaveData( keyName, value, type );
				// XLog.Info( this, $"Saving arbitrary data {keyName} = {value}" );
				keys.Add( keyName );

				// XLog.Debug( this, $"Saved '{keyName}' = '{value}' on {component}" );
			}
		}

		// XLog.Debug( this,
		// 	$"Saved {keys.Count} arbitrary data keys, {item.SaveData.Count} total. Calling OnSave on IPersistent components on {this}" );


		// XLog.Debug( this, $"Found {persistentInterfaceComponents.Count()} IPersistent components on {this}" );

		foreach ( var persistentInterfaceComponent in persistentInterfaceComponents )
		{
			// XLog.Debug( this, $"Calling OnSave on {persistentInterfaceComponent}" );
			persistentInterfaceComponent.OnSave( item );
		}

		// XLog.Debug( this, $"Calling SaveExtraPersistence on {this}" );
		// SaveExtraPersistence( item );
	}

	public void LoadPersistence( PersistentItem item )
	{
		var components = GetComponents<Component>();

		var persistentInterfaceComponents = GetComponents<IPersistent>().ToList();

		// foreach ( var persistentInterfaceComponent in persistentInterfaceComponents )
		// {
		// 	XLog.Debug( this, $"Calling OnPreLoad on {persistentInterfaceComponent}" );
		// 	persistentInterfaceComponent.OnPreLoad( item );
		// }

		foreach ( var component in components )
		{
			var properties = TypeLibrary.GetPropertyDescriptions( component );

			foreach ( var property in properties )
			{
				/*if ( property.HasAttribute<SaveDataAttribute>() )
				{
					var value = item.GetArbitraryData( property.PropertyType, property.Name );
					property.SetValue( component, value );
					// XLog.Info( this, $"Set {property.Name} to {value} on {component}" );
				}*/

				var saveDataAttribute = property.GetCustomAttribute<SaveDataAttribute>();
				if ( saveDataAttribute == null )
				{
					continue;
				}

				// Log.Info( $"{this} {property.Name} identity = {property.Identity.ToString( "X" )}" );

				if ( !string.IsNullOrEmpty( saveDataAttribute.OnLoadMethodName ) )
				{
					/*var method = component.GetType().GetMethod( saveDataAttribute.OnLoadMethodName );
					if ( method != null )
					{
						method.Invoke( component,
							new object[] { item.GetArbitraryData( property.PropertyType, property.Name ) } );
						continue;
					}
					*/
				}

				if ( !string.IsNullOrEmpty( saveDataAttribute.Key ) )
				{
					// first try using the key from the attribute
					var keyData = item.GetSaveData( property.PropertyType, saveDataAttribute.Key );
					if ( keyData != null )
					{
						property.SetValue( component, keyData );
						continue;
					}
				}

				// if that fails, try using the property name
				// we do this to maintain backwards compatibility with old saves that don't have the key set
				var propertyData = item.GetSaveData( property.PropertyType, property.Name );
				if ( propertyData != null )
				{
					property.SetValue( component, propertyData );
				}
			}
		}

		// if ( persistentInterfaceComponents.Count > 0 )
		// 	XLog.Info( this, $"Found {persistentInterfaceComponents.Count()} IPersistent components on {this.GameObject.Name}" );

		foreach ( var persistentInterfaceComponent in persistentInterfaceComponents )
		{
			// XLog.Info( this, $"Calling persistence OnLoad on {persistentInterfaceComponent} on {this.GameObject.Name}" );
			persistentInterfaceComponent.OnLoad( item );
		}

		// LoadExtraPersistence( item, source );

		if ( ItemData == null )
		{
			Log.Info( "Overriding item data with persistent item data" );
			ItemData = ItemData.Get( item.ItemId );
		}
	}

	public void SetItemData( ItemData itemData )
	{
		ItemData = itemData;
	}

	public void CalculateSize()
	{
		throw new NotImplementedException();
	}

	public void RemoveFromWorld()
	{
		if ( WorldLayerObject.IsValid() && WorldLayerObject.World.IsValid() )
		{
			WorldLayerObject.World.WorldItems.Remove( this );
		}

		GameObject.Destroy();
	}
}
