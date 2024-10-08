namespace Clover.Data;

[GameResource( "PlantData", "plant", "Plant" )]
public class PlantData : ItemData
{
	
	[Property] public GameObject PlantedScene { get; set; }
	
}
