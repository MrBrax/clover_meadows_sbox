using System.IO;
using System.Text;
using Clover.Items;
using Clover.Player;
using Sandbox.UI;

namespace Clover.Ui;

public partial class PaintUi
{
	public struct DecalEntry
	{
		public FloorDecal.DecalData Decal;
		public string ResourcePath;
	}
	
	private List<DecalEntry> Decals = new();
	
	private Texture DrawTexture;
	private Texture GridTexture;

	private Panel Canvas;
	private Panel Grid;
	private Panel Crosshair;
	
	private List<Color32> Palette = new List<Color32>();
	
	private int LeftPaletteIndex = 0;
	private int RightPaletteIndex = 1;
	private int CurrentPaletteIndex = 0;
	
	private string CurrentFileName = "";
	private string CurrentName = "";
	
	private int TextureSize = 32;
	
	private Color32 ForegroundColor => Palette.ElementAtOrDefault(LeftPaletteIndex);
	private Color32 BackgroundColor => Palette.ElementAtOrDefault(RightPaletteIndex);
	
	private Color GetCurrentColor()
	{
		return Palette[CurrentPaletteIndex];
	}

	protected override void OnStart()
	{
		base.OnStart();
		
		Palette = FloorDecal.GetPalette().ToList();

		InitialiseTexture();

		Panel.ButtonInput = PanelInputType.UI;

		Enabled = false;
		
		PopulateDecals();
	}

	private void InitialiseTexture()
	{
		DrawTexture = Texture.Create( TextureSize, TextureSize ).WithDynamicUsage().Finish();
		Clear();
		
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
			var decal = FloorDecal.ReadDecal( $"decals/{file}" );
			Decals.Add( new DecalEntry
			{
				Decal = decal,
				ResourcePath = $"decals/{file}"
			} );
		}
		
	}
	
	private void LoadDecal( string path )
	{
		Log.Info( $"Loading decal {path}" );
		var decal = FloorDecal.ReadDecal( path );
		CurrentName = decal.Name;
		CurrentFileName = Path.GetFileNameWithoutExtension( path );
		DrawTexture.Update( decal.Texture.GetPixels(), 0, 0, decal.Width, decal.Height );
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
	
	private bool _isDrawing;
	
	private Vector2 GetCurrentMousePixel( )
	{
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
		
		if ( !IsMouseInsideCanvas() )
		{
			return;
		}
		
		var mousePosition = GetCurrentMousePixel();
		
		var crosshairX = (mousePosition.x / DrawTexture.Width * Canvas.Box.Rect.Width) * Panel.ScaleFromScreen;
		crosshairX += 10;
		
		var crosshairY = (mousePosition.y / DrawTexture.Height * Canvas.Box.Rect.Height) * Panel.ScaleFromScreen;
		crosshairY += 10;
		
		var crosshairSize = ( Canvas.Box.Rect.Width / TextureSize ) * Panel.ScaleFromScreen;
		
		Crosshair.Style.Set( "left", $"{crosshairX}px" );
		Crosshair.Style.Set( "top", $"{crosshairY}px" );
		Crosshair.Style.Width = crosshairSize;
		Crosshair.Style.Height = crosshairSize;

		if ( _isDrawing )
		{
			
			DrawTexture.Update( GetCurrentColor(), (int)mousePosition.x, (int)mousePosition.y );
		}
		
	}
	
	private void OnCanvasMouseDown( PanelEvent e )
	{
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
	}
	
	private void OnCanvasMouseUp( PanelEvent e )
	{
		Log.Info("MouseUp");
		_isDrawing = false;
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
	
		writer.Write( 'C');
		writer.Write( 'L');
		writer.Write( 'P');
		writer.Write( 'T');
		// writer.Write( 0 );
	
		writer.Write( (int)1 ); // version
	
		writer.Write( DrawTexture.Width ); // width
		writer.Write( DrawTexture.Height ); // height
	
		// writer.Write( 0 );
	
		writer.Write( CurrentName ); // name, 16 chars
	
		// writer.Write( 0 );
	
		writer.Write( Game.SteamId ); // author

		writer.Seek( 64, SeekOrigin.Begin );
		
		var texturePixels = DrawTexture.GetPixels();
		
		for ( var i = 0; i < ( 32 * 32 ); i++ )
		{
			var paletteColor = GetClosestPaletteColor( texturePixels[i] );
			if ( paletteColor == -1 )
			{
				Log.Error( $"Color {texturePixels[i]} not found in palette" );
				paletteColor = 0;
			}
			
			writer.Write( (byte)paletteColor );
			
		}
	
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
			var distance = new Vector3( texturePixel.r, texturePixel.g, texturePixel.b ).Distance( new Vector3( paletteColor.r, paletteColor.g, paletteColor.b ) );
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
		DrawTexture.Update( Palette[ RightPaletteIndex ], new Rect(0, 0, DrawTexture.Width, DrawTexture.Height) );
	}
	
}

public interface IPaintEvent
{
	public void OnFileSaved( string path );
}
