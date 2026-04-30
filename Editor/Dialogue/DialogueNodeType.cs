using System;
using System.Diagnostics.CodeAnalysis;
using Editor.NodeEditor;
namespace Clover.Dialogue
{
	public class DialogueNodeType : INodeType
	{
		public bool IsCommon => true;

		public Menu.PathElement[] Path => new Menu.PathElement[]
		{
			new()
			{
				Name = "Dialogue Node"
			}
		};

		public bool TryGetInput( Type valueType, [NotNullWhen( true )] out string name )
		{
			if ( valueType == typeof( DialogueLink ) || valueType == typeof( object ) )
			{
				name = "input";
				return true;
			}
			name = null;
			return false;
		}

		public bool TryGetOutput( Type valueType, [NotNullWhen( true )] out string name )
		{
			if ( valueType == typeof( DialogueLink ) || valueType == typeof( object ) )
			{
				name = "next";
				return true;
			}
			name = null;
			return false;
		}

		public INode CreateNode( IGraph graph )
		{
			if ( graph is not DialogueGraph dialogueGraph ) return null;
			/*var node = EditorTypeLibrary.Create<BaseDialogueNode>( "BaseDialogueNode" );
			node.EmbeddedResource = new()
			{
				ResourceCompiler = "embed"
			};*/
			var node = new BaseDialogueNode();
			node.Id = Guid.NewGuid().ToString();
			node.Text = "...";
			node.Speaker = "NPC";
			var nodeData = new DialogueNodeData( node, dialogueGraph );
			return nodeData;
		}
	}
}
