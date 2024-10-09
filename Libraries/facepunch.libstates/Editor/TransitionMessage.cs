using Editor.NodeEditor;

namespace Sandbox.States.Editor;

public record TransitionMessage( TransitionItem Item ) : ILabelSource
{
	protected StateMachineView View => Item.Source.View;
	public Transition Transition => Item.Transition!;

	public string Title => "Message Trigger";

	public string? Description => Transition.Message is not null
		? "This transition is taken after the state machine receives this message."
		: null;

	public string? Icon => Transition.Message is not null ? "email" : null;
	public string? Text => Transition.Message is { } message ? $"\"{message}\"" : null;

	public bool IsValid => Transition.Message is not null;

	public void BuildAddContextMenu( global::Editor.Menu menu )
	{
		if ( Transition.Message is not null ) return;

		menu.AddMenu( "Add Message Trigger", "email" ).AddLineEdit( "Value", value: "run", autoFocus: true, onSubmit:
			message =>
			{
				if ( string.IsNullOrEmpty( message ) )
				{
					return;
				}

				View.LogEdit( "Transition Message Added" );

				Transition.Message = message;
				Item.ForceUpdate();
			} );
	}

	public void BuildModifyContextMenu( global::Editor.Menu menu )
	{
		var currentMessage = Transition.Message!;

		menu.AddHeading( "Message Trigger" );
		menu.AddLineEdit( "Value", value: currentMessage, autoFocus: true, onSubmit:
			message =>
			{
				if ( string.IsNullOrEmpty( message ) )
				{
					return;
				}

				View.LogEdit( "Transition Message Changed" );

				Transition.Message = message;
				Item.ForceUpdate();
			} );
		menu.AddOption( "Clear", "clear", action: () =>
		{
			View.LogEdit( "Transition Message Removed" );

			Transition.Message = null;
			Item.ForceUpdate();
		} );
	}

	public void Delete()
	{
		Transition.Message = null;
	}

	public void DoubleClick()
	{
		var menu = new global::Editor.Menu { DeleteOnClose = true };

		BuildModifyContextMenu( menu );

		menu.OpenAtCursor( true );
	}
}
