﻿using System;
using Clover.Carriable;
using Clover.Components;
using Clover.Inventory;
using Clover.Items;
using Clover.Persistence;
using Sandbox.Utility;

namespace Clover.Data;

[GameResource( "Item", "item", "item", Icon = "weekend" )]
[Category( "Clover/Items" )]
[Icon( "weekend" )]
// [JsonPolymorphic]
// [JsonDerivedType( typeof(ItemData), "item" )]
// [JsonDerivedType( typeof(ToolData), "tool" )]
public class ItemData : GameResource
{
	/// <summary>
	///  Unique identifier for this item.
	/// </summary>
	[Property]
	public string Id { get; set; }

	[Button( "Generate ID" )]
	public void GenerateId()
	{
		Id = $"{ResourceName}:{Crc64.FromString( ResourcePath )}";
	}

	[Property] public string Name { get; set; }

	[Property, TextArea] public string Description { get; set; }

	[Property, Group( "World" )] public int Width { get; set; } = 1;
	[Property, Group( "World" )] public int Height { get; set; } = 1;

	// [Property, Group( "World" )] // default to floor and underground
	// public World.ItemPlacement Placements { get; set; } = World.ItemPlacement.Floor | World.ItemPlacement.Underground;

	[Property, Group( "World" )] public bool CanBeBuried { get; set; } = true;

	[Property, Group( "Inventory" )] public bool CanDrop { get; set; } = true;
	[Property, Group( "Inventory" )] public bool CanPlace { get; set; } = true;
	[Property, Group( "Inventory" )] public bool DisablePickup { get; set; } = false;

	[Property, Group( "Inventory" )] public bool IsStackable { get; set; } = false;

	[Property, ShowIf( nameof(IsStackable), true )]
	public int StackSize { get; set; } = 1;

	[Property, Group( "Scenes" )] public GameObject ModelScene { get; set; }
	[Property, Group( "Scenes" )] public GameObject DropScene { get; set; }
	[Property, Group( "Scenes" )] public GameObject PlaceScene { get; set; }

	[Property, ReadOnly, Group( "Scenes" )]
	public virtual GameObject DefaultTypeScene => PlaceScene;

	[Property, ImageAssetPath] public string Icon { get; set; }

	[Property, Group( "Object" )] // TODO: move this to yet another class?
	[Description( "Custom world object to spawn. Don't use this unless you know what you're doing." )]
	public ObjectData ObjectData { get; set; }

	public bool HideInSpawnMenu { get; set; }

	public Vector3 PlaceModeOffset { get; set; }


	[Hide]
	public delegate int GetPriceDelegate( DateTime dateTime );

	[Property, Group( "Shop" )] public int BaseBuyPrice { get; set; } = 0;
	[Property, Group( "Shop" )] public bool CanSell { get; set; } = true;
	[Property, Group( "Shop" )] public int BaseSellPrice { get; set; } = 0;
	[Property, Group( "Shop" )] public bool CanBuy { get; set; } = true;
	[Property, Group( "Shop" )] public GetPriceDelegate GetCustomSellPrice { get; set; }
	[Property, Group( "Shop" )] public GetPriceDelegate GetCustomBuyPrice { get; set; }


	public static T GetById<T>( string id ) where T : ItemData
	{
		return ResourceLibrary.GetAll<T>()
			       .FirstOrDefault( i => i.ResourceName == id || i.ResourcePath == id || i.Id == id ) ??
		       throw new Exception( $"Item data not found: {id}" );
	}

	public virtual string GetIcon()
	{
		return Icon ?? "ui/icons/default_item.png";
	}

	public virtual Texture GetIconTexture()
	{
		return Texture.Load( FileSystem.Mounted, GetIcon() );
	}

	/// <summary>
	///   Returns a list of grid positions for this item.
	/// </summary>
	/// <param name="itemRotation">Amount of rotation to apply to the item.</param>
	/// <param name="originOffset">Offset to apply to the item.</param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public List<Vector2Int> GetGridPositions( World.ItemRotation itemRotation, Vector2Int originOffset = default )
	{
		var positions = new List<Vector2Int>();
		if ( Width == 0 || Height == 0 ) throw new Exception( "Item has no size" );

		if ( Width == 1 && Height == 1 )
		{
			return
				new List<Vector2Int>
				{
					originOffset
				}; // if the item is 1x1, return the origin since it's the only position
		}

		if ( itemRotation == World.ItemRotation.North )
		{
			for ( var x = 0; x < Width; x++ )
			{
				for ( var y = 0; y < Height; y++ )
				{
					positions.Add( new Vector2Int( originOffset.x + x, originOffset.y + y ) );
				}
			}
		}
		else if ( itemRotation == World.ItemRotation.South )
		{
			for ( var x = 0; x < Width; x++ )
			{
				for ( var y = 0; y < Height; y++ )
				{
					positions.Add( new Vector2Int( originOffset.x + x, originOffset.y - y ) );
				}
			}
		}
		else if ( itemRotation == World.ItemRotation.East )
		{
			for ( var x = 0; x < Height; x++ )
			{
				for ( var y = 0; y < Width; y++ )
				{
					positions.Add( new Vector2Int( originOffset.x + x, originOffset.y + y ) );
				}
			}
		}
		else if ( itemRotation == World.ItemRotation.West )
		{
			for ( var x = 0; x < Height; x++ )
			{
				for ( var y = 0; y < Width; y++ )
				{
					positions.Add( new Vector2Int( originOffset.x - x, originOffset.y + y ) );
				}
			}
		}

		return positions;
	}

	public Vector2Int GetBounds( World.ItemRotation itemRotation )
	{
		if ( itemRotation == World.ItemRotation.North || itemRotation == World.ItemRotation.South )
		{
			return new Vector2Int( Width, Height );
		}
		else
		{
			return new Vector2Int( Height, Width );
		}
	}

	public int GetMaxBounds()
	{
		return Math.Max( Width, Height );
	}

	public bool IsSameAs( ItemData item )
	{
		return item != null && (item.ResourcePath == ResourcePath || item.GetIdentifier() == GetIdentifier());
	}

	public GameObject SpawnPlaced()
	{
		return Spawn( PlaceScene );
	}

	public GameObject SpawnDropped()
	{
		return Spawn( DropScene );
	}

	private GameObject Spawn( GameObject scene )
	{
		if ( !scene.IsValid() ) return null;
		return scene.Clone();
	}

	// TODO: why both ItemAction and ContextMenuItem?
	public struct ItemAction
	{
		public string Name;
		public string Icon;
		public string Image;
		public Action Action;
	}

	public virtual IEnumerable<ItemAction> GetActions( InventorySlot<PersistentItem> slot )
	{
		// yield break;

		if ( ObjectData.IsValid() )
		{
			yield return new ItemAction
			{
				Name = "Spawn",
				Icon = "add",
				Action = () =>
				{
					slot.SpawnObject();
				}
			};
		}

		var player = slot.InventoryContainer.Player;
		if ( player != null )
		{
			if ( slot.GetItem().ItemData.CanBeBuried )
			{
				var shovel = player.Equips.GetEquippedItem<Shovel>( Equips.EquipSlot.Tool );

				if ( shovel != null )
				{
					var hole = player.World.GetNodeLink<Hole>( player.GetAimingGridPosition() );

					if ( hole != null )
					{
						yield return new ItemAction
						{
							Name = "Bury",
							Icon = "file_upload",
							Action = () =>
							{
								shovel.BuryItem( slot, hole );
								shovel.GameObject.PlaySound( shovel.FillSound );
								slot.TakeOneOrDelete();
							}
						};
					}
					else
					{
						// Log.Warning( "No hole found" );
					}
				}
				else
				{
					// Log.Warning( "No shovel equipped" );
				}
			}
			else
			{
				// Log.Warning( "Item does not support underground placement" );
			}
		}
		else
		{
			// Log.Warning( "Player is null" );
		}
	}

	public static ItemData Get( string id )
	{
		var itemData = ResourceLibrary.GetAll<ItemData>()
			.FirstOrDefault( x => x.Id == id || x.ResourceName == id || x.ResourcePath == id );
		if ( itemData == null )
		{
			Log.Error( $"Item data not found: {id}" );
		}

		return itemData;
	}

	public PersistentItem CreatePersistentItem()
	{
		return new PersistentItem { ItemId = GetIdentifier(), };
	}

	/*protected override void PostLoad()
	{
		base.PostLoad();
		if ( string.IsNullOrEmpty( Id ) )
		{
			// create id based on path
			// TODO: is this a good idea?
			Log.Warning( $"Item {ResourcePath} has no id, generating one" );
			// Id = $"{ResourceName}:{Crc64.FromString( ResourcePath )}";
		}
	}*/

	public string GetIdentifier()
	{
		return Id;
	}

	public virtual void OnPersistentItemInitialize( PersistentItem persistentItem )
	{
	}
}
