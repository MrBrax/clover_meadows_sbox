using System.Text.Json.Serialization;

namespace Clover;

public class WorldNodeLink
{
	
	[JsonIgnore] public World World;
	[JsonIgnore] public GameObject Node;
	
	public Vector2Int GridPosition;
	
	public World.ItemRotation GridRotation;
	public World.ItemPlacement GridPlacement;
	public World.ItemPlacementType PlacementType;

	public Vector2Int Size;
	
	public string ItemId;

	public WorldNodeLink( World world, GameObject item )
	{
		World = world;
		Node = item;
		// GetData( node );
		// LoadItemData();
	}

	public bool ShouldBeSaved()
	{
		/*// return true;
		if ( Node is IWorldItem worldItem )
		{
			return worldItem.ShouldBeSaved();
		}*/

		return true;
	}

	public void DestroyNode()
	{
		Node.Destroy();
	}
}
