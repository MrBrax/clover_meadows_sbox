using Clover.Player;

namespace Clover.Interactable;

[Category( "Clover/Interactable" )]
public class InteractSound : Component, IInteract
{
	[Property] public SoundEvent Sound { get; set; }

	public void StartInteract( PlayerCharacter player )
	{
		SoundEx.Play( Sound, WorldPosition );
	}

	public void FinishInteract( PlayerCharacter player )
	{
	}

	public string GetInteractName()
	{
		return "Use";
	}
}
