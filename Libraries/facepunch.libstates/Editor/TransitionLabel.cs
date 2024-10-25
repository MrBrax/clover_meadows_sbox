using Editor;

namespace Sandbox.States.Editor;

public sealed class TransitionLabel : GraphicsItem, IContextMenuSource, IDeletable, IDoubleClickable
{
	public TransitionItem Transition { get; }
	public ILabelSource Source { get; }

	public float MaxWidth { get; set; } = 256f;

	private bool _canShowText;

	public string? Icon => Source.Icon;
	public string? Text => Source.Text;

	public TransitionLabel( TransitionItem parent, ILabelSource source )
		: base( parent )
	{
		Transition = parent;
		Source = source;

		HoverEvents = true;
		Selectable = true;

		ZIndex = -1;

		Cursor = CursorShape.Finger;
	}

	private void SetFont()
	{
		Paint.SetFont( "roboto", 10f );
	}

	public void Layout()
	{
		SetFont();

		var iconWidth = string.IsNullOrEmpty( Icon ) ? 0f : 24f;

		_canShowText = MaxWidth - iconWidth >= 8f;

		var textWidth = string.IsNullOrEmpty( Text ) || !_canShowText
			? 0f
			: Paint.MeasureText( new Rect( 0f, 0f, MaxWidth - iconWidth, 24f ), Text, TextFlag.LeftCenter | TextFlag.SingleLine ).Width;

		PrepareGeometryChange();

		Size = new Vector2( iconWidth + textWidth, 24f );
	}

	protected override void OnHoverEnter( GraphicsHoverEvent e )
	{
		Tooltip = Source.Description;
	}

	protected override void OnSelectionChanged()
	{
		base.OnSelectionChanged();

		if ( Selected )
		{
			Transition.Selected = true;
		}
	}

	protected override void OnPaint()
	{
		if ( !Source.IsValid ) return;

		SetFont();

		var hovered = Hovered;
		var selected = Selected;

		var color = TransitionItem.GetPenColor( hovered, selected );

		if ( !selected && Source.Color is { } overrideColor )
		{
			color = hovered ? overrideColor.Desaturate( 0.5f ).Lighten( 0.5f ) : overrideColor;
		}

		Paint.ClearBrush();
		Paint.SetPen( color );

		var iconWidth = string.IsNullOrEmpty( Icon ) ? 0f : 22f;

		if ( Width >= 24f && !string.IsNullOrEmpty( Icon ) )
		{
			Paint.DrawIcon( new Rect( 0f, 0f, 24f, Height ), Icon, 12f );
		}

		if ( _canShowText && !string.IsNullOrEmpty( Text ) )
		{
			Paint.DrawText( new Rect( iconWidth, 0f, Width - iconWidth, Height ), Text, TextFlag.LeftCenter | TextFlag.SingleLine );
		}
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
		Transition.ForceUpdate();
	}

	public void DoubleClick()
	{
		Source.DoubleClick();
	}
}
