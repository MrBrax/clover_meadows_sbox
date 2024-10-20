using System;
using Clover.Player;

namespace Clover.Data;

[GameResource( "Dialogue Collection", "dcol", "Dialogue Collection", Icon = "question_answer" )]
public class DialogueCollection : GameResource
{
	public struct DialogueCollectionEntry
	{
		public delegate bool ConditionDelegate( DialogueWindow window, PlayerCharacter player,
			List<GameObject> targets );

		public ConditionDelegate Condition { get; set; }

		// public Func<bool> Condition { get; set; }
		public Dialogue DialogueData { get; set; }
	}

	[Property] public string Name { get; set; } = "Dialogue Collection";

	[Property, InlineEditor] public List<DialogueCollectionEntry> Entries { get; set; } = new();
}
