using System;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Clover.Carriable;
using Clover.Data;
using Clover.Items;

namespace Clover.Persistence;

[JsonDerivedType( typeof(Persistence.PersistentItem), "base" )]
public class PersistentItem
{
	[Property] public string ItemId { get; set; }

	[Property] public string PackageIdent { get; set; }

	[JsonIgnore] public bool IsPackage => !string.IsNullOrEmpty( PackageIdent );

	/// <summary>
	///  The backbone of the persistence system. This is where you can store any data you want about an item.
	///  Don't access this directly, use <see cref="GetSaveData{T}"/> and <see cref="SetSaveData"/> instead.
	/// </summary>
	[Property]
	public Dictionary<string, object> ArbitraryData { get; set; } = new();

	[JsonIgnore]
	public ItemData ItemData
	{
		get => ItemData.Get( ItemId );
	}

	[JsonIgnore] public virtual bool IsStackable => ItemData.IsStackable;
	[JsonIgnore] public virtual int StackSize => ItemData.StackSize;

	public void Initialize()
	{
		ItemData?.OnPersistentItemInitialize( this );
	}

	public object GetSaveData( Type type, string key )
	{
		// XLog.Info( this, "Keys: " + string.Join( ", ", ArbitraryData.Keys ) );
		if ( ArbitraryData.TryGetValue( key, out var obj ) )
		{
			if ( obj == null )
			{
				return null;
			}

			// i don't even know why this started happening but apparently it sometimes doesn't need to deserialize
			if ( obj.GetType() == type || obj.GetType().IsSubclassOf( type ) )
			{
				return obj;
			}

			/*if ( obj is float single )
			{
				return Convert.ChangeType( single, type );
			}

			if ( obj is double @double )
			{
				return Convert.ChangeType( @double, type );
			}

			if ( obj is int integer )
			{
				return Convert.ChangeType( integer, type );
			}*/

			if ( obj is not JsonElement jsonElement )
			{
				Log.Error( $"Arbitrary data '{key}' on '{this}' is not a JsonElement: {obj} (type: {obj.GetType()})" );
				return null;
			}

			object data;

			try
			{
				data = JsonSerializer.Deserialize( jsonElement.GetRawText(), type, GameManager.JsonOptions );
			}
			catch ( Exception e )
			{
				Log.Error( $"GetSaveData - Failed to deserialize '{key}' on '{this}' to type {type}: {e.Message}" );
				Log.Error( e );
				return null;
			}

			if ( data is JsonElement jsonElement2 )
			{
				Log.Error( $"Deserialized {key} on {this} as {data} ({data?.GetType()})" );
				return null;
			}

			return data;
		}

		return null;
	}

	/// <summary>
	///  Get arbitrary data from this item. If the key doesn't exist, it will return the default value.
	///  Use <see cref="SetSaveData"/> to store arbitrary data.
	/// </summary>
	/// <param name="key">Key to get the value from</param>
	/// <param name="defaultValue">Value to return if the key doesn't exist</param>
	/// <typeparam name="T">The same type as you saved with <see cref="SetSaveData"/></typeparam>
	/// <returns></returns>
	public T GetSaveData<T>( string key, T defaultValue = default )
	{
		return TryGetSaveData<T>( key, out var value ) ? value : defaultValue;
	}

	/// <summary>
	/// Same as <see cref="GetSaveData{T}"/> but returns false if the key doesn't exist.
	/// </summary>
	/// <param name="key">Key to get the value from</param>
	/// <param name="value">Value of the key</param>
	/// <typeparam name="T">The same type as you saved with <see cref="SetSaveData"/></typeparam>
	/// <returns></returns>
	public bool TryGetSaveData<T>( string key, out T value )
	{
		if ( ArbitraryData.TryGetValue( key, out var obj ) )
		{
			if ( obj == null )
			{
				value = default;
				return false;
			}

			// i don't even know why this started happening but apparently it sometimes doesn't need to deserialize
			if ( obj is T t )
			{
				value = t;
				return true;
			} // Check if obj can be cast to T (handles base/derived class relationships)
			else if ( (typeof(T).IsAssignableFrom( obj.GetType() ) ||
			           obj.GetType().IsAssignableFrom( typeof(T) )) )
			{
				try
				{
					value = (T)Convert.ChangeType( obj, typeof(T) );
					return true;
				}
				catch
				{
					// Fallback to normal deserialization if conversion fails
					Log.Warning( $"Type conversion failed for {key}, falling back to deserialization" );
				}
			}

			if ( obj is not JsonElement jsonElement )
			{
				Log.Error( $"Arbitrary data {key} on {this} is not a JsonElement: {obj} ({obj.GetType()})" );
				value = default;
				return false;
			}

			value = JsonSerializer.Deserialize<T>( jsonElement.GetRawText(), GameManager.JsonOptions );

			return true;
		}

		value = default;
		return false;
	}

	/// <summary>
	///  Set arbitrary data on this item. Arbitrary in this case means that you can store any type of data that can be serialized.
	///  Even complex types like classes and lists should work.
	/// </summary>
	/// <param name="key">The key to store the data under</param>
	/// <param name="value">Any serializable object</param>
	[Icon( "description" )]
	public void SetSaveData( string key, object value, Type type = null )
	{
		if ( !ValidateKey( this, key ) )
		{
			return;
		}

		if ( !ValidateValue( this, value, key ) )
		{
			return;
		}

		ArbitraryData[key] = value;
	}

	public void SetSaveData<T>( string key, T value )
	{
		if ( !ValidateKey( this, key ) )
		{
			return;
		}

		if ( !ValidateValue( this, value, key ) )
		{
			return;
		}

		ArbitraryData[key] = value;
	}

	public static bool ValidateKey( PersistentItem itemCheck, string key )
	{
		if ( string.IsNullOrEmpty( key ) )
		{
			Log.Error( $"SetSaveData - Key cannot be null or empty on {itemCheck}" );
			return false;
		}

		return true;
	}

	public static bool ValidateValue<T>( PersistentItem itemCheck, T value, string context )
	{
		if ( value is Vector3 vector3 )
		{
			if ( vector3.IsNaN || vector3.IsInfinity )
			{
				Log.Error(
					$"SetSaveData - Value for '{context}' on '{itemCheck}' cannot be NaN or Infinity: {vector3}" );
				return false;
			}
		}

		if ( value is float single )
		{
			if ( float.IsNaN( single ) || float.IsInfinity( single ) )
			{
				Log.Error(
					$"SetSaveData - Value for '{context}' on '{itemCheck}' cannot be NaN or Infinity: {single}" );
				return false;
			}
		}

		if ( value is double @double )
		{
			if ( double.IsNaN( @double ) || double.IsInfinity( @double ) )
			{
				Log.Error(
					$"SetSaveData - Value for '{context}' on '{itemCheck}' cannot be NaN or Infinity: {@double}" );
				return false;
			}
		}

		// lists and arrays
		if ( value is IList list )
		{
			foreach ( var item in list )
			{
				if ( item is float f )
				{
					if ( float.IsNaN( f ) || float.IsInfinity( f ) )
					{
						Log.Error(
							$"SetSaveData - List item for '{context}' on '{itemCheck}' cannot be NaN or Infinity: {f}" );
						return false;
					}
				}

				if ( item is double d )
				{
					if ( double.IsNaN( d ) || double.IsInfinity( d ) )
					{
						Log.Error(
							$"SetSaveData - List item for '{context}' on '{itemCheck}' cannot be NaN or Infinity: {d}" );
						return false;
					}
				}

				if ( item is Vector3 v3 )
				{
					if ( v3.IsNaN || v3.IsInfinity )
					{
						Log.Error(
							$"SetSaveData - List item for '{context}' on '{itemCheck}' cannot be NaN or Infinity: {v3}" );
						return false;
					}
				}
			}
		}

		// dictionaries
		if ( value is IDictionary dict )
		{
			// check keys
			foreach ( var key in dict.Keys )
			{
				var item = dict[key];
				if ( item is float f )
				{
					if ( float.IsNaN( f ) || float.IsInfinity( f ) )
					{
						Log.Error(
							$"SetSaveData - Dictionary item for '{context}' on '{itemCheck}' cannot be NaN or Infinity: {f}" );
						return false;
					}
				}

				if ( item is double d )
				{
					if ( double.IsNaN( d ) || double.IsInfinity( d ) )
					{
						Log.Error(
							$"SetSaveData - Dictionary item for '{context}' on '{itemCheck}' cannot be NaN or Infinity: {d}" );
						return false;
					}
				}

				if ( item is Vector3 v3 )
				{
					if ( v3.IsNaN || v3.IsInfinity )
					{
						Log.Error(
							$"SetSaveData - Dictionary item for '{context}' on '{itemCheck}' cannot be NaN or Infinity: {v3}" );
						return false;
					}
				}
			}

			// check values
			foreach ( var item in dict.Values )
			{
				if ( item is float f )
				{
					if ( float.IsNaN( f ) || float.IsInfinity( f ) )
					{
						Log.Error(
							$"SetSaveData - Dictionary item for '{context}' on '{itemCheck}' cannot be NaN or Infinity: {f}" );
						return false;
					}
				}

				if ( item is double d )
				{
					if ( double.IsNaN( d ) || double.IsInfinity( d ) )
					{
						Log.Error(
							$"SetSaveData - Dictionary item for '{context}' on '{itemCheck}' cannot be NaN or Infinity: {d}" );
						return false;
					}
				}

				if ( item is Vector3 v3 )
				{
					if ( v3.IsNaN || v3.IsInfinity )
					{
						Log.Error(
							$"SetSaveData - Dictionary item for '{context}' on '{itemCheck}' cannot be NaN or Infinity: {v3}" );
						return false;
					}
				}
			}
		}
		/*var type = value.GetType();

		// Check if the type is serializable by System.Text.Json
		try
		{
			JsonSerializer.Serialize( value, type, BraxnetGame.JsonOptions );
		}
		catch ( Exception ex )
		{
			XLog.Warning( this, $"SetSaveData - Value of type {type} is not serializable: {ex.Message}" );
			return false;
		}*/

		return true;
	}


	public virtual string GetName()
	{
		return ItemData?.Name;
	}

	public virtual string GetDescription()
	{
		return ItemData?.Description;
	}

	public virtual string GetIcon()
	{
		return ItemData?.GetIcon();
	}

	public virtual Texture GetIconTexture()
	{
		return ItemData?.GetIconTexture();
	}

	/// <summary>
	///		 Returns true if this item can be merged with the other item. Throws an exception if it can't.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public virtual bool CanMergeWith( PersistentItem other )
	{
		return true;
	}

	public virtual void MergeWith( PersistentItem other )
	{
		return;
	}

	public PersistentItem Clone()
	{
		// TODO: DON'T DO THIS KIDS, PLEASE FIND A BETTER WAY
		return JsonSerializer.Deserialize<PersistentItem>( JsonSerializer.Serialize( this, GameManager.JsonOptions ),
			GameManager.JsonOptions );
	}

	/*public GameObject Create()
	{
		var gameObject = new GameObject();

		if ( gameObject.Components.TryGet<WorldItem>( out var worldItem ) )
		{
			worldItem.NodeLink.Persistence = this;
		}

		if ( gameObject.Components.TryGet<Persistent>( out var persistent ) )
		{
			persistent.OnItemLoad( this );
		}

		return gameObject;
	}*/

	// TODO: maybe less hardcoded and repeated code
	public static PersistentItem Create( GameObject gameObject )
	{
		if ( !gameObject.IsValid() ) throw new Exception( "Item is null" );

		var persistentItem = new PersistentItem();

		if ( gameObject.Components.TryGet<WorldItem>( out var worldItem ) )
		{
			persistentItem.ItemId = worldItem.ItemData.GetIdentifier();
		}

		if ( gameObject.Components.TryGet<BaseCarriable>( out var carriable ) )
		{
			persistentItem.ItemId ??= carriable.ItemData.GetIdentifier();
		}

		if ( gameObject.Components.TryGet<WorldObject>( out var worldObject ) )
		{
			worldObject.OnObjectSaveAction?.Invoke( persistentItem );
		}

		if ( gameObject.Components.TryGet<Persistent>( out var persistent ) )
		{
			persistent.OnItemSave( persistentItem );
		}

		if ( gameObject.Components.TryGet<IPersistent>( out var persistent2 ) )
		{
			persistent2.OnSave( persistentItem );
		}

		var nodeLink = WorldManager.Instance.GetWorldNodeLink( gameObject );
		if ( nodeLink != null )
		{
			nodeLink.OnNodeSave();
			persistentItem = nodeLink.GetPersistence();
		}

		return persistentItem;
	}

	public static PersistentItem Create( ItemData itemData, bool initialize = false )
	{
		var item = new PersistentItem { ItemId = itemData.GetIdentifier() };

		if ( initialize ) item.Initialize();

		return item;
	}

	public static PersistentItem Create( string itemId, bool initialize = false )
	{
		return Create( ItemData.Get( itemId ), initialize );
	}

	/// <summary>
	///  Might sound stupid but don't use this unless you're spawning a carriable.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public BaseCarriable SpawnCarriable()
	{
		if ( ItemData is not ToolData toolData ) throw new Exception( $"ItemData is not a ToolData for {ItemId}" );

		var carriable = toolData.SpawnCarriable();

		if ( carriable == null ) throw new Exception( $"Carriable is null for {ItemId}" );

		carriable.Durability = GetSaveData<int>( "Durability" );

		if ( carriable.GameObject.Components.TryGet<Persistent>( out var persistent ) )
		{
			persistent.OnItemLoad( this );
		}

		if ( carriable.GameObject.Components.TryGet<IPersistent>( out var persistent2 ) )
		{
			persistent2.OnLoad( this );
		}

		return carriable;
	}

	public async Task<Package> GetPackage()
	{
		return await Package.Fetch( PackageIdent, false );
	}
}
