using System;

namespace Clover.Persistence;

public class WorldSaveData
{
	
	public string Name;
	// public Dictionary<Vector2Int, Dictionary<World.ItemPlacement, NodeEntry>> Items = new();
	public List<PersistentWorldItem> Items = new();

	/*public struct NodeEntry
	{
		public PersistentItem Item;
		public WorldNodeLink NodeLink;
	}*/

	public Dictionary<string, string> Wallpapers = new();
	public Dictionary<string, string> Floors = new();

	public DateTime LastSave = DateTime.Now;
	
}
