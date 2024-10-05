using System;
using Clover.Carriable;
using Clover.Components;
using Clover.Persistence;
using Sandbox.UI;

namespace Clover.Ui;

public partial class InventoryUiEquip : IEquipChanged
{
	public Inventory.Inventory Inventory { get; set; }

	public Equips.EquipSlot Slot { get; set; }

	private BaseCarriable Tool => Inventory.Player.Equips.GetEquippedItem<BaseCarriable>( Slot );

	public void OnEquippedItemChanged( GameObject owner, Equips.EquipSlot slot, GameObject item )
	{
		if ( Inventory.GameObject != owner || slot != Slot )
		{
			Log.Info( "InventoryUiEquip: OnEquippedItemChanged: Not this slot" );
			return;
		}

		StateHasChanged();
	}

	public void OnEquippedItemRemoved( GameObject owner, Equips.EquipSlot slot )
	{
		if ( Inventory.GameObject != owner || slot != Slot )
		{
			Log.Info( "InventoryUiEquip: OnEquippedItemRemoved: Not this slot" );
			return;
		}

		StateHasChanged();
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Inventory, Slot, Tool );
	}

	protected override void OnRightClick( MousePanelEvent e )
	{
		base.OnRightClick( e );
		if ( Tool == null ) return;
		
		Unequip();
		
	}

	public void Unequip( int targetSlot = -1 )
	{
		// Inventory.Player.Equips.RemoveEquippedItem( Slot );

		var freeIndex = Inventory.Container.GetFirstFreeEmptyIndex();
		if ( freeIndex == -1 )
		{
			Log.Error( "No free slots available" );
			return;
		}
		
		targetSlot = targetSlot == -1 ? freeIndex : targetSlot;
		
		var item = Inventory.Player.Equips.GetEquippedItem( Slot );
		if ( item == null )
		{
			Log.Error( "No item equipped" );
			return;
		}

		var persistentItem = PersistentItem.Create( item );
		
		Inventory.Container.AddItemToIndex( persistentItem, targetSlot );
		
		Inventory.Player.Equips.RemoveEquippedItem( Slot, true );
		
		Inventory.Player.Save();
		
		StateHasChanged();

	}
}
