using System.IO;
using System.Text;
using Clover.Persistence;
using Clover.Ui;
using Clover.Utilities;
using Sandbox.Diagnostics;

namespace Clover.Items;

[Category( "Clover/Items" )]
public class FloorDecal : Component, IPersistent, IPaintEvent
{
	[RequireComponent] public WorldItem WorldItem { get; private set; }

	[Property] public DecalRenderer DecalRenderer { get; set; }

	[Property] public ModelRenderer ModelRenderer { get; set; }

	public string TexturePath;


	public void UpdateDecal()
	{
		// Update decal

		if ( TexturePath.EndsWith( ".decal" ) )
		{
			var material = Material.Create( $"{TexturePath}.vmat", "shaders/floor_decal.shader" );

			Decals.DecalData decal;

			try
			{
				decal = Decals.ReadDecal( TexturePath );
			}
			catch ( System.Exception e )
			{
				Log.Error( e.Message );
				return;
			}

			material.Set( "Color", decal.Texture );

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

	void IPaintEvent.OnFileSaved( string path )
	{
		if ( TexturePath == path )
		{
			Log.Info( "Updating decal" );
			UpdateDecal();
		}
	}
}
