using Clover.Inventory;
using Clover.Items;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Carriable;

public class BaseCarriable : Component, IPersistent, IPickupable
{
	public int Durability { get; set; } = 100;

	public TimeUntil NextUse;

	public GameObject Holder { get; set; }
	public PlayerCharacter Player => Holder.GetComponent<PlayerCharacter>();

	// public ToolData ItemData { get; set; }

	public void SetHolder( GameObject holder )
	{
		Holder = holder;
	}

	public virtual bool CanUse()
	{
		return NextUse <= 0 && Durability > 0;
	}

	public virtual float CustomPlayerSpeed()
	{
		return 1;
	}

	public virtual void OnEquip( GameObject holder )
	{
		SetHolder( holder );
	}

	public virtual void OnUnequip()
	{
		Holder = null;
	}

	public virtual void OnUseDown()
	{
	}

	public virtual void OnUseUp()
	{
	}

	public virtual void OnBreak()
	{
	}

	/*public void OnSave( WorldNodeLink nodeLink )
	{
		nodeLink.Persistence?.SetArbitraryData( "Durability", Durability );
	}

	public void OnLoad( WorldNodeLink nodeLink )
	{
		if ( nodeLink.Persistence == null ) return;
		if ( nodeLink.Persistence.TryGetArbitraryData<int>( "Durability", out var durability ) )
		{
			Durability = durability;
		}
	}*/

	public bool CanPickup( PlayerCharacter player )
	{
		return true;
	}

	public void OnPickup( PlayerCharacter player )
	{
		player.Inventory.PickUpItem( GetComponent<WorldItem>().NodeLink );
	}

	public void OnSave( PersistentItem item )
	{
		item.SetArbitraryData( "Durability", Durability );
	}

	public void OnLoad( PersistentItem item )
	{
		if ( item.TryGetArbitraryData<int>( "Durability", out var durability ) )
		{
			Durability = durability;
		}
	}
}
