using Editor;
using System.Collections.Generic;
using System.Linq;

namespace Sandbox.States.Editor;

public class StateMachineEditorWindow : DockWindow
{
	internal static List<StateMachineEditorWindow> AllWindows { get; } = new List<StateMachineEditorWindow>();
	private List<StateMachineView> Views { get; } = new();

	public StateMachineView? FocusedView => Views.LastOrDefault();

	public StateMachineEditorWindow()
	{
		DeleteOnClose = true;

		Size = new Vector2( 1280, 720 );

		// Make this window stay on top of the editor, by making it a dialog
		Parent = EditorWindow;
		WindowFlags = WindowFlags.Dialog | WindowFlags.Customized | WindowFlags.CloseButton | WindowFlags.WindowSystemMenuHint | WindowFlags.WindowTitle | WindowFlags.MaximizeButton;

		WindowTitle = "State Machine Editor";

		SetWindowIcon( "smart_toy" );

		AllWindows.Add( this );
	}

	public StateMachineView Open( StateMachineComponent stateMachine )
	{
		var view = new StateMachineView( stateMachine, this );

		var sibling = Views.LastOrDefault();

		Views.Add( view );

		if ( sibling is null )
		{
			DockManager.AddDock( null, view, DockArea.RightOuter, split: 1f );
		}
		else
		{
			DockManager.AddDock( sibling, view, DockArea.Inside );
		}

		DockManager.Update();

		return view;
	}

	protected override void OnClosed()
	{
		base.OnClosed();

		AllWindows.Remove( this );
	}

	protected override void OnFocus( FocusChangeReason reason )
	{
		base.OnFocus( reason );

		// Move this window to the end of the list, so it has priority
		// when opening a new graph

		AllWindows.Remove( this );
		AllWindows.Add( this );
	}

	internal void OnFocusView( StateMachineView view )
	{
		Views.Remove( view );
		Views.Add( view );
	}

	internal void OnRemoveView( StateMachineView view )
	{
		Views.Remove( view );
	}

	[Shortcut( "editor.quit", "CTRL+Q", ShortcutType.Window )]
	void Quit()
	{
		Close();
	}

	[Shortcut( "editor.save", "CTRL+S", ShortcutType.Window )]
	public void Save()
	{
		var active = Views.FirstOrDefault( x => x is { IsValid: true, Visible: true } );

		active?.StateMachine.Scene.Editor.Save( false );
	}

	[Shortcut( "editor.cut", "CTRL+X", ShortcutType.Window )]
	private void CutSelection()
	{
		FocusedView?.CutSelection();
	}

	[Shortcut( "editor.copy", "CTRL+C", ShortcutType.Window )]
	private void CopySelection()
	{
		FocusedView?.CopySelection();
	}

	[Shortcut( "editor.paste", "CTRL+V", ShortcutType.Window )]
	private void PasteSelection()
	{
		FocusedView?.PasteSelection();
	}

	[Shortcut( "editor.select-all", "CTRL+A", ShortcutType.Window )]
	private void SelectAll()
	{
		FocusedView?.SelectAll();
	}

	[Shortcut( "editor.delete", "DEL" )]
	private void Delete()
	{
		FocusedView?.DeleteSelection();
	}

	[Shortcut( "editor.flip", "TAB" )]
	private void Flip()
	{
		FocusedView?.FlipSelection();
	}

	[Shortcut( "editor.duplicate", "CTRL+D" )]
	private void Duplicate()
	{
		FocusedView?.DuplicateSelection();
	}

	[Shortcut( "editor.undo", "CTRL+Z", ShortcutType.Window )]
	private void Undo()
	{
		FocusedView?.Undo();
	}

	[Shortcut( "editor.redo", "CTRL+Y", ShortcutType.Window )]
	private void Redo()
	{
		FocusedView?.Redo();
	}
}

