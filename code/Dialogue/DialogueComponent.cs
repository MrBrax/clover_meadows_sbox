using System.Threading.Tasks;
using Clover.Components;
using Clover.Npc;

namespace Clover.Dialogue;

public class DialogueComponent : SingletonComponent<DialogueComponent>
{
	// private List<DialogueInstance> _dialogueInstances = new();

	// [Property] public DialogueData Dialogue { get; set; }

	public DialogueData CurrentDialogue { get; set; }

	// public BaseDialogueNode RootNode { get; set; }
	public BaseDialogueNode CurrentNode { get; set; }

	public BaseNpc CurrentNpc { get; set; }

	public bool IsInDialogue => CurrentDialogue != null && CurrentNode != null;

	private TaskCompletionSource<Variant> _dialogueFinishedTcs;
	private Variant _dialogueReturnValue;

	[Property, ReadOnly]
	private Dictionary<string, Variant> _dialogueVariables = new();

	public void SetDialogueVariable( string key, Variant value )
	{
		_dialogueVariables[key] = value;
	}

	public void ClearDialogueVariable( string key )
	{
		_dialogueVariables.Remove( key );
	}

	public Variant GetDialogueVariable( string key )
	{
		return _dialogueVariables.GetValueOrDefault( key );
	}

	public void ClearDialogueVariables()
	{
		_dialogueVariables.Clear();
	}

	public Dictionary<string, Variant> GetAllDialogueVariables()
	{
		return new Dictionary<string, Variant>( _dialogueVariables );
	}

	public void LoadDialogue( DialogueData dialogue )
	{
		/*RootNode = dialogue.Nodes.FirstOrDefault();
		if ( RootNode == null )
		{
			Log.Error( "Dialogue has no root node." );
			return;
		}

		Log.Info( $"Loaded dialogue {dialogue.ResourcePath} with root node ID {RootNode.Id}." );

		SetCurrentNode( RootNode.Id );*/

		ClearDialogueVariables();

		if ( _dialogueFinishedTcs != null )
		{
			Log.Warning( "A dialogue is already in progress, overriding with new dialogue." );
			_dialogueFinishedTcs.SetResult( default );
		}

		CurrentDialogue = dialogue;
		if ( CurrentDialogue == null )
		{
			Log.Error( "Dialogue is null." );
			return;
		}

		if ( CurrentDialogue.Nodes.Count == 0 )
		{
			Log.Error( "Dialogue has no nodes." );
			return;
		}

		_dialogueFinishedTcs = new TaskCompletionSource<Variant>();

		SetCurrentNode( CurrentDialogue.Nodes[0].Id );

		Sound.Play( "sound/newcity/dialogue/dialogue.open.sound" );

		IDialogueEvent.Post( x => x.OnDialogueStart( dialogue, CurrentNode ) );
	}

	public void SetCurrentNode( string nodeId )
	{
		/*if ( RootNode == null ) return;
		var node = FindNodeById( RootNode, nodeId );
		if ( node != null )
		{
			Log.Info( $"Setting current node to ID {nodeId}." );
			CurrentNode = node;
		}
		else
		{
			Log.Error( $"Node with ID {nodeId} not found in dialogue." );
		}*/

		if ( CurrentDialogue == null ) return;
		var node = FindNodeById( nodeId );
		if ( node != null )
		{
			if ( CurrentNode != null )
			{
				CurrentNode.OnNodeExited();
			}

			Log.Info( $"Setting current node to ID {nodeId}." );
			CurrentNode = node;
			CurrentNode.OnNodeEntered();

			// MainHud.GetPanel<DialoguePanel>()?.OnNodeChanged();
			// var dialoguePanel = MainHud.GetOrCreatePanel<DialoguePanel>();
			// if ( dialoguePanel.IsValid() )
			// {
			// 	dialoguePanel.OnNodeChanged();
			// }
		}
		else
		{
			Log.Error( $"Node with ID {nodeId} not found in dialogue." );
		}
	}

	public BaseDialogueNode FindNodeById( string nodeId )
	{
		if ( CurrentDialogue == null ) return null;
		return CurrentDialogue.Nodes.FirstOrDefault( n => n.Id == nodeId );
	}

	public void SelectChoice( DialogueChoice choice )
	{
		if ( choice == null ) return;
		if ( !string.IsNullOrEmpty( choice.TargetNodeId ) )
		{
			Log.Info( $"Selecting choice '{choice.Text}', going to node ID {choice.TargetNodeId}." );
			SetCurrentNode( choice.TargetNodeId );
		}
		else if ( choice.OnSelected != null )
		{
			Log.Info( $"Selecting choice '{choice.Text}', executing action." );
			choice.OnSelected();
		}
		else if ( choice.ExitsDialogue )
		{
			Log.Info( $"Selecting choice '{choice.Text}', exiting dialogue with return value {choice.ReturnValue}." );
			ExitDialogue( choice.ReturnValue );
		}
		else
		{
			Log.Error( $"No action or target node defined for choice: {choice.Text}" );
		}
	}

	public void AdvanceDialogue()
	{
		if ( CurrentNode == null ) return;

		if ( CurrentNode.Choices.Count > 0 )
		{
			Log.Error(
				"Cannot advance dialogue: current node has choices that must be selected before advancing." );
			return;
		}

		if ( !string.IsNullOrEmpty( CurrentNode.NextNodeId ) )
		{
			Log.Info( $"Advance dialogue, moving to NextNodeId {CurrentNode.NextNodeId}." );
			SetCurrentNode( CurrentNode.NextNodeId );
			Sound.Play( "sound/newcity/dialogue/dialogue.advance.sound" );
			return;
		}

		if ( CurrentDialogue != null )
		{
			var currentRootIndex = CurrentDialogue.Nodes.FindIndex( n => n.Id == CurrentNode.Id );
			if ( currentRootIndex >= 0 && currentRootIndex < CurrentDialogue.Nodes.Count - 1 )
			{
				Log.Info(
					$"Advance dialogue, moving to next root node ID {CurrentDialogue.Nodes[currentRootIndex + 1].Id}." );
				SetCurrentNode( CurrentDialogue.Nodes[currentRootIndex + 1].Id );
				Sound.Play( "sound/newcity/dialogue/dialogue.advance.sound" );
				return;
			}
		}

		Log.Info( "End of dialogue reached, no more nodes to advance to." );
		ExitDialogue();
	}

	public void ExitDialogue( Variant returnValueOverride = default )
	{
		if ( CurrentNode != null )
		{
			CurrentNode.OnNodeExited();

			var returnValue = CurrentNode.ReturnValue;
			if ( returnValue != default && returnValueOverride == default )
			{
				Log.Info( $"Returning value from dialogue: {returnValue}" );
				_dialogueFinishedTcs?.SetResult( returnValue );
			}
			else if ( returnValueOverride != default )
			{
				returnValue = returnValueOverride;
				Log.Info( $"Returning overridden value from dialogue: {returnValue}" );
				_dialogueFinishedTcs?.SetResult( returnValue );
			}
			else
			{
				Log.Info( "No return value from dialogue." );
				_dialogueFinishedTcs?.SetResult( default );
			}

		}

		IDialogueEvent.Post( x => x.OnDialogueEnd( CurrentDialogue, CurrentNode, returnValueOverride ) );

		CurrentDialogue = null;
		CurrentNode = null;
		NpcName = null;

		if ( CurrentNpc.IsValid() )
		{
			// CurrentNpc.ResetMouth();
			// CurrentNpc.ClearLookAt();
			// CurrentNpc.RpcNotifyDialogueEnded();
			CurrentNpc?.EndInteraction(); // TODO: good place for this?
			CurrentNpc?.OnDialogueEnd();
			CurrentNpc = null;
		}

		if ( CameraNode.IsValid() )
		{
			CameraNode.Priority = 0;
			CameraNode = null;
		}

		_dialogueFinishedTcs = null;

		Log.Info( "Dialogue exited." );

		Sound.Play( "sound/newcity/dialogue/dialogue.close.sound" );
	}

	public bool IsFinalDialogueNode( BaseDialogueNode node )
	{
		if ( node == null ) return false;
		if ( node.ExitsDialogue ) return true;
		if ( !string.IsNullOrEmpty( node.NextNodeId ) ) return false;
		if ( node.Choices.Count > 0 ) return false;

		if ( CurrentDialogue != null )
		{
			var currentRootIndex = CurrentDialogue.Nodes.FindIndex( n => n.Id == node.Id );
			if ( currentRootIndex >= 0 && currentRootIndex < CurrentDialogue.Nodes.Count - 1 )
				return false;
		}

		return true;
	}

	public string NpcName { get; set; }
	public void SetNpcName( string npcName )
	{
		NpcName = npcName;
	}

	public void SetNpc( BaseNpc npc )
	{
		CurrentNpc = npc;
		if ( npc != null && string.IsNullOrEmpty( NpcName ) )
		{
			NpcName = npc.Name;
		}
	}

	public CameraNode CameraNode { get; set; }

	public void SetCameraNode( CameraNode cameraNode )
	{
		if ( CameraNode.IsValid() )
		{
			Log.Info( $"Clearing previous dialogue camera node {CameraNode}." );
			CameraNode.Priority = 0;
		}

		if ( cameraNode == null || !cameraNode.IsValid() )
		{
			CameraNode = null;
			Log.Info( "Clearing dialogue camera node." );
			return;
		}

		Log.Info( $"Setting dialogue camera node to {cameraNode}." );

		CameraNode = cameraNode;
		CameraNode.Priority = 15;
	}

	public async Task<Variant> WaitForFinishedDialogueAsync()
	{
		if ( _dialogueFinishedTcs == null )
		{
			Log.Warning( "WaitForFinishedDialogueAsync called but no dialogue is currently active." );
			return default;
		}

		return await _dialogueFinishedTcs.Task;
	}

}
