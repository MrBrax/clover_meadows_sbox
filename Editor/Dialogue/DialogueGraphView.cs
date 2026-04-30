using System;
using Editor.NodeEditor;
using Connection=Editor.NodeEditor.Connection;
namespace Clover.Dialogue
{
	public class DialogueGraphView : GraphView
	{
		private static readonly DialogueNodeType DefaultNodeType = new();

		public Action OnSaveRequested { get; set; }

		private string _cookieKey;

		protected override string ViewCookie => _cookieKey;

		public DialogueGraphView( Widget parent ) : base( parent ) {}

		public void LoadDialogue( DialogueData data, string assetPath )
		{
			_cookieKey = $"dialogue_editor.{assetPath.Replace( '/', '_' ).Replace( '.', '_' )}";
			Graph = new DialogueGraph( data );
		}

		protected override IEnumerable<INodeType> GetRelevantNodes( NodeQuery query )
		{
			yield return DefaultNodeType;
		}

		protected override HandleConfig OnGetHandleConfig( Type type )
		{
			if ( type == typeof( DialogueLink ) )
				return new HandleConfig( "Dialogue", Color.Parse( "#38bdf8" )!.Value, HandleShape.Circle );

			return base.OnGetHandleConfig( type );
		}

		protected override Connection CreateConnection( PlugOut nodeOutput, PlugIn dropTarget, bool uiOnly = false )
		{
			var conn = base.CreateConnection( nodeOutput, dropTarget, uiOnly );
			if ( !uiOnly ) OnSaveRequested?.Invoke();
			return conn;
		}

		public override void RemoveConnection( Connection c )
		{
			base.RemoveConnection( c );
			OnSaveRequested?.Invoke();
		}

		public override void RemoveNode( NodeUI node )
		{
			base.RemoveNode( node );
			OnSaveRequested?.Invoke();
		}

		public override NodeUI CreateNewNode( INodeType type, Vector2 position )
		{
			var nodeUI = base.CreateNewNode( type, position );
			OnSaveRequested?.Invoke();
			return nodeUI;
		}
	}
}
