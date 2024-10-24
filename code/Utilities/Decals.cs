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

		public byte[] Image;
		public Texture Texture;
	}

	public static Color32[] GetPalette( string name )
	{
		var paletteTexture = Texture.Load( FileSystem.Mounted, $"materials/palettes/{name}.png" );
		var palette = paletteTexture.GetPixels();
		return palette;
	}

	public static DecalData ReadDecal( string filePath )
	{
		Log.Info( "Loading palette" );
		// var palette = new Color[256];

		var palette = GetPalette( "windows-95-256-colours-1x" );

		Log.Info( "Loading decal" );

		var stream = FileSystem.Data.OpenRead( filePath );
		var reader = new BinaryReader( stream, Encoding.UTF8 );

		var magic = new string( reader.ReadChars( 4 ) );
		Log.Info( $"Magic: {magic}" );

		var version = reader.ReadUInt32();
		Log.Info( $"Version: {version}" );

		if ( version < 1 )
		{
			Log.Error( "Decal version is too old" );
			return default;
		}

		var width = reader.ReadInt32();
		var height = reader.ReadInt32();

		var name = reader.ReadString();
		Log.Info( $"Name: {name}" );

		var author = reader.ReadUInt64();
		Log.Info( $"Author: {author}" );

		// seek to 64 for now
		// var pos = reader.BaseStream.Position;
		reader.BaseStream.Seek( 64, SeekOrigin.Begin );

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
			Image = imageBytes
		};

		reader.Close();
		stream.Close();

		var texture = Texture.Create( decalData.Width, decalData.Height ).Finish();


		Log.Info( $"Texture: {texture}" );

		var allPixels = new Color32[decalData.Width * decalData.Height];

		// 4x 0-63 colors per byte packed
		for ( var i = 0; i < decalData.Image.Length; i++ )
		{
			/*var rawByte = decalData.Image[i];

			Log.Info( $"Raw byte: {rawByte}" );

			var values = new byte[4];
			values[0] =

			for ( var j = 0; j < values.Length; j++ )
			{
				Log.Info( $"Pixel {(i * 4) + j}: {values[j]}/64 {(int)values[j]}" );
				var color = Palette[values[j]];
				allPixels[(i * 4) + j] = color;
			}*/

			var color = palette.ElementAtOrDefault( decalData.Image[i] );
			allPixels[i] = color;
		}

		texture.Update( allPixels, 0, 0, decalData.Width, decalData.Height );

		decalData.Texture = texture;

		return decalData;
	}
}
