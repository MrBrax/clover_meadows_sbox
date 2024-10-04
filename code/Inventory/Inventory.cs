﻿using Clover.Persistence;
using Clover.Player;

namespace Clover.Inventory;

public class Inventory : Component
{
	
	[RequireComponent] public PlayerCharacter Player { get; private set; }
	
	public InventoryContainer Container { get; private set; } = new InventoryContainer();
	
	public void PickUpItem( PersistentItem item )
	{
		Container.AddItem( item, true );
	}
	
	public void PickUpItem( GameObject gameObject )
	{
		PickUpItem( WorldManager.Instance.ActiveWorld.GetItem( gameObject ) );
	}
	
	public void PickUpItem( WorldNodeLink nodeLink )
	{
		if ( string.IsNullOrWhiteSpace( nodeLink.ItemId ) ) throw new System.Exception( "Item data is null" );

		/*if ( nodeLink.IsBeingPickedUp )
		{
			Logger.Warn( $"Item {nodeLink.ItemDataPath} is already being picked up" );
			return;
		}*/

		Log.Info( $"Picking up item {nodeLink.ItemId}" );

		// var inventoryItem = PersistentItem.Create( nodeLink );
		var inventoryItem = new PersistentItem();
		

		if ( inventoryItem == null )
		{
			throw new System.Exception( "Failed to create inventory item" );
		}

		inventoryItem.ItemId = nodeLink.ItemId;

		/* var index = Container.GetFirstFreeEmptyIndex();
		if ( index == -1 )
		{
			throw new InventoryFullException( "Inventory is full." );
		}

		var slot = new InventorySlot<PersistentItem>( Container )
		{
			Index = index
		};

		slot.SetItem( inventoryItem ); */

		Log.Info( $"Picked up item {nodeLink.ItemId}" );

		// NodeExtensions.SetCollisionState( nodeLink.Node, false );

		/*var player = Owner as PlayerController;

		player.InCutscene = true;
		player.CutsceneTarget = Vector3.Zero;
		player.Velocity = Vector3.Zero;

		nodeLink.IsBeingPickedUp = true;

		// TODO: needs dupe protection
		var tween = player.GetTree().CreateTween();
		var t = tween.Parallel().TweenProperty( nodeLink.Node, "global_position", player.GlobalPosition + Vector3.Up * 0.5f, 0.2f );
		tween.Parallel().TweenProperty( nodeLink.Node, "scale", Vector3.One * 0.1f, 0.3f ).SetTrans( Tween.TransitionType.Cubic ).SetEase( Tween.EaseType.Out );
		tween.TweenCallback( Callable.From( () =>
		{
			player.World.RemoveItem( nodeLink );
			player.InCutscene = false;

			// PlayPickupSound();
			PickUpItem( inventoryItem );

		} ) );*/
		
		PickUpItem( inventoryItem );
		
		nodeLink.Remove();

	}
	
}