namespace Clover.Data;

[GameResource( "Animal Data", "animal", "Animal" )]
public class AnimalData : ItemData
{
	
	// [Property] public string Name { get; set; }
	
	// [Property, TextArea] public string Description { get; set; }
	
	[Property, Group("Animal")] public GameObject LiveScene { get; set; }
	
}
