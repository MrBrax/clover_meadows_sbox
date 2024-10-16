using Clover.Player;

namespace Clover.Interactable;

/// <summary>
///  Plays a simple animation on a SkinnedModelRenderer when interacted with.
/// </summary>
[Category( "Clover/Interactable" )]
[Icon( "directions_run" )]
public class Animation : Component, IInteract
{
	
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	
	[Property] public string AnimationName { get; set; }

	public void StartInteract( PlayerCharacter player )
	{
		Renderer.Set( AnimationName, true );
	}

	public void FinishInteract( PlayerCharacter player )
	{
		
	}
	
}
