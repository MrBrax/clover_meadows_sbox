using System.IO;
using System.Text;

namespace Clover.Utilities;

public class Decals
{
	public struct DecalData
	{
		public int Width;
		public int Height;

		public string Name;

		public ulong Author;

		public string Palette;

		public byte[] Image;
		public Texture Texture;
	}

	public static List<string> GetPalettes()
	{
		var palettes = new List<string>();

		var files = FileSystem.Mounted.FindFile( "materials/palettes", "*.png" );

		foreach ( var file in files )
		{
			var name = Path.GetFileNameWithoutExtension( file );
			palettes.Add( name );
		}

		return palettes;
	}

	public static Color32[] GetPalette( string name )
	{
		var paletteTexture = Texture.Load( FileSystem.Mounted, $"materials/palettes/{name}.png" );
		var palette = paletteTexture.GetPixels();

		// swap out last color for transparent
		// TODO: do this in palettes
		palette[255] = new Color32( 0, 0, 0, 0 );

		return palette;
	}

	public static int GetClosestPaletteColor( Color32[] palette, Color32 texturePixel )
	{
		var minDistance = float.MaxValue;
		var closestColor = -1;

		for ( var i = 0; i < palette.Length; i++ )
		{
			var paletteColor = palette[i];
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

	public static DecalData ReadDecal( string filePath )
	{
		Log.Info( "Loading palette" );
		// var palette = new Color[256];

		Log.Info( "Loading decal" );

		var stream = FileSystem.Data.OpenRead( filePath );
		var reader = new BinaryReader( stream, Encoding.UTF8 );

		var magic = new string( reader.ReadChars( 4 ) );
		Log.Info( $"Magic: {magic}" );

		var version = reader.ReadUInt32();
		Log.Info( $"Version: {version}" );

		if ( version < 2 )
		{
			// Log.Error( "Decal version is too old" );
			// return default;
			throw new System.Exception( "Decal version is too old" );
		}

		var width = reader.ReadInt32();
		var height = reader.ReadInt32();

		var name = reader.ReadString();
		Log.Info( $"Name: {name}" );

		var author = reader.ReadUInt64();
		Log.Info( $"Author: {author}" );

		var paletteName = reader.ReadString();
		Log.Info( $"Palette: {paletteName}" );

		// seek to 64 for now
		// var pos = reader.BaseStream.Position;
		// reader.BaseStream.Seek( 64, SeekOrigin.Begin );

		Log.Info( reader.BaseStream.Length - reader.BaseStream.Position );
		Log.Info( (width * height) );

		var imageBytes = reader.ReadBytes( width * height );

		Log.Info( $"Image bytes: {imageBytes.Length}" );

		var decalData = new DecalData
		{
			Width = width,
			Height = height,
			Name = name,
			Author = author,
			Palette = paletteName,
			Image = imageBytes
		};

		reader.Close();
		stream.Close();

		var palette = GetPalette( paletteName );

		var texture = Texture.Create( decalData.Width, decalData.Height ).Finish();

		var allPixels = ByteArrayToColor32( decalData.Image, palette );

		texture.Update( allPixels, 0, 0, decalData.Width, decalData.Height );

		decalData.Texture = texture;

		return decalData;
	}

	public static Color32[] ByteArrayToColor32( byte[] byteArray, Color32[] palette )
	{
		var result = new Color32[byteArray.Length];

		for ( var i = 0; i < byteArray.Length; i++ )
		{
			var color = palette.ElementAtOrDefault( byteArray[i] );
			result[i] = color;
		}

		return result;
	}
}
