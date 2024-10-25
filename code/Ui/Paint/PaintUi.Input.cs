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

		// CanvasSize += (int)(value.y * -30);
		// var zoomFactor = (int)(1 + (value.y * 0.1f));
		// Log.Info( $"Zoom factor: {zoomFactor}" );
		// CanvasSize *= zoomFactor;
		// CanvasSize = Math.Clamp( CanvasSize, 32, 2048 );

		// CanvasZoom += value.y * -0.2f;
		// CanvasZoom = Math.Clamp( CanvasZoom, MinCanvasZoom, MaxCanvasZoom );
		// UpdateCanvas();

		Zoom( value.y * -0.2f, GetCurrentMousePixel() );
	}

	private void OnCanvasMouseDown( PanelEvent e )
	{
		PushUndo();

		if ( e is MousePanelEvent ev )
		{
			if ( PaintToolClass != null )
			{
				PaintToolClass.OnMouseDown( ev.MouseButton );
			}

			if ( ev.MouseButton == MouseButtons.Left )
			{
				CurrentPaletteIndex = LeftPaletteIndex;
				_isDrawing = true;

				_mouseUpPosition = null;
				_mouseDownPosition = GetCurrentMousePixel();

				if ( CurrentTool == PaintTool.Eyedropper )
				{
					Eyedropper( GetCurrentMousePixel(), ev.MouseButton );
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
			}
			else if ( ev.MouseButton == MouseButtons.Right )
			{
				CurrentPaletteIndex = RightPaletteIndex;
				_isDrawing = true;

				_mouseUpPosition = null;
				_mouseDownPosition = GetCurrentMousePixel();

				if ( CurrentTool == PaintTool.Eyedropper )
				{
					Eyedropper( GetCurrentMousePixel(), ev.MouseButton );
				}

				if ( CurrentTool == PaintTool.Clone )
				{
					_isMoving = false;
					ClearPreview();
				}
			}
			else
			{
				Log.Info( "Unknown mouse button" );
			}
		}

		RedoStack.Clear();

		e.StopPropagation();
	}

	private void OnCanvasMouseUp( PanelEvent e )
	{
		if ( !_isDrawing ) return;

		_isDrawing = false;
		_lastBrushPosition = null;

		_mouseUpPosition = GetCurrentMousePixel();

		if ( _mouseDownPosition.HasValue && _mouseUpPosition.HasValue )
		{
			if ( PaintToolClass != null )
			{
				PaintToolClass.OnMouseUp();
			}


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
