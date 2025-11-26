namespace Clover.Persistence;

public class PersistentWorldItem
{
	public string ItemId { get; set; }
	// public string PrefabPath;

	public Vector3 WPosition { get; set; }
	public Angles WAngles { get; set; }

	// public Vector2Int Position { get; set; }
	// public World.ItemRotation Rotation { get; set; }

	// public World.ItemPlacement Placement;
	public World.ItemPlacementType PlacementType { get; set; }

	public PersistentItem Item { get; set; }
}
