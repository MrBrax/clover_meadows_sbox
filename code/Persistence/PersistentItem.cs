using System.Text.Json.Serialization;

namespace Clover.Persistence;

[JsonDerivedType( typeof( Persistence.PersistentItem ), "base" )]
public class PersistentItem
{
	
	[Property] public string ItemId { get; set; }
	
}
