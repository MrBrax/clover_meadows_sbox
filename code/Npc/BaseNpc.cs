using Clover.Components;

namespace Clover.Npc;

[Title( "Base Npc" )]
[Icon( "face" )]
[Category( "Clover/Npc" )]
public class BaseNpc : Component
{
	[RequireComponent] public NavMeshAgent Agent { get; set; }

	[Property] public string Name { get; set; }

	public void LoadDialogue( Dialogue dialogue )
	{
		var window = DialogueManager.Instance.DialogueWindow;

		if ( window == null )
		{
			Log.Error( "BaseNpc: No dialogue window found" );
			return;
		}

		window.Enabled = true;

		window.LoadDialogue( dialogue );
	}
}
