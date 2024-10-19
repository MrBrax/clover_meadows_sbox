using System;
using System.Text.Json.Serialization;
using Clover;
using Clover.Player;

namespace Sandbox;

[GameResource( "Dialogue", "dlg", "dialogue" )]
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
		[Property] public string Label { get; set; }

		// [Property] public string Id { get; set; }
		// [Property] public string IdTarget { get; set; }
		[Property] public DialogueAction OnSelect { get; set; }

		[Property, Description( "Will only show if OnSelect is null" )]

		public List<DialogueNode> Nodes { get; set; } = new();

		public override string ToString()
		{
			return Label + " -> " + Nodes.Count;
		}
	}

	public class DialogueNode
	{
		[Property] public string Id { get; set; } = Guid.NewGuid().ToString()[..8];

		[Property, Description( "I don't think the player ever talks, but this is here just in case." )]
		public bool IsPlayer { get; set; }


		[Property, HideIf( nameof(IsPlayer), true )]
		public int Speaker { get; set; }

		[Property, TextArea] public string Text { get; set; }

		// [Property] public List<string> Choices { get; set; } = new();
		[Property] public List<DialogueChoice> Choices { get; set; } = new();

		[Property, Description( "This is the action that will be executed when this node is entered." )]
		public DialogueAction OnEnter { get; set; }

		[Property] public DialogueAction OnExit { get; set; }

		[Property] public bool IsHidden { get; set; }

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
