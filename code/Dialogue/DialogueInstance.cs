namespace Clover.Dialogue;

public class DialogueInstance
{
	public SteamId PlayerId { get; set; }
	public DialogueData DialogueData { get; set; }
	public string CurrentNodeId { get; set; }

	// public DialogueNode CurrentNode =>
	// 	DialogueData.GetNodeById( CurrentNodeId ) ?? throw new Exception( $"Node with ID {CurrentNodeId} not found." );
}
