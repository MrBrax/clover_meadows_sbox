using System;

namespace Clover.Persistence;

/// <summary>
///   Marks a property as arbitrary data, which automatically gets serialized and deserialized when saving and loading
/// </summary>
public class SaveDataAttribute : Attribute
{
	public SaveDataAttribute()
	{
	}

	/*public SaveDataAttribute( string key )
	{
		Key = key;
	}*/

	public SaveDataAttribute( string key = "", string onSaveMethodName = "", string onLoadMethodName = "" )
	{
		Key = key;
		OnSaveMethodName = onSaveMethodName;
		OnLoadMethodName = onLoadMethodName;
	}

	public string Key { get; set; }
	public string OnSaveMethodName { get; set; }
	public string OnLoadMethodName { get; set; }

	public object DefaultValue { get; set; }

	public bool ResetOnPickup { get; set; }
}
