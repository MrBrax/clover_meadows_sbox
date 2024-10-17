using Clover.WorldBuilder;

namespace Clover.Items;

public class ShopDisplay : Component
{
	[Property] public GameObject Container { get; set; }

	public ShopManager.ShopItem Item { get; set; }
}
