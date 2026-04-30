namespace Clover.Dialogue;

public interface IDialogueEvent : ISceneEvent<IDialogueEvent>
{
	void OnDialogueStart( DialogueData data, BaseDialogueNode node ) {}
	void OnDialogueEnd( DialogueData data, BaseDialogueNode node, Variant returnValue ) {}

}
