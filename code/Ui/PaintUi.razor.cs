using System;
using System.IO;
using System.Text;
using Clover.Items;
using Clover.Player;
using Clover.Utilities;
using Sandbox.UI;

namespace Clover.Ui;

public partial class PaintUi
{
	public struct DecalEntry
	{
		public Decals.DecalData Decal;
		public string ResourcePath;
	}

	public enum PaintTool
	{
		Pencil,
		Eraser,

		// Line,
		Fill,
		Spray
	}

	private PaintTool CurrentTool = PaintTool.Pencil;

	private List<DecalEntry> Decals = new();

	private Texture DrawTexture;
	private Texture GridTexture;

	private byte[] DrawTextureData;

	private Panel Window;
	private Panel CanvasContainer;
	private Panel Canvas;
	private Panel Grid;
	private Panel Crosshair;

	private string PaletteName = "windows-95-256-colours-1x";
	private List<Color32> Palette = new List<Color32>();

	private int LeftPaletteIndex = 0;
	private int RightPaletteIndex = 1;
	private int CurrentPaletteIndex = 0;

	private string CurrentFileName = "";
	private string CurrentName = "";

	private int TextureSize = 32;

	private int CanvasSize = 512;

	private int BrushSize = 1;

	private Stack<byte[]> UndoStack = new(30);
	private Stack<byte[]> RedoStack = new(30);

	private Color32 ForegroundColor => Palette.ElementAtOrDefault( LeftPaletteIndex );
	private Color32 BackgroundColor => Palette.ElementAtOrDefault( RightPaletteIndex );

	private Color GetCurrentColor()
	{
		return Palette[CurrentPaletteIndex];
	}

	protected override void OnStart()
	{
		base.OnStart();

		Palette = Utilities.Decals.GetPalette( PaletteName ).ToList();

		InitialiseTexture();

		ResetPaint();

		Panel.ButtonInput = PanelInputType.UI;

		Enabled = false;

		PopulateDecals();
	}

	private void ResetPaint()
	{
		CurrentTool = PaintTool.Pencil;
		CurrentPaletteIndex = 0;
		LeftPaletteIndex = 0;
		RightPaletteIndex = 1;
		CurrentFileName = "";
		CurrentName = "";
		BrushSize = 1;
		CanvasSize = 512;
		RedoStack.Clear();
		UndoStack.Clear();
		UpdateCanvas();
		Clear();
	}

	private void InitialiseTexture()
	{
		DrawTexture = Texture.Create( TextureSize, TextureSize ).WithDynamicUsage().Finish();
		DrawTextureData = new byte[TextureSize * TextureSize];
		Clear();

		UndoStack = new();

		// draw line grid with guide lines
		GridTexture = Texture.Create( 1024, 1024 ).Finish();
		var pixels = new Color32[1024 * 1024];
		for ( var x = 0; x < 1024; x++ )
		{
			for ( var y = 0; y < 1024; y++ )
			{
				var color = Color.Transparent;
				if ( x % 32 == 0 || y % 32 == 0 )
				{
					color = Color.Gray;
				}

				if ( x % 128 == 0 || y % 128 == 0 )
				{
					color = Color.White;
				}

				pixels[x + y * 1024] = color;
			}
		}

		GridTexture.Update( pixels );
	}

	private void PopulateDecals()
	{
		Decals.Clear();

		var files = FileSystem.Data.FindFile( "decals", "*.decal" );
		foreach ( var file in files )
		{
			Decals.DecalData decal;
			try
			{
				decal = Utilities.Decals.ReadDecal( $"decals/{file}" );
			}
			catch ( Exception e )
			{
				Log.Error( e.Message );
				continue;
			}

			Decals.Add( new DecalEntry { Decal = decal, ResourcePath = $"decals/{file}" } );
		}
	}

	private void LoadDecal( string path )
	{
		Log.Info( $"Loading decal {path}" );

		Decals.DecalData decal;

		try
		{
			decal = Utilities.Decals.ReadDecal( path );
		}
		catch ( Exception e )
		{
			Log.Error( e.Message );
			return;
		}

		CurrentName = decal.Name;
		PaletteName = decal.Palette;
		CurrentFileName = Path.GetFileNameWithoutExtension( path );
		DrawTexture.Update( decal.Texture.GetPixels(), 0, 0, decal.Width, decal.Height );
		DrawTextureData = decal.Image;
	}

	private void SetColor( PanelEvent ev, int index )
	{
		var e = ev as MousePanelEvent;
		Log.Info( $"Setting color to {index}" );
		if ( e.MouseButton == MouseButtons.Left )
		{
			LeftPaletteIndex = index;
		}
		else if ( e.MouseButton == MouseButtons.Right )
		{
			RightPaletteIndex = index;
		}
		else
		{
			Log.Info( "Unknown mouse button" );
		}
	}

	private void SetPalette( string name )
	{
		PaletteName = name;
		Palette = Utilities.Decals.GetPalette( PaletteName ).ToList();
		PushByteDataToTexture();
	}

	private bool _isDrawing;

	private Vector2 GetCurrentMousePixel()
	{
		if ( !Canvas.IsValid() )
		{
			return Vector2.Zero;
		}

		var mousePosition = Mouse.Position;
		var canvasPosition = Canvas.Box.Rect.Position;
		var canvasSize = Canvas.Box.Rect.Size;

		var mousePositionInCanvas = mousePosition - canvasPosition;
		var mousePositionInCanvasNormalized = mousePositionInCanvas / canvasSize;

		var x = (int)(mousePositionInCanvasNormalized.x * DrawTexture.Width);
		var y = (int)(mousePositionInCanvasNormalized.y * DrawTexture.Height);

		return new Vector2( x, y );
	}

	private bool IsMouseInsideCanvas()
	{
		var mousePosition = Mouse.Position;
		var canvasPosition = Canvas.Box.Rect.Position;
		var canvasSize = Canvas.Box.Rect.Size;

		return mousePosition.x >= canvasPosition.x && mousePosition.x <= canvasPosition.x + canvasSize.x &&
		       mousePosition.y >= canvasPosition.y && mousePosition.y <= canvasPosition.y + canvasSize.y;
	}

	protected override void OnUpdate()
	{
		// TODO: mouse inputs don't work in panels
		if ( Input.MouseWheel.x != 0 )
		{
			CanvasSize += (int)(Input.MouseWheel.x * 10);
			Canvas.Style.Width = CanvasSize;
			Canvas.Style.Height = CanvasSize;
		}

		if ( Input.Released( "PaintUndo" ) )
		{
			Undo();
			return;
		}
		else if ( Input.Released( "PaintRedo" ) )
		{
			Redo();
			return;
		}

		/*if ( !IsMouseInsideCanvas() )
		{
			return;
		}*/

		var mousePosition = GetCurrentMousePixel();

		var brushSizeOffset = BrushSize == 1 ? 0 : MathF.Ceiling( BrushSize / 2f );

		var brushPosition = new Vector2( mousePosition.x - brushSizeOffset, mousePosition.y - brushSizeOffset );

		DrawCrosshair( brushPosition );

		if ( _isDrawing )
		{
			if ( CurrentTool == PaintTool.Pencil )
			{
				Draw( brushPosition );
			}
			else if ( CurrentTool == PaintTool.Fill )
			{
				Fill( brushPosition );
			}
			else if ( CurrentTool == PaintTool.Spray )
			{
				Spray( brushPosition );
			}
			else if ( CurrentTool == PaintTool.Eraser )
			{
				DrawTexture.Update( BackgroundColor,
					new Rect( brushPosition.x, brushPosition.y, BrushSize, BrushSize ) );
				PushRectToByteData( new Rect( brushPosition.x, brushPosition.y, BrushSize, BrushSize ) );
			}
		}
	}

	private void DrawCrosshair( Vector2 brushPosition )
	{
		var texturePixelScreenSize = CanvasSize / TextureSize;

		var crosshairX = Canvas.Box.Left * Panel.ScaleFromScreen;
		crosshairX += texturePixelScreenSize * brushPosition.x;
		crosshairX += CanvasContainer.ScrollOffset.x * Panel.ScaleFromScreen;

		var crosshairY = Canvas.Box.Top * Panel.ScaleFromScreen;
		crosshairY += texturePixelScreenSize * brushPosition.y;
		crosshairY += CanvasContainer.ScrollOffset.y * Panel.ScaleFromScreen;

		var crosshairSize = texturePixelScreenSize * BrushSize;

		/*if ( CurrentTool == PaintTool.Fill )
		{
			crosshairSize = texturePixelScreenSize;
		}
		else if ( CurrentTool == PaintTool.Spray )
		{
			// crosshairSize = texturePixelScreenSize;
		}*/

		Crosshair.Style.Left = Length.Pixels( crosshairX );
		Crosshair.Style.Top = Length.Pixels( crosshairY );
		Crosshair.Style.Width = crosshairSize;
		Crosshair.Style.Height = crosshairSize;
	}

	private Vector2? _lastBrushPosition;

	private void Draw( Vector2 brushPosition )
	{
		var rect = new Rect( brushPosition.x, brushPosition.y, BrushSize, BrushSize );
		DrawTexture.Update( GetCurrentColor(), rect );

		PushRectToByteData( rect );

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

		DrawTextureData[positionX + positionY * DrawTexture.Width] = replacementColor;
		DrawTexture.Update( GetCurrentColor(), new Rect( positionX, positionY, 1, 1 ) );

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
					DrawTexture.Update( GetCurrentColor(), rect );
					PushRectToByteData( rect );
				}
			}
		}
	}

	private void PushByteDataToTexture()
	{
		Log.Info( DrawTextureData.Length );
		DrawTexture.Update( Utilities.Decals.ByteArrayToColor32( DrawTextureData, Palette.ToArray() ), 0, 0,
			DrawTexture.Width,
			DrawTexture.Height );
	}

	private void PushRectToByteData( Rect rect )
	{
		for ( var x = (int)rect.Left; x < rect.Left + rect.Width; x++ )
		{
			for ( var y = (int)rect.Top; y < rect.Top + rect.Height; y++ )
			{
				var index = x + y * DrawTexture.Width;
				if ( index < DrawTextureData.Length )
				{
					DrawTextureData[index] = (byte)CurrentPaletteIndex;
				}
				else
				{
					Log.Warning( $"Index {index} out of range" );
				}
			}
		}
	}

	/// <summary>
	///  Draw a line between two points using Bresenham's line algorithm
	/// </summary>
	/// <param name="lastBrushPosition"></param>
	/// <param name="brushPosition"></param>
	private void DrawLineBetween( Vector2 lastBrushPosition, Vector2 brushPosition )
	{
		var x0 = (int)lastBrushPosition.x;
		var y0 = (int)lastBrushPosition.y;
		var x1 = (int)brushPosition.x;
		var y1 = (int)brushPosition.y;

		var dx = Math.Abs( x1 - x0 );
		var dy = Math.Abs( y1 - y0 );

		var sx = x0 < x1 ? 1 : -1;
		var sy = y0 < y1 ? 1 : -1;

		var err = dx - dy;

		while ( true )
		{
			var rect = new Rect( x0, y0, BrushSize, BrushSize );
			DrawTexture.Update( GetCurrentColor(), rect );
			PushRectToByteData( rect );

			if ( x0 == x1 && y0 == y1 )
			{
				break;
			}

			var e2 = 2 * err;
			if ( e2 > -dy )
			{
				err -= dy;
				x0 += sx;
			}

			if ( e2 < dx )
			{
				err += dx;
				y0 += sy;
			}
		}
	}

	private void OnCanvasMouseDown( PanelEvent e )
	{
		PushUndo();

		if ( e is MousePanelEvent ev )
		{
			if ( ev.MouseButton == MouseButtons.Left )
			{
				CurrentPaletteIndex = LeftPaletteIndex;
				_isDrawing = true;
			}
			else if ( ev.MouseButton == MouseButtons.Right )
			{
				CurrentPaletteIndex = RightPaletteIndex;
				_isDrawing = true;
			}
			else
			{
				Log.Info( "Unknown mouse button" );
			}
		}

		Log.Info( "MouseDown" );

		RedoStack.Clear();

		e.StopPropagation();
	}

	private void OnCanvasMouseUp( PanelEvent e )
	{
		Log.Info( "MouseUp" );
		_isDrawing = false;
		_lastBrushPosition = null;
		// PushUndo();
	}

	private void Save()
	{
		if ( string.IsNullOrEmpty( CurrentFileName ) )
		{
			PlayerCharacter.Local.Notify( Notifications.NotificationType.Error, "No file name" );
			return;
		}

		if ( string.IsNullOrEmpty( CurrentName ) )
		{
			PlayerCharacter.Local.Notify( Notifications.NotificationType.Error, "No name" );
			return;
		}

		var data = new byte[64 + (32 * 32)];

		// var stream = new MemoryStream( data );
		var stream = FileSystem.Data.OpenWrite( $"decals/{CurrentFileName}.decal" );
		var writer = new BinaryWriter( stream, Encoding.UTF8 );

		writer.Write( 'C' );
		writer.Write( 'L' );
		writer.Write( 'P' );
		writer.Write( 'T' );
		// writer.Write( 0 );

		writer.Write( (int)2 ); // version

		writer.Write( DrawTexture.Width ); // width
		writer.Write( DrawTexture.Height ); // height

		// writer.Write( 0 );

		writer.Write( CurrentName ); // name, 16 chars

		// writer.Write( 0 );

		writer.Write( Game.SteamId ); // author

		writer.Write( PaletteName ); // palette name

		// writer.Seek( 64, SeekOrigin.Begin );

		/*var texturePixels = DrawTexture.GetPixels();

		for ( var i = 0; i < (32 * 32); i++ )
		{
			var paletteColor = GetClosestPaletteColor( texturePixels[i] );
			if ( paletteColor == -1 )
			{
				Log.Error( $"Color {texturePixels[i]} not found in palette" );
				paletteColor = 0;
			}

			writer.Write( (byte)paletteColor );
		}*/

		writer.Write( DrawTextureData );

		writer.Flush();

		stream.Close();

		PopulateDecals();

		Scene.RunEvent<IPaintEvent>( x => x.OnFileSaved( $"decals/{CurrentFileName}.decal" ) );
	}

	private int GetClosestPaletteColor( Color32 texturePixel )
	{
		var minDistance = float.MaxValue;
		var closestColor = -1;

		for ( var i = 0; i < Palette.Count; i++ )
		{
			var paletteColor = Palette[i];
			var distance =
				new Vector3( texturePixel.r, texturePixel.g, texturePixel.b ).Distance( new Vector3( paletteColor.r,
					paletteColor.g, paletteColor.b ) );
			if ( distance < minDistance )
			{
				minDistance = distance;
				closestColor = i;
			}
		}

		return closestColor;
	}

	private void Clear()
	{
		DrawTexture.Update( Palette[RightPaletteIndex], new Rect( 0, 0, DrawTexture.Width, DrawTexture.Height ) );
		DrawTextureData = Enumerable.Repeat( (byte)RightPaletteIndex, DrawTexture.Width * DrawTexture.Height )
			.ToArray();

		_lastBrushPosition = null;

		PushUndo();
	}

	private void ZoomIn()
	{
		CanvasSize *= 2;
		CanvasSize = CanvasSize.SnapToGrid( 32 );
		UpdateCanvas();
	}

	private void ZoomOut()
	{
		CanvasSize /= 2;
		CanvasSize = CanvasSize.SnapToGrid( 32 );
		UpdateCanvas();
	}

	private void ZoomReset()
	{
		CanvasSize = 512;
		UpdateCanvas();
	}

	private void SetBrushSize( int size )
	{
		BrushSize = size;
	}

	private void IncreaseBrushSize()
	{
		BrushSize++;
	}

	private void DecreaseBrushSize()
	{
		BrushSize--;
		if ( BrushSize < 1 )
		{
			BrushSize = 1;
		}
	}

	private void UpdateCanvas()
	{
		Canvas.Style.Width = CanvasSize;
		Canvas.Style.Height = CanvasSize;
		// Grid.Style.Width = CanvasSize;
		// Grid.Style.Height = CanvasSize;
	}

	private void Undo()
	{
		if ( UndoStack.Count == 0 )
		{
			Log.Info( "Nothing to undo" );
			return;
		}

		PushRedo();

		DrawTextureData = UndoStack.Pop();
		PushByteDataToTexture();
	}

	private void Redo()
	{
		if ( RedoStack.Count == 0 )
		{
			Log.Info( "Nothing to redo" );
			return;
		}

		DrawTextureData = RedoStack.Pop();
		PushByteDataToTexture();
	}

	private void PushUndo()
	{
		UndoStack.Push( DrawTextureData.ToArray() );
		Log.Info( $"Undo stack size: {UndoStack.Count}" );
		UndoStack.TrimExcess();
	}

	private void PushRedo()
	{
		RedoStack.Push( DrawTextureData.ToArray() );
		Log.Info( $"Redo stack size: {RedoStack.Count}" );
		RedoStack.TrimExcess();
	}
}

public interface IPaintEvent
{
	public void OnFileSaved( string path );
}
