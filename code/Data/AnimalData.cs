namespace Clover.Data;

[GameResource( "Animal Data", "animal", "Animal" )]
public class AnimalData : GameResource
{
	
	[Property] public string Name { get; set; }
	
	[Property, TextArea] public string Description { get; set; }
	
	[Property] public GameObject LiveScene { get; set; }
	
}
