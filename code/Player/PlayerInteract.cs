using Clover.Interactable;

namespace Clover.Player;

public class PlayerInteract : Component
{
	private IInteract _currentInteractable;

	[Property] public BoxCollider InteractCollider { get; set; }

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
