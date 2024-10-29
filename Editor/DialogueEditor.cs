using Clover.Data;
using Editor.ActionGraphs;

namespace Clover;

[CustomEditor( typeof(Dialogue.DialogueNode) )]
public class DialogueNodeEditor  : ControlWidget
{
	public DialogueNodeEditor(SerializedProperty property) : base(property)
	{
		
		Layout = Layout.Column();
		Layout.Spacing = 10;

		PaintBackground = false;

		// Serialize the property as a MyClass object
		var serializedObject = property.GetValue<Dialogue.DialogueNode>().GetSerialized();
		if ( serializedObject is null )
		{
			Log.Error( "Failed to get serialized object" );
			return;
		}

		serializedObject.TryGetProperty( nameof(Dialogue.DialogueNode.Id), out var id );
		serializedObject.TryGetProperty( nameof(Dialogue.DialogueNode.Text), out var text );
		serializedObject.TryGetProperty( nameof(Dialogue.DialogueNode.Choices), out var choices );
		serializedObject.TryGetProperty( nameof(Dialogue.DialogueNode.OnEnter), out var onEnter );
		serializedObject.TryGetProperty( nameof(Dialogue.DialogueNode.OnExit), out var onExit );
		serializedObject.TryGetProperty( nameof(Dialogue.DialogueNode.IsHidden), out var isHidden );

		var topControl = Layout.Row();
		topControl.Spacing = 10;
		
		var idControl = Layout.Row();
		idControl.Spacing = 10;
		idControl.Add( new Label( "ID" ) { HorizontalSizeMode = SizeMode.Default } );
		idControl.Add( new StringControlWidget( id ) { HorizontalSizeMode = SizeMode.Expand } );
		topControl.Add( idControl );
		
		var isHiddenControl = Layout.Row();
		isHiddenControl.Spacing = 10;
		isHiddenControl.Add( new BoolControlWidget( isHidden ) { HorizontalSizeMode = SizeMode.CanShrink } );
		isHiddenControl.Add( new Label( "Is Hidden" ) { HorizontalSizeMode = SizeMode.Default } );
		topControl.Add( isHiddenControl );
		
		Layout.Add( topControl );
		
		var actionsControl = Layout.Row();
		actionsControl.Spacing = 10;
		
		var onEnterControl = Layout.Column();
		onEnterControl.Spacing = 10;
		onEnterControl.Add( new Label( "OnEnter" ) { HorizontalSizeMode = SizeMode.Default } );
		onEnterControl.Add( new ActionControlWidget( onEnter ) { HorizontalSizeMode = SizeMode.Expand } );
		actionsControl.Add( onEnterControl );
		
		var onExitControl = Layout.Column();
		onExitControl.Spacing = 10;
		onExitControl.Add( new Label( "OnExit" ) { HorizontalSizeMode = SizeMode.Default } );
		onExitControl.Add( new ActionControlWidget( onExit ) { HorizontalSizeMode = SizeMode.Expand } );
		actionsControl.Add( onExitControl );
		
		Layout.Add( actionsControl );
		
		
		Layout.Add( new Label( "Text" ) { HorizontalSizeMode = SizeMode.Default } );
		Layout.Add( new TextAreaControlWidget( text ) { HorizontalSizeMode = SizeMode.Expand } );
		
		Layout.Add( new Label( "Choices" ) { HorizontalSizeMode = SizeMode.Default } );
		Layout.Add( new ListControlWidget( choices ) { HorizontalSizeMode = SizeMode.Expand } );
		
	}
}

[CustomEditor( typeof(Dialogue.DialogueChoice) )]
public class DialogueChoiceEditor : ControlWidget
{
	public DialogueChoiceEditor( SerializedProperty property ) : base( property )
	{

		Layout = Layout.Column();
		Layout.Spacing = 10;

		PaintBackground = false;

		// Serialize the property as a MyClass object
		var serializedObject = property.GetValue<Dialogue.DialogueChoice>().GetSerialized();
		if ( serializedObject is null )
		{
			Log.Error( "Failed to get serialized object" );
			return;
		}

		serializedObject.TryGetProperty( nameof(Dialogue.DialogueChoice.Label), out var label );
		serializedObject.TryGetProperty( nameof(Dialogue.DialogueChoice.OnSelect), out var onSelect );
		serializedObject.TryGetProperty( nameof(Dialogue.DialogueChoice.JumpToId), out var jumpToId );
		serializedObject.TryGetProperty( nameof(Dialogue.DialogueChoice.Nodes), out var nodes );

		var labelControl = Layout.Row();
		labelControl.Spacing = 10;
		labelControl.Add( new Label( "Label" ) { HorizontalSizeMode = SizeMode.Default } );
		labelControl.Add( new StringControlWidget( label ) { HorizontalSizeMode = SizeMode.Expand } );
		Layout.Add( labelControl );
		
		var onSelectControl = Layout.Row();
		onSelectControl.Spacing = 10;
		onSelectControl.Add( new Label( "OnSelect" ) { HorizontalSizeMode = SizeMode.Default } );
		onSelectControl.Add( new ActionControlWidget( onSelect ) { HorizontalSizeMode = SizeMode.Expand } );
		Layout.Add( onSelectControl );
		
		var jumpToIdControl = Layout.Row();
		jumpToIdControl.Spacing = 10;
		jumpToIdControl.Add( new Label( "JumpToId" ) { HorizontalSizeMode = SizeMode.Default } );
		jumpToIdControl.Add( new StringControlWidget( jumpToId ) { HorizontalSizeMode = SizeMode.Expand } );
		Layout.Add( jumpToIdControl );
		
		Layout.Add( new Label( "Nodes" ) { HorizontalSizeMode = SizeMode.Default } );
		Layout.Add( new ListControlWidget( nodes ) { HorizontalSizeMode = SizeMode.Expand } );
		

		
	}
}
