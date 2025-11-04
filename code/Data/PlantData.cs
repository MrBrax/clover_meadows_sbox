using Clover.Inventory;
using Clover.Persistence;

namespace Clover.Data;

[AssetType( Name = "PlantData", Extension = "plant" )]
public class PlantData : ItemData
{
	[Property, Group( "Plant" )] public GameObject PlantedScene { get; set; }

	/*public override IEnumerable<ItemAction> GetActions( InventorySlot<PersistentItem> slot )
	{
		yield return new ItemAction
		{
			Name = "Plant",
			Icon = "park",
			Action = slot.Plant
		};
	}*/
}
