using Clover.Data;
using Clover.Items;

namespace Clover.WorldBuilder;

public class ShopManager : Component
{
	[Property] public string StoreId { get; set; }

	[Property] public List<ShopDisplay> Displays { get; set; }

	// private ShopInventoryData _shopInventoryData;

	// [Property] public Shopkeeper Shopkeeper { get; set; }

	public class ShopItem
	{
		public string ItemId { get; set; }
		public int Price { get; set; }
		public int Stock { get; set; }
		// public int CustomCategory { get; set; }

		public ItemData ItemData { get; set; }
	}
}
