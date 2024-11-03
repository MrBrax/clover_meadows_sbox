using Clover.Data;
using Clover.Interactable;
using Clover.Inventory;
using Clover.Items;
using Clover.Npc;
using Clover.Ui;

namespace Clover.Player;

[Title( "Player Interact" )]
[Icon( "inventory" )]
[Category( "Clover/Player" )]
public class PlayerInteract : Component
{
	[RequireComponent] public PlayerCharacter Player { get; set; }

	private IInteract _currentInteractable;

	[Property] public BoxCollider InteractCollider { get; set; }

	[Property] public GameObject Cursor { get; set; }

	[Property] public SoundEvent UseFailSound { get; set; }
	[Property] public SoundEvent PickUpFailSound { get; set; }

	public GameObject InteractionTarget { get; set; }

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

	public bool CanInteract()
	{
		if ( Player.ItemPlacer.IsPlacing || Player.ItemPlacer.IsMoving ) return false;
		if ( Player.InCutscene ) return false;
		if ( Player.VehicleRider.Vehicle.IsValid() ) return false;
		if ( InteractionTarget.IsValid() )
		{
			if ( InteractionTarget.GetComponent<BaseNpc>().IsValid() ) return false;
		}

		return true;
	}

	public bool HasInteractable()
	{
		return FindInteractable() != null;
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		if ( !CanInteract() )
		{
			return;
		}

		var interactable = FindInteractable();
		var moveable = FindMoveable();
		var pickupableNode = GetPickupableNode();

		if ( Input.Pressed( "use" ) )
		{
			if ( interactable != null )
			{
				_currentInteractable = interactable;
				_currentInteractable.StartInteract( Player );

				if ( !Networking.IsHost )
				{
					using ( Rpc.FilterInclude( Connection.Host ) )
					{
						_currentInteractable.StartInteractHost( Player );
					}
				}

				Input.Clear( "use" );
			}
			else
			{
				Log.Warning( "No interactable found" );
				// Notifications.Instance.AddNotification( Notifications.NotificationType.Warning, "No interactable found" );
				Sound.Play( UseFailSound, WorldPosition );
			}

			return;
		}
		else if ( Input.Released( "use" ) )
		{
			if ( _currentInteractable != null )
			{
				_currentInteractable.FinishInteract( Player );

				if ( !Networking.IsHost )
				{
					using ( Rpc.FilterInclude( Connection.Host ) )
					{
						_currentInteractable.FinishInteractHost( Player );
					}
				}

				_currentInteractable = null;
				Input.Clear( "use" );
			}

			return;
		}

		if ( Input.Pressed( "pickup" ) )
		{
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
			return;
		}

		/*if ( Input.Pressed( "move" ) )
		{
			if ( !Player.World.Data.DisableItemPlacement )
			{
				if ( moveable.IsValid() )
				{
					Log.Info( "Moving..." );

					Mouse.Visible = true;

					Player.ItemPlacer.StartMovingPlacedItem( moveable.GetComponent<WorldItem>() );

					Input.Clear( "move" );
				}
				else
				{
					Mouse.Visible = false;
					Log.Warning( "No interactable found" );
					// Notifications.Instance.AddNotification( Notifications.NotificationType.Warning, "No interactable found" );
					Sound.Play( UseFailSound, WorldPosition );
				}
			}

			return;
		}*/


		GameObject target = null;
		if ( interactable is Component interactableComponent )
		{
			target = interactableComponent.GameObject;
		}
		else if ( moveable.IsValid() )
		{
			target = moveable;
		}
		else if ( pickupableNode is Component pickupableNodeComponent && pickupableNode.CanPickup( Player ) )
		{
			target = pickupableNodeComponent.GameObject;
		}

		if ( target != null )
		{
			if ( target.Components.TryGet<WorldItem>( out var worldItem ) )
			{
				worldItem.ItemHighlight.Enabled = true;
			}
		}


		if ( Cursor.IsValid() && Cursor.Enabled )
		{
			var gridPosition = Player.GetAimingGridPosition();
			var worldPosition = WorldManager.Instance.ActiveWorld.ItemGridToWorld( gridPosition );
			Cursor.WorldPosition = worldPosition;
		}
	}

	public IPickupable GetPickupableNode()
	{
		var touchingItems = InteractCollider.Touching;

		foreach ( var collider in touchingItems )
		{
			if ( collider.GameObject.Components.TryGet<IPickupable>( out var pickupable ) )
			{
				return pickupable;
			}
		}

		return null;
	}

	/*public WorldItem GetWorldItemFromInteract()
	{
		foreach ( var collider in InteractCollider.Touching )
		{
			var checkGameObject = collider.GameObject;

			while ( checkGameObject != null )
			{
				if ( checkGameObject.Components.TryGet<IInteract>( out var interactable ) )
				{
					if ( checkGameObject.Components.TryGet<WorldItem>( out var worldItem ) )
					{
						return worldItem;
					}
				}

				checkGameObject = checkGameObject.Parent;
			}
		}

		return null;
	}*/

	public IInteract FindInteractable()
	{
		foreach ( var collider in InteractCollider.Touching )
		{
			/*var checkGameObject = collider.GameObject;

			while ( checkGameObject != null )
			{

				if ( checkGameObject.Components.TryGet<IInteract>( out var interactable ) )
				{


					return interactable;
				}

				checkGameObject = checkGameObject.Parent;
			}*/

			/*if ( collider.GameObject.Components.TryGet<IInteract>( out var interactable,
				    FindMode.EverythingInSelfAndAncestors ) )
			{
				return interactable;
			}*/

			var components = collider.GameObject.Components.GetAll<IInteract>( FindMode.EverythingInSelfAndAncestors );
			foreach ( var component in components )
			{
				if ( component.CanInteract( Player ) )
				{
					return component;
				}
			}
		}

		// Log.Info( "# Reached root, no interactable found." );

		return null;
	}

	public GameObject FindMoveable()
	{
		foreach ( var collider in InteractCollider.Touching )
		{
			var worldItem = collider.GameObject.Components.Get<WorldItem>( FindMode.EverythingInSelfAndAncestors );
			if ( worldItem != null && worldItem.CanPickup( Player ) )
			{
				return collider.GameObject;
			}
		}

		return null;
	}
}
