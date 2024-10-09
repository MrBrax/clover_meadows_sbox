using Editor;

namespace Sandbox.States.Editor;

public sealed class StateLabel : GraphicsItem, IContextMenuSource, IDeletable, IDoubleClickable
{
	public StateItem State { get; }
	public ILabelSource Source { get; }

	public string? Icon => Source.Icon;
	public string? Text => Source.Text;

	public StateLabel( StateItem parent, ILabelSource source )
		: base( parent )
	{
		State = parent;
		Source = source;

		HoverEvents = true;
		Selectable = true;

		ZIndex = 1;
		Size = 0f;

		Cursor = CursorShape.Finger;
	}

	private void SetFont()
	{
		Paint.SetFont( "roboto", 20f );
	}

	public void Layout()
	{
		PrepareGeometryChange();

		if ( !Source.IsValid )
		{
			Size = 0f;
		}
		else
		{
			Size = 32f;
			Tooltip = Source.Description;
		}
	}

	protected override void OnPaint()
	{
		if ( !Source.IsValid ) return;

		SetFont();

		var hovered = Hovered;
		var selected = Selected || State.Selected;

		var color = TransitionItem.GetPenColor( hovered, selected );

		if ( !selected && Source.Color is { } overrideColor )
		{
			color = hovered ? overrideColor.Desaturate( 0.5f ).Lighten( 0.5f ) : overrideColor;
		}

		Paint.ClearPen();
		Paint.SetBrush( Color.Black.WithAlpha( State.IsEmoji ? 0.75f : 0.5f ) );
		Paint.DrawRect( LocalRect.Shrink( 2f ), 3f );

		Paint.SetPen( color );
		Paint.DrawIcon( LocalRect, Icon, 24f );
	}

	public void OnContextMenu( ContextMenuEvent e )
	{
		e.Accepted = true;

		Selected = true;

		var menu = new global::Editor.Menu { DeleteOnClose = true };

		Source.BuildModifyContextMenu( menu );

		menu.OpenAtCursor( true );
	}

	public void Delete()
	{
		Source.Delete();
		State.ForceUpdate();
	}

	public void DoubleClick()
	{
		Source.DoubleClick();
	}
}
