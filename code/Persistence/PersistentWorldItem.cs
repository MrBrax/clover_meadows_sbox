﻿namespace Clover.Persistence;

public class PersistentWorldItem
{
	
	public string ItemId;
	public string PrefabPath;
	
	public Vector2Int Position;
	public World.ItemRotation Rotation;
	public World.ItemPlacement Placement;
	public World.ItemPlacementType PlacementType;
	
	public PersistentItem Item;

}