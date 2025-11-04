using Clover.Inventory;
using Clover.Persistence;

namespace Clover.Data;

[AssetType( Name = "SeedData", Extension = "seed" )]
public class SeedData : ItemData
{
	[Property, Group( "Seed" )] public ItemData SpawnedItemData { get; set; }

	public override IEnumerable<ItemAction> GetActions( InventorySlot<PersistentItem> slot )
	{
		yield return new ItemAction { Name = "Plant", Icon = "park", Action = slot.Plant };
	}
}
