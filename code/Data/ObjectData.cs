namespace Clover.Data;

[GameResource("ObjectData", "odata", "Object Data")]
public class ObjectData : GameResource // TODO: better name
{
	
	[Property] public string Name { get; set; }
	[Property, TextArea] public string Description { get; set; }
	
	[Property] public GameObject Prefab { get; set; }
	
}
