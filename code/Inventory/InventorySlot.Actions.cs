using System;
using System.Resources;
using Clover.Components;
using Clover.Data;
using Clover.Items;
using Clover.Persistence;
using Clover.Ui;

namespace Clover.Inventory;

public sealed partial class InventorySlot<TItem> where TItem : PersistentItem
{
	public void Drop()
	{
		if ( !Networking.IsHost )
		{
			Log.Error( "Only the host can drop items for now." );
			return;
		}

		Log.Info( $"Dropping item {PersistentItem.ItemData.ResourceName}" );
		var position = InventoryContainer.Player.GetAimingGridPosition();
		var playerRotation =
			World.GetItemRotationFromDirection(
				World.Get4Direction( InventoryContainer.Player.PlayerController.Yaw ) );

		/*try
		{*/
		InventoryContainer.Player.World.SpawnDroppedNode( PersistentItem, position, playerRotation );
		/*}
		catch ( Exception e )
		{
			//x NodeManager.UserInterface.ShowWarning( e.Message );
			Log.Error( e.Message );
			return;
		}*/

		Sound.Play( "sounds/interact/item_drop.sound", InventoryContainer.Owner.WorldPosition );

		TakeOneOrDelete();
	}

	public void Place()
	{
		if ( !Networking.IsHost )
		{
			Log.Error( "Only the host can place items for now." );
			return;
		}

		InventoryContainer.Player.ItemPlacer.StartPlacingInventorySlot( Index );
		InventoryUi.Instance.Close();
	}

	public void Equip()
	{
		if ( PersistentItem.ItemData is IEdibleData )
		{
			HoldEdible();
			return;
		}

		Components.Equips.EquipSlot slot;

		// get slot from item
		/*if ( _persistentItem is Persistence.BaseCarriable )
		{
			slot = Components.Equips.EquipSlot.Tool;
		}
		else if ( _persistentItem is ClothingItem clothingItem )
		{
			slot = clothingItem.EquipSlot;
			if ( slot == 0 )
			{
				// throw new Exception( $"Invalid equip slot for {clothingItem} ({clothingItem.GetName()}) {clothingItem.ItemData.GetType()}" );
				NodeManager.UserInterface.ShowWarning( "Invalid equip slot for this item." );
			}
		}
		else
		{
			// throw new Exception( $"Item {_persistentItem} is not equipable." );
			NodeManager.UserInterface.ShowWarning( "This item is not equipable." );
			return;
		}*/

		slot = Equips.EquipSlot.Tool; // TODO: get slot from item

		if ( PersistentItem.ItemData is not ToolData toolData )
		{
			Log.Error( "Item is not a tool" ); // TODO: handle other types of items
			return;
		}

		// create a copy of the currently equipped item
		PersistentItem currentEquip = null;
		if ( InventoryContainer.Player.Equips.HasEquippedItem( slot ) )
		{
			currentEquip =
				Persistence.PersistentItem.Create( InventoryContainer.Player.Equips.GetEquippedItem( slot ) );
		}

		/*if ( _persistentItem is Persistence.BaseCarriable carriable )
		{
			var carriableNode = carriable.Create();
			InventoryContainer.Player.Equips.SetEquippedItem( Components.Equips.EquipSlot.Tool, carriableNode );
		}
		else if ( _persistentItem is ClothingItem clothingItem )
		{
			var clothingNode = clothingItem.Create();
			InventoryContainer.Player.Equips.SetEquippedItem( slot, clothingNode );
		}*/

		// equip the item
		if ( toolData != null )
		{
			var tool = GetItem().SpawnCarriable();
			InventoryContainer.Player.Equips.SetEquippedItem( slot, tool.GameObject );
		}

		var currentIndex = Index;

		// remove this item from inventory, making place for the previously equipped item
		Delete();

		// if there was a previously equipped item, add it back to the inventory
		if ( currentEquip != null )
		{
			InventoryContainer.AddItemToIndex( currentEquip, currentIndex );
		}

		InventoryContainer.Player.Save();
	}

	/*
	public void Bury()
	{
		Logger.Info( "Burying item" );

		var pos = InventoryContainer.Player.Interact.GetAimingGridPosition();
		var floorItem = InventoryContainer.Player.World.GetItem( pos, World.ItemPlacement.Floor );
		if ( floorItem.Node is not Hole hole )
		{
			return;
		}

		// spawn item underground
		InventoryContainer.Player.World.SpawnPersistentNode( _persistentItem, pos, World.ItemRotation.North, World.ItemPlacement.Underground,
			true );

		// remove hole so it isn't obstructing the dirt that will be spawned next
		InventoryContainer.Player.World.RemoveItem( hole );

		// spawn dirt on top
		InventoryContainer.Player.World.SpawnNode( ResourceManager.LoadItemFromId<ItemData>( "buried_item" ), pos,
			World.ItemRotation.North, World.ItemPlacement.Floor, false );

		Logger.Info( "Item buried" );

		TakeOneOrDelete();
	}

	public void Plant()
	{
		var pos = InventoryContainer.Player.Interact.GetAimingGridPosition();
		var floorItem = InventoryContainer.Player.World.GetItem( pos, World.ItemPlacement.Floor );
		if ( floorItem.Node is not Hole hole )
		{
			return;
		}

		if ( _persistentItem.ItemData is SeedData seedData )
		{

			var plantItemData = seedData.SpawnedItemData;

			if ( plantItemData == null )
			{
				// throw new System.Exception( "Seed data does not have a spawned item data." );
				NodeManager.UserInterface.ShowWarning( "This seed does not have a plant data." );
				return;
			}

			// remove hole so it isn't obstructing the plant that will be spawned next
			InventoryContainer.Player.World.RemoveItem( hole );

			if ( plantItemData is PlantData plantData )
			{
				InventoryContainer.Player.World.SpawnNode( plantItemData, plantData.PlantedScene, pos, World.ItemRotation.North, World.ItemPlacement.Floor );
			}
			else
			{
				// throw new System.Exception( "Spawned item data is not a plant data. Unsupported for now." );
				NodeManager.UserInterface.ShowWarning( "Spawned item data is not a plant data. Unsupported for now." );
				return;
			}

			TakeOneOrDelete();

			return;

		}
		else if ( _persistentItem.ItemData is PlantData plantData )
		{

			var plantScene = plantData.PlantedScene;

			if ( plantScene == null )
			{
				// throw new System.Exception( "Plant data does not have a planted scene." );
				NodeManager.UserInterface.ShowWarning( "This plant does not have a planted scene." );
			}

			// remove hole so it isn't obstructing the plant that will be spawned next
			InventoryContainer.Player.World.RemoveItem( hole );

			InventoryContainer.Player.World.SpawnNode( plantData, plantScene, pos, World.ItemRotation.North, World.ItemPlacement.Floor );

			TakeOneOrDelete();

			return;

		}

		// throw new System.Exception( "Item data is not a seed or plant data." );

		NodeManager.UserInterface.ShowWarning( "This item is not a seed or plant." );

		// Inventory.World.RemoveItem( floorItem );
	}

	public void SetWallpaper()
	{

		if ( _persistentItem.ItemData is not WallpaperData wallpaperData )
		{
			// throw new System.Exception( "Item data is not a wallpaper data." );
			NodeManager.UserInterface.ShowWarning( "This item is not a wallpaper." );
			return;
		}

		var interiorManager = InventoryContainer.Player.World.GetNodesOfType<InteriorManager>().FirstOrDefault();

		if ( interiorManager == null || !GodotObject.IsInstanceValid( interiorManager ) )
		{
			// throw new System.Exception( "Interior manager not found." );
			NodeManager.UserInterface.ShowWarning( "Interior manager not found. Are you inside a house?" );
			return;
		}

		// TODO: get which room to set the wallpaper for
		interiorManager.SetWallpaper( "first", wallpaperData );

		TakeOneOrDelete();

	}

	public void SetFlooring()
	{

		if ( _persistentItem.ItemData is not FlooringData floorData )
		{
			// throw new System.Exception( "Item data is not a flooring data." );
			NodeManager.UserInterface.ShowWarning( "This item is not a flooring." );
			return;
		}

		var interiorManager = InventoryContainer.Player.World.GetNodesOfType<InteriorManager>().FirstOrDefault();

		if ( interiorManager == null || !GodotObject.IsInstanceValid( interiorManager ) )
		{
			// throw new System.Exception( "Interior manager not found." );
			NodeManager.UserInterface.ShowWarning( "Interior manager not found. Are you inside a house?" );
			return;
		}

		// TODO: get which room to set the wallpaper for
		interiorManager.SetFloor( "first", floorData );

		TakeOneOrDelete();

	}

	public void Eat()
	{

		// TODO: check with some kind of interface if the item is edible
		if ( _persistentItem.ItemData is not FruitData foodData )
		{
			// throw new System.Exception( "Item data is not a food data." );
			NodeManager.UserInterface.ShowWarning( "This item is not edible." );
			return;
		}

		InventoryContainer.Player.Inventory.GetNode<AudioStreamPlayer3D>( "ItemEat" ).Play();

		Logger.Info( "Eating food" );

		TakeOneOrDelete();

	}

	public Texture2D GetIconTexture()
	{
		return _persistentItem.GetIconTexture();
	}*/

	public void Split( int amount )
	{
		if ( Amount <= 1 )
		{
			//x NodeManager.UserInterface.ShowWarning( "Can't split a single item." );
			return;
		}

		if ( amount > Amount )
		{
			//x NodeManager.UserInterface.ShowWarning( "Can't split more than the amount of items in the slot." );
			return;
		}

		if ( InventoryContainer.FreeSlots <= 0 )
		{
			//x NodeManager.UserInterface.ShowWarning( "No free slots." );
			return;
		}

		SetAmount( Amount - amount );

		var newItem = PersistentItem.Clone();

		// var slot = new InventorySlot<PersistentItem>( InventoryContainer );
		// slot.SetItem( newItem );
		// slot.Amount = amount;

		var slot = InventoryContainer.AddItem( newItem );
		slot.SetAmount( amount );
	}

	/*public void Open()
	{
		if ( _persistentItem is not Persistence.Gift gift ) throw new Exception( "Item is not a gift" );

		if ( gift.Items.Count == 0 ) throw new Exception( "Gift is empty" );

		if ( InventoryContainer.FreeSlots < gift.Items.Count ) throw new Exception( "Not enough free slots" );

		foreach ( var item in gift.Items )
		{
			InventoryContainer.AddItem( item );
		}

		TakeOneOrDelete();

	}
	*/

	public void SpawnObject()
	{
		if ( !Networking.IsHost )
		{
			Log.Error( "Only the host can spawn objects for now." );
			return;
		}

		var objectData = PersistentItem.ItemData.ObjectData;

		if ( objectData == null )
		{
			Log.Error( "Item does not have an object data" );
			return;
		}

		var gameObject = objectData.Prefab.Clone();

		var worldObject = gameObject.GetComponent<WorldObject>();
		worldObject.WorldLayerObject.SetLayer( InventoryContainer.Player.WorldLayerObject.Layer, true );

		var size = gameObject.GetBounds().Size.Length;

		worldObject.WorldPosition = InventoryContainer.Player.WorldPosition +
		                            InventoryContainer.Player.Model.WorldRotation.Forward * size;

		TakeOneOrDelete();
	}

	public void Plant()
	{
		if ( !Networking.IsHost )
		{
			Log.Error( "Only the host can plant objects for now." );
			return;
		}

		if ( PersistentItem.ItemData is not SeedData seedData )
		{
			Log.Error( "Item is not a seed" );
			return;
		}

		var aimingGridPosition = InventoryContainer.Player.GetAimingGridPosition();
		var floorItem = InventoryContainer.Player.World.GetWorldItem<Hole>( aimingGridPosition );
		if ( floorItem == null )
		{
			Log.Warning( "No hole found" );
			return;
		}

		var spawnedItemData = seedData.SpawnedItemData;

		if ( spawnedItemData == null )
		{
			Log.Error( "Seed does not have a spawned item data" );
			return;
		}

		floorItem.RemoveFromWorld();

		if ( spawnedItemData is PlantData plantData )
		{
			InventoryContainer.Player.World.SpawnCustomItem( plantData, plantData.PlantedScene, aimingGridPosition,
				World.ItemRotation.North );
		}
		else
		{
			Log.Error( "Spawned item data is not a plant data" );
			return;
		}

		TakeOneOrDelete();
	}

	public void Destroy()
	{
		TakeOneOrDelete();
	}

	public void HoldEdible()
	{
		var carriedPersistentItem = Persistence.PersistentItem.Create( "carried_edible:4023053997083351548" );
		carriedPersistentItem.SetSaveData( "EdibleData", PersistentItem.ItemData.GetIdentifier() );

		var carriedEdible = carriedPersistentItem.SpawnCarriable();

		InventoryContainer.Player.Equips.SetEquippedItem( Equips.EquipSlot.Tool, carriedEdible.GameObject );

		TakeOneOrDelete();
	}
}
