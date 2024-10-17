using System.Text.Json.Serialization;
using Clover.Data;
using Clover.Items;

namespace Clover.WorldBuilder;

public class ShopManager : Component
{
	[Property] public string StoreId { get; set; }

	[Property] public List<ShopDisplay> Displays { get; set; }

	// private ShopInventoryData _shopInventoryData;

	// [Property] public Shopkeeper Shopkeeper { get; set; }


	private void GenerateItems()
	{
		foreach ( var display in Displays )
		{
		}
	}

	public class ShopItem
	{
		public string ItemId { get; set; }
		public int Price { get; set; }
		public int Stock { get; set; }
		public string DisplayId { get; set; }
		[JsonIgnore] public ItemData ItemData => ItemData.Get( ItemId );
	}

	public List<ShopItem> Items { get; set; }
}
