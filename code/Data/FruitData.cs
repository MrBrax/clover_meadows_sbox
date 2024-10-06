using Clover.Inventory;
using Clover.Persistence;

namespace Clover.Data;

[GameResource( "Fruit Data", "fruit", "FruitData" )]
public class FruitData : ItemData
{
	[Property] public GameObject InTreeScene { get; set; }


	public override IEnumerable<ItemAction> GetActions( InventorySlot<PersistentItem> slot )
	{
		// TODO: Add "Eat" action
		yield return new ItemAction { Name = "Eat", Action = () => { slot.Delete(); } };
	}
}
