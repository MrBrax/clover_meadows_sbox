using Clover.Inventory;
using Clover.Persistence;

namespace Clover.Data;

[AssetType( Name = "Food Data", Extension = "food" )]
public class FoodData : ItemData, IEdibleData
{
	[Property, Group( "Food" )] public GameObject HoldScene { get; set; }

	[Property, Group( "Food" )] public IEdibleData.EdibleType Type { get; set; } = IEdibleData.EdibleType.Food;

	public override IEnumerable<ItemAction> GetActions( InventorySlot slot )
	{
		yield return new ItemAction { Name = "Hold", Icon = "restaurant", Action = slot.HoldEdible };

		foreach ( var action in base.GetActions( slot ) )
		{
			yield return action;
		}
	}
}
