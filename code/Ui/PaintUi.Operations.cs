﻿using System;

namespace Clover.Ui;

public partial class PaintUi
{
	private Vector2? _lastBrushPosition;

	private void Draw( Vector2 brushPosition )
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

	private void Fill( Vector2 brushPosition )
	{
		var targetColor = DrawTextureData[(int)brushPosition.x + (int)brushPosition.y * DrawTexture.Width];
		var replacementColor = (byte)CurrentPaletteIndex;

		if ( targetColor == replacementColor )
		{
			return;
		}

		FloodFill( (int)brushPosition.x, (int)brushPosition.y, targetColor, replacementColor );
	}

	private void FloodFill( int positionX, int positionY, byte targetColor, byte replacementColor )
	{
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

		FloodFill( positionX + 1, positionY, targetColor, replacementColor );
		FloodFill( positionX - 1, positionY, targetColor, replacementColor );
		FloodFill( positionX, positionY + 1, targetColor, replacementColor );
		FloodFill( positionX, positionY - 1, targetColor, replacementColor );
	}

	private TimeSince _lastSpray;

	/// <summary>
	///  Spray random paint in a circle around the brush position
	/// </summary>
	/// <param name="brushPosition"></param>
	private void Spray( Vector2 brushPosition )
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

	private void Eraser( Vector2 brushPosition )
	{
		// DrawTexture.Update( BackgroundColor,
		// 	new Rect( brushPosition.x, brushPosition.y, BrushSize, BrushSize ) );
		// PushRectToByteData( new Rect( brushPosition.x, brushPosition.y, BrushSize, BrushSize ) );
		PushRectToBoth( new Rect( brushPosition.x, brushPosition.y, BrushSize, BrushSize ) );
	}

	private void Eyedropper( Vector2 brushPosition, MouseButtons mouseButton )
	{
		var index = DrawTextureData[(int)brushPosition.x + (int)brushPosition.y * DrawTexture.Width];
		if ( mouseButton == MouseButtons.Left )
		{
			LeftPaletteIndex = index;
		}
		else if ( mouseButton == MouseButtons.Right )
		{
			RightPaletteIndex = index;
		}
	}

	private void DrawLine( Vector2 startPos, Vector2 endPos )
	{
		DrawLineBetween( startPos, endPos );
	}

	private void LinePreview( Vector2 brushPosition )
	{
		ClearPreview();

		if ( !_mouseDownPosition.HasValue )
		{
			return;
		}

		DrawLineBetweenTex( PreviewTexture, PreviewColor, _mouseDownPosition.Value, brushPosition );
	}

	private void RectanglePreview( Vector2 brushPosition )
	{
		ClearPreview();

		if ( !_mouseDownPosition.HasValue )
		{
			return;
		}

		DrawRectangle( _mouseDownPosition.Value, brushPosition, true );
	}

	// draw outline of rectangle
	private void DrawRectangle( Vector2 mouseDownPosition, Vector2 mouseUpPosition, bool preview = false )
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

	private void CirclePreview( Vector2 brushPosition )
	{
		ClearPreview();

		if ( !_mouseDownPosition.HasValue )
		{
			return;
		}

		DrawCircle( _mouseDownPosition.Value, brushPosition, true );
	}

	private void DrawCircle( Vector2 mouseDownPosition, Vector2 mouseUpPosition, bool preview = false )
	{
		var x = Math.Min( mouseDownPosition.x, mouseUpPosition.x );
		var y = Math.Min( mouseDownPosition.y, mouseUpPosition.y );
		var width = Math.Abs( mouseDownPosition.x - mouseUpPosition.x );
		var height = Math.Abs( mouseDownPosition.y - mouseUpPosition.y );

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
}
