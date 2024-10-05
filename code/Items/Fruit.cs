using Clover.Data;

namespace Clover.Items;

public class Fruit : Component
{
	
	[RequireComponent] public WorldItem WorldItem { get; set; }
	
	[Property] public FruitData FruitData { get; set; }
	
}
