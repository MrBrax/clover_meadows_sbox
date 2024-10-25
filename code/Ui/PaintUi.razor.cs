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
		Spray,
		Eyedropper,

		Line,
		Rectangle,
		Circle
	}

	private PaintTool CurrentTool = PaintTool.Pencil;

	private List<DecalEntry> Decals = new();
	private List<Texture> Images = new();

	private Texture DrawTexture;
	private Texture GridTexture;
	private Texture PreviewTexture;

	private byte[] DrawTextureData;

	private Panel Window;
	private Panel CanvasContainer;
	private Panel Canvas;
	private Panel Grid;
	private Panel Crosshair;
	private Panel PreviewOverlay;

	private string PaletteName = "windows-95-256-colours-1x";
	private List<Color32> Palette = new List<Color32>();

	private int LeftPaletteIndex = 0;
	private int RightPaletteIndex = 1;
	private int CurrentPaletteIndex = 0;

	private string CurrentFileName = "";
	private string CurrentName = "";

	private int TextureSize = 32;

	private int CanvasSize = 512;

	// private Color PreviewColor = Color.Red;
	private Color PreviewColor => GetCurrentColor();

	// private int BrushSize = 1;

	private Dictionary<PaintTool, int> BrushSizes = new()
	{
		{ PaintTool.Pencil, 1 },
		{ PaintTool.Eraser, 1 },
		{ PaintTool.Fill, 1 },
		{ PaintTool.Spray, 1 },
		{ PaintTool.Eyedropper, 1 },
		{ PaintTool.Line, 1 },
		{ PaintTool.Rectangle, 1 },
		{ PaintTool.Circle, 1 }
	};

	private int BrushSize
	{
		get => BrushSizes[CurrentTool];
		set => BrushSizes[CurrentTool] = value;
	}

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
		PopulateImages();
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

	private void New()
	{
		ResetPaint();
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

		// draw preview overlay
		PreviewTexture = Texture.Create( TextureSize, TextureSize ).WithDynamicUsage().Finish();
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

	private void PopulateImages()
	{
		Images.Clear();

		var files = FileSystem.Data.FindFile( "decals", "*.png" );
		foreach ( var file in files )
		{
			var texture = Texture.Load( FileSystem.Data, $"decals/{file}" );
			Images.Add( texture );
		}

		Log.Info( $"Loaded {Images.Count} images" );
	}

	private void LoadImage( Texture texture )
	{
		// resize image to 32x32
		var resizedTexture = Texture.Create( TextureSize, TextureSize ).WithDynamicUsage().Finish();
		resizedTexture.Update( texture.GetPixels(), 0, 0, TextureSize, TextureSize );

		// pick best fitting colors
		var pixels = resizedTexture.GetPixels();

		var newBytes = new byte[TextureSize * TextureSize];

		for ( var i = 0; i < pixels.Length; i++ )
		{
			var pixel = pixels[i];
			var closestColor = Utilities.Decals.GetClosestPaletteColor( Palette.ToArray(), pixel );
			// pixels[i] = Palette[closestColor];
			newBytes[i] = (byte)closestColor;
		}

		DrawTextureData = newBytes;
		PushByteDataToTexture();
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

	private Vector2Int GetCurrentMousePixel()
	{
		if ( !Canvas.IsValid() )
		{
			return Vector2Int.Zero;
		}

		var mousePosition = Mouse.Position;
		var canvasPosition = Canvas.Box.Rect.Position;
		var canvasSize = Canvas.Box.Rect.Size;

		var mousePositionInCanvas = mousePosition - canvasPosition;
		var mousePositionInCanvasNormalized = mousePositionInCanvas / canvasSize;

		var x = (int)(mousePositionInCanvasNormalized.x * DrawTexture.Width);
		var y = (int)(mousePositionInCanvasNormalized.y * DrawTexture.Height);

		return new Vector2Int( x, y );
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
		else if ( Input.Released( "PaintPencil" ) )
		{
			CurrentTool = PaintTool.Pencil;
		}
		/*else if ( Input.Released( "PaintEraser" ) )
		{
			CurrentTool = PaintTool.Eraser;
		}
		else if ( Input.Released( "PaintFill" ) )
		{
			CurrentTool = PaintTool.Fill;
		}
		else if ( Input.Released( "PaintSpray" ) )
		{
			CurrentTool = PaintTool.Spray;
		}*/
		else if ( Input.Released( "PaintEyedropper" ) )
		{
			CurrentTool = PaintTool.Eyedropper;
		}

		/*if ( !IsMouseInsideCanvas() )
		{
			return;
		}*/

		var mousePosition = GetCurrentMousePixel();

		var brushSizeOffset = BrushSize == 1 ? 0 : MathF.Ceiling( BrushSize / 2f );

		var brushPosition = new Vector2Int( (int)Math.Round( mousePosition.x - brushSizeOffset ),
			(int)Math.Round( mousePosition.y - brushSizeOffset ) );

		DrawCrosshair( brushPosition );

		// PreviewTexture.Update( Color.Red, brushPosition.x, brushPosition.y );

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
				Eraser( brushPosition );
			}
			else if ( CurrentTool == PaintTool.Line )
			{
				LinePreview( brushPosition );
			}
			else if ( CurrentTool == PaintTool.Rectangle )
			{
				RectanglePreview( brushPosition );
			}
			else if ( CurrentTool == PaintTool.Circle )
			{
				CirclePreview( brushPosition );
			}
		}
	}

	private void DrawCrosshair( Vector2Int brushPosition )
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

	private void PushRectToBoth( Rect rect )
	{
		PushRectToByteData( rect );
		PushRectToTexture( rect );
	}

	private void PushByteDataToTexture()
	{
		DrawTexture.Update( Utilities.Decals.ByteArrayToColor32( DrawTextureData, Palette.ToArray() ), 0, 0,
			DrawTexture.Width,
			DrawTexture.Height );
	}

	private void PushRectToTexture( Rect rect )
	{
		DrawTexture.Update( GetCurrentColor(), rect );
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
			// DrawTexture.Update( GetCurrentColor(), rect );
			// PushRectToByteData( rect );
			PushRectToBoth( rect );

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

	// TODO: this is a duplicate of DrawLineBetween
	private void DrawLineBetweenTex( Texture tex, Color32 col, Vector2 lastBrushPosition, Vector2 brushPosition )
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
			tex.Update( col, x0, y0 );

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


	private Vector2? _mouseDownPosition;
	private Vector2? _mouseUpPosition;

	private void OnCanvasMouseDown( PanelEvent e )
	{
		PushUndo();

		if ( e is MousePanelEvent ev )
		{
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
		Log.Info( "MouseUp" );
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
		}

		ClearPreview();

		_mouseDownPosition = null;
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

		var stream = FileSystem.Data.OpenWrite( $"decals/{CurrentFileName}.decal" );
		var writer = new BinaryWriter( stream, Encoding.UTF8 );

		writer.Write( 'C' );
		writer.Write( 'L' );
		writer.Write( 'P' );
		writer.Write( 'T' );

		writer.Write( (int)2 ); // version

		writer.Write( DrawTexture.Width ); // width
		writer.Write( DrawTexture.Height ); // height

		writer.Write( CurrentName ); // name, 16 chars

		writer.Write( Game.SteamId ); // author

		writer.Write( PaletteName ); // palette name

		writer.Write( DrawTextureData );

		writer.Flush();

		stream.Close();

		PopulateDecals();

		Scene.RunEvent<IPaintEvent>( x => x.OnFileSaved( $"decals/{CurrentFileName}.decal" ) );
	}


	private void Clear()
	{
		DrawTexture.Update( Palette[RightPaletteIndex], new Rect( 0, 0, DrawTexture.Width, DrawTexture.Height ) );
		DrawTextureData = Enumerable.Repeat( (byte)RightPaletteIndex, DrawTexture.Width * DrawTexture.Height )
			.ToArray();

		ClearPreview();

		_lastBrushPosition = null;

		PushUndo();
	}

	private void ClearPreview()
	{
		PreviewTexture.Update( Color32.Transparent, new Rect( 0, 0, PreviewTexture.Width, PreviewTexture.Height ) );
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


	protected override int BuildHash()
	{
		return HashCode.Combine( CurrentTool );
	}
}

public interface IPaintEvent
{
	public void OnFileSaved( string path );
}
