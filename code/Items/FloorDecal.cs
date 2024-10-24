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
	
	// 64 colors in the palette
	public Color[] Palette = new Color[64]
	{
		new Color( 0, 0, 0, 0 ),
		new Color( 0, 0, 0, 255 ),
		new Color( 0, 0, 85, 255 ),
		new Color( 0, 0, 170, 255 ),
		new Color( 85, 0, 0, 255 ),
		new Color( 85, 0, 85, 255 ),
		new Color( 85, 0, 170, 255 ),
		new Color( 85, 0, 255, 255 ),
		new Color( 0, 85, 0, 255 ),
		new Color( 0, 85, 85, 255 ),
		new Color( 0, 85, 170, 255 ),
		new Color( 0, 85, 255, 255 ),
		new Color( 85, 85, 0, 255 ),
		new Color( 85, 85, 85, 255 ),
		new Color( 85, 85, 170, 255 ),
		new Color( 85, 85, 255, 255 ),
		new Color( 170, 0, 0, 255 ),
		new Color( 170, 0, 85, 255 ),
		new Color( 170, 0, 170, 255 ),
		new Color( 170, 0, 255, 255 ),
		new Color( 255, 0, 0, 255 ),
		new Color( 255, 0, 85, 255 ),
		new Color( 255, 0, 170, 255 ),
		new Color( 255, 0, 255, 255 ),
		new Color( 170, 85, 0, 255 ),
		new Color( 170, 85, 85, 255 ),
		new Color( 170, 85, 170, 255 ),
		new Color( 170, 85, 255, 255 ),
		new Color( 255, 85, 0, 255 ),
		new Color( 255, 85, 85, 255 ),
		new Color( 255, 85, 170, 255 ),
		new Color( 255, 85, 255, 255 ),
		new Color( 170, 170, 0, 255 ),
		new Color( 170, 170, 85, 255 ),
		new Color( 170, 170, 170, 255 ),
		new Color( 170, 170, 255, 255 ),
		new Color( 255, 170, 0, 255 ),
		new Color( 255, 170, 85, 255 ),
		new Color( 255, 170, 170, 255 ),
		new Color( 255, 170, 255, 255 ),
		new Color( 0, 255, 0, 255 ),
		new Color( 0, 255, 85, 255 ),
		new Color( 0, 255, 170, 255 ),
		new Color( 0, 255, 255, 255 ),
		new Color( 85, 255, 0, 255 ),
		new Color( 85, 255, 85, 255 ),
		new Color( 85, 255, 170, 255 ),
		new Color( 85, 255, 255, 255 ),
		new Color( 170, 255, 0, 255 ),
		new Color( 170, 255, 85, 255 ),
		new Color( 170, 255, 170, 255 ),
		new Color( 170, 255, 255, 255 ),
		new Color( 255, 255, 0, 255 ),
		new Color( 255, 255, 85, 255 ),
		new Color( 255, 255, 170, 255 ),
		new Color( 255, 255, 255, 255 ),
		new Color( 0, 0, 0, 0 ),
		new Color( 0, 0, 0, 255 ),
		new Color( 0, 0, 85, 255 ),
		new Color( 0, 0, 170, 255 ),
		new Color( 85, 0, 0, 255 ),
		new Color( 85, 0, 85, 255 ),
		new Color( 85, 0, 170, 255 ),
		new Color( 85, 0, 255, 255 ),
	};

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
			Log.Info( (width * height) / 4 );

			var imageBytes = reader.ReadBytes( (width * height) / 4 );

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

			// 4 pixels per byte
			for ( var i = 0; i < decalData.Image.Length; i++ )
			{
				var rawByte = decalData.Image[i];

				var pixels = new[]
				{
					( rawByte >> 0 ) & 0x3,
					( rawByte >> 2 ) & 0x3,
					( rawByte >> 4 ) & 0x3,
					( rawByte >> 6 ) & 0x3,
				};
				
				var y = i / 32;

				for ( var j = 0; j < 4; j++ )
				{
					var x = ( i % 32 ) * 4 + j;
					var pixel = pixels[j];
					Log.Info( $"Pixel: {pixel} @ {x}, {y}" );
					var color = Palette[pixel];
					texture.Update( color, x, y );
				}
			}

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
