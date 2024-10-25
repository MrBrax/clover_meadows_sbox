namespace Clover.Ui.Tools;

public class DrawTool : BasePaintTool
{
	private int BrushSize = 1;

	private Vector2Int? _lastBrushPosition;

	public DrawTool( PaintUi paintUi ) : base( paintUi )
	{
	}

	private void Draw()
	{
		var brushPosition = PaintUi.GetCurrentBrushPosition();

		var rect = new Rect( brushPosition.x, brushPosition.y, BrushSize, BrushSize );
		// DrawTexture.Update( GetCurrentColor(), rect );

		// PushRectToByteData( rect );
		PaintUi.PushRectToBoth( rect );

		// Draw line between last and current position
		if ( _lastBrushPosition.HasValue && _lastBrushPosition.Value != brushPosition )
		{
			// DrawLineBetween( _lastBrushPosition.Value, brushPosition );
		}

		_lastBrushPosition = brushPosition;
	}
}
