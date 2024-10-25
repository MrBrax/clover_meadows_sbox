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

		if ( texture.Width != TextureSize )
		{
			Log.Error( $"Image must be {TextureSize}x{TextureSize} at the moment" );
			return;
		}

		// resize image
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
}
