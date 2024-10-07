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
	[Property] public string ObjectId { get; set; }
	
	[Property] public string PackageIdent { get; set; }
	
	[JsonIgnore] public bool IsPackage => !string.IsNullOrEmpty( PackageIdent );

	[Property] public Dictionary<string, object> ArbitraryData { get; set; } = new();

	[JsonIgnore]
	public ItemData ItemData
	{
		get => ItemData.Get( ItemId );
	}
	
	[JsonIgnore]
	public ObjectData ObjectData
	{
		get => ObjectData.Get( ObjectId );
	}

	[JsonIgnore] public virtual bool IsStackable => ItemData.IsStackable;
	[JsonIgnore] public virtual int StackSize => ItemData.StackSize;
	
	public T GetArbitraryData<T>( string key )
	{
		return TryGetArbitraryData<T>( key, out var value ) ? value : default;
	}

	public bool TryGetArbitraryData<T>( string key, out T value )
	{
		if ( ArbitraryData.TryGetValue( key, out var obj ) )
		{
			if ( obj is not JsonElement jsonElement )
			{
				Log.Error( $"Arbitrary data {key} is not a JsonElement" );
				value = default;
				return false;
			}

			value = JsonSerializer.Deserialize<T>( jsonElement.GetRawText() );

			return true;
		}

		value = default;
		return false;
	}

	[Description( "Set arbitrary data on this item." )]
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

	public static PersistentItem Create( GameObject gameObject )
	{
		if ( !gameObject.IsValid() ) throw new Exception( "Item is null" );

		var persistentItem = new PersistentItem();
		
		if ( gameObject.Components.TryGet<WorldItem>( out var worldItem ) )
		{
			persistentItem.ItemId = worldItem.ItemData.ResourceName;
		}
		
		if ( gameObject.Components.TryGet<BaseCarriable>( out var carriable ) )
		{
			persistentItem.ItemId ??= carriable.ItemData.ResourceName;
		}

		if ( gameObject.Components.TryGet<Persistent>( out var persistent ) )
		{
			persistent.OnItemSave( persistentItem );
		}
		
		if ( gameObject.Components.TryGet<IPersistent>( out var persistent2 ) )
		{
			persistent2.OnSave( persistentItem );
		}
		
		if ( gameObject.Components.TryGet<WorldObject>( out var worldObject ) )
		{
			persistentItem.ObjectId = worldObject.ObjectData?.ResourceName;
		}

		var nodeLink = WorldManager.Instance.GetWorldNodeLink( gameObject );
		if ( nodeLink != null )
		{
			nodeLink.OnNodeSave();
			persistentItem = nodeLink.GetPersistence();
		}

		return persistentItem;
	}
	
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
