using Clover.Player;

namespace Clover.Interactable;

public interface IInteract
{
	
	void StartInteract( PlayerCharacter player );
	
	void FinishInteract( PlayerCharacter player );
	
}
