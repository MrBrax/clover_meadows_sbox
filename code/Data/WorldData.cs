namespace Clover.Data;

[GameResource("World", "world", "world")]
public class WorldData : GameResource
{
	
	[Property] public string Title { get; set; }
	
	[Property] public GameObject Prefab { get; set; }
	
	[Property] public bool IsInside { get; set; }
	
	[Property] public int Width { get; set; }
	
	[Property] public int Height { get; set; }
	
}
