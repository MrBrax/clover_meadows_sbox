using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Clover.Data;
using Clover.Persistence;
using Clover.Player;
using Sandbox.Diagnostics;

namespace Clover.Inventory;

public sealed partial class InventoryContainer
{

	public Guid Id { get; set; } = Guid.NewGuid();

	[JsonIgnore] public GameObject Owner { get; set; }

	[JsonIgnore] public PlayerCharacter Player => Owner.GetComponent<PlayerCharacter>();

	[JsonInclude] public int MaxItems { get; set; } = 20;

	[JsonInclude] public List<InventorySlot<PersistentItem>> Slots = new();
	
	
	public delegate void InventoryChangedEventHandler();
	[Property] public event InventoryChangedEventHandler InventoryChanged;

	public InventoryContainer()
	{
		Log.Info( "Creating inventory with default slots" );
	}

	public InventoryContainer( int slots )
	{
		Log.Info( $"Creating inventory with {slots} slots" );
		MaxItems = slots;
	}

	public InventorySlot<PersistentItem> GetSlotByIndex( int index )
	{
		return Slots.FirstOrDefault( slot => slot.Index == index );
	}

	public struct InventoryContainerEntry
	{
		public int Index;
		public InventorySlot<PersistentItem> Slot;
		public bool HasSlot => Slot != null;
	}

	public IEnumerable<InventoryContainerEntry> GetEnumerator()
	{
		for ( var i = 0; i < MaxItems; i++ )
		{
			yield return new InventoryContainerEntry { Index = i, Slot = GetSlotByIndex( i ) };
		}
	}

	/// <summary>
	/// Returns the index of the first empty slot in the inventory.
	/// </summary>
	/// <returns>The index of the first empty slot, or -1 if no empty slot is found.</returns>
	public int GetFirstFreeEmptyIndex()
	{
		foreach ( var slot in GetEnumerator() )
		{
			if ( slot.Slot == null )
			{
				return slot.Index;
			}
		}

		return -1;
	}

	public IEnumerable<InventorySlot<PersistentItem>> GetUsedSlots()
	{
		return Slots.ToImmutableList();
	}

	public int FreeSlots => MaxItems - Slots.Count;

	public void RemoveSlots()
	{
		Log.Trace( "Removing slots" );
		Slots.Clear();
	}

	public void ImportSlot( InventorySlot<PersistentItem> slot )
	{
		if ( Slots.Count >= MaxItems )
		{
			// throw new System.Exception( "Inventory is full." );
			throw new InventoryFullException( "Inventory is full." );
		}

		slot.InventoryContainer = this;

		// if the slot has no index or the index is already taken, assign a new index
		if ( slot.Index == -1 || GetSlotByIndex( slot.Index ) != null )
		{
			Log.Warning( "Imported slot has no index or index is already taken, assigning new index" );
			slot.Index = GetFirstFreeEmptyIndex();
		}

		Slots.Add( slot );

		RecalculateIndexes();
	}

	/// <summary>
	///  Add an item to the inventory.
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	/// <exception cref="InventoryFullException"></exception>
	public InventorySlot<PersistentItem> AddItem( PersistentItem item, bool merge = false )
	{
		InventorySlot<PersistentItem> slot;

		if ( !merge )
		{
			var index = GetFirstFreeEmptyIndex();
			if ( index == -1 )
			{
				throw new InventoryFullException( "Inventory is full." );
			}

			slot = new InventorySlot<PersistentItem>( this );
			slot.Index = index;
			slot.SetItem( item );

			Slots.Add( slot );

		}
		else
		{
			slot = GetSlotWithItem( item.ItemData );
			if ( slot == null || !item.ItemData.IsStackable )
			{
				// slot = new InventorySlot<PersistentItem>( this );
				// slot.SetItem( item );
				// Slots.Add( slot );
				return AddItem( item, false );

			}
			else
			{
				if ( slot.Amount + 1 > item.ItemData.StackSize )
				{
					throw new Exception( "Cannot merge item, stack size exceeded" );
				}

				slot.SetAmount( slot.Amount + 1 );
			}
		}

		RecalculateIndexes();

		OnChange();

		return slot;
	}

	/// <summary>
	///  Add an item to the inventory at a specific index.
	/// </summary>
	/// <param name="item"></param>
	/// <param name="index"></param>
	/// <exception cref="InventoryFullException"></exception>
	/// <exception cref="SlotTakenException"></exception>
	/// <exception cref="Exception"></exception>
	public void AddItemToIndex( PersistentItem item, int index = -1 )
	{

		if ( index == -1 )
		{
			index = GetFirstFreeEmptyIndex();
			if ( index == -1 )
			{
				throw new InventoryFullException( "Inventory is full." );
			}
		}

		if ( GetSlotByIndex( index ) != null )
		{
			// TODO: merge items if possible
			throw new SlotTakenException( $"Slot {index} is already taken." );
		}

		if ( item.ItemData == null ) throw new Exception( "ItemData is null" );

		var slot = new InventorySlot<PersistentItem>( this );
		slot.Index = index;
		slot.SetItem( item );

		Slots.Add( slot );

		RecalculateIndexes();

		// OnInventoryChanged?.Invoke();
		OnChange();
	}


	/// <summary>
	///  Recalculate the indexes of all slots in the inventory, keeping old indexes
	/// </summary>
	public void RecalculateIndexes()
	{
		var index = 0;
		foreach ( var slot in GetUsedSlots() )
		{
			// slot.Index = index++;

			if ( slot.Index == -1 )
			{
				Log.Trace( "Slot has no index, assigning new index" );
				slot.Index = index++;
			}
			else
			{
				Log.Trace( $"Slot has index {slot.Index}, keeping index" );
				index = slot.Index + 1;
			}
		}

		// OnInventoryChanged?.Invoke( this );
	}

	/// <summary>
	///    Reset the index for all slots in the inventory based on their location in the list
	/// </summary>
	public void ResetIndexes()
	{
		var index = 0;
		foreach ( var slot in GetUsedSlots() )
		{
			slot.Index = index++;
		}

		// OnInventoryChanged?.Invoke( this );
	}

	/* public void SortSlots()
	{
		Slots.Sort( SlotSortingFunc );
	} */

	/* public void SortByType()
	{
		// MergeAllSlots();
		// XLog.Info( "Inventory", $"Sorting inventory {Id} by type" );
		Slots.Sort( ( a, b ) => string.Compare( a.ItemType, b.ItemType, StringComparison.Ordinal ) );
		// RecalculateIndexes();
		ResetIndexes();
		// OnInventoryChanged?.Invoke( this );
		// SyncToPlayerList();
	} */

	public void SortByName()
	{
		// MergeAllSlots();
		// XLog.Info( "Inventory", $"Sorting inventory {Id} by name" );
		Slots.Sort( ( a, b ) => string.Compare( a.GetName(), b.GetName(), StringComparison.Ordinal ) );
		// RecalculateIndexes();
		ResetIndexes();
		// OnInventoryChanged?.Invoke( this );
		// SyncToPlayerList();
	}

	public void SortByIndex()
	{
		// MergeAllSlots();
		// XLog.Info( "Inventory", $"Sorting inventory {Id} by index" );
		Slots.Sort( ( a, b ) => a.Index.CompareTo( b.Index ) );
		// RecalculateIndexes();
		ResetIndexes();
		// OnInventoryChanged?.Invoke( this );
		// SyncToPlayerList();
	}

	private static int SlotSortingFunc( InventorySlot<PersistentItem> a, InventorySlot<PersistentItem> b )
	{
		var itemA = a.GetItem();
		var itemB = b.GetItem();

		if ( itemA == null && itemB == null )
		{
			return 0;
		}

		if ( itemA == null )
		{
			return 1;
		}

		if ( itemB == null )
		{
			return -1;
		}

		return string.Compare( itemA.GetName(), itemB.GetName(), System.StringComparison.Ordinal );
	}

	/* public override void _Process( double delta )
	{
		if ( Input.IsActionJustPressed( "UseTool" ) )
		{
			/* if ( CurrentCarriable != null )
			{
				CurrentCarriable.OnUse( Player );
			} *

			if ( Player.HasEquippedItem( PlayerController.EquipSlot.Tool ) )
			{
				Player.GetEquippedItem<Carriable.BaseCarriable>( PlayerController.EquipSlot.Tool ).OnUse( Player );
			}

			/*var testItem = new InventoryItem( this );
			testItem.ItemDataPath = "res://items/furniture/polka_chair/polka_chair.tres";
			testItem.DTO = new BaseDTO
			{
				ItemDataPath = "res://items/furniture/polka_chair/polka_chair.tres",
			};
			
			var slot = GetFirstFreeSlot();
			if ( slot == null )
			{
				throw new System.Exception( "No free slots." );
				return;
			}
			
			slot.SetItem( testItem );*
		}
		else if ( Input.IsActionJustPressed( "Drop" ) )
		{
			/*var item = Items.FirstOrDefault();
			if ( item != null )
			{
				DropItem( item );
			}*
		}
	} */

	public void OnChange()
	{
		// OnInventoryChanged?.Invoke();
		//x EmitSignal( SignalName.InventoryChanged );
	}

	public void RemoveSlot( InventorySlot<PersistentItem> inventorySlot )
	{
		Slots.Remove( inventorySlot );
		RecalculateIndexes();
		OnChange();
	}

	public void RemoveSlot( int index )
	{
		var slot = GetSlotByIndex( index );
		if ( slot == null )
		{
			throw new Exception( $"Slot {index} not found." );
		}

		RemoveSlot( slot );
	}

	public bool MoveSlot( int slotIndexFrom, int slotIndexTo )
	{
		if ( slotIndexFrom < 0 || slotIndexFrom >= MaxItems )
		{
			// Log.Error( $"SlotIndexFrom {slotIndexFrom} is out of range" );
			throw new ArgumentOutOfRangeException( $"Move: SlotIndexFrom {slotIndexFrom} is out of range" );
		}

		if ( slotIndexTo < 0 || slotIndexTo >= MaxItems )
		{
			// Log.Error( $"SlotIndexTo {slotIndexTo} is out of range" );
			// return false;
			throw new ArgumentOutOfRangeException( $"Move: SlotIndexTo {slotIndexTo} is out of range" );
		}

		/* if ( !AllowSlotMoving )
		{
			throw new Exception( "You cannot move items in this inventory" );
		} */

		var slotFrom = GetSlotByIndex( slotIndexFrom );
		var slotTo = GetSlotByIndex( slotIndexTo );

		if ( slotFrom == null )
		{
			// Log.Error( $"SlotFrom {slotIndexFrom} is null" );
			// return false;
			throw new Exception( $"Move: SlotFrom {slotIndexFrom} is null" );
		}

		if ( slotFrom == slotTo )
		{
			// throw new Exception( $"SlotFrom {slotIndexFrom} is the same as SlotTo {slotIndexTo}" );
			return false; // don't throw an exception, just error silently
		}

		if ( slotTo != null )
		{
			if ( slotFrom.CanMergeWith( slotTo ) )
			{
				slotFrom.MergeWith( slotTo );
				return true;
			}

			return SwapSlot( slotIndexFrom, slotIndexTo );
		}

		slotFrom.Index = slotIndexTo;

		// Slots.Sort( ( a, b ) => a.Index.CompareTo( b.Index ) );
		// SortByIndex();
		RecalculateIndexes();

		// FixEventRegistration();

		// SyncToPlayerList();

		// OnInventoryChanged?.Invoke( this );
		OnChange();

		return true;
	}

	public bool SwapSlot( int slotIndexFrom, int slotIndexTo )
	{
		if ( slotIndexFrom < 0 || slotIndexFrom >= MaxItems )
		{
			// Log.Error( $"SlotIndexFrom {slotIndexFrom} is out of range" );
			// return false;
			throw new ArgumentOutOfRangeException( $"Swap: SlotIndexFrom {slotIndexFrom} is out of range" );
		}

		if ( slotIndexTo < 0 || slotIndexTo >= MaxItems )
		{
			// Log.Error( $"SlotIndexTo {slotIndexTo} is out of range" );
			// return false;
			throw new ArgumentOutOfRangeException( $"Swap: SlotIndexTo {slotIndexTo} is out of range" );
		}

		/* if ( !AllowSlotMoving )
		{
			throw new Exception( "You cannot move items in this inventory" );
		} */

		var slotFrom = GetSlotByIndex( slotIndexFrom );
		var slotTo = GetSlotByIndex( slotIndexTo );

		if ( slotFrom == null )
		{
			// Log.Error( $"SlotFrom {slotIndexFrom} is null" );
			// return false;
			throw new Exception( $"Swap: SlotFrom {slotIndexFrom} is null" );
		}

		if ( slotTo == null )
		{
			// Log.Error( $"SlotTo {slotIndexTo} is null" );
			// return false;
			throw new Exception( $"Swap: SlotTo {slotIndexTo} is null" );
		}

		slotFrom.Index = slotIndexTo;
		slotTo.Index = slotIndexFrom;

		// Slots.Sort( ( a, b ) => a.Index.CompareTo( b.Index ) );
		// SortByIndex();
		RecalculateIndexes();

		// SyncToPlayerList();

		// OnInventoryChanged?.Invoke( this );
		OnChange();

		return true;
	}

	public void DeleteAll()
	{
		Slots.Clear();
		// OnInventoryChanged?.Invoke();
		OnChange();
	}

	public bool HasItem( ItemData item )
	{
		return Slots.Any( slot => slot.HasItem && slot.GetItem().ItemData.IsSameAs( item ) );
	}

	public bool HasItem( ItemData item, int quantity )
	{
		return Slots.Any( slot => slot.HasItem && slot.GetItem().ItemData.IsSameAs( item ) && slot.Amount >= quantity );
	}

	public InventorySlot<PersistentItem> GetSlotWithItem( ItemData item )
	{
		return Slots.FirstOrDefault( slot => slot.HasItem && slot.GetItem().ItemData.IsSameAs( item ) );
	}

	public InventorySlot<PersistentItem> GetSlotWithItem( ItemData item, int quantity )
	{
		return Slots.FirstOrDefault( slot => slot.HasItem && slot.GetItem().ItemData.IsSameAs( item ) && slot.Amount >= quantity );
	}

	public IEnumerable<InventorySlot<PersistentItem>> GetSlotsWithItem( ItemData item )
	{
		return Slots.Where( slot => slot.HasItem && slot.GetItem().ItemData.IsSameAs( item ) );
	}

	public void RemoveItem( ItemData item, int quantity )
	{
		var slot = GetSlotWithItem( item, quantity );
		if ( slot == null )
		{
			throw new Exception( $"Item {item.Name} not found in inventory" );
		}

		slot.SetAmount( slot.Amount - quantity );

		if ( slot.Amount <= 0 )
		{
			RemoveSlot( slot );
		}

		OnChange();
	}

	public bool CanFit( PersistentItem item )
	{
		if ( FreeSlots > 0 ) return true;

		var slots = GetSlotsWithItem( item.ItemData );
		if ( slots.Count() == 0 ) return false;

		var slotWithStackSpaceLeft = slots.Where( s => s.Amount < s._persistentItem.ItemData.StackSize ).ToList();

		if ( slotWithStackSpaceLeft.Count() > 0 ) return true;

		return false;

	}

	public bool CanFit( List<PersistentItem> results )
	{
		var freeSlots = FreeSlots;
		var items = results.Count;

		if ( freeSlots >= items )
		{
			return true;
		}

		foreach ( var result in results )
		{
			if ( !CanFit( result ) )
			{
				return false;
			}
		}

		/* var stackableItems = results.Where( r => r.ItemData.StackSize > 1 ).ToList();
		var nonStackableItems = results.Where( r => r.ItemData.StackSize == 1 ).ToList();

		var stackableItemsCount = stackableItems.Count;
		var nonStackableItemsCount = nonStackableItems.Count;

		if ( stackableItemsCount == 0 )
		{
			return false;
		}

		var stackableItemsTotal = stackableItems.Sum( r => r.ItemData.StackSize );
		var stackableItemsFreeSlots = stackableItemsTotal - stackableItemsCount;

		if ( stackableItemsFreeSlots >= nonStackableItemsCount )
		{
			return true;
		} */

		return true;
	}

}


public class InventoryFullException : System.Exception
{
	public InventoryFullException( string message ) : base( message )
	{
	}
}

public class SlotTakenException : System.Exception
{
	public SlotTakenException( string message ) : base( message )
	{
	}
}
