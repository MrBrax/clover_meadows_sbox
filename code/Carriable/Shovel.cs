using Clover.Data;
using Clover.Inventory;
using Clover.Items;
using Clover.Persistence;

namespace Clover.Carriable;

[Category( "Clover/Carriable" )]
public class Shovel : BaseCarriable
{
	[Property] public SoundEvent HitSound { get; set; }
	[Property] public SoundEvent DigSound { get; set; }
	[Property] public SoundEvent FillSound { get; set; }


	public override void OnUseDown()
	{
		if ( !CanUse() ) return;

		NextUse = 1f;

		/*if ( !Networking.IsHost )
		{
			Log.Error( "Only the host can use world altering items for now." );
			return;
		}*/
	}

	[Broadcast]
	public override void OnUseDownHost()
	{
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
			/*var undergroundItem = worldItems.FirstOrDefault( x => x.GridPlacement == World.ItemPlacement.Underground );
			if ( undergroundItem != null )
			{
				DigUpItem( pos, undergroundItem );
				return;
			}*/

			foreach ( var floorItem in worldItems )
			{
				if ( floorItem.Node.Components.TryGet<Hole>( out var hole ) )
				{
					FillHole( pos );
					break;
				}
				else if ( floorItem.Node.Components.TryGet<IDiggable>( out var diggable ) )
				{
					DigUpFloorItem( pos, floorItem, diggable );
					break;
				}
				else
				{
					HitItem( pos, floorItem );
					break;
				}
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
		var hitbox = Player.PlayerInteract.InteractCollider;

		if ( hitbox.Touching.FirstOrDefault( x => x.GameObject != Holder && x.GameObject.Tags.Has( "player" ) ) !=
		     null )
		{
			Log.Warning( "Can't dig while touching player." );
			return false;
		}

		var trace = Scene.Trace.Ray( worldPos + Vector3.Up * 16f, worldPos + Vector3.Down * 32f )
			.WithTag( "terrain" )
			.Run();

		if ( !trace.Hit ) return false;

		var surface = trace.Surface;

		return surface.ResourceName == "grass" || surface.ResourceName == "dirt";
	}

	private void HitItem( Vector2Int pos, WorldNodeLink floorItem )
	{
		if ( floorItem.ItemData.Name == "Tree stump" )
		{
			floorItem.Remove();
			return;
		}

		Log.Info( $"Hit {floorItem.ItemData.Name} at {pos}" );
	}

	private void DigHole( Vector2Int pos )
	{
		Log.Info( $"Dug hole at {pos}" );

		Player.SnapToGrid();
		Player.ModelLookAt( Player.World.ItemGridToWorld( pos ) );

		var holeData = Data.ItemData.Get( "hole" );
		var hole = Player.World.SpawnPlacedNode( holeData, pos, World.ItemRotation.North );

		SoundEx.Play( DigSound, WorldPosition );

		Durability--;
	}

	private void FillHole( Vector2Int pos )
	{
		Log.Info( $"Filled hole at {pos}" );

		var hole = Player.World.GetNodeLink<Hole>( pos );
		if ( hole == null )
		{
			Log.Info( "No hole found." );
			return;
		}

		if ( hole.Node.Components.TryGet<Hole>( out var holeItem ) )
		{
			Player.World.RemoveItem( holeItem.GameObject );

			Player.SnapToGrid();
			Player.ModelLookAt( Player.World.ItemGridToWorld( pos ) );

			Durability--;

			SoundEx.Play( FillSound, WorldPosition );
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
		item.RunSavePersistence();
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

		var dirtQuery = Player.World.GetItems( pos ).ToList();
		/*if ( dirt != null && dirt.ItemData?.ResourceName == "buried_item" )
		{
			Player.World.RemoveItem( dirt );
		}*/

		if ( dirtQuery.Count > 0 )
		{
			foreach ( var dirt in dirtQuery.Where( dirt => dirt.ItemData.ResourceName == "buried_item" ) )
			{
				Player.World.RemoveItem( dirt );
			}
		}

		DigHole( pos );
	}

	/// <summary>
	///  Normally you can only dig up items that are placed underground, but some items like flowers and tree stumps are placed on the floor.
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="item"></param>
	private void DigUpFloorItem( Vector2Int pos, WorldNodeLink item, IDiggable diggable )
	{
		Log.Info( $"Dug up {item.ItemData.Name} at {pos}" );

		/*if ( giveItem )
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
		}*/

		if ( !diggable.CanDig() )
		{
			Log.Warning( "Can't dig up item." );
			return;
		}

		if ( !diggable.OnDig( Player, item ) )
		{
			Log.Warning( "Failed to dig up item." );
			return;
		}

		Player.World.RemoveItem( item );

		DigHole( pos );
	}

	public void BuryItem( InventorySlot<PersistentItem> slot, WorldNodeLink hole )
	{
		var gridPos = hole.GridPosition;

		hole.RunSavePersistence();
		// var item = hole.Persistence;

		hole.Remove();

		// main item
		// Player.World.SpawnDroppedNode( slot.PersistentItem, gridPos, World.ItemRotation.North, World.ItemPlacement.Underground );

		// dirt
		var dirt = Player.World.SpawnPlacedNode( Data.ItemData.Get( "buried_item" ), gridPos,
			World.ItemRotation.North );

		var node = dirt.Node;

		node.Components.Get<BuriedItem>().SetItem( slot.PersistentItem );
	}
}
