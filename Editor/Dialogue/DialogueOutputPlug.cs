using System;
using Editor.NodeEditor;
namespace Clover.Dialogue
{
	// ============================================================
//  Output plug — represents a "Next" connection or a choice
// ============================================================

	public class DialogueOutputPlug : IPlugOut
	{
		private readonly DialogueNodeData _owner;
		private readonly int _choiceIndex; // -1 = "Next", >= 0 = choice

		public INode Node => _owner;
		public string Identifier => _choiceIndex < 0 ? "next" : $"choice_{_choiceIndex}";
		public Type Type => typeof( DialogueLink );

		public DisplayInfo DisplayInfo => new()
		{
			Name = _choiceIndex < 0
				? "Next"
				: (_owner.DialogueNode.Choices.Count > _choiceIndex
					? (_owner.DialogueNode.Choices[_choiceIndex].Text ?? $"Choice {_choiceIndex + 1}")
					: $"Choice {_choiceIndex + 1}")
		};

		public bool ShowLabel => _choiceIndex >= 0;
		public bool AllowStretch => _choiceIndex >= 0;
		public bool ShowConnection => true;
		public bool InTitleBar => false;
		public bool IsReachable => true;
		public string ErrorMessage => null;

		public ValueEditor CreateEditor( NodeUI node, Plug plug ) => null;
		public Menu CreateContextMenu( NodeUI node, Plug plug ) => null;
		public void OnDoubleClick( NodeUI node, Plug plug, MouseEvent e ) {}

		public string TargetNodeId => _choiceIndex < 0
			? _owner.DialogueNode.NextNodeId
			: (_owner.DialogueNode.Choices.Count > _choiceIndex
				? _owner.DialogueNode.Choices[_choiceIndex].TargetNodeId
				: null);

		public void SetTarget( string nodeId )
		{
			if ( _choiceIndex < 0 )
			{
				_owner.DialogueNode.NextNodeId = nodeId;
			}
			else if ( _owner.DialogueNode.Choices.Count > _choiceIndex )
			{
				_owner.DialogueNode.Choices[_choiceIndex].TargetNodeId = nodeId;
			}
			_owner.FireChanged();
		}

		public void ClearTarget()
		{
			if ( _choiceIndex < 0 )
			{
				_owner.DialogueNode.NextNodeId = null;
			}
			else if ( _owner.DialogueNode.Choices.Count > _choiceIndex )
			{
				_owner.DialogueNode.Choices[_choiceIndex].TargetNodeId = null;
			}
			_owner.FireChanged();
		}

		public DialogueOutputPlug( DialogueNodeData owner, int choiceIndex = -1 )
		{
			_owner = owner;
			_choiceIndex = choiceIndex;
		}
	}
}
