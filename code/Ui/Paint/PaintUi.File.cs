using System;
using System.IO;
using Clover.Player;
using Clover.Utilities;

namespace Clover.Ui;

public partial class PaintUi
{
	private void PopulateDecals()
	{
		Decals.Clear();

		var files = Utilities.Decals.GetAllDecals();
		foreach ( var file in files )
		{
			Decals.DecalData decal;
			try
			{
				decal = Utilities.Decals.ReadDecal( $"decals/{file}.decal" );
			}
			catch ( Exception e )
			{
				Log.Error( e.Message );
				continue;
			}

			Decals.Add( new DecalEntry { Decal = decal, FileName = file } );
		}
	}


	private void LoadDecal( string fileName )
	{
		Log.Info( $"Loading decal {fileName}" );

		Decals.DecalData decal;

		try
		{
			decal = Utilities.Decals.ReadDecal( $"decals/{fileName}.decal" );
		}
		catch ( Exception e )
		{
			Log.Error( e.Message );
			return;
		}

		OpenPaint( PaintType.Decal, decal.Width, decal.Height, false );

		CurrentName = decal.Name;
		SetPalette( decal.Palette );
		_currentPaintType = decal.PaintType;
		Monochrome = decal.PaintType == PaintType.Pumpkin || decal.Palette == "monochrome";
		CurrentDecalData = decal;
		CurrentFileName = Path.GetFileNameWithoutExtension( fileName );

		DrawTexture.Update( decal.Texture.GetPixels(), 0, 0, decal.Width, decal.Height );
		DrawTextureData = decal.Image;

		ZoomReset();
	}

	private static readonly string[] supportedImageTypes = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".webp" };

	private void PopulateImages()
	{
		Images.Clear();

		FileSystem.Data.CreateDirectory( "decals/import" );

		var files = FileSystem.Data.FindFile( "decals/import", "*" );
		foreach ( var file in files )
		{
			if ( !supportedImageTypes.Contains( Path.GetExtension( file ) ) )
			{
				PlayerCharacter.Local.Notify( Notifications.NotificationType.Error, $"Unsupported image type {file}" );
				continue;
			}

			var texture = Texture.Load( FileSystem.Data, $"decals/import/{file}" );
			Images.Add( texture );
		}

		Log.Info( $"Loaded {Images.Count} images" );
	}

	private void LoadImage( Texture texture )
	{
		Log.Info( $"Loading image {texture.ResourcePath}, {texture.Width}x{texture.Height}" );

		/*if ( texture.Width != TextureSize )
		{
			Log.Error( $"Image must be {TextureSize}x{TextureSize} at the moment" );
			return;
		}*/

		// resize image
		var resizedTexture = Texture.Create( TextureSize.x, TextureSize.y ).WithDynamicUsage().Finish();

		resizedTexture.Update(
			ResizeTexture( texture.GetPixels(), new Vector2Int( texture.Width, texture.Height ), TextureSize ),
			0,
			0,
			TextureSize.x,
			TextureSize.y
		);

		// pick best fitting colors
		var pixels = resizedTexture.GetPixels();

		var newBytes = new byte[TextureSize.x * TextureSize.y];

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
				PaintType = _currentPaintType,
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

	public void Open( string texturePath )
	{
		// TODO: actually load with the full path
		LoadDecal( Path.GetFileNameWithoutExtension( texturePath ) );
	}
}
