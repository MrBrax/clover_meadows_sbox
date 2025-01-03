﻿using System;
using System.IO;
using System.Text;
using Clover.Ui;
using Sandbox.Diagnostics;
using Sandbox.Utility;

namespace Clover.Utilities;

public class Decals
{
	public struct DecalData
	{
		public int Width;
		public int Height;

		public PaintUi.PaintType PaintType;

		public string Name;

		public ulong Author;
		public string AuthorName;

		public string Palette;

		public byte[] Image;

		public DateTime Created;
		public DateTime Modified;

		// not stored in file
		public Texture Texture;

		public string GetHash()
		{
			var data = Crc64.FromBytes( Image ).ToString();
			return Crc64.FromString( $"{Name}:{Author}:{Palette}:{Modified}:{data}" ).ToString();
		}

		public DecalDataRpc ToRpc()
		{
			return new DecalDataRpc
			{
				Width = Width,
				Height = Height,
				PaintType = PaintType,
				Name = Name,
				Author = Author,
				AuthorName = AuthorName,
				Palette = Palette,
				Image = Image
			};
		}
	}

	public struct DecalDataRpc
	{
		public int Width { get; set; }
		public int Height { get; set; }

		public PaintUi.PaintType PaintType { get; set; }

		public string Name { get; set; }

		public ulong Author { get; set; }
		public string AuthorName { get; set; }

		public string Palette { get; set; }

		public byte[] Image { get; set; }

		public bool Dummy()
		{
			return true;
		}

		public Texture GetTexture()
		{
			Assert.NotNull( Palette, "Palette is null" );

			var palette = GetPalette( Palette );

			var texture = Texture.Create( Width, Height ).Finish();

			var allPixels = ByteArrayToColor32( Image, palette );

			texture.Update( allPixels, 0, 0, Width, Height );

			return texture;
		}

		public DecalData ToDecalData()
		{
			return new DecalData
			{
				Width = Width,
				Height = Height,
				PaintType = PaintType,
				Name = Name,
				Author = Author,
				AuthorName = AuthorName,
				Palette = Palette,
				Image = Image,
				Texture = GetTexture()
			};
		}
	}

	public static Texture GetDecalTexture( DecalDataRpc decal )
	{
		Assert.NotNull( decal.Palette, "Palette is null" );

		var palette = GetPalette( decal.Palette );

		var texture = Texture.Create( decal.Width, decal.Height ).Finish();

		var allPixels = ByteArrayToColor32( decal.Image, palette );

		texture.Update( allPixels, 0, 0, decal.Width, decal.Height );

		return texture;
	}

	public static DecalData ToDecalData( DecalDataRpc decal )
	{
		return new DecalData
		{
			Width = decal.Width,
			Height = decal.Height,
			Name = decal.Name,
			Author = decal.Author,
			AuthorName = decal.AuthorName,
			Palette = decal.Palette,
			Image = decal.Image,
			Texture = GetDecalTexture( decal )
		};
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
		if ( !paletteTexture.IsValid() )
		{
			Log.Error( $"Failed to load palette {name}" );
			return null;
		}

		var palette = paletteTexture.GetPixels();

		// swap out last color for transparent
		// TODO: do this in palettes
		if ( palette.Length == 256 )
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

	public static List<string> GetAllDecals()
	{
		FileSystem.Data.CreateDirectory( "decals" );
		return FileSystem.Data.FindFile( "decals", "*.decal" ).Select( Path.GetFileNameWithoutExtension ).ToList();
	}

	public static void WriteDecal( Stream stream, DecalData decalData )
	{
		var writer = new BinaryWriter( stream, Encoding.UTF8 );

		writer.Write( 'C' );
		writer.Write( 'L' );
		writer.Write( 'P' );
		writer.Write( 'T' );

		writer.Write( 6 ); // version

		writer.Write( decalData.Width ); // width
		writer.Write( decalData.Height ); // height

		writer.Write( decalData.PaintType.AsInt() ); // paint type

		writer.Write( decalData.Name ); // name, 16 chars

		writer.Write( decalData.Author ); // author

		writer.Write( decalData.AuthorName ); // author name

		writer.Write( decalData.Palette ); // palette name

		writer.Write( decalData.Created.ToBinary() );

		writer.Write( decalData.Modified.ToBinary() );

		// writer.Write( decalData.Image );
		var cbytes = CompressBytes( decalData.Image );
		writer.Write( cbytes.Length );
		writer.Write( cbytes );

		writer.Flush();
	}

	public static DecalData ReadDecal( string filePath )
	{
		Log.Info( "Loading decal" );

		if ( !filePath.EndsWith( ".decal" ) )
		{
			throw new Exception( $"Invalid file extension: {filePath}" );
		}

		var stream = FileSystem.Data.OpenRead( filePath );
		var reader = new BinaryReader( stream, Encoding.UTF8 );

		var magic = new string( reader.ReadChars( 4 ) );
		// Log.Info( $"Magic: {magic}" );

		var version = reader.ReadUInt32();
		// Log.Info( $"Version: {version}" );

		if ( version < 6 )
		{
			stream.Close();
			reader.Close();
			throw new System.Exception( "Decal version is too old" );
		}

		var width = reader.ReadInt32();
		var height = reader.ReadInt32();

		var paintType = (PaintUi.PaintType)reader.ReadInt32();

		var name = reader.ReadString();
		// Log.Info( $"Name: {name}" );

		var author = reader.ReadUInt64();
		// Log.Info( $"Author: {author}" );

		var authorName = reader.ReadString();
		// Log.Info( $"Author Name: {authorName}" );

		var paletteName = reader.ReadString();
		// Log.Info( $"Palette: {paletteName}" );

		var created = DateTime.FromBinary( reader.ReadInt64() );

		var modified = DateTime.FromBinary( reader.ReadInt64() );

		var cbyteLength = reader.ReadInt32();

		var imageBytes = reader.ReadBytes( cbyteLength );
		imageBytes = DecompressBytes( imageBytes );

		// Log.Info( $"Image bytes: {imageBytes.Length}" );

		var decalData = new DecalData
		{
			Width = width,
			Height = height,
			PaintType = paintType,
			Name = name,
			Author = author,
			AuthorName = authorName,
			Palette = paletteName,
			Created = created,
			Modified = modified,
			Image = imageBytes
		};

		reader.Close();
		stream.Close();

		var palette = GetPalette( paletteName );

		if ( palette == null )
		{
			Log.Error( "Failed to load palette" );
			return default;
		}

		decalData.Texture = GetDecalTexture( decalData.ToRpc() );

		return decalData;
	}

	public static Color32[] ByteArrayToColor32( byte[] byteArray, Color32[] palette )
	{
		Assert.NotNull( palette, "Palette is null" );
		Assert.NotNull( byteArray, "Byte array is null" );
		Assert.True( palette.Length > 0, "Palette is empty" );
		Assert.True( byteArray.Length > 0, "Byte array is empty" );

		var result = new Color32[byteArray.Length];

		for ( var i = 0; i < byteArray.Length; i++ )
		{
			var color = palette.ElementAtOrDefault( byteArray[i] );
			result[i] = color;
		}

		return result;
	}

	public static byte[] CompressBytes( byte[] bytes )
	{
		using var memoryStream = new MemoryStream();
		using var gZipStream =
			new System.IO.Compression.GZipStream( memoryStream, System.IO.Compression.CompressionMode.Compress );
		gZipStream.Write( bytes, 0, bytes.Length );
		gZipStream.Close();
		return memoryStream.ToArray();
	}

	public static byte[] DecompressBytes( byte[] bytes )
	{
		using var memoryStream = new MemoryStream();
		using var gZipStream =
			new System.IO.Compression.GZipStream( new MemoryStream( bytes ),
				System.IO.Compression.CompressionMode.Decompress );
		gZipStream.CopyTo( memoryStream );
		return memoryStream.ToArray();
	}
}
