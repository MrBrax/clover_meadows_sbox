using System;
using System.Text.Json;
using Editor.NodeEditor;
namespace Clover.Dialogue
{
	public class DialogueGraph : IGraph
	{
		public DialogueData DialogueData { get; }

		private readonly List<DialogueNodeData> _nodes = new();
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			WriteIndented = false
		};

		public IEnumerable<DialogueNodeData> DialogueNodes => _nodes;
		public IEnumerable<INode> Nodes => _nodes;

		public DialogueGraph( DialogueData data )
		{
			DialogueData = data;
			AutoLayoutIfNeeded( data );
			foreach ( var node in data.Nodes )
				_nodes.Add( new DialogueNodeData( node, this ) );
		}

		private static void AutoLayoutIfNeeded( DialogueData data )
		{
			if ( data.Nodes.Count == 0 ) return;
			if ( data.Nodes.Any( n => n.EditorPosition != Vector2.Zero ) ) return;

			var x = 0f;
			foreach ( var node in data.Nodes )
			{
				node.EditorPosition = new Vector2( x, 0 );
				x += 300f;
			}
		}

		public void AddNode( INode node )
		{
			if ( node is not DialogueNodeData nodeData ) return;
			_nodes.Add( nodeData );
			DialogueData.Nodes.Add( nodeData.DialogueNode );
		}

		public void RemoveNode( INode node )
		{
			if ( node is not DialogueNodeData nodeData ) return;
			var id = nodeData.DialogueNode.Id;

			foreach ( var other in _nodes )
			{
				if ( other.DialogueNode.NextNodeId == id )
					other.DialogueNode.NextNodeId = null;

				foreach ( var choice in other.DialogueNode.Choices )
				{
					if ( choice.TargetNodeId == id )
						choice.TargetNodeId = null;
				}
			}

			_nodes.Remove( nodeData );
			DialogueData.Nodes.Remove( nodeData.DialogueNode );
		}

		public string SerializeNodes( IEnumerable<INode> nodes )
		{
			var list = nodes.OfType<DialogueNodeData>()
				.Select( n => n.DialogueNode )
				.ToList();
			return JsonSerializer.Serialize( list, JsonOptions );
		}

		public IEnumerable<INode> DeserializeNodes( string serialized )
		{
			var list = JsonSerializer.Deserialize<List<BaseDialogueNode>>( serialized );
			if ( list == null ) return Enumerable.Empty<INode>();

			foreach ( var node in list )
			{
				node.Id = Guid.NewGuid().ToString();
				node.NextNodeId = null;
				foreach ( var choice in node.Choices )
					choice.TargetNodeId = null;
			}

			return list.Select( n => new DialogueNodeData( n, this ) );
		}

		public DialogueNodeData FindByDialogueNode( BaseDialogueNode node ) =>
			_nodes.FirstOrDefault( n => n.DialogueNode == node );
	}
}
