using Clover.Inventory;
using Clover.Items;
using Clover.Persistence;

namespace Clover.Carriable;

public class Shovel : BaseCarriable
{
	[Property] public SoundEvent HitSound { get; set; }
	[Property] public SoundEvent DigSound { get; set; }
	[Property] public SoundEvent FillSound { get; set; }


	public override void OnUseDown()
	{
		if ( !CanUse() ) return;

		NextUse = 1f;

		var pos = Player.GetAimingGridPosition();

		var worldPos = Player.World.ItemGridToWorld( pos );

		if ( !CanDigAt( worldPos ) )
		{
			Log.Warning( "Can't dig here." );
			return;
		}

		var worldItems = Player.World.GetItems( pos ).ToList();

		if ( worldItems.Count == 0 )
		{
			DigHole( pos );
			return;
		}
		else
		{
			
			var undergroundItem = worldItems.FirstOrDefault( x => x.GridPlacement == World.ItemPlacement.Underground );
			if ( undergroundItem != null )
			{
				DigUpItem( pos, undergroundItem );
				return;
			}
			
			var floorItem = worldItems.FirstOrDefault( x => x.GridPlacement == World.ItemPlacement.Floor );
			if ( floorItem != null )
			{
				if ( floorItem.Node.Components.TryGet<Hole>( out var hole ) )
				{
					FillHole( pos );
				}
				else if ( floorItem.Node.Components.TryGet<IDiggable>( out var diggable ) )
				{
					DigUpFloorItem( pos, floorItem, diggable.GiveItemWhenDug() );
				}
				else
				{
					HitItem( pos, floorItem );
				}

				return;
			}
			
		}

		Log.Warning( "No action taken." );
	}

	/*public override void OnUseUp()
	{
		base.OnUseUp();
	}*/

	private bool CanDigAt( Vector3 worldPos )
	{
		/*var state = GetWorld3D().DirectSpaceState;
		var query = new Trace( state ).CastRay( PhysicsRayQueryParameters3D.Create( worldPos + Vector3.Up, worldPos + Vector3.Down, World.TerrainLayer ) );

		if ( query == null )
		{
			Log.Warning( $"No query found for {worldPos}" );
			return false;
		}

		var worldMesh = query.Collider.GetAncestorOfType<WorldMesh>();

		if ( worldMesh != null )
		{
			var surface = worldMesh.Surface;
			if ( surface == null )
			{
				Log.Warning( $"No SurfaceData found for {worldMesh}" );
				return false;
			}

			return surface.IsDiggable;
		}

		Log.Warning( $"No WorldMesh found for {query.Collider}" );

		return false;*/

		return true;
	}

	private void HitItem( Vector2Int pos, WorldNodeLink floorItem )
	{
		if ( floorItem.ItemData.Name == "Tree stump" )
		{
			floorItem.Remove();
			// GetNode<AudioStreamPlayer3D>( "HitSound" ).Play();
			return;
		}

		Log.Info( $"Hit {floorItem.ItemData.Name} at {pos}" );
		// GetNode<AudioStreamPlayer3D>( "HitSound" ).Play();
	}

	private void DigHole( Vector2Int pos )
	{
		Log.Info( $"Dug hole at {pos}" );

		Player.SnapToGrid();
		Player.ModelLookAt( Player.World.ItemGridToWorld( pos ) );

		/*var holeData = Loader.LoadResource<ItemData>( "res://items/misc/hole/hole.tres" );
		/*var hole = Inventory.World.SpawnPlacedItem<Hole>( holeData, pos, World.ItemPlacement.Floor,
			World.RandomItemRotation() );#1#
		var hole = World.SpawnNode( holeData, pos, World.RandomItemRotation(), World.ItemPlacement.Floor,
			false );*/

		var holeData = Data.ItemData.Get( "hole" );
		var hole = Player.World.SpawnPlacedNode( holeData, pos, World.ItemRotation.North, World.ItemPlacement.Floor );


		// GetNode<AudioStreamPlayer3D>( "DigSound" ).Play();
		GameObject.PlaySound( DigSound );

		Durability--;
		// Inventory.Player.Save();

		// Inventory.World.Save();
	}

	private void FillHole( Vector2Int pos )
	{
		Log.Info( $"Filled hole at {pos}" );

		var hole = Player.World.GetItem( pos, World.ItemPlacement.Floor );
		if ( hole == null )
		{
			Log.Info( "No hole found." );
			return;
		}

		if ( hole.Node.Components.TryGet<Hole>( out var holeItem ) )
		{
			Player.World.RemoveItem( holeItem.GameObject );
			// Inventory.World.Save();

			Player.SnapToGrid();

			Durability--;
			// Inventory.Player.Save();

			// GetNode<AudioStreamPlayer3D>( "FillSound" ).Play();
			GameObject.PlaySound( FillSound );
		}
		else
		{
			Log.Warning( "Not a hole." );
		}

		// TODO: check if hole has item in it
	}

	private void DigUpItem( Vector2Int pos, WorldNodeLink item )
	{
		Log.Info( $"Dug up {item.ItemData.Name} at {pos}" );

		// var inventoryItem = PersistentItem.Create( item );
		item.RefreshPersistence();
		var inventoryItem = item.GetPersistence();

		try
		{
			Player.Inventory.PickUpItem( inventoryItem );
		}
		catch ( InventoryFullException e )
		{
			Log.Warning( e.Message );
			return;
		}
		catch ( System.Exception e )
		{
			Log.Error( e.Message );
			return;
		}

		Player.World.RemoveItem( item );

		var dirt = Player.World.GetItem( pos, World.ItemPlacement.Floor );
		if ( dirt != null && dirt.ItemData?.ResourceName == "buried_item" )
		{
			Player.World.RemoveItem( dirt );
		}

		DigHole( pos );
	}

	/// <summary>
	///  Normally you can only dig up items that are placed underground, but some items like flowers and tree stumps are placed on the floor.
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="item"></param>
	private void DigUpFloorItem( Vector2Int pos, WorldNodeLink item, bool giveItem )
	{
		Log.Info( $"Dug up {item.ItemData.Name} at {pos}" );

		if ( giveItem )
		{
			// var inventoryItem = PersistentItem.Create( item );

			item.RefreshPersistence();
			var inventoryItem = item.GetPersistence();

			try
			{
				Player.Inventory.PickUpItem( inventoryItem );
			}
			catch ( InventoryFullException e )
			{
				Log.Warning( e.Message );
				return;
			}
			catch ( System.Exception e )
			{
				Log.Error( e.Message );
				return;
			}
		}

		Player.World.RemoveItem( item );

		DigHole( pos );
	}

	public void BuryItem( InventorySlot<PersistentItem> slot, WorldNodeLink hole )
	{
		
		var gridPos = hole.GridPosition;
		
		hole.RefreshPersistence();
		// var item = hole.Persistence;
		
		hole.Remove();
		
		// main item
		Player.World.SpawnDroppedNode( slot.PersistentItem, gridPos, World.ItemRotation.North, World.ItemPlacement.Underground );
		
		// dirt
		Player.World.SpawnPlacedNode( Data.ItemData.Get( "buried_item" ), gridPos, World.ItemRotation.North, World.ItemPlacement.Floor );


	}
}
