using Clover.Inventory;
using Clover.Items;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Data;

public class BuriedItem : Component, IPersistent, IDiggable
{
	[RequireComponent] public WorldItem WorldItem { get; private set; }

	private PersistentItem Item { get; set; }

	public void OnSave( PersistentItem item )
	{
		item.SetArbitraryData( "Item", Item );
	}

	public void OnLoad( PersistentItem item )
	{
		Item = item.GetArbitraryData<PersistentItem>( "Item" );
	}

	public void SetItem( PersistentItem item )
	{
		Item = item;
	}

	public PersistentItem GetItem()
	{
		return Item;
	}

	public bool CanDig()
	{
		return true;
	}

	public bool OnDig( PlayerCharacter player, WorldNodeLink item )
	{
		if ( Item == null )
		{
			Log.Warning( "No item to dig up" );
			return false;
		}

		try
		{
			player.Inventory.PickUpItem( Item );
		}
		catch ( InventoryFullException e )
		{
			Log.Warning( e.Message );
			return false;
		}
		catch ( System.Exception e )
		{
			Log.Error( e.Message );
			return false;
		}

		return true;
	}
}
