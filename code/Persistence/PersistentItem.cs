using System.Text.Json.Serialization;

namespace Clover.Persistence;

[JsonDerivedType( typeof( Persistence.PersistentItem ), "base" )]
public class PersistentItem
{
	
	[Property] public string ItemId { get; set; }
	
	[Property] public Dictionary<string, object> ArbitraryData { get; set; } = new();
	
	public string GetArbitraryString( string key )
	{
		if ( ArbitraryData.TryGetValue( key, out var value ) )
		{
			return value.ToString();
		}
		return null;
	}
	
	public T GetArbitraryData<T>( string key )
	{
		if ( ArbitraryData.TryGetValue( key, out var value ) )
		{
			return (T)value;
		}
		return default;
	}
	
	public void SetArbitraryData( string key, object value )
	{
		ArbitraryData[key] = value;
	}
	
}
