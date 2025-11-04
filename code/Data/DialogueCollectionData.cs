using System;
using Clover.Player;

namespace Clover.Data;

[AssetType( Name = "Dialogue Collection", Extension = "dcol" )]
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
