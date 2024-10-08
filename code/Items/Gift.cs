using Clover.Interactable;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Items;

public class Gift : Component, IInteract, IPersistent
{

	private IList<PersistentItem> Items { get; set; } = new List<PersistentItem>();
	
	public void StartInteract( PlayerCharacter player )
	{
		var freeSlots = player.Inventory.Container.FreeSlots;
		if ( freeSlots < Items.Count )
		{
			Log.Warning( "Not enough space in inventory" );
			return;
		}
		
		foreach ( var item in Items )
		{
			player.Inventory.PickUpItem( item );
		}
		
		GameObject.Destroy();
	}

	public void FinishInteract( PlayerCharacter player )
	{
		
	}

	public void OnSave( PersistentItem item )
	{
		item.SetArbitraryData( "Items", Items );
	}

	public void OnLoad( PersistentItem item )
	{
		// TODO: Check if we can arbitrarily deserialize complex types
		Items = item.GetArbitraryData<IList<PersistentItem>>( "Items" );
	}
}
