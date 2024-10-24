using System.IO;
using System.Text;
using Clover.Persistence;
using Sandbox.Diagnostics;

namespace Clover.Items;

[Category( "Clover/Items" )]
public class FloorDecal : Component, IPersistent
{
	[RequireComponent] public WorldItem WorldItem { get; private set; }

	[Property] public DecalRenderer DecalRenderer { get; set; }

	[Property] public ModelRenderer ModelRenderer { get; set; }

	public string TexturePath;
	
	public struct DecalData
	{
		public int Width;
		public int Height;

		public string Name;

		public ulong Author;

		public byte[] Image;
	}

	public void UpdateDecal()
	{
		// Update decal

		if ( TexturePath.EndsWith( ".decal" ) )
		{
			
			Log.Info( "Loading palette");
			// var palette = new Color[256];

			var paletteTexture = Texture.Load( FileSystem.Mounted, "materials/windows-95-256-colours-1x.png" );
			var palette = paletteTexture.GetPixels();
			
			Log.Info( "Loading decal" );

			var stream = FileSystem.Data.OpenRead( TexturePath );
			var reader = new BinaryReader( stream, Encoding.UTF8 );

			var magic = new string( reader.ReadChars( 4 ) );
			Log.Info( $"Magic: {magic}" );

			var version = reader.ReadUInt32();
			Log.Info( $"Version: {version}" );

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

			var material = Material.Create( $"{TexturePath}.vmat", "shaders/floor_decal.shader" );

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
			
			texture.Update( allPixels, 0, 0, decalData.Width, decalData.Height);

			material.Set( "Color", texture );

			ModelRenderer.MaterialOverride = material;
		}
		else
		{
			var material = Material.Create( $"{TexturePath}.vmat", "shaders/floor_decal.shader" );

			// material.Set( "Normal", Texture.Load( FileSystem.Mounted, "materials/default/default_normal.tga" ) );
			// material.Set( "Roughness", Texture.Load( FileSystem.Mounted, "materials/default/default_rough.tga" ) );
			// material.Set( "Metalness", Texture.Load( FileSystem.Mounted, "materials/default/default_metal.tga" ) );
			// material.Set( "AmbientOcclusion", Texture.Load( FileSystem.Mounted, "materials/default/default_ao.tga" ) );

			material.Set( "Color", Texture.Load( FileSystem.Data, TexturePath ) );

			// DecalRenderer.Material = material;
			ModelRenderer.MaterialOverride = material;
		}
	}

	public void OnSave( PersistentItem item )
	{
		item.SetArbitraryData( "TexturePath", TexturePath );
	}

	public void OnLoad( PersistentItem item )
	{
		TexturePath = item.GetArbitraryData<string>( "TexturePath" );
		UpdateDecal();
	}
}
