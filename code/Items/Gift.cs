using Clover.Data;
using Clover.Interactable;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Items;

[Category( "Clover/Items" )]
public class Gift : Component, IInteract, IPersistent
{
	[RequireComponent] public WorldItem WorldItem { get; private set; }

	private IList<PersistentItem> Items { get; set; } = new List<PersistentItem>();

	public void StartInteract( PlayerCharacter player )
	{
		if ( !Networking.IsHost )
		{
			Log.Error( "Only the host can use world altering items for now." );
			return;
		}

		if ( Items.Count == 0 )
		{
			Log.Warning( "No items to pick up" );
			return;
		}

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

		WorldItem.NodeLink.Remove();
	}

	public void FinishInteract( PlayerCharacter player )
	{
	}

	public string GetInteractName()
	{
		return "Open gift";
	}

	public void OnSave( PersistentItem item )
	{
		item.SetSaveData( "Items", Items );
	}

	public void OnLoad( PersistentItem item )
	{
		Items = item.GetSaveData<IList<PersistentItem>>( "Items" );
	}
}
