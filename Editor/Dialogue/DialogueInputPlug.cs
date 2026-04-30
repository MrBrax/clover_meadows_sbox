using System;
using Editor.NodeEditor;
namespace Clover.Dialogue
{
	public class DialogueInputPlug : IPlugIn
	{
		private readonly DialogueNodeData _owner;
		private readonly DialogueGraph _graph;
		private readonly Dictionary<string, float> _handleOffsets = new();

		public INode Node => _owner;
		public string Identifier => "input";
		public Type Type => typeof( DialogueLink );
		public DisplayInfo DisplayInfo => new()
		{
			Name = "In"
		};

		public bool ShowLabel => false;
		public bool AllowStretch => false;
		public bool ShowConnection => true;
		public bool InTitleBar => false;
		public bool IsReachable => true;
		public string ErrorMessage => null;

		public ValueEditor CreateEditor( NodeUI node, Plug plug ) => null;
		public Menu CreateContextMenu( NodeUI node, Plug plug ) => null;
		public void OnDoubleClick( NodeUI node, Plug plug, MouseEvent e ) {}

		public IPlugOut ConnectedOutput
		{
			get
			{
				var myId = _owner.DialogueNode.Id;
				foreach ( var other in _graph.DialogueNodes )
				{
					if ( other == _owner ) continue;

					foreach ( var output in other.Outputs.OfType<DialogueOutputPlug>() )
					{
						if ( output.TargetNodeId == myId )
							return output;
					}
				}
				return null;
			}
			set
			{
				if ( value == null )
				{
					var current = ConnectedOutput;
					if ( current is DialogueOutputPlug output )
						output.ClearTarget();
				}
				else if ( value is DialogueOutputPlug outputPlug )
				{
					outputPlug.SetTarget( _owner.DialogueNode.Id );
				}
			}
		}

		public float? GetHandleOffset( string name ) =>
			_handleOffsets.TryGetValue( name, out var v ) ? v : null;

		public void SetHandleOffset( string name, float? value )
		{
			if ( value.HasValue ) _handleOffsets[name] = value.Value;
			else _handleOffsets.Remove( name );
		}

		public DialogueInputPlug( DialogueNodeData owner, DialogueGraph graph )
		{
			_owner = owner;
			_graph = graph;
		}
	}
}
