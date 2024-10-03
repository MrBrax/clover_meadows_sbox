using System;

namespace Clover.Data;

[GameResource("Item", "item", "item")]
public class ItemData : GameResource
{
	
	[Property] public string Name { get; set; }
	
	[Property, TextArea] public string Description { get; set; }
	
	[Property] public int Width { get; set; }
	[Property] public int Height { get; set; }

	[Property]
	public World.ItemPlacement Placements { get; set; } = World.ItemPlacement.Floor & World.ItemPlacement.Underground;
	
	[Property] public bool CanDrop { get; set; } = true;
	[Property] public bool DisablePickup { get; set; } = false;
	[Property] public bool IsStackable { get; set; } = false;
	[Property] public int StackSize { get; set; } = 1;
	
	[Property] public GameObject ModelScene { get; set; }
	[Property] public GameObject CarryScene { get; set; }
	[Property] public GameObject DropScene { get; set; }
	[Property] public GameObject PlaceScene { get; set; }
	
	[Property, ImageAssetPath] public string Icon { get; set; }
	
	public virtual GameObject DefaultTypeScene => PlaceScene;
	
	public static T GetById<T>( string id ) where T : ItemData
	{
		return ResourceLibrary.GetAll<T>().FirstOrDefault( i => i.ResourceName == id );
	}
	
	public virtual Texture GetIcon()
	{
		return Icon != null ? Texture.Load( FileSystem.Mounted, Icon ) : null;
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
			return new List<Vector2Int> { originOffset }; // if the item is 1x1, return the origin since it's the only position
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

}
