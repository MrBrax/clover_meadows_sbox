using System;
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
	///  Don't access this directly, use <see cref="GetArbitraryData{T}"/> and <see cref="SetArbitraryData"/> instead.
	/// </summary>
	[Property] public Dictionary<string, object> ArbitraryData { get; set; } = new();

	[JsonIgnore]
	public ItemData ItemData
	{
		get => ItemData.Get( ItemId );
	}

	[JsonIgnore] public virtual bool IsStackable => ItemData.IsStackable;
	[JsonIgnore] public virtual int StackSize => ItemData.StackSize;
	
	/// <summary>
	///  Get arbitrary data from this item. If the key doesn't exist, it will return the default value.
	///  Use <see cref="SetArbitraryData"/> to store arbitrary data.
	/// </summary>
	/// <param name="key">Key to get the value from</param>
	/// <param name="defaultValue">Value to return if the key doesn't exist</param>
	/// <typeparam name="T">The same type as you saved with <see cref="SetArbitraryData"/></typeparam>
	/// <returns></returns>
	public T GetArbitraryData<T>( string key, T defaultValue = default )
	{
		return TryGetArbitraryData<T>( key, out var value ) ? value : defaultValue;
	}

	/// <summary>
	/// Same as <see cref="GetArbitraryData{T}"/> but returns false if the key doesn't exist.
	/// </summary>
	/// <param name="key">Key to get the value from</param>
	/// <param name="value">Value of the key</param>
	/// <typeparam name="T">The same type as you saved with <see cref="SetArbitraryData"/></typeparam>
	/// <returns></returns>
	public bool TryGetArbitraryData<T>( string key, out T value )
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

	public static PersistentItem Create( ItemData itemData )
	{
		return new PersistentItem
		{
			ItemId = itemData.GetIdentifier()
		};
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
		
		carriable.Durability = GetArbitraryData<int>( "Durability" );
		
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
