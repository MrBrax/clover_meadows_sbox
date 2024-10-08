namespace Clover.Data;

[GameResource("ObjectData", "odata", "Object Data", Icon = "rocket_launch")]
[Icon("rocket_launch")]
public class ObjectData : GameResource // TODO: better name
{
	
	[Property] public string Name { get; set; }
	[Property, TextArea] public string Description { get; set; }
	
	[Property] public GameObject Prefab { get; set; }
	[Property] public bool CanPickup { get; set; }
	
	[Property] public ItemData PickupData { get; set; }

	public static ObjectData Get( string objectId )
	{
		return ResourceLibrary.GetAll<ObjectData>().FirstOrDefault( x => x.ResourceName == objectId );
	}
}
