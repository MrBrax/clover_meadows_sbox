using System;
using System.IO;
using System.Text;
using Clover.Player;
using Clover.Ui.Tools;
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
		Circle,

		Move,
		Clone,

		Dodge,
		Burn,
	}

	private PaintTool _currentTool = PaintTool.Pencil;

	private PaintTool CurrentTool
	{
		get => _currentTool;
		set
		{
			_currentTool = value;
			_isDrawing = false;
			_isMoving = false;
			ClearPreview();
		}
	}

	private BasePaintTool PaintToolClass;

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
	private List<Color32> Palette = new();

	private byte[] FavoriteColors = new byte[FavoriteColorAmount];

	private int LeftPaletteIndex = 0;
	private int RightPaletteIndex = 1;
	private int CurrentPaletteIndex = 0;

	private Decals.DecalData CurrentDecalData;
	private string CurrentFileName = "";
	private string CurrentName = "";

	private int TextureSize = 32;

	private int BaseCanvasSize = 512;
	private float CanvasZoom = 1.0f;
	private float MinCanvasZoom = 0.1f;
	private float MaxCanvasZoom = 20.0f;

	private int CanvasSize;

	private static int FavoriteColorAmount = 40;

	private bool ShowPalettes = false;
	private bool ShowFileActions = false;
	private bool ShowFavoritesEditor = false;

	private int SelectedFavorite = -1;


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
		return Palette[CurrentPaletteIndex];
	}

	protected override void OnStart()
	{
		base.OnStart();

		PaintToolClass = new DrawTool( this );

		Palette = Utilities.Decals.GetPalette( PaletteName ).ToList();

		InitialiseTexture();

		ResetPaint();

		Panel.ButtonInput = PanelInputType.UI;

		Enabled = false;

		PopulateDecals();
		PopulateImages();

		LoadFavoriteColors();
	}

	private void ResetPaint()
	{
		Log.Info( "[Paint] Reset" );
		// CurrentTool = PaintTool.Pencil;

		LeftPaletteIndex = Utilities.Decals.GetClosestPaletteColor( Palette.ToArray(), Color.Black );
		RightPaletteIndex = Utilities.Decals.GetClosestPaletteColor( Palette.ToArray(), Color.White );
		CurrentPaletteIndex = LeftPaletteIndex;

		CurrentDecalData = new Decals.DecalData();
		CurrentFileName = "";
		CurrentName = "";
		// BrushSize = 1;
		CanvasZoom = 1.0f;

		_isDrawing = false;
		_isMoving = false;

		RedoStack.Clear();
		UndoStack.Clear();
		UpdateCanvas();
		Clear();
	}

	private void New()
	{
		Log.Info( "[Paint] New" );
		ResetPaint();
	}

	private void InitialiseTexture()
	{
		Log.Info( "[Paint] Initialising texture" );

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
		CurrentDecalData = decal;
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
		Log.Info( $"Loading image {texture.ResourcePath}, {texture.Width}x{texture.Height}" );

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
		LoadFavoriteColors();
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
		var canvasPosition = Canvas.Box.Rect.Position;
		var canvasSize = Canvas.Box.Rect.Size;

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
		// TODO: mouse inputs don't work in panels
		/*if ( Input.MouseWheel.x != 0 )
		{
			CanvasSize += (int)(Input.MouseWheel.x * 10);
			Canvas.Style.Width = CanvasSize;
			Canvas.Style.Height = CanvasSize;
		}*/

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

		if ( PaintToolClass != null )
		{
			PaintToolClass.OnUpdate();
		}

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
		var texturePixelScreenSize = CanvasSize / TextureSize;

		var crosshairRound = false;

		var crosshairX = Canvas.Box.Left * Panel.ScaleFromScreen;
		crosshairX += texturePixelScreenSize * brushPosition.x;
		crosshairX += CanvasContainer.ScrollOffset.x * Panel.ScaleFromScreen;

		var crosshairY = Canvas.Box.Top * Panel.ScaleFromScreen;
		crosshairY += texturePixelScreenSize * brushPosition.y;
		crosshairY += CanvasContainer.ScrollOffset.y * Panel.ScaleFromScreen;

		var crosshairSize = texturePixelScreenSize * BrushSize; // 

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
		DrawTexture.Update( Utilities.Decals.ByteArrayToColor32( DrawTextureData, Palette.ToArray() ), 0, 0,
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

		Stream stream;

		try
		{
			stream = FileSystem.Data.OpenWrite( $"decals/{CurrentFileName}.decal" );
		}
		catch ( Exception e )
		{
			Log.Error( e.Message );
			PlayerCharacter.Local.Notify( Notifications.NotificationType.Error, "Failed to save file" );
			return;
		}

		Utilities.Decals.WriteDecal( stream,
			new Decals.DecalData
			{
				Width = DrawTexture.Width,
				Height = DrawTexture.Height,
				Name = CurrentName,
				Palette = PaletteName,
				Image = DrawTextureData,
				Author = (ulong)Game.SteamId,
				AuthorName = Connection.Local.DisplayName,
				Created = CurrentDecalData.Created == DateTime.MinValue ? DateTime.Now : CurrentDecalData.Created,
				Modified = DateTime.Now,
			} );

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
		PreviewTexture?.Update( Color32.Transparent, new Rect( 0, 0, PreviewTexture.Width, PreviewTexture.Height ) );
	}

	private void Zoom( float value )
	{
		Log.Info( $"Zooming {value}" );
		CanvasZoom += value;
		CanvasZoom = Math.Clamp( CanvasZoom, MinCanvasZoom, MaxCanvasZoom );

		// CanvasSize = (int)(BaseCanvasSize * CanvasZoom);

		CanvasSize = (int)Math.Round( (BaseCanvasSize * CanvasZoom).SnapToGrid( 32 ) );

		UpdateCanvas();
	}

	private void ZoomIn()
	{
		// CanvasSize *= 2;
		// CanvasSize = CanvasSize.SnapToGrid( 32 );
		// CanvasZoom *= 2;
		// CanvasZoom = Math.Clamp( CanvasZoom, MinCanvasZoom, MaxCanvasZoom );
		// UpdateCanvas();
		Zoom( 0.1f );
	}

	private void ZoomOut()
	{
		// CanvasSize /= 2;
		// CanvasSize = CanvasSize.SnapToGrid( 32 );
		// CanvasZoom /= 2;
		// CanvasZoom = Math.Clamp( CanvasZoom, MinCanvasZoom, MaxCanvasZoom );
		// UpdateCanvas();
		Zoom( -0.1f );
	}

	private void ZoomReset()
	{
		// CanvasSize = 512;
		CanvasZoom = 1.0f;
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
		// var size = BaseCanvasSize * CanvasZoom;

		Canvas.Style.Width = Length.Pixels( CanvasSize );
		Canvas.Style.Height = Length.Pixels( CanvasSize );

		Log.Info( $"Canvas size: {Canvas.Style.Width}" );

		// Grid.Style.Width = CanvasSize;
		// Grid.Style.Height = CanvasSize;
	}

	private Color GetColorFromByte( byte index )
	{
		return Palette[index];
	}

	private Color GetColorFromByte( int index )
	{
		return Palette[index];
	}

	private void ToggleShowFavoritesEditor()
	{
		ShowFavoritesEditor = !ShowFavoritesEditor;
		if ( !ShowFavoritesEditor )
		{
			SaveFavoriteColors();
		}
	}

	private void LoadFavoriteColors()
	{
		var path = $"colorfavorites-{PaletteName}.dat";
		if ( !FileSystem.Data.FileExists( path ) )
		{
			GenerateFavoriteColors();
			return;
		}

		try
		{
			var stream = FileSystem.Data.OpenRead( path );
			var reader = new BinaryReader( stream, Encoding.UTF8 );

			FavoriteColors = reader.ReadBytes( FavoriteColorAmount );

			stream.Close();
		}
		catch ( Exception e )
		{
			Log.Error( e.Message );
		}
	}

	private void SaveFavoriteColors()
	{
		var stream = FileSystem.Data.OpenWrite( $"colorfavorites-{PaletteName}.dat" );
		var writer = new BinaryWriter( stream, Encoding.UTF8 );

		writer.Write( FavoriteColors );

		writer.Flush();

		stream.Close();
	}

	private void GenerateFavoriteColors()
	{
		FavoriteColors = new byte[FavoriteColorAmount];
		for ( var i = 0; i < FavoriteColorAmount; i++ )
		{
			FavoriteColors[i] = (byte)i;
		}

		FavoriteColors[0] = (byte)Utilities.Decals.GetClosestPaletteColor( Palette.ToArray(), Color.Black );
		FavoriteColors[1] = (byte)Utilities.Decals.GetClosestPaletteColor( Palette.ToArray(), Color.White );
		FavoriteColors[2] = 255;
	}

	private void FavoriteEditorColorButtonClick( PanelEvent e, int colorIndex )
	{
		FavoriteColors[SelectedFavorite] = (byte)colorIndex;
	}

	private void FavoriteColorButtonClick( PanelEvent e, int index )
	{
		if ( ShowFavoritesEditor )
		{
			SelectedFavorite = index;
		}
		else
		{
			SetColor( e, FavoriteColors[index] );
		}
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
