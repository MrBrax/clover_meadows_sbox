using Braxnet;
using Clover.Items;
using Clover.Persistence;
using Clover.Player;
using Clover.Ui;

namespace Clover.Inventory;

public class Inventory : Component
{
	[RequireComponent] public PlayerCharacter Player { get; private set; }

	[Property, ReadOnly] public int ItemCount => Container.Slots.Count;

	[Icon( "inventory" )] public InventoryContainer Container { get; private set; } = new();

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
		PickUpItem( WorldManager.Instance.ActiveWorld.GetNodeLink( gameObject ) );
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
		var inventoryItem = PersistentItem.Create( nodeLink.Node );

		if ( inventoryItem == null )
		{
			throw new System.Exception( "Failed to create inventory item" );
		}

		if ( !Container.CanFit( inventoryItem ) )
		{
			Player.Notify( Notifications.NotificationType.Error, "Inventory is full" );
			return;
		}

		Log.Info( $"Picked up item {nodeLink.ItemId}" );

		Player.StartCutscene();
		Player.CharacterController.Velocity = Vector3.Zero;

		nodeLink.IsBeingPickedUp = true;

		// TODO: probably use tags instead
		foreach ( var collider in nodeLink.Node.Components.GetAll<Collider>( FindMode.EverythingInSelfAndDescendants ) )
		{
			collider.Enabled = false;
		}

		var tween = TweenManager.CreateTween();
		tween.AddPosition( nodeLink.Node, Player.WorldPosition + Vector3.Up * 16f, 0.2f );
		var scale = tween.AddScale( nodeLink.Node, Vector3.Zero, 0.3f );
		scale.Parallel = true;

		await tween.Wait();

		PickUpItem( inventoryItem );

		Sound.Play( "sounds/interact/item_pickup.sound", WorldPosition );
		Log.Info( WorldPosition );

		nodeLink.Remove();

		Player.EndCutscene();

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

		item.ItemId = gameObject.ObjectData.PickupData.GetIdentifier();

		PickUpItem( item );

		gameObject.DestroyGameObject();
	}
}
