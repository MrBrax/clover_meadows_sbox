using Clover.Inventory;
using Clover.Persistence;

namespace Clover.Data;

[GameResource( "SeedData", "seed", "Seed", Icon = "yard" )]
public class SeedData : ItemData
{
	
	[Property] public ItemData SpawnedItemData { get; set; }
	
	public override IEnumerable<ItemAction> GetActions( InventorySlot<PersistentItem> slot )
	{
		yield return new ItemAction
		{
			Name = "Plant", 
			Icon = "park",
			Action = slot.Plant
		};
	}
	
}
