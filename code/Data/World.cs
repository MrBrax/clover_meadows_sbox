namespace Clover.Data;

[GameResource("World", "world", "world")]
public class World : GameResource
{
	
	[Property] public string Title { get; set; }
	
	[Property] public GameObject Prefab { get; set; }
	
}
