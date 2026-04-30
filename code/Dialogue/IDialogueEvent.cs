// Copyright Braxnet 2026 unless specified otherwise
// https://braxnet.online
namespace Clover.Dialogue;

public interface IDialogueEvent : ISceneEvent<IDialogueEvent>
{
	void OnDialogueStart( DialogueData data, BaseDialogueNode node ) {}
	void OnDialogueEnd( DialogueData data, BaseDialogueNode node, Variant returnValue ) {}

}
