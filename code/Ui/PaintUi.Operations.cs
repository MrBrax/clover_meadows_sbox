using System;

namespace Clover.Ui;

public partial class PaintUi
{
	private Vector2Int? _lastBrushPosition;

	private void Draw( Vector2Int brushPosition )
	{
		var rect = new Rect( brushPosition.x, brushPosition.y, BrushSize, BrushSize );
		// DrawTexture.Update( GetCurrentColor(), rect );

		// PushRectToByteData( rect );
		PushRectToBoth( rect );

		// Draw line between last and current position
		if ( _lastBrushPosition.HasValue && _lastBrushPosition.Value != brushPosition )
		{
			DrawLineBetween( _lastBrushPosition.Value, brushPosition );
		}

		_lastBrushPosition = brushPosition;
	}

	private void Fill( Vector2Int brushPosition )
	{
		var targetColor = DrawTextureData[brushPosition.x + brushPosition.y * DrawTexture.Width];
		var replacementColor = (byte)CurrentPaletteIndex;

		if ( targetColor == replacementColor )
		{
			return;
		}

		FloodFill( brushPosition, targetColor, replacementColor );
	}

	private void FloodFill( Vector2Int position, byte targetColor, byte replacementColor )
	{
		var positionX = position.x;
		var positionY = position.y;
		if ( positionX < 0 || positionX >= DrawTexture.Width || positionY < 0 || positionY >= DrawTexture.Height )
		{
			return;
		}

		if ( DrawTextureData[positionX + positionY * DrawTexture.Width] != targetColor )
		{
			return;
		}

		// DrawTextureData[positionX + positionY * DrawTexture.Width] = replacementColor;
		// DrawTexture.Update( GetCurrentColor(), new Rect( positionX, positionY, 1, 1 ) );
		PushRectToBoth( new Rect( positionX, positionY, 1, 1 ) );

		// FloodFill( positionX + 1, positionY, targetColor, replacementColor );
		// FloodFill( positionX - 1, positionY, targetColor, replacementColor );
		// FloodFill( positionX, positionY + 1, targetColor, replacementColor );
		// FloodFill( positionX, positionY - 1, targetColor, replacementColor );
		
		FloodFill( position + Vector2Int.Right, targetColor, replacementColor );
		FloodFill( position + Vector2Int.Left, targetColor, replacementColor );
		FloodFill( position + Vector2Int.Up, targetColor, replacementColor );
		FloodFill( position + Vector2Int.Down, targetColor, replacementColor );
		
	}

	private TimeSince _lastSpray;

	/// <summary>
	///  Spray random paint in a circle around the brush position
	/// </summary>
	/// <param name="brushPosition"></param>
	private void Spray( Vector2Int brushPosition )
	{
		var radius = BrushSize * 3;
		var radiusSquared = radius * radius;

		if ( _lastSpray > 0.03f )
		{
			_lastSpray = 0;
		}
		else
		{
			return;
		}

		for ( var i = 0; i < 30; i++ )
		{
			// var randomX = MathF.RoundToInt( MathF.Random.Range( -radius, radius ) );
			// var randomY = MathF.RoundToInt( MathF.Random.Range( -radius, radius ) );
			var randomX = Random.Shared.Next( -radius, radius );
			var randomY = Random.Shared.Next( -radius, radius );

			if ( randomX * randomX + randomY * randomY <= radiusSquared )
			{
				var x = brushPosition.x + randomX;
				var y = brushPosition.y + randomY;

				if ( x >= 0 && x < DrawTexture.Width && y >= 0 && y < DrawTexture.Height )
				{
					var rect = new Rect( x, y, 1, 1 );
					// DrawTexture.Update( GetCurrentColor(), rect );
					// PushRectToByteData( rect );
					PushRectToBoth( rect );
				}
			}
		}
	}

	private void Eraser( Vector2Int brushPosition )
	{
		// DrawTexture.Update( BackgroundColor,
		// 	new Rect( brushPosition.x, brushPosition.y, BrushSize, BrushSize ) );
		// PushRectToByteData( new Rect( brushPosition.x, brushPosition.y, BrushSize, BrushSize ) );
		PushRectToBoth( new Rect( brushPosition.x, brushPosition.y, BrushSize, BrushSize ) );
	}

	private void Eyedropper( Vector2Int brushPosition, MouseButtons mouseButton )
	{
		var index = DrawTextureData[brushPosition.x + brushPosition.y * DrawTexture.Width];
		if ( mouseButton == MouseButtons.Left )
		{
			LeftPaletteIndex = index;
		}
		else if ( mouseButton == MouseButtons.Right )
		{
			RightPaletteIndex = index;
		}
	}

	private void DrawLine( Vector2Int startPos, Vector2Int endPos )
	{
		DrawLineBetween( startPos, endPos );
	}

	private void LinePreview( Vector2Int brushPosition )
	{
		ClearPreview();

		if ( !_mouseDownPosition.HasValue )
		{
			return;
		}

		DrawLineBetweenTex( PreviewTexture, PreviewColor, _mouseDownPosition.Value, brushPosition );
	}

	private void RectanglePreview( Vector2Int brushPosition )
	{
		ClearPreview();

		if ( !_mouseDownPosition.HasValue )
		{
			return;
		}

		DrawRectangle( _mouseDownPosition.Value, brushPosition, true );
	}

	// draw outline of rectangle
	private void DrawRectangle( Vector2Int mouseDownPosition, Vector2Int mouseUpPosition, bool preview = false )
	{
		var x = Math.Min( mouseDownPosition.x, mouseUpPosition.x );
		var y = Math.Min( mouseDownPosition.y, mouseUpPosition.y );
		var width = Math.Abs( mouseDownPosition.x - mouseUpPosition.x );
		var height = Math.Abs( mouseDownPosition.y - mouseUpPosition.y );

		if ( preview )
		{
			PreviewTexture.Update( PreviewColor, new Rect( x, y, width, 1 ) );
			PreviewTexture.Update( PreviewColor, new Rect( x, y, 1, height ) );
			PreviewTexture.Update( PreviewColor, new Rect( x + width, y, 1, height ) );
			PreviewTexture.Update( PreviewColor, new Rect( x, y + height, width + 1, 1 ) ); // TODO: why +1?
		}
		else
		{
			PushRectToBoth( new Rect( x, y, width, 1 ) );
			PushRectToBoth( new Rect( x, y, 1, height ) );
			PushRectToBoth( new Rect( x + width, y, 1, height ) );
			PushRectToBoth( new Rect( x, y + height, width + 1, 1 ) ); // TODO: why +1?
		}
	}

	private void CirclePreview( Vector2Int brushPosition )
	{
		ClearPreview();

		if ( !_mouseDownPosition.HasValue )
		{
			return;
		}

		DrawCircle( _mouseDownPosition.Value, brushPosition, true );
	}

	private void DrawCircle( Vector2Int startPosition, Vector2Int endPosition, bool preview = false )
	{
		var x = Math.Min( startPosition.x, endPosition.x );
		var y = Math.Min( startPosition.y, endPosition.y );
		var width = Math.Abs( startPosition.x - endPosition.x );
		var height = Math.Abs( startPosition.y - endPosition.y );

		var radius = Math.Min( width, height ) / 2;
		var centerX = x + width / 2;
		var centerY = y + height / 2;

		for ( var i = 0; i < 360; i++ )
		{
			var angle = MathX.DegreeToRadian( i );
			var circleX = centerX + MathF.Cos( angle ) * radius;
			var circleY = centerY + MathF.Sin( angle ) * radius;

			if ( preview )
			{
				PreviewTexture.Update( PreviewColor,
					new Rect( (int)Math.Round( circleX ), (int)Math.Round( circleY ), 1, 1 ) );
			}
			else
			{
				PushRectToBoth( new Rect( (int)Math.Round( circleX ), (int)Math.Round( circleY ), 1, 1 ) );
			}
		}
	}
	
	private void PasteClipboard( Vector2Int position )
	{
		Log.Info( $"Pasting clipboard at {position}, size {ClipboardSize}" );
		
		/*var x = position.x;
		var y = position.y;

		for ( var i = 0; i < ClipboardSize.x; i++ )
		{
			for ( var j = 0; j < ClipboardSize.y; j++ )
			{
				var index = x + i + (y + j) * DrawTexture.Width;
				if ( index >= 0 && index < DrawTextureData.Length )
				{
					DrawTextureData[index] = ClipboardData[i + j * ClipboardSize.x];
				}
			}
		}*/
		
		var width = ClipboardSize.x;
		var height = ClipboardSize.y;
		
		// clamp so it doesn't go out of bounds
		var x = Math.Clamp( position.x, 0, DrawTexture.Width );
		var y = Math.Clamp( position.y, 0, DrawTexture.Height );
		
		width = Math.Min( width, DrawTexture.Width - x );
		height = Math.Min( height, DrawTexture.Height - y );
		
		if ( width == 0 || height == 0 )
		{
			return;
		}
		
		for ( var i = 0; i < width; i++ )
		{
			for ( var j = 0; j < height; j++ )
			{
				var index = i + j * width;
				DrawTextureData[x + i + (y + j) * DrawTexture.Width] = ClipboardData[index];
			}
		}

		PushByteDataToTexture();
	}
	
	private void FillArea( Vector2Int start, Vector2Int end, int colorIndex )
	{
		var x = Math.Min( start.x, end.x );
		var y = Math.Min( start.y, end.y );
		var width = Math.Abs( start.x - end.x );
		var height = Math.Abs( start.y - end.y );
		
		PushRectToBoth( new Rect( x, y, width, height ), colorIndex );

		/*for ( var i = 0; i < width; i++ )
		{
			for ( var j = 0; j < height; j++ )
			{
				var index = x + i + (y + j) * DrawTexture.Width;
				if ( index >= 0 && index < DrawTextureData.Length )
				{
					DrawTextureData[index] = (byte)colorIndex;
				}
			}
		}

		PushByteDataToTexture();*/
	}
	
}
