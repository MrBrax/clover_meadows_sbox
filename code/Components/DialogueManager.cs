namespace Clover.Components;

public class DialogueManager : Component
{
	public static DialogueManager Instance => Game.ActiveScene.GetAllComponents<DialogueManager>().FirstOrDefault();
	[Property] public DialogueWindow DialogueWindow { get; set; }
}
