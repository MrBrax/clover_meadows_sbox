using System.Text.Json.Serialization;
using Clover.Data;

namespace Clover.Persistence;

[JsonDerivedType( typeof( Persistence.PersistentItem ), "base" )]
public class PersistentItem
{
	
	[Property] public string ItemId { get; set; }
	
	[Property] public Dictionary<string, object> ArbitraryData { get; set; } = new();
	
	[JsonIgnore] public ItemData ItemData
	{
		get => ResourceLibrary.GetAll<ItemData>().FirstOrDefault( x => x.ResourceName == ItemId );
	}
	
	public string GetArbitraryString( string key )
	{
		if ( ArbitraryData.TryGetValue( key, out var value ) )
		{
			return value.ToString();
		}
		return null;
	}
	
	public int GetArbitraryInt( string key )
	{
		if ( ArbitraryData.TryGetValue( key, out var value ) )
		{
			return (int)value;
		}
		return 0;
	}
	
	public float GetArbitraryFloat( string key )
	{
		if ( ArbitraryData.TryGetValue( key, out var value ) )
		{
			return (float)value;
		}
		return 0;
	}
	
	public bool GetArbitraryBool( string key )
	{
		if ( ArbitraryData.TryGetValue( key, out var value ) )
		{
			return (bool)value;
		}
		return false;
	}
	
	public T GetArbitraryData<T>( string key )
	{
		if ( ArbitraryData.TryGetValue( key, out var value ) )
		{
			return (T)value;
		}
		return default;
	}
	
	[Description( "Set arbitrary data on this item." )]
	[Icon("description")]
	public void SetArbitraryData( string key, object value )
	{
		ArbitraryData[key] = value;
	}
	
	public virtual string GetName()
	{
		return ItemData?.Name;
	}
	
	public virtual string GetDescription()
	{
		return ItemData?.Description;
	}
	
}
