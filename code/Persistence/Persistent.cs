namespace Clover.Persistence;

[Category( "Clover/Persistence" )]
[Icon( "save" )]
public class Persistent : Component
{
	
	public delegate void OnObjectSave( PersistentItem item );

	[Property] public OnObjectSave OnItemSave { get; set; }

	public delegate void OnObjectLoad( PersistentItem item );

	[Property] public OnObjectLoad OnItemLoad { get; set; }
	
}
