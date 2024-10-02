/*public static class MyEditorMenu
{
	[Menu("Editor", "$title/My Menu Option")]
	public static void OpenMyMenu()
	{
		EditorUtility.DisplayDialog("It worked!", "This is being called from your library's editor code!");
	}
}*/

public class DialogueEditor : BaseResourceEditor<Dialogue>
{
	SerializedObject Object;

	public DialogueEditor()
	{
		Layout = Layout.Column();
	}

	protected override void Initialize( Asset asset, Dialogue resource )
	{
		Layout.Clear( true );

		// Object = resource.GetSerialized();

		var sheet = new ControlSheet();
		// sheet.AddProperty( resource, x => x.TestValue );

		var so = resource.GetSerialized();
		// sheet.AddRow( so.GetProperty( nameof( resource.TestValue ) ) );

		var list = new ListControlWidget( so.GetProperty( nameof( resource.Nodes ) ) );
		sheet.Add( list );
		
		/*foreach ( var node in resource.Nodes )
		{
			var nodeSheet = new ControlSheet();
			nodeSheet.AddProperty( node, x => x.Label );
			nodeSheet.AddProperty( node, x => x.Speaker );

			if ( node is Dialogue.TextNode textNode )
			{
				nodeSheet.AddProperty( textNode, x => x.Body );
			}
			else if ( node is Dialogue.ChoiceNode choiceNode )
			{
				nodeSheet.AddProperty( choiceNode, x => x.Text );

				var choicesSheet = new ControlSheet();
				foreach ( var choice in choiceNode.Choices )
				{
					var choiceSheet = new ControlSheet();
					choiceSheet.AddProperty( choice, x => x.Text );
					choiceSheet.AddProperty( choice, x => x.Target );
					choicesSheet.Add( choiceSheet );
				}

				nodeSheet.Add( choicesSheet );
			}
			else
			{
				// nodeSheet.Add( new Label( "Unknown node type" ) );
			}

			sheet.Add( nodeSheet );
		}*/

		/*var addButton = new Button( "Add Node" );
		addButton.Pressed += () =>
		{
			var node = new Dialogue.TextNode();
			node.Label = "New Text Node";
			resource.Nodes.Add( node );
			// Initialize( asset, resource );
			NoteChanged();
		};
		sheet.Add( addButton );*/

		Layout.Add( sheet );

		so.OnPropertyChanged += ( p ) =>
		{
			NoteChanged( p );
		};

		/*Object.OnPropertyChanged += ( p ) =>
		{
			NoteChanged( p );
		};*/

		/*var button = Layout.Add( new Button( "Add text node" ) );
		button.Pressed +=  () =>
		{
			var node = new Dialogue.TextNode();
			node.Label = "New Text Node";
			resource.Nodes.Add( node );
			Initialize( asset, resource );
		};*/
	}
}
