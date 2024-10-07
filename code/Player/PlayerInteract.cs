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
		base.OnAwake();

		Cursor.Parent = null;
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
		base.OnFixedUpdate();

		if ( Input.Pressed( "use" ) )
		{
			var interactable = FindInteractable();
			if ( interactable != null )
			{
				_currentInteractable = interactable;
				_currentInteractable.StartInteract( GetComponent<PlayerCharacter>() );
			}
			else
			{
				Sound.Play( UseFailSound, WorldPosition );
			}
		}
		else if ( Input.Released( "use" ) )
		{
			if ( _currentInteractable != null )
			{
				_currentInteractable.FinishInteract( GetComponent<PlayerCharacter>() );
				_currentInteractable = null;
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
		foreach ( var collider in InteractCollider.Touching )
		{
			if ( collider.GameObject.Components.TryGet<IPickupable>( out var pickupable ) )
			{
				if ( collider.GameObject.Components.TryGet<WorldItem>( out var worldItem ) )
				{
					if ( worldItem.NodeLink.GridPlacement != World.ItemPlacement.Floor &&
					     worldItem.NodeLink.GridPlacement != World.ItemPlacement.OnTop )
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
			if ( collider.GameObject.Components.TryGet<IInteract>( out var interactable ) )
			{
				if ( collider.GameObject.Components.TryGet<WorldItem>( out var worldItem ) )
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
		}

		return null;
	}
}
