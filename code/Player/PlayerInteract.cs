using Clover.Interactable;
using Clover.Inventory;
using Clover.Items;

namespace Clover.Player;

public class PlayerInteract : Component
{
	[RequireComponent] public PlayerCharacter Player { get; set; }

	private IInteract _currentInteractable;

	[Property] public BoxCollider InteractCollider { get; set; }

	[Property] public GameObject Cursor { get; set; }
	
	[Property] public SoundEvent UseFailSound { get; set; }
	[Property] public SoundEvent PickUpFailSound { get; set; }

	protected override void OnAwake()
	{
		if ( IsProxy ) return;
		if ( Cursor != null )
		{
			Cursor.Parent = null;
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if ( Cursor.IsValid() )
		{
			Cursor.Destroy();
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		if ( Input.Pressed( "use" ) )
		{
			var interactable = FindInteractable();
			if ( interactable != null )
			{
				_currentInteractable = interactable;
				_currentInteractable.StartInteract( GetComponent<PlayerCharacter>() );
				Input.Clear( "use" );
			}
			else
			{
				Log.Warning( "No interactable found" );
				Sound.Play( UseFailSound, WorldPosition );
			}
		}
		else if ( Input.Released( "use" ) )
		{
			if ( _currentInteractable != null )
			{
				_currentInteractable.FinishInteract( GetComponent<PlayerCharacter>() );
				_currentInteractable = null;
				Input.Clear( "use" );
			}
		}

		if ( Input.Pressed( "pickup" ) )
		{
			PickUp();
		}

		if ( Cursor.IsValid() )
		{
			var gridPosition = Player.GetAimingGridPosition();
			var worldPosition = WorldManager.Instance.ActiveWorld.ItemGridToWorld( gridPosition );
			Cursor.WorldPosition = worldPosition;
		}
	}

	private void PickUp()
	{
		var pickupableNode = GetPickupableNode();

		if ( pickupableNode != null )
		{
			if ( pickupableNode.CanPickup( Player ) )
			{
				pickupableNode.OnPickup( Player );
				return;
			}
		}

		Log.Warning( "No pickupable node found" );
		
		Sound.Play( PickUpFailSound, WorldPosition );
		
	}

	private IPickupable GetPickupableNode()
	{
		var touchingItems = InteractCollider.Touching;
		
		
		
		foreach ( var collider in touchingItems )
		{
			if ( collider.GameObject.Components.TryGet<IPickupable>( out var pickupable ) )
			{
				if ( collider.GameObject.Components.TryGet<WorldItem>( out var worldItem ) )
				{
					// only allow picking up items on the floor or on top of other items
					if ( worldItem.NodeLink.GridPlacement != World.ItemPlacement.Floor &&
					     worldItem.NodeLink.GridPlacement != World.ItemPlacement.OnTop )
					{
						continue;
					}
					
					// don't allow picking up items that have items on top of them
					if ( worldItem.HasItemOnTop() )
					{
						continue;
					}
				}

				return pickupable;
			}
		}

		return null;
	}

	private IInteract FindInteractable()
	{

		foreach ( var collider in InteractCollider.Touching )
		{
			var checkGameObject = collider.GameObject;
			
			// Log.Info( $"# Checking base collider {checkGameObject.Name}" );

			while ( checkGameObject != null )
			{
				// Log.Info( $" - Checking (parent?) {checkGameObject.Name}" );
				if ( checkGameObject.Components.TryGet<IInteract>( out var interactable ) )
				{
					if ( checkGameObject.Components.TryGet<WorldItem>( out var worldItem ) )
					{
						if ( worldItem.NodeLink.GridPlacement != World.ItemPlacement.Floor &&
						     worldItem.NodeLink.GridPlacement != World.ItemPlacement.OnTop &&
						     worldItem.NodeLink.GridPlacement != World.ItemPlacement.Wall
						   )
						{
							continue;
						}
					}

					return interactable;
				}
				
				checkGameObject = checkGameObject.Parent;

			}
		}
		
		// Log.Info( "# Reached root, no interactable found." );

		return null;
	}
}
