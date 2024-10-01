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
		
		Object = resource.GetSerialized();
		
		var sheet = new ControlSheet();
		sheet.AddObject( Object );
		Layout.Add( sheet );
		
		
		var button = Layout.Add( new Button( "Add text node" ) );
		button.Pressed +=  () =>
		{
			var node = new Dialogue.TextNode();
			node.Label = "New Text Node";
			resource.Nodes.Add( node );
			Initialize( asset, resource );
		};
		
		
	}
}
