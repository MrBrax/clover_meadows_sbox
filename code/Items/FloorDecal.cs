using Clover.Persistence;

namespace Clover.Items;

public class FloorDecal : Component, IPersistent
{
	[RequireComponent] public WorldItem WorldItem { get; private set; }

	[Property] public DecalRenderer DecalRenderer { get; set; }

	[Property] public ModelRenderer ModelRenderer { get; set; }

	public string TexturePath;

	private void UpdateDecal()
	{
		// Update decal

		var material = Material.Create( $"{TexturePath}.vmat", "shaders/floor_decal.shader" );

		// material.Set( "Normal", Texture.Load( FileSystem.Mounted, "materials/default/default_normal.tga" ) );
		// material.Set( "Roughness", Texture.Load( FileSystem.Mounted, "materials/default/default_rough.tga" ) );
		// material.Set( "Metalness", Texture.Load( FileSystem.Mounted, "materials/default/default_metal.tga" ) );
		// material.Set( "AmbientOcclusion", Texture.Load( FileSystem.Mounted, "materials/default/default_ao.tga" ) );

		material.Set( "Color", Texture.Load( FileSystem.Data, TexturePath ) );

		// DecalRenderer.Material = material;
		ModelRenderer.MaterialOverride = material;
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
