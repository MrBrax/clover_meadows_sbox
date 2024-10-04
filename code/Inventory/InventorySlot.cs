using System;
using System.Text.Json.Serialization;
using Clover.Persistence;
using Sandbox.Diagnostics;

namespace Clover.Inventory;

public sealed partial class InventorySlot<TItem> where TItem : PersistentItem
{


	[JsonInclude] public int Index { get; set; } = -1;

	[JsonInclude, JsonPropertyName( "_item" )] public TItem _persistentItem;

	/// <summary>
	/// The amount of the item in the inventory slot. Not applicable for non-stackable items.
	/// </summary>
	[JsonInclude] public int Amount { get; private set; } = 1;

	[JsonIgnore] public bool IsStackable => _persistentItem.Stackable;
	[JsonIgnore] public bool MaxStackReached => _persistentItem.MaxStack <= Amount;

	public InventorySlot( InventoryContainer inventory )
	{
		InventoryContainer = inventory;
	}

	public InventorySlot()
	{
	}


	[JsonIgnore] public InventoryContainer InventoryContainer { get; set; }

	[JsonIgnore] public bool HasItem => _persistentItem != null;

	public void SetItem( TItem item )
	{
		_persistentItem = item;
		InventoryContainer.OnChange();
	}

	public TItem GetItem()
	{
		return _persistentItem;
	}

	public T GetItem<T>() where T : TItem
	{
		return (T)_persistentItem;
	}

	public string GetName()
	{
		return _persistentItem.GetName();
	}
	
	public object GetIcon()
	{
		return _persistentItem.GetIcon();
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
		if ( _persistentItem == null || other._persistentItem == null )
		{
			Log.Info( "CanMerge: Item is null" );
			return false;
		}

		// abort if item types are not the same
		if ( _persistentItem.GetType() != other._persistentItem.GetType() )
		{
			Log.Info( "CanMerge: Item types are not the same" );
			return false;
		}

		// abort if either item is not stackable
		if ( _persistentItem.Stackable == false || other._persistentItem.Stackable == false )
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
		if ( _persistentItem.MaxStack < Amount + other.Amount )
		{
			Log.Info( $"CanMerge: Stack cannot hold the amount ({_persistentItem.MaxStack} < {Amount + other.Amount})" );
			return false;
		}

		try
		{
			_persistentItem.CanMergeWith( other._persistentItem );
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
		if ( !_persistentItem.CanMergeWith( other._persistentItem ) )
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
		_persistentItem.MergeWith( other._persistentItem );

		// delete and sync this one
		Delete();
		// FixEventRegistration();
		// InventoryContainer.SyncToPlayerList();

		// XLog.Info( "InventoryContainerSlot", $"Merged {other.Amount} items into slot {Index}" );
	}

	private void TakeOneOrDelete()
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
