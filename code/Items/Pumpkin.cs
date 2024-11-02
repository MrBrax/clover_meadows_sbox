using Clover.Interactable;
using Clover.Player;
using Clover.Ui;

namespace Clover.Items;

public class Pumpkin : DecalItem, IInteract
{
	[RequireComponent] public WorldItem WorldItem { get; private set; }

	[Property] public ModelRenderer ModelRenderer { get; set; }

	public override void OnMaterialUpdate( Material material )
	{
		base.OnMaterialUpdate( material );

		Log.Info( "Pumpkin material updated" );


		// var pumpkinMaterial = Material.Create( $"{DecalHash}_pumpkin5.vmat", "shaders/complex.shader" );
		var pumpkinMaterial = Material
			.Load( "items/seasonal/halloween/pumpkin_01/pumpkin_body.vmat" ).CreateCopy();

		// pumpkinMaterial.Set( "F_SELF_ILLUM", 1 );
		// pumpkinMaterial.Set( "SelfIllum", 1 );
		// pumpkinMaterial.Set( "SELF_ILLUM", 1 );

		Log.Info( DecalTexture.ResourcePath );

		pumpkinMaterial.Set( "SelfIllumMask", DecalTexture );
		pumpkinMaterial.Set( "Self_Illum_Mask", DecalTexture );

		// pumpkinMaterial.Set( "TextureSelfIllumMask", _decalTexture );
		// pumpkinMaterial.Set( "Self Illum Mask", _decalTexture );
		// pumpkinMaterial.Set( "Self Illum", _decalTexture );
		// pumpkinMaterial.Set( "SelfIllum", _decalTexture );

		// pumpkinMaterial.Set( "Self Illum Mask", _decalTexture );
		// pumpkinMaterial.Set( "g_flSelfIllumBrightness", 6f );
		// pumpkinMaterial.Set( "SelfIllumBrightness", 6f );
		// pumpkinMaterial.Set( "Self Illum Brightness", 6f );

		// pumpkinMaterial.

		/*pumpkinMaterial.Set( "Color",
			Texture.Load( FileSystem.Mounted, "items/seasonal/halloween/pumpkin_01/pumpkin_body.png" ) );

		pumpkinMaterial.Set( "Normal",
			Texture.Load( FileSystem.Mounted, "materials/default/default_normal.tga" ) );
		pumpkinMaterial.Set( "Roughness",
			Texture.Load( FileSystem.Mounted, "materials/default/default_rough.tga" ) );

		pumpkinMaterial.Set( "AmbientOcclusion",
			Texture.Load( FileSystem.Mounted, "materials/default/default_ao.tga" ) );*/

		ModelRenderer.SetMaterialOverride( pumpkinMaterial, "paint" );
	}

	public void StartInteract( PlayerCharacter player )
	{
		// MainUi.Instance.Components.Get<PaintUi>( true ).OpenPaint( PaintUi.PaintType.Pumpkin, 128, 64, true );
		if ( string.IsNullOrEmpty( TexturePath ) )
		{
			MainUi.Instance.Components.Get<PaintUi>( true ).OpenPaint( PaintUi.PaintType.Pumpkin, 128, 64, true );
		}
		else
		{
			MainUi.Instance.Components.Get<PaintUi>( true ).Open( TexturePath );
		}
	}

	public string GetInteractName()
	{
		return "Paint";
	}
}
