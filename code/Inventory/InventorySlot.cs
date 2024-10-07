using System;
using System.Text.Json.Serialization;
using Clover.Persistence;
using Sandbox.Diagnostics;

namespace Clover.Inventory;

public sealed partial class InventorySlot<TItem> where TItem : PersistentItem
{


	[JsonInclude] public int Index { get; set; } = -1;

	[JsonInclude, JsonPropertyName( "_item" )] public TItem PersistentItem;

	/// <summary>
	/// The amount of the item in the inventory slot. Not applicable for non-stackable items.
	/// </summary>
	[JsonInclude] public int Amount { get; private set; } = 1;

	[JsonIgnore] public bool IsStackable => PersistentItem.IsStackable;
	[JsonIgnore] public bool MaxStackReached => PersistentItem.StackSize <= Amount;

	public InventorySlot( InventoryContainer inventory )
	{
		InventoryContainer = inventory;
	}

	public InventorySlot()
	{
	}


	[JsonIgnore] public InventoryContainer InventoryContainer { get; set; }

	[JsonIgnore] public bool HasItem => PersistentItem != null;

	public void SetItem( TItem item )
	{
		PersistentItem = item;
		InventoryContainer.OnChange();
	}

	public TItem GetItem()
	{
		return PersistentItem;
	}

	public T GetItem<T>() where T : TItem
	{
		return (T)PersistentItem;
	}

	public string GetName()
	{
		return PersistentItem.GetName();
	}
	
	public string GetIcon()
	{
		return PersistentItem.GetIcon();
	}
	
	public Texture GetIconTexture()
	{
		return PersistentItem.GetIconTexture();
	}

	public void Delete()
	{
		InventoryContainer.RemoveSlot( Index );
		// _item = null;
		// Inventory.OnChange();
		// Inventory.Player.Save();
	}

	/// <summary>
	/// Sets the amount of items in the inventory slot.
	/// </summary>
	/// <param name="amount">The new amount of items.</param>
	public void SetAmount( int amount )
	{
		Amount = amount;
		InventoryContainer.OnChange();
	}

	public bool CanMergeWith( InventorySlot<TItem> other )
	{

		// abort if either item is null
		if ( PersistentItem == null || other.PersistentItem == null )
		{
			Log.Info( "CanMerge: Item is null" );
			return false;
		}

		// abort if item types are not the same
		if ( PersistentItem.GetType() != other.PersistentItem.GetType() )
		{
			Log.Info( "CanMerge: Item types are not the same" );
			return false;
		}

		// abort if either item is not stackable
		if ( PersistentItem.IsStackable == false || other.PersistentItem.IsStackable == false )
		{
			Log.Info( "CanMerge: Item is not stackable" );
			return false;
		}

		// abort if amount is zero. this should never happen
		if ( Amount <= 0 )
		{
			Log.Info( "CanMerge: Amount is zero" );
			return false;
		}

		// abort if stack can't hold the amount
		if ( PersistentItem.StackSize < Amount + other.Amount )
		{
			Log.Info( $"CanMerge: Stack cannot hold the amount ({PersistentItem.StackSize} < {Amount + other.Amount})" );
			return false;
		}

		try
		{
			PersistentItem.CanMergeWith( other.PersistentItem );
		}
		catch ( Exception e )
		{
			// XLog.Error( "InventoryContainerSlot",
			// 	$"CanMerge: Item cannot merge with other item in slot {Index} or other slot {other.Index}" );
			Log.Warning( $"CanMerge: {e.Message}" );
			return false;
		}

		return true;
	}

	/// <summary>
	/// Merge this slot into another slot. It will delete this slot and sync the other slot.
	/// </summary>
	/// <param name="other"></param>
	/// <exception cref="Exception"></exception>
	public void MergeWith( InventorySlot<TItem> other )
	{
		// sanity check!
		if ( other == null )
		{
			throw new Exception( "Cannot merge with null slot" );
		}

		// sanity check!!
		if ( other.InventoryContainer == null )
		{
			throw new Exception( "Cannot merge with slot with null inventory" );
		}

		// this is the same as the sanity check called in CanMergeWith
		if ( !CanMergeWith( other ) )
		{
			throw new Exception( "Cannot merge with slot with incompatible item" );
		}

		// sanity check!!!
		if ( !PersistentItem.CanMergeWith( other.PersistentItem ) )
		{
			throw new Exception( "Cannot merge with slot with incompatible item" );
		}

		/* if ( !InventoryContainer.AllowSlotInteract )
		{
			throw new Exception( "You cannot interact with this inventory" );
		}

		if ( !other.InventoryContainer.AllowSlotInteract )
		{
			throw new Exception( "You cannot interact with the receiving inventory" );
		} */

		// merge into other
		// other.Amount += Amount;
		other.SetAmount( other.Amount + Amount );

		// other.InventoryContainer.FixEventRegistration();
		// other.InventoryContainer.SyncToPlayerList();

		// call merge on the item, by default it does nothing
		PersistentItem.MergeWith( other.PersistentItem );

		// delete and sync this one
		Delete();
		// FixEventRegistration();
		// InventoryContainer.SyncToPlayerList();

		// XLog.Info( "InventoryContainerSlot", $"Merged {other.Amount} items into slot {Index}" );
	}

	public void TakeOneOrDelete()
	{
		if ( !IsStackable )
		{
			Delete();
		}
		else if ( Amount > 1 )
		{
			// Amount--;
			SetAmount( Amount - 1 );
		}
		else
		{
			Delete();
		}
	}

	
}
