using Sandbox.UI;

namespace Clover.Ui;

public partial class PaintUi
{
	protected override void OnMouseWheel( Vector2 value )
	{
		base.OnMouseWheel( value );

		if ( !IsMouseInsideCanvasContainer() )
		{
			Log.Info( "Mouse not inside canvas" );
			return;
		}

		if ( _isPanning ) return;

		Zoom( value.y * -0.2f, Mouse.Position );
	}

	private bool _isPanning;

	protected override void OnMouseDown( MousePanelEvent e )
	{
		base.OnMouseDown( e );

		if ( e.MouseButton == MouseButtons.Middle && IsMouseInsideCanvasContainer() )
		{
			_isPanning = true;
			_lastPanPosition = Panel.MousePosition;
			Log.Info( "Panning on" );
			e.StopPropagation();
		}
	}

	protected override void OnMouseUp( MousePanelEvent e )
	{
		base.OnMouseUp( e );

		if ( e.MouseButton == MouseButtons.Middle )
		{
			_isPanning = false;
			Log.Info( "Panning off" );
			e.StopPropagation();
		}
	}

	private Vector2 _lastPanPosition;

	protected override void OnMouseMove( MousePanelEvent e )
	{
		base.OnMouseMove( e );

		if ( _isPanning )
		{
			Pan( Panel.MousePosition - _lastPanPosition );
			_lastPanPosition = Panel.MousePosition;
			e.StopPropagation();
		}
	}

	private void Pan( Vector2 delta )
	{
		var panSpeed = 1.0f * Panel.ScaleFromScreen;

		CanvasSquare.Style.Left =
			Length.Pixels( CanvasSquare.Style.Left.GetValueOrDefault().GetPixels( 1 ) + (delta.x * panSpeed) );

		CanvasSquare.Style.Top =
			Length.Pixels( CanvasSquare.Style.Top.GetValueOrDefault().GetPixels( 1 ) + (delta.y * panSpeed) );
	}

	private void OnCanvasMouseDown( PanelEvent e )
	{
		if ( e is MousePanelEvent ev )
		{
			if ( ev.MouseButton == MouseButtons.Left )
			{
				PushUndo();

				CurrentPaletteIndex = LeftPaletteIndex;
				_isDrawing = true;

				_mouseUpPosition = null;
				_mouseDownPosition = GetCurrentMousePixel();

				if ( CurrentTool == PaintTool.Eyedropper )
				{
					Eyedropper( GetCurrentMousePixel(), ev.MouseButton );
					CurrentTool = _previousTool;
				}

				if ( (CurrentTool == PaintTool.Move || CurrentTool == PaintTool.Clone) && _isMoving )
				{
					PasteClipboard( _mouseDownPosition.Value );

					if ( CurrentTool == PaintTool.Move )
					{
						ClearPreview();
						_isDrawing = false;
						_isMoving = false;
					}
				}

				if ( CurrentTool == PaintTool.Burn )
				{
					Burn( GetCurrentBrushPosition() );
				}
				else if ( CurrentTool == PaintTool.Dodge )
				{
					Dodge( GetCurrentBrushPosition() );
				}

				RedoStack.Clear();
				e.StopPropagation();
			}
			else if ( ev.MouseButton == MouseButtons.Right )
			{
				PushUndo();

				CurrentPaletteIndex = RightPaletteIndex;
				_isDrawing = true;

				_mouseUpPosition = null;
				_mouseDownPosition = GetCurrentMousePixel();

				if ( CurrentTool == PaintTool.Eyedropper )
				{
					Eyedropper( GetCurrentMousePixel(), ev.MouseButton );
					CurrentTool = _previousTool;
				}

				if ( CurrentTool == PaintTool.Clone )
				{
					_isMoving = false;
					ClearPreview();
				}

				RedoStack.Clear();
				e.StopPropagation();
			}
			else
			{
				Log.Info( "Unknown mouse button" );
			}
		}
	}

	private void OnCanvasMouseUp( PanelEvent e )
	{
		if ( !_isDrawing ) return;

		_isDrawing = false;
		_lastBrushPosition = null;

		_mouseUpPosition = GetCurrentMousePixel();

		if ( _mouseDownPosition.HasValue && _mouseUpPosition.HasValue )
		{
			if ( CurrentTool == PaintTool.Line )
			{
				DrawLine( _mouseDownPosition.Value, _mouseUpPosition.Value );
			}
			else if ( CurrentTool == PaintTool.Rectangle )
			{
				DrawRectangle( _mouseDownPosition.Value, _mouseUpPosition.Value );
			}
			else if ( CurrentTool == PaintTool.Circle )
			{
				DrawCircle( _mouseDownPosition.Value, _mouseUpPosition.Value );
			}
			else if ( CurrentTool == PaintTool.Move && !_isMoving )
			{
				SetClipboard( _mouseDownPosition.Value, _mouseUpPosition.Value );
				FillArea( _mouseDownPosition.Value, _mouseUpPosition.Value, RightPaletteIndex );
				_isMoving = true;
			}
			else if ( CurrentTool == PaintTool.Clone && !_isMoving )
			{
				SetClipboard( _mouseDownPosition.Value, _mouseUpPosition.Value );
				_isMoving = true;
				// _isDrawing = true;
			}
		}

		ClearPreview();

		_mouseDownPosition = null;
	}
}
