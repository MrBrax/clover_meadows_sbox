using Clover.Inventory;
using Clover.Persistence;

namespace Clover.Data;

[GameResource( "Fruit Data", "fruit", "FruitData" )]
public class FruitData : ItemData, IEdibleData
{
	[Property, Group( "Fruit" )] public GameObject InTreeScene { get; set; }

	[Property, Group( "Food" )] public GameObject HoldScene { get; set; }

	public override IEnumerable<ItemAction> GetActions( InventorySlot<PersistentItem> slot )
	{
		// TODO: Add "Eat" action
		/*yield return new ItemAction
		{
			Name = "Eat",
			Icon = "restaurant",
			Action = slot.Delete
		};*/

		yield return new ItemAction { Name = "Hold", Icon = "restaurant", Action = slot.HoldEdible };

		foreach ( var action in base.GetActions( slot ) )
		{
			yield return action;
		}
	}
}
