using System.IO;
using System.Text;
using Clover.Items;
using Sandbox.UI;

namespace Clover.Ui;

public partial class PaintUi
{
	private Texture DrawTexture;

	private Panel Canvas;
	
	private List<Color32> Palette = new List<Color32>();
	
	private int LeftPaletteIndex = 0;
	private int RightPaletteIndex = 1;
	private int CurrentPaletteIndex = 0;
	
	private Color32 ForegroundColor => Palette[LeftPaletteIndex];
	private Color32 BackgroundColor => Palette[RightPaletteIndex];
	
	private Color GetCurrentColor()
	{
		return Palette[CurrentPaletteIndex];
	}

	protected override void OnStart()
	{
		base.OnStart();
		
		Palette = FloorDecal.GetPalette().ToList();

		DrawTexture = Texture.Create( 32, 32 ).WithDynamicUsage().Finish();

		Panel.ButtonInput = PanelInputType.UI;
		
		Panel.AddEventListener( "onmousedown", ( e ) =>
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
		} );
		
		Panel.AddEventListener( "onmouseup", ( e ) =>
		{
			Log.Info("MouseUp");
			_isDrawing = false;
		} );
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

	protected override void OnUpdate()
	{

		if ( _isDrawing )
		{
			var mousePosition = GetCurrentMousePixel();
			DrawTexture.Update( GetCurrentColor(), (int)mousePosition.x, (int)mousePosition.y );
		}
		
	}

	/*private void OnCanvasClick( PanelEvent e )
	{
		var mousePosition = (e.This.MousePosition / e.This.Box.Rect.Width);

		// Log.Info( $"Mouse Position: {mousePosition}" );
		
		var x = (int)(mousePosition.x * DrawTexture.Width);
		var y = (int)(mousePosition.y * DrawTexture.Height);
		
		Log.Info( $"X: {x}, Y: {y}" );

		DrawTexture.Update( Color.Red, x, y );
	}*/
	
	
	/*private void OnCanvasMouseDown( PanelEvent e )
	{
		// var mousePosition = GetCurrentMousePixel();

		// DrawTexture.Update( Color.Red, (int)mousePosition.x, (int)mousePosition.y );
		Log.Info("MouseDown");
	}
	
	private void OnCanvasMouseUp( PanelEvent e )
	{
		// DrawTexture.Apply();
		Log.Info("MouseUp");
	}
	
	private void OnCanvasMouseMove( MousePanelEvent e )
	{
		
		Log.Info( e.MouseButton  );
		// if ( e.MouseButton != MouseButtons.Left ) return;

		// var mousePosition = GetCurrentMousePixel();

		// DrawTexture.Update( Color.Red, (int)mousePosition.x, (int)mousePosition.y );
	}*/
	
	private void Save()
	{
		
		var data = new byte[64 + (32 * 32)];

		// var stream = new MemoryStream( data );
		var stream = FileSystem.Data.OpenWrite( "decals/test.decal" );
		var writer = new BinaryWriter( stream, Encoding.UTF8 );
	
		writer.Write( 'C');
		writer.Write( 'L');
		writer.Write( 'P');
		writer.Write( 'T');
		// writer.Write( 0 );
	
		writer.Write( 1 ); // version
	
		writer.Write( DrawTexture.Width ); // width
		writer.Write( DrawTexture.Height ); // height
	
		// writer.Write( 0 );
	
		writer.Write( "Test Pattern AAA" ); // name, 16 chars
	
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
