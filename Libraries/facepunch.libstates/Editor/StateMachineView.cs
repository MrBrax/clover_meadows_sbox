using Editor;
using System.Collections.Generic;
using System;
using System.Linq;
using Editor.NodeEditor;
using Facepunch.ActionGraphs;
using System.IO.Compression;
using System.IO;
using System.Text;
using Sandbox.Utility;
using Sandbox.UI;

namespace Sandbox.States.Editor;

public interface IContextMenuSource
{
	void OnContextMenu( ContextMenuEvent e );
}

public interface IDeletable
{
	void Delete();
}

public interface IDoubleClickable
{
	void DoubleClick();
}

public class StateMachineView : GraphicsView
{
	private static Dictionary<Guid, StateMachineView> AllViews { get; } = new Dictionary<Guid, StateMachineView>();

	public static StateMachineView Open( StateMachineComponent stateMachine )
	{
		var guid = stateMachine.Id;

		if ( !AllViews.TryGetValue( guid, out var inst ) || !inst.IsValid )
		{
			var window = StateMachineEditorWindow.AllWindows.LastOrDefault( x => x.IsValid )
				?? new StateMachineEditorWindow();

			AllViews[guid] = inst = window.Open( stateMachine );
		}

		inst.Window?.Show();
		inst.Window?.Focus();

		inst.Show();
		inst.Focus();

		inst.Window?.DockManager.RaiseDock( inst.Name );

		return inst;
	}

	public StateMachineComponent StateMachine { get; }

	public StateMachineEditorWindow Window { get; }

	GraphView.SelectionBox? _selectionBox;
	private bool _dragging;
	private Vector2 _lastMouseScenePosition;

	private Vector2 _lastCenter;
	private Vector2 _lastScale;

	private readonly Dictionary<State, StateItem> _stateItems = new();
	private readonly Dictionary<Transition, TransitionItem> _transitionItems = new();
	private readonly Dictionary<UnorderedPair<int>, List<TransitionItem>> _neighboringTransitions = new( EqualityComparer<UnorderedPair<int>>.Default );

	private TransitionItem? _transitionPreview;
	private bool _wasDraggingTransition;

	private string? _lastEditName;

	private readonly Stack<(string Name, string Json)> _undoStack = new();
	private readonly Stack<(string Name, string Json)> _redoStack = new();

	public float GridSize => 32f;

	private string ViewCookie => $"statemachine.{StateMachine.Id}";

	public StateMachineView( StateMachineComponent stateMachine, StateMachineEditorWindow window )
		: base( null )
	{
		StateMachine = stateMachine;
		Window = window;

		Name = $"View:{stateMachine.Id}";

		WindowTitle = $"{stateMachine.Scene.Name} - {stateMachine.GameObject.Name}";

		SetBackgroundImage( "toolimages:/grapheditor/grapheditorbackgroundpattern_shader.png" );

		Antialiasing = true;
		TextAntialiasing = true;
		BilinearFiltering = true;

		SceneRect = new Rect( -100000, -100000, 200000, 200000 );

		HorizontalScrollbar = ScrollbarMode.Off;
		VerticalScrollbar = ScrollbarMode.Off;
		MouseTracking = true;

		UpdateItems();

		PushHistoryInternal( "Initial" );
	}

	protected override void OnFocus( FocusChangeReason reason )
	{
		base.OnFocus( reason );

		Window.OnFocusView( this );
	}

	protected override void OnClosed()
	{
		base.OnClosed();

		Window.OnRemoveView( this );

		if ( AllViews.TryGetValue( StateMachine.Id, out var view ) && view == this )
		{
			AllViews.Remove( StateMachine.Id );
		}
	}

	protected override void OnWheel( WheelEvent e )
	{
		Zoom( e.Delta > 0 ? 1.1f : 0.90f, e.Position );
		e.Accept();
	}

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		if ( e.IsDoubleClick )
		{
			if ( GetItemAt( ToScene( e.LocalPosition ) ) is IDoubleClickable target )
			{
				target.DoubleClick();

				e.Accepted = true;
				return;
			}
		}

		if ( e.MiddleMouseButton )
		{
			e.Accepted = true;
			return;
		}

		if ( e.RightMouseButton )
		{
			e.Accepted = GetItemAt( ToScene( e.LocalPosition ) ) is null;
			return;
		}

		if ( e.LeftMouseButton )
		{
			_dragging = true;
		}
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		base.OnMouseReleased( e );

		_selectionBox?.Destroy();
		_selectionBox = null;
		_dragging = false;

		if ( _stateItems.Values.Any( x => x.HasMoved ) )
		{
			LogEdit( "State Moved" );

			foreach ( var stateItem in _stateItems.Values )
			{
				stateItem.HasMoved = false;
			}
		}

		if ( _transitionPreview?.Target is { } target )
		{
			LogEdit( "Transition Added" );

			var transition = _transitionPreview.Source.State.AddTransition( target.State );

			if ( _transitionPreview.Transition is { } copy )
			{
				transition.CopyFrom( copy );
			}

			AddTransitionItem( transition );
		}

		if ( _transitionPreview is not null )
		{
			_transitionPreview?.Destroy();
			_transitionPreview = null;

			_wasDraggingTransition = true;

			e.Accepted = true;

			UpdateTransitionNeighbors();
		}
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		var scenePos = ToScene( e.LocalPosition );

		if ( _dragging && e.ButtonState.HasFlag( MouseButtons.Left ) && !_transitionPreview.IsValid() )
		{
			if ( !_selectionBox.IsValid() && !SelectedItems.Any( x => x.IsValid() && x.Movable ) && !Items.Any( x => x.Hovered ) )
			{
				Add( _selectionBox = new GraphView.SelectionBox( scenePos, this ) );
			}

			if ( _selectionBox != null )
			{
				_selectionBox.EndScene = scenePos;
			}
		}
		else if ( _dragging )
		{
			_selectionBox?.Destroy();
			_selectionBox = null;
			_dragging = false;
		}

		if ( e.ButtonState.HasFlag( MouseButtons.Middle ) ) // or space down?
		{
			var delta = scenePos - _lastMouseScenePosition;
			Translate( delta );
			e.Accepted = true;
			Cursor = CursorShape.ClosedHand;
		}
		else
		{
			Cursor = CursorShape.None;
		}

		if ( _transitionPreview.IsValid() )
		{
			var oldTarget = _transitionPreview.Target;

			_transitionPreview.TargetPosition = scenePos;

			if ( GetStateItemAt( scenePos ) is { } newTarget && newTarget != _transitionPreview.Source )
			{
				_transitionPreview.Target = newTarget;
			}
			else
			{
				_transitionPreview.Target = null;
			}

			if ( oldTarget != _transitionPreview.Target )
			{
				UpdateTransitionNeighbors();
			}

			_transitionPreview.Layout();
		}

		e.Accepted = true;

		_lastMouseScenePosition = ToScene( e.LocalPosition );
	}

	private StateItem? GetStateItemAt( Vector2 scenePos )
	{
		return GetItemAt( scenePos ) switch
		{
			StateItem stateItem => stateItem,
			StateLabel stateLabel => stateLabel.State,
			_ => null
		};
	}

	protected override void OnContextMenu( ContextMenuEvent e )
	{
		if ( _wasDraggingTransition )
		{
			return;
		}

		var menu = new global::Editor.Menu { DeleteOnClose = true };
		var scenePos = ToScene( e.LocalPosition );

		if ( GetItemAt( scenePos ) is IContextMenuSource source )
		{
			source.OnContextMenu( e );

			if ( e.Accepted ) return;
		}

		e.Accepted = true;

		menu.AddHeading( "Create State" );

		menu.AddLineEdit( "Name", autoFocus: true, onSubmit: name =>
		{
			LogEdit( "State Added" );

			using var _ = StateMachine.Scene.Push();

			var state = StateMachine.AddState();

			state.Name = name ?? "Unnamed";
			state.EditorPosition = scenePos.SnapToGrid( GridSize ) - 64f;

			if ( !StateMachine.InitialState.IsValid() )
			{
				StateMachine.InitialState = state;
			}

			AddStateItem( state );
		} );

		menu.OpenAtCursor( true );
	}

	[EditorEvent.Frame]
	private void OnFrame()
	{
		SaveViewCookie();

		if ( _lastEditName is not null )
		{
			StateMachine.Scene.EditLog( _lastEditName, StateMachine );
			PushHistoryInternal( _lastEditName );

			_lastEditName = null;
		}

		_wasDraggingTransition = false;

		var needsUpdate = false;

		foreach ( var (state, item) in _stateItems )
		{
			if ( !state.IsValid )
			{
				needsUpdate = true;
				break;
			}
		}

		foreach ( var (transition, item) in _transitionItems )
		{
			if ( !transition.IsValid )
			{
				needsUpdate = true;
				break;
			}
		}

		if ( needsUpdate )
		{
			UpdateItems();
		}

		foreach ( var item in _stateItems.Values )
		{
			item.Frame();
		}

		foreach ( var item in _transitionItems.Values )
		{
			item.Frame();
		}
	}

	[Shortcut( "Reset View", "Home", ShortcutType.Window )]
	private void OnResetView()
	{
		var defaultView = GetDefaultView();

		_lastScale = Scale = defaultView.Scale;
		_lastCenter = Center = defaultView.Center;
	}

	private void SaveViewCookie()
	{
		var center = Center;
		var scale = Scale;

		if ( _lastCenter == center && _lastScale == scale )
		{
			return;
		}

		if ( ViewCookie is { } viewCookie )
		{
			if ( _lastCenter != center )
			{
				EditorCookie.Set( $"{viewCookie}.view.center", center );
			}

			if ( _lastScale != scale )
			{
				EditorCookie.Set( $"{viewCookie}.view.scale", scale );
			}
		}

		_lastCenter = center;
		_lastScale = scale;
	}

	private void RestoreViewFromCookie()
	{
		if ( ViewCookie is not { } cookieName ) return;

		var defaultView = GetDefaultView();

		Scale = EditorCookie.Get( $"{cookieName}.view.scale", defaultView.Scale );
		Center = EditorCookie.Get( $"{cookieName}.view.center", defaultView.Center );
	}

	private (Vector2 Center, Vector2 Scale) GetDefaultView()
	{
		if ( _stateItems.Count == 0 )
		{
			return (Vector2.Zero, Vector2.One);
		}

		var allBounds = _stateItems.Values
			.Select( x => new Rect( x.Position, x.Size ) )
			.ToArray();

		var bounds = allBounds[0];

		foreach ( var rect in allBounds.Skip( 1 ) )
		{
			bounds.Add( rect );
		}

		// TODO: resize to fit
		return (bounds.Center, Vector2.One);
	}

	private readonly struct UnorderedPair<T> : IEquatable<UnorderedPair<T>>
		where T : IEquatable<T>
	{
		public T A { get; }
		public T B { get; }

		public UnorderedPair( T a, T b )
		{
			A = a;
			B = b;
		}

		public bool Equals( UnorderedPair<T> other )
		{
			return A.Equals( other.A ) && B.Equals( other.B ) || A.Equals( other.B ) && B.Equals( other.A );
		}

		public override int GetHashCode()
		{
			return A.GetHashCode() ^ B.GetHashCode();
		}
	}

	public void UpdateItems()
	{
		ItemHelper<State, StateItem>.Update( this, StateMachine.States, _stateItems, AddStateItem );
		var transitionsChanged = ItemHelper<Transition, TransitionItem>.Update( this, StateMachine.States.SelectMany( x => x.Transitions ), _transitionItems, AddTransitionItem );

		if ( transitionsChanged )
		{
			UpdateTransitionNeighbors();
		}

		RestoreViewFromCookie();
	}

	private void UpdateTransitionNeighbors()
	{
		_neighboringTransitions.Clear();

		foreach ( var item in Items.OfType<TransitionItem>().Where( x => x.Target is not null ) )
		{
			var key = new UnorderedPair<int>( item.Source.State.Id, item.Target!.State.Id );

			if ( !_neighboringTransitions.TryGetValue( key, out var list ) )
			{
				_neighboringTransitions[key] = list = new List<TransitionItem>();
			}

			list.Add( item );
		}

		foreach ( var list in _neighboringTransitions.Values )
		{
			list.Sort();

			foreach ( var item in list )
			{
				item.Layout();
			}
		}
	}

	private void AddStateItem( State state )
	{
		var item = new StateItem( this, state );
		_stateItems.Add( state, item );
		Add( item );
	}

	private void AddTransitionItem( Transition transition )
	{
		var source = GetStateItem( transition.Source );
		var target = GetStateItem( transition.Target );

		if ( source is null || target is null ) return;

		var item = new TransitionItem( transition, source, target );
		_transitionItems.Add( transition, item );
		Add( item );
	}

	public StateItem? GetStateItem( State state )
	{
		return _stateItems!.GetValueOrDefault( state );
	}

	public TransitionItem? GetTransitionItem( Transition transition )
	{
		return _transitionItems!.GetValueOrDefault( transition );
	}

	public (int Index, int Count) GetTransitionPosition( TransitionItem item )
	{
		if ( item.Target is null )
		{
			return (0, 1);
		}

		var key = new UnorderedPair<int>( item.Source.State.Id, item.Target.State.Id );

		if ( !_neighboringTransitions.TryGetValue( key, out var list ) )
		{
			return (0, 1);
		}

		return (list.IndexOf( item ), list.Count);
	}

	public void StartCreatingTransition( StateItem source, Transition? copy = null )
	{
		DeselectAll();

		_transitionPreview?.Destroy();

		_transitionPreview = new TransitionItem( copy, source, null )
		{
			TargetPosition = source.Center
		};

		_transitionPreview.Layout();

		Add( _transitionPreview );
	}

	public void DeselectAll()
	{
		foreach ( var item in SelectedItems.Where( x => x.IsValid ).ToArray() )
		{
			item.Selected = false;
		}
	}

	public void SelectAll()
	{
		foreach ( var item in Items.Where( x => x.Selectable ) )
		{
			item.Selected = true;
		}
	}

	private IDisposable PushSerializationScope()
	{
		var sceneScope = StateMachine.Scene.Push();
		var targetScope = ActionGraph.PushTarget( InputDefinition.Target( typeof(GameObject), StateMachine.GameObject ) );

		return new DisposeAction( () =>
		{
			targetScope.Dispose();
			sceneScope.Dispose();
		} );
	}

	public void LogEdit( string name )
	{
		_lastEditName ??= name;
	}

	private void PushHistoryInternal( string name )
	{
		using var scope = PushSerializationScope();

		try
		{
			var serialized = StateMachine.SerializeAll();

			if ( _undoStack.TryPeek( out var prev ) && string.Equals( prev.Json, serialized, StringComparison.Ordinal ) )
			{
				return;
			}

			_redoStack.Clear();
			_undoStack.Push( (name, serialized) );
		}
		catch ( Exception e )
		{
			Log.Error( e );
		}
	}

	public void CutSelection()
	{
		CopySelection();
		DeleteSelection();
	}

	private const string ClipboardPrefix = "fsm:";

	private (IReadOnlyList<State> States, IReadOnlyList<Transition> Transitions) GetSelectionForCopy()
	{
		var states = SelectedItems
			.Where( x => x.IsValid )
			.OfType<StateItem>()
			.Select( x => x.State )
			.ToArray();

		var transitions = SelectedItems
			.Where( x => x.IsValid )
			.OfType<TransitionItem>()
			.Where( x => x.Transition != null )
			.Select( x => x.Transition! )
			.ToArray();

		return (states, transitions);
	}

	public void CopySelection()
	{
		using var scope = PushSerializationScope();

		var selection = GetSelectionForCopy();

		if ( selection.States.Count == 0 ) return;

		using var ms = new MemoryStream();
		using ( var zs = new GZipStream( ms, CompressionMode.Compress ) )
		{
			var data = Encoding.UTF8.GetBytes( StateMachine.Serialize( selection.States, selection.Transitions ) );
			zs.Write( data, 0, data.Length );
		}

		EditorUtility.Clipboard.Copy( $"{ClipboardPrefix}{Convert.ToBase64String( ms.ToArray() )}" );
	}

	public void DeleteSelection()
	{
		LogEdit( "Delete Selection" );

		var deletable = SelectedItems
			.Where( x => x.IsValid )
			.OfType<IDeletable>()
			.ToArray();

		foreach ( var item in deletable )
		{
			item.Delete();
		}
	}

	public void FlipSelection()
	{
		LogEdit( "Flip Selection" );

		var flippable = SelectedItems
			.Where( x => x.IsValid )
			.OfType<TransitionItem>()
			.Where( x => x is { Transition: not null, IsPreview: false } )
			.Select( x => x.Transition! )
			.ToArray();

		var flipped = new HashSet<Transition>();

		foreach ( var item in flippable )
		{
			var copy = item.Target.AddTransition( item.Source );
			copy.CopyFrom( item );
			item.Remove();

			flipped.Add( copy );
		}

		UpdateItems();

		foreach ( var item in _transitionItems.Values )
		{
			item.Selected = item.Transition is not null && flipped.Contains( item.Transition );
		}
	}

	private void PostDuplicate( IReadOnlyList<State> states, IReadOnlyList<Transition> transitions, Vector2 offset )
	{
		foreach ( var state in states )
		{
			state.EditorPosition += offset;
		}

		UpdateItems();
		DeselectAll();

		foreach ( var state in states )
		{
			GetStateItem( state )!.Selected = true;
		}

		foreach ( var transition in transitions )
		{
			GetTransitionItem( transition )!.Selected = true;
		}
	}

	public void DuplicateSelection()
	{
		using var scope = PushSerializationScope();

		var selection = GetSelectionForCopy();

		// TODO: duplicate transitions only?

		if ( selection.States.Count == 0 ) return;

		var serialized = StateMachine.Serialize( selection.States, selection.Transitions );
		var duplicated = StateMachine.DeserializeInsert( serialized );

		PostDuplicate( duplicated.States, duplicated.Transitions, GridSize );
	}

	public void PasteSelection()
	{
		using var scope = PushSerializationScope();

		var buffer = EditorUtility.Clipboard.Paste();
		if ( string.IsNullOrWhiteSpace( buffer ) ) return;
		if ( !buffer.StartsWith( ClipboardPrefix ) ) return;

		buffer = buffer[ClipboardPrefix.Length..];

		byte[] decompressedData;

		try
		{
			using var ms = new MemoryStream( Convert.FromBase64String( buffer ) );
			using var zs = new GZipStream( ms, CompressionMode.Decompress );
			using var outStream = new MemoryStream();
			zs.CopyTo( outStream );
			decompressedData = outStream.ToArray();
		}
		catch
		{
			Log.Warning( "Paste is not valid base64" );
			return;
		}

		try
		{
			LogEdit( "Paste" );

			var decompressed = Encoding.UTF8.GetString( decompressedData );
			var duplicated = StateMachine.DeserializeInsert( decompressed );

			if ( !duplicated.States.Any() )
				return;

			// using var undoScope = UndoScope( "Paste Selection" );

			var averagePos = new Vector2(
				duplicated.States.Average( x => x.EditorPosition.x ),
				duplicated.States.Average( x => x.EditorPosition.y ) );

			var offset = (_lastMouseScenePosition - averagePos).SnapToGrid( GridSize );

			PostDuplicate( duplicated.States, duplicated.Transitions, offset );
		}
		catch ( Exception e )
		{
			Log.Warning( $"Paste is not valid json: {e}" );
		}
	}

	public void Undo()
	{
		if ( _undoStack.Count <= 1 ) return;

		_redoStack.Push( _undoStack.Pop() );
		RestoreFromUndoStack();
	}

	public void Redo()
	{
		if ( !_redoStack.TryPop( out var item ) ) return;

		_undoStack.Push( item );
		RestoreFromUndoStack();
	}

	private void RestoreFromUndoStack()
	{
		using var scope = PushSerializationScope();

		StateMachine.DeserializeAll( _undoStack.Peek().Json );
		UpdateItems();
	}

	private static class ItemHelper<TSource, TItem>
		where TSource : notnull
		where TItem : GraphicsItem
	{
		[ThreadStatic] private static HashSet<TSource>? SourceSet;
		[ThreadStatic] private static List<TSource>? ToRemove;

		public static bool Update( GraphicsView view, IEnumerable<TSource> source, Dictionary<TSource, TItem> dict, Action<TSource> add )
		{
			SourceSet ??= new HashSet<TSource>();
			SourceSet.Clear();

			ToRemove ??= new List<TSource>();
			ToRemove.Clear();

			var changed = false;

			foreach ( var component in source )
			{
				SourceSet.Add( component );
			}

			foreach ( var (state, item) in dict )
			{
				if ( !SourceSet.Contains( state ) )
				{
					item.Destroy();
					ToRemove.Add( state );

					changed = true;
				}
			}

			foreach ( var removed in ToRemove )
			{
				dict.Remove( removed );
			}

			foreach ( var component in SourceSet )
			{
				if ( !dict.ContainsKey( component ) )
				{
					add( component );

					changed = true;
				}
			}

			return changed;
		}
	}
}
