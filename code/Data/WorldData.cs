namespace Clover.Data;

[GameResource("World", "world", "world")]
public class WorldData : GameResource
{
	
	[Property] public string Title { get; set; }
	
	[Property] public GameObject Prefab { get; set; }
	
	[Property, Group("Weather")] public bool IsInside { get; set; }
	
	[Property, Group("Grid")] public int Width { get; set; }
	
	[Property, Group("Grid")] public int Height { get; set; }


	[Property] public float MaxItemAltitude = 0;

}
