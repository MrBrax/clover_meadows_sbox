using System;
namespace Clover.Dialogue;

public class DialogueChoice
{
	public string Text { get; set; }
	public Action OnSelected { get; set; }

	/// <summary>
	///  The ID of the target node to go to when this choice is selected.
	///  The entire dialogue tree is searched for a node with this ID, so any node can be targeted.
	/// </summary>
	public string TargetNodeId { get; set; }

	public bool ExitsDialogue { get; set; } = false;

	[ShowIf( nameof( ExitsDialogue ), true )]
	public Variant ReturnValue { get; set; }

}
