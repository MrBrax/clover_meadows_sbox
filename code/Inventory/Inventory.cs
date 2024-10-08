using Braxnet;
using Clover.Items;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Inventory;

public class Inventory : Component
{
	[RequireComponent] public PlayerCharacter Player { get; private set; }

	[Property, ReadOnly] public int ItemCount => Container.Slots.Count;

	public InventoryContainer Container { get; private set; } = new();

	protected override void OnAwake()
	{
		base.OnAwake();
		Container.Owner = GameObject;
	}

	public bool PickUpItem( PersistentItem item )
	{
		try
		{
			Container.AddItem( item, true );
		}
		catch ( InventoryFullException e )
		{
			Log.Warning( e.Message );
			return false;
		}

		return true;
	}

	public void PickUpItem( GameObject gameObject )
	{
		PickUpItem( WorldManager.Instance.ActiveWorld.GetItem( gameObject ) );
	}

	public async void PickUpItem( WorldNodeLink nodeLink )
	{
		if ( string.IsNullOrWhiteSpace( nodeLink.ItemId ) ) throw new System.Exception( "Item data is null" );

		if ( nodeLink.IsBeingPickedUp )
		{
			Log.Warning( $"Item {nodeLink} is already being picked up" );
			return;
		}

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

		Player.InCutscene = true;
		Player.CutsceneTarget = Vector3.Zero;
		Player.CharacterController.Velocity = Vector3.Zero;

		nodeLink.IsBeingPickedUp = true;

		var tween = TweenManager.CreateTween();
		tween.AddPosition( nodeLink.Node, Player.WorldPosition + Vector3.Up * 16f, 0.2f );
		var scale = tween.AddScale( nodeLink.Node, Vector3.Zero, 0.3f );
		scale.Parallel = true;

		await tween.Wait();

		PickUpItem( inventoryItem );

		Sound.Play( "sounds/interact/item_pickup.sound", WorldPosition );
		Log.Info( WorldPosition );

		nodeLink.Remove();

		Player.InCutscene = false;

		Player.Save();
	}

	public void PickUpItem( WorldObject gameObject )
	{
		if ( gameObject.IsBeingPickedUp ) return;

		gameObject.IsBeingPickedUp = true;

		var item = new PersistentItem();

		if ( item == null )
		{
			throw new System.Exception( "Failed to create inventory item" );
		}

		item.ObjectId = gameObject.ObjectData.ResourceName;

		PickUpItem( item );

		gameObject.DestroyGameObject();
	}
}
