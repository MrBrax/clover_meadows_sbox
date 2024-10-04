using Clover.Interactable;

namespace Clover.Player;

public class PlayerInteract : Component
{
	
	[RequireComponent] public PlayerCharacter Player { get; set; }
	
	private IInteract _currentInteractable;

	[Property] public BoxCollider InteractCollider { get; set; }
	
	[Property] public GameObject Cursor { get; set; }

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
		}
		else if ( Input.Released( "use" ) )
		{
			if ( _currentInteractable != null )
			{
				_currentInteractable.FinishInteract( GetComponent<PlayerCharacter>() );
				_currentInteractable = null;
			}
		}
		
		if ( Cursor.IsValid() )
		{
			var gridPosition = Player.GetAimingGridPosition();
			var worldPosition = WorldManager.Instance.ActiveWorld.ItemGridToWorld( gridPosition );
			Cursor.WorldPosition = worldPosition;
		}
	}

	private IInteract FindInteractable()
	{
		Log.Info( InteractCollider.Touching.Count() );

		foreach ( var collider in InteractCollider.Touching )
		{
			if ( collider.GameObject.Components.TryGet<IInteract>( out var interactable ) )
			{
				return interactable;
			}
		}

		return null;
	}
}
