﻿using System;
using System.IO;
using System.Text;
using Clover.Player;
using Clover.Ui.Tools;
using Clover.Utilities;
using Sandbox.Diagnostics;
using Sandbox.UI;

namespace Clover.Ui;

public partial class PaintUi
{
	public struct DecalEntry
	{
		public Decals.DecalData Decal;

		// public string ResourcePath;
		public string FileName;
	}

	// TODO: maybe don't hardcode this
	public enum PaintType
	{
		Decal = 1,
		Pumpkin = 2,
		Image = 3,
		Snowman = 4,
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
		Circle,

		Move,
		Clone,

		Dodge,
		Burn,
	}

	private PaintTool _previousTool = PaintTool.Pencil;

	private PaintTool _currentTool = PaintTool.Pencil;

	private PaintTool CurrentTool
	{
		get => _currentTool;
		set
		{
			_previousTool = _currentTool;
			_currentTool = value;
			_isDrawing = false;
			_isMoving = false;
			ClearPreview();
		}
	}

	private PaintType _currentPaintType = PaintType.Decal;

	private List<DecalEntry> Decals = new();
	private List<Texture> Images = new();

	private Texture DrawTexture;

	// private Texture GridTexture;
	private Texture PreviewTexture;

	private byte[] DrawTextureData;

	private Panel Window;
	private Panel CanvasContainer;
	private Panel CanvasSquare;
	private Panel CanvasImage;
	private Panel Grid;
	private Panel Crosshair;
	private Panel PreviewOverlay;

	private bool Monochrome = false;

	private string PaletteName = "windows-95-256-colours-1x";
	private List<Color32> Palette = new();

	private byte[] FavoriteColors = new byte[FavoriteColorAmount];

	private int LeftPaletteIndex = 0;
	private int RightPaletteIndex = 1;
	private int CurrentPaletteIndex = 0;

	private Decals.DecalData CurrentDecalData;
	private string CurrentFileName = "";
	private string CurrentName = "";

	// private int TextureSize = 32;
	private Vector2Int TextureSize = new(32, 32);

	private int BaseCanvasSize = 512;
	private float CanvasZoom = 10.0f;
	private float MinCanvasZoom = 0.1f;
	private float MaxCanvasZoom = 20f;

	// private int CanvasSize;

	private static int FavoriteColorAmount = 40;

	private bool ShowPalettes = false;
	private bool ShowFileActions = false;
	private bool ShowFavoritesEditor = false;

	private int SelectedFavorite = -1; // TODO: don't allow setting when -1

	private bool ShowGrid = true;

	private Vector2Int ClipboardSize;
	private byte[] ClipboardData;

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
		{ PaintTool.Circle, 1 },
		{ PaintTool.Move, 1 },
		{ PaintTool.Clone, 1 },
		{ PaintTool.Dodge, 1 },
		{ PaintTool.Burn, 1 },
	};

	private bool ShowBrushSizeForCurrentTool()
	{
		return CurrentTool is PaintTool.Pencil or PaintTool.Spray or PaintTool.Eraser or PaintTool.Burn
			or PaintTool.Dodge;
	}

	private int BrushSize
	{
		get => BrushSizes[CurrentTool];
		set => BrushSizes[CurrentTool] = value;
	}

	private Color32 ForegroundColor => Palette.ElementAtOrDefault( LeftPaletteIndex );
	private Color32 BackgroundColor => Palette.ElementAtOrDefault( RightPaletteIndex );

	private Color GetCurrentColor()
	{
		if ( CurrentPaletteIndex >= Palette.Count )
		{
			Log.Error( $"CurrentPaletteIndex {CurrentPaletteIndex} is out of bounds" );
			return default;
		}

		return Palette[CurrentPaletteIndex];
	}

	public void OpenPaint( PaintType type, int width, int height, bool monochrome )
	{
		if ( !ValidateTextureSize( width, height ) )
		{
			Log.Error( "Invalid texture size" );
			return;
		}

		_currentPaintType = type;
		TextureSize = new Vector2Int( width, height );
		Monochrome = monochrome;
		Enabled = true;

		if ( Monochrome )
		{
			SetPalette( "monochrome" );
		}
		else
		{
			SetPalette( "windows-95-256-colours-1x" );
		}

		InitialiseTexture();
		LoadFavoriteColors();
	}

	private static bool ValidateTextureSize( int width, int height )
	{
		if ( width < 1 || height < 1 )
		{
			return false;
		}

		if ( width > 1024 || height > 1024 )
		{
			return false;
		}

		// check if it's a power of 2 (thanks copilot)
		if ( (width & (width - 1)) != 0 || (height & (height - 1)) != 0 )
		{
			return false;
		}

		return true;
	}

	protected override void OnStart()
	{
		base.OnStart();

		Palette = Utilities.Decals.GetPalette( PaletteName ).ToList();

		InitialiseTexture();

		Panel.ButtonInput = PanelInputType.UI;

		Enabled = false;

		PopulateDecals();
		PopulateImages();

		LoadFavoriteColors();
	}

	private void ResetPaint()
	{
		CurrentDecalData = new Decals.DecalData();
		CurrentFileName = "";
		CurrentName = "";
		ZoomReset();

		_isDrawing = false;
		_isMoving = false;

		RedoStack.Clear();
		UndoStack.Clear();

		ClearTexture();
	}

	/*private void ResetDecalPaint()
	{
		Log.Info( "[Paint] Reset" );

		// PaletteName = "windows-95-256-colours-1x";
		// Palette = Utilities.Decals.GetPalette( PaletteName ).ToList();
		SetPalette( "windows-95-256-colours-1x" );

		LeftPaletteIndex = Utilities.Decals.GetClosestPaletteColor( Palette.ToArray(), Color.Black );
		RightPaletteIndex = Utilities.Decals.GetClosestPaletteColor( Palette.ToArray(), Color.White );
		CurrentPaletteIndex = LeftPaletteIndex;

		UpdateCanvas();
		Clear();
	}

	private void ResetPumpkinPaint()
	{
		Log.Info( "[Paint] Reset" );

		// PaletteName = "monochrome";
		// Palette = Utilities.Decals.GetPalette( PaletteName ).ToList();
		SetPalette( "monochrome" );

		LeftPaletteIndex = 0;
		RightPaletteIndex = 1;
		CurrentPaletteIndex = LeftPaletteIndex;

		UpdateCanvas();
		Clear();
	}*/

	private void New()
	{
		Log.Info( "[Paint] New" );

		ResetPaint();
	}

	private void InitialiseTexture()
	{
		Log.Info( "[Paint] Initialising texture" );

		DrawTexture = Texture.Create( TextureSize.x, TextureSize.y ).WithDynamicUsage().Finish();
		DrawTextureData = new byte[TextureSize.x * TextureSize.y];
		ClearTexture();

		UndoStack = new();

		/*// draw line grid with guide lines
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

		GridTexture.Update( pixels );*/

		// draw preview overlay
		PreviewTexture = Texture.Create( TextureSize.x, TextureSize.y ).WithDynamicUsage().Finish();
	}


	private void SetColor( PanelEvent ev, int index )
	{
		if ( ev is not MousePanelEvent e )
		{
			Log.Warning( "Event is not MousePanelEvent" );
			return;
		}

		Log.Info( $"Setting color to {index}" );

		if ( e.MouseButton == MouseButtons.Left )
		{
			Log.Info( $"Setting left color to {index}" );
			LeftPaletteIndex = index;
		}
		else if ( e.MouseButton == MouseButtons.Right )
		{
			Log.Info( $"Setting right color to {index}" );
			RightPaletteIndex = index;
		}
		else
		{
			Log.Warning( "Unknown mouse button" );
		}
	}

	private void SetPalette( string name )
	{
		PaletteName = name;
		Palette = Utilities.Decals.GetPalette( PaletteName ).ToList();

		LeftPaletteIndex = Utilities.Decals.GetClosestPaletteColor( Palette.ToArray(), Color.Black );
		RightPaletteIndex = Utilities.Decals.GetClosestPaletteColor( Palette.ToArray(), Color.White );
		CurrentPaletteIndex = LeftPaletteIndex;

		if ( DrawTextureData != null ) PushByteDataToTexture();
		LoadFavoriteColors();
	}

	private bool _isDrawing;

	private Vector2Int GetCurrentMousePixel()
	{
		if ( !CanvasImage.IsValid() )
		{
			return Vector2Int.Zero;
		}

		var mousePosition = Mouse.Position;
		var canvasPosition = CanvasImage.Box.Rect.Position;
		var canvasSize = CanvasImage.Box.Rect.Size;

		var mousePositionInCanvas = mousePosition - canvasPosition;
		var mousePositionInCanvasNormalized = mousePositionInCanvas / canvasSize;

		var x = (int)(mousePositionInCanvasNormalized.x * DrawTexture.Width);
		var y = (int)(mousePositionInCanvasNormalized.y * DrawTexture.Height);

		return new Vector2Int( x, y );
	}

	public Vector2Int GetCurrentBrushPosition()
	{
		var mousePosition = GetCurrentMousePixel();

		var brushSizeOffset = BrushSize == 1 ? 0 : MathF.Ceiling( BrushSize / 2f );

		var brushPosition = new Vector2Int( (int)Math.Round( mousePosition.x - brushSizeOffset ),
			(int)Math.Round( mousePosition.y - brushSizeOffset ) );
		return brushPosition;
	}

	private bool IsMouseInsideCanvas()
	{
		var mousePosition = Mouse.Position;
		var canvasPosition = CanvasImage.Box.Rect.Position;
		var canvasSize = CanvasImage.Box.Rect.Size;

		return mousePosition.x >= canvasPosition.x && mousePosition.x <= canvasPosition.x + canvasSize.x &&
		       mousePosition.y >= canvasPosition.y && mousePosition.y <= canvasPosition.y + canvasSize.y;
	}

	private bool IsMouseInsideCanvasContainer()
	{
		var mousePosition = Mouse.Position;
		var canvasPosition = CanvasContainer.Box.Rect.Position;
		var canvasSize = CanvasContainer.Box.Rect.Size;

		return mousePosition.x >= canvasPosition.x && mousePosition.x <= canvasPosition.x + canvasSize.x &&
		       mousePosition.y >= canvasPosition.y && mousePosition.y <= canvasPosition.y + canvasSize.y;
	}


	protected override void OnUpdate()
	{
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

		var brushPosition = GetCurrentBrushPosition();

		DrawCrosshair( brushPosition );

		// PreviewTexture.Update( Color.Red, brushPosition.x, brushPosition.y );

		if ( (CurrentTool == PaintTool.Move || CurrentTool == PaintTool.Clone) && _isMoving )
		{
			// Log.Info( "Moving" );
			ClearPreview();
			PasteClipboardToPreview( brushPosition );
		}

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
			else if ( CurrentTool == PaintTool.Rectangle || CurrentTool == PaintTool.Move ||
			          CurrentTool == PaintTool.Clone )
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
		if ( !CanvasImage.IsValid() )
		{
			Log.Warning( "CanvasImage is not valid in DrawCrosshair" );
			return;
		}

		// var texturePixelScreenSize = CanvasSize / TextureSize.y;
		var texturePixelScreenSize = DrawTexture.Width * CanvasZoom / TextureSize.x;

		var crosshairRound = false;

		var crosshairX = CanvasImage.Box.Left * Panel.ScaleFromScreen;
		crosshairX += texturePixelScreenSize * brushPosition.x;
		crosshairX += CanvasContainer.ScrollOffset.x * Panel.ScaleFromScreen;
		crosshairX -= CanvasContainer.Box.Left * Panel.ScaleFromScreen;

		var crosshairY = CanvasImage.Box.Top * Panel.ScaleFromScreen;
		crosshairY += texturePixelScreenSize * brushPosition.y;
		crosshairY += CanvasContainer.ScrollOffset.y * Panel.ScaleFromScreen;
		crosshairY -= CanvasContainer.Box.Top * Panel.ScaleFromScreen;

		var crosshairSize = texturePixelScreenSize * BrushSize;

		if ( CurrentTool == PaintTool.Fill )
		{
			// crosshairSize = texturePixelScreenSize;
		}
		else if ( CurrentTool == PaintTool.Spray )
		{
			crosshairSize *= 6;
			crosshairX -= crosshairSize / 2f;
			crosshairY -= crosshairSize / 2f;
			crosshairRound = true;
		}


		Crosshair.Style.Left = Length.Pixels( (int)crosshairX );
		Crosshair.Style.Top = Length.Pixels( (int)crosshairY );
		Crosshair.Style.Width = crosshairSize;
		Crosshair.Style.Height = crosshairSize;


		Crosshair.Style.BorderTopLeftRadius = crosshairRound ? Length.Percent( 100 ) : Length.Pixels( 0 );
		Crosshair.Style.BorderTopRightRadius = crosshairRound ? Length.Percent( 100 ) : Length.Pixels( 0 );
		Crosshair.Style.BorderBottomLeftRadius = crosshairRound ? Length.Percent( 100 ) : Length.Pixels( 0 );
		Crosshair.Style.BorderBottomRightRadius = crosshairRound ? Length.Percent( 100 ) : Length.Pixels( 0 );
	}

	public void PushRectToBoth( Rect rect, int colorIndex = -1 )
	{
		PushRectToByteData( rect, colorIndex );
		PushRectToTexture( rect, colorIndex );
	}

	private void PushByteDataToTexture()
	{
		Assert.NotNull( DrawTextureData, "DrawTextureData is null" );
		Assert.NotNull( Palette, "Palette is null" );
		Assert.True( Palette.Count > 0, "Palette is empty" );
		Assert.True( DrawTextureData.Length > 0, "DrawTextureData is empty" );
		Assert.True( DrawTextureData.Length == DrawTexture.Width * DrawTexture.Height,
			"DrawTextureData length does not match texture size" );

		var data = Utilities.Decals.ByteArrayToColor32( DrawTextureData, Palette.ToArray() );

		DrawTexture.Update( data, 0, 0,
			DrawTexture.Width,
			DrawTexture.Height );
	}

	private void PushRectToTexture( Rect rect, int colorIndex = -1 )
	{
		DrawTexture.Update( colorIndex == -1 ? GetCurrentColor() : Palette[colorIndex], rect );
	}

	private void PushRectToByteData( Rect rect, int colorIndex = -1 )
	{
		for ( var x = (int)rect.Left; x < rect.Left + rect.Width; x++ )
		{
			for ( var y = (int)rect.Top; y < rect.Top + rect.Height; y++ )
			{
				var index = x + y * DrawTexture.Width;
				if ( index >= 0 && index < DrawTextureData.Length )
				{
					DrawTextureData[index] = (byte)(colorIndex == -1 ? CurrentPaletteIndex : colorIndex);
				}
			}
		}
	}

	/// <summary>
	///  Draw a line between two points using Bresenham's line algorithm
	/// </summary>
	/// <param name="lastBrushPosition"></param>
	/// <param name="brushPosition"></param>
	private void DrawLineBetween( Vector2Int lastBrushPosition, Vector2Int brushPosition )
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
	private void DrawLineBetweenTex( Texture tex, Color32 col, Vector2Int lastBrushPosition, Vector2Int brushPosition )
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


	private Vector2Int? _mouseDownPosition;
	private Vector2Int? _mouseUpPosition;

	private bool _isMoving;

	private void SetClipboard( Vector2Int start, Vector2Int end )
	{
		Log.Info( $"Setting clipboard at {start} to {end}" );

		var x = Math.Min( start.x, end.x );
		var y = Math.Min( start.y, end.y );
		var width = Math.Abs( start.x - end.x );
		var height = Math.Abs( start.y - end.y );

		ClipboardSize = new Vector2Int( width, height );
		ClipboardData = new byte[width * height];

		for ( var i = 0; i < width; i++ )
		{
			for ( var j = 0; j < height; j++ )
			{
				var index = x + i + (y + j) * DrawTexture.Width;
				if ( index >= 0 && index < DrawTextureData.Length )
				{
					ClipboardData[i + j * width] = DrawTextureData[index];
				}
			}
		}
	}

	private void PasteClipboardToPreview( Vector2Int position )
	{
		// Log.Info( $"Pasting clipboard to preview at {position}, size {ClipboardSize}" );

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

		var colors = new Color32[width * height];

		for ( var i = 0; i < width; i++ )
		{
			for ( var j = 0; j < height; j++ )
			{
				var index = i + j * width;
				colors[index] = Palette[ClipboardData[i + j * ClipboardSize.x]];
			}
		}

		PreviewTexture.Update( colors, x, y, width, height );
	}

	private void ClearTexture()
	{
		if ( DrawTexture.IsValid() && DrawTextureData != null )
		{
			if ( RightPaletteIndex < Palette.Count )
			{
				DrawTexture.Update( Palette[RightPaletteIndex],
					new Rect( 0, 0, DrawTexture.Width, DrawTexture.Height ) );
				DrawTextureData = Enumerable.Repeat( (byte)RightPaletteIndex, DrawTexture.Width * DrawTexture.Height )
					.ToArray();
			}
			else
			{
				Log.Warning( "Right palette index is out of bounds" );
			}
		}
		else
		{
			Log.Warning( "DrawTexture/DrawTextureData is not valid" );
		}

		ClearPreview();

		_lastBrushPosition = null;

		PushUndo();
	}

	private void ClearPreview()
	{
		PreviewTexture?.Update( Color32.Transparent, new Rect( 0, 0, PreviewTexture.Width, PreviewTexture.Height ) );
	}

	private void Zoom( float value, Vector2 target = default )
	{
		CanvasZoom += value;
		CanvasZoom = Math.Clamp( CanvasZoom, MinCanvasZoom, MaxCanvasZoom );

		// CanvasSize = (int)(BaseCanvasSize * CanvasZoom);

		Log.Info( $"Zooming to {CanvasZoom}" );

		// CanvasSize = (int)Math.Round( (BaseCanvasSize * CanvasZoom).SnapToGrid( 32 ) );

		UpdateCanvas();
	}

	private void ZoomIn()
	{
		Zoom( 0.2f );
	}

	private void ZoomOut()
	{
		Zoom( -0.2f );
	}

	private void ZoomReset()
	{
		// CanvasSize = 512;
		CanvasZoom = 10.0f;
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
		// CanvasImage.Style.Width = Length.Pixels( CanvasSize );
		// CanvasImage.Style.Height = Length.Pixels( CanvasSize );

		if ( !CanvasImage.IsValid() )
		{
			Log.Warning( "CanvasImage is not valid" );

			Task.Frame().ContinueWith( _ =>
			{
				if ( !CanvasImage.IsValid() )
				{
					Log.Warning( "CanvasImage is still not valid" );
					return;
				}

				UpdateCanvas();
			} );

			return;
		}

		CanvasImage.Style.Width = Length.Pixels( DrawTexture.Width * CanvasZoom );
		CanvasImage.Style.Height = Length.Pixels( DrawTexture.Height * CanvasZoom );
	}

	private Color GetColorFromByte( byte index )
	{
		if ( index >= Palette.Count )
		{
			Log.Error( $"Index {index} is out of bounds" );
			return Color.Transparent;
		}

		return Palette[index];
	}

	private Color GetColorFromByte( int index )
	{
		if ( index >= Palette.Count )
		{
			Log.Error( $"Index {index} is out of bounds" );
			return Color.Transparent;
		}

		return Palette[index];
	}


	protected override int BuildHash()
	{
		return HashCode.Combine( CurrentTool );
	}

	private static ReadOnlySpan<Color32> ResizeTexture( Color32[] sourcePixels, Vector2Int sourceSize,
		Vector2Int destinationSize )
	{
		if ( sourceSize == destinationSize )
		{
			return sourcePixels;
		}

		// Old square code
		/*var destinationPixels = new Color32[destinationSize * destinationSize];

		var ratio = (float)destinationSize / sourceSize;

		for ( var y = 0; y < destinationSize; y++ )
		{
			for ( var x = 0; x < destinationSize; x++ )
			{
				var sourceX = (int)(x / ratio);
				var sourceY = (int)(y / ratio);

				var sourceIndex = sourceX + sourceY * sourceSize;
				var destinationIndex = x + y * destinationSize;

				destinationPixels[destinationIndex] = sourcePixels[sourceIndex];
			}
		}*/

		var destinationPixels = new Color32[destinationSize.x * destinationSize.y];

		var ratioX = (float)destinationSize.x / sourceSize.x;
		var ratioY = (float)destinationSize.y / sourceSize.y;

		for ( var y = 0; y < destinationSize.y; y++ )
		{
			for ( var x = 0; x < destinationSize.x; x++ )
			{
				var sourceX = (int)(x / ratioX);
				var sourceY = (int)(y / ratioY);

				var sourceIndex = sourceX + sourceY * sourceSize.x;
				var destinationIndex = x + y * destinationSize.x;

				destinationPixels[destinationIndex] = sourcePixels[sourceIndex];
			}
		}


		return destinationPixels;
	}
}

public interface IPaintEvent
{
	public void OnFileSaved( string path );
}
