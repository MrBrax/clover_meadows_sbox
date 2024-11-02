using System;
using System.IO;
using System.Text;
using Sandbox.UI;

namespace Clover.Ui;

public partial class PaintUi
{
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
}
