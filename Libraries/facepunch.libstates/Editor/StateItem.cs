using System;
using System.Linq;
using Editor;
using Editor.NodeEditor;
using Facepunch.ActionGraphs;
using static System.Net.Mime.MediaTypeNames;
using static Editor.Label;

namespace Sandbox.States.Editor;

public sealed class StateItem : GraphicsItem, IContextMenuSource, IDeletable
{
	public static Color PrimaryColor { get; } = Color.Parse( "#5C79DB" )!.Value;
	public static Color InitialColor { get; } = Color.Parse( "#BCA5DB" )!.Value;

	public StateMachineView View { get; }
	public State State { get; }

	public float Radius => 64f;

	public event Action? PositionChanged;

	private bool _rightMousePressed;

	private int _lastHash;

	private readonly StateLabel _enterLabel;
	private readonly StateLabel _updateLabel;
	private readonly StateLabel _leaveLabel;

	internal bool HasMoved { get; set; }

	public StateItem( StateMachineView view, State state )
	{
		View = view;
		State = state;

		Size = new Vector2( Radius * 2f, Radius * 2f );
		Position = state.EditorPosition;

		Movable = true;
		Selectable = true;
		HoverEvents = true;

		Cursor = CursorShape.Finger;

		_enterLabel = new StateLabel( this, new StateEnterAction( this ) );
		_updateLabel = new StateLabel( this, new StateUpdateAction( this ) );
		_leaveLabel = new StateLabel( this, new StateLeaveAction( this ) );

		UpdateTooltip();
		AlignLabels();
	}

	public override Rect BoundingRect => base.BoundingRect.Grow( 16f );

	public override bool Contains( Vector2 localPos )
	{
		return (LocalRect.Center - localPos).LengthSquared < Radius * Radius;
	}

	protected override void OnPaint()
	{
		var borderColor = Selected 
			? Color.Yellow : Hovered
			? Color.White : Color.White.Darken( 0.125f );

		var fillColor = State.StateMachine?.InitialState == State
			? InitialColor
			: PrimaryColor;

		fillColor = fillColor
			.Lighten( Selected ? 0.5f : Hovered ? 0.25f : 0f )
			.Desaturate( Selected ? 0.5f : Hovered ? 0.25f : 0f );

		Paint.SetBrushRadial( LocalRect.Center - LocalRect.Size * 0.125f, Radius * 1.5f, fillColor.Lighten( 0.5f ), fillColor.Darken( 0.75f ) );
		Paint.DrawCircle( Size * 0.5f, Size );

		Paint.SetPen( borderColor, Selected || Hovered ? 3f : 2f );
		Paint.SetBrushRadial( LocalRect.Center, Radius, 0.75f, Color.Black.WithAlpha( 0f ), 1f, Color.Black.WithAlpha( 0.25f ) );
		Paint.DrawCircle( Size * 0.5f, Size );

		if ( State.StateMachine?.CurrentState == State )
		{
			Paint.ClearBrush();
			Paint.DrawCircle( Size * 0.5f, Size + 8f );
		}

		var titleRect = (State.OnEnterState ?? State.OnUpdateState ?? State.OnLeaveState) is not null
			? new Rect( 0f, Size.y * 0.35f - 12f, Size.x, 24f )
			: new Rect( 0f, Size.y * 0.5f - 12f, Size.x, 24f );

		Paint.ClearBrush();

		if ( IsEmoji )
		{
			Paint.SetFont( "roboto", Size.y * 0.5f, 600 );
			Paint.SetPen( Color.White );
			Paint.DrawText( new Rect( 0f, -4f, Size.x, Size.y ), State.Name );
		}
		else
		{
			Paint.SetFont( "roboto", 12f, 600 );
			Paint.SetPen( Color.Black.WithAlpha( 0.5f ) );
			Paint.DrawText( new Rect( titleRect.Position + 2f, titleRect.Size ), State.Name );

			Paint.SetPen( borderColor );
			Paint.DrawText( titleRect, State.Name );
		}
	}

	public bool IsEmoji => State.Name.Length == 2 && State.Name[0] >= 0x8000 && char.ConvertToUtf32( State.Name, 0 ) != -1;

	protected override void OnMousePressed( GraphicsMouseEvent e )
	{
		base.OnMousePressed( e );

		if ( e.RightMouseButton )
		{
			_rightMousePressed = true;

			e.Accepted = true;
		}
	}

	protected override void OnMouseReleased( GraphicsMouseEvent e )
	{
		base.OnMouseReleased( e );

		if ( e.RightMouseButton && _rightMousePressed )
		{
			_rightMousePressed = false;

			e.Accepted = true;
		}
	}

	protected override void OnMouseMove( GraphicsMouseEvent e )
	{
		if ( _rightMousePressed && !Contains( e.LocalPosition ) )
		{
			_rightMousePressed = false;

			View.StartCreatingTransition( this );
		}

		base.OnMouseMove( e );
	}

	public void OnContextMenu( ContextMenuEvent e )
	{
		e.Accepted = true;
		Selected = true;

		var menu = new global::Editor.Menu { DeleteOnClose = true };

		menu.AddHeading( "State" );

		menu.AddMenu( "Rename", "edit" ).AddLineEdit( "Rename", State.Name, onSubmit: value =>
		{
			View.LogEdit( "State Renamed" );

			State.Name = value ?? "Unnamed";
			Update();
		}, autoFocus: true );

		if ( State.StateMachine.InitialState != State )
		{
			menu.AddOption( "Make Initial", "start", action: () =>
			{
				View.LogEdit( "Initial State Assigned" );

				State.StateMachine.InitialState = State;
				Update();
			} );
		}

		menu.AddSeparator();

		foreach ( var label in Children.OfType<StateLabel>() )
		{
			label.Source.BuildAddContextMenu( menu );
		}

		menu.AddSeparator();
		menu.AddOption( "Delete", "delete", action: Delete );

		menu.OpenAtCursor( true );
	}

	protected override void OnMoved()
	{
		HasMoved = true;

		State.EditorPosition = Position.SnapToGrid( View.GridSize );

		UpdatePosition();
	}

	public void UpdatePosition()
	{
		Position = State.EditorPosition;

		PositionChanged?.Invoke();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		var transitions = View.Items.OfType<TransitionItem>()
			.Where( x => x.Source == this || x.Target == this )
			.ToArray();

		foreach ( var transition in transitions )
		{
			transition.Destroy();
		}
	}

	public void Delete()
	{
		View.LogEdit( "State Removed" );

		if ( State.StateMachine.InitialState == State )
		{
			State.StateMachine.InitialState = null;
		}

		var transitions = View.Items.OfType<TransitionItem>()
			.Where( x => x.Source == this || x.Target == this )
			.ToArray();

		foreach ( var transition in transitions )
		{
			transition.Delete();
		}

		State.Remove();
		Destroy();
	}

	private void UpdateTooltip()
	{
		Tooltip = State.StateMachine.InitialState == State ? $"State <b>{State.Name}</b> <i>(initial)</i>" : $"State <b>{State.Name}</b>";
	}

	private void AlignLabels()
	{
		var labels = Children.OfType<StateLabel>()
			.ToArray();

		foreach ( var label in labels )
		{
			label.Layout();
		}

		var size = new Vector2( 32f, 32f );
		var totalWidth = labels.Sum( x => x.Width );

		var origin = Size * 0.5f - new Vector2( totalWidth * 0.5f, size.y * 0.5f );

		if ( !IsEmoji )
		{
			origin.y += Radius / 6f;
		}

		foreach ( var label in labels )
		{
			label.Position = origin;
			label.Update();

			origin.x += label.Width;
		}
	}

	public void ForceUpdate()
	{
		if ( !IsValid ) return;

		AlignLabels();
		Update();
	}

	public void Frame()
	{
		var hash = HashCode.Combine( State.StateMachine.InitialState == State, State.StateMachine.CurrentState == State );
		if ( hash == _lastHash ) return;

		_lastHash = hash;

		UpdateTooltip();
		AlignLabels();
		Update();
	}
}
