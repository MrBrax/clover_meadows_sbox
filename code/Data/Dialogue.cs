using System;
using Clover.Player;

namespace Clover.Data;

[GameResource( "Dialogue", "dlg", "dialogue", Icon = "chat" )]
public class Dialogue : GameResource
{
	/*public class DialogueAction
	{
		[Property, ActionGraphInclude] public GameObject Player { get; set; }
		[Property] public List<GameObject> Targets { get; set; }
		[Property] public DialogueNode Node { get; set; }
		[Property] public DialogueChoice Choice { get; set; }
	}*/

	public delegate void DialogueAction(
		DialogueWindow window,
		PlayerCharacter player,
		List<GameObject> targets,
		DialogueNode node,
		DialogueChoice choice
	);

	public delegate bool DialogueCondition(
		DialogueWindow window,
		PlayerCharacter player,
		List<GameObject> targets,
		DialogueNode node,
		DialogueChoice choice
	);

	public class DialogueChoice
	{
		[Property] public string Label { get; set; } = null;

		// [Property] public string Id { get; set; }
		// [Property] public string IdTarget { get; set; }
		[Property] public DialogueAction OnSelect { get; set; } = null;
		[Property] public string JumpToId { get; set; } = null;

		[Property, Description( "Will only show if OnSelect is null" )]

		public List<DialogueNode> Nodes { get; set; } = new();

		public override string ToString()
		{
			if ( Nodes.Count == 0 && !string.IsNullOrEmpty( JumpToId ) )
			{
				return $"{Label} -> JUMP:{JumpToId}";
			}
			else if ( Nodes.Count == 0 && OnSelect != null )
			{
				return $"{Label} -> ACTION";
			}

			return $"{Label} -> {Nodes.Count} nodes";
		}
	}

	public class DialogueNode
	{
		[Property] public string Id { get; set; } = Guid.NewGuid().ToString()[..8];

		[Property, Description( "I don't think the player ever talks, but this is here just in case." )]
		public bool IsPlayer { get; set; } = false;


		[Property, HideIf( nameof(IsPlayer), true )]
		public int Speaker { get; set; } = 0;

		[Property, TextArea] public string Text { get; set; } = null;

		// [Property] public List<string> Choices { get; set; } = new();
		[Property] public List<DialogueChoice> Choices { get; set; } = new();

		[Property, Description( "This is the action that will be executed when this node is entered." )]
		public DialogueAction OnEnter { get; set; } = null;

		[Property] public DialogueAction OnExit { get; set; } = null;

		/// <summary>
		///   If true, this node will not be advanced to automatically, and must be jumped to manually.
		/// </summary>
		[Property]
		public bool IsHidden { get; set; } = false;

		public override string ToString()
		{
			if ( !string.IsNullOrEmpty( Id ) )
			{
				return $"#{Id} - {Speaker}: {Text}";
			}

			return $"{Speaker}: {Text}";
		}
	}

	[Property, InlineEditor] public List<DialogueNode> Nodes { get; set; } = new();
}
