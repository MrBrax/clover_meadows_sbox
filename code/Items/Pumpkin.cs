using Clover.Interactable;
using Clover.Player;
using Clover.Ui;

namespace Clover.Items;

public class Pumpkin : DecalItem, IInteract
{
	[RequireComponent] public WorldItem WorldItem { get; private set; }

	[Property] public ModelRenderer ModelRenderer { get; set; }

	[Property] public Material PumpkinMaterial { get; set; }
	[Property, ImageAssetPath] public string PumpkinColor { get; set; }

	[Property] public SpotLight SpotLight { get; set; }

	public override void OnMaterialUpdate( Material material )
	{
		base.OnMaterialUpdate( material );

		Log.Info( $"Pumpkin material updated: {TexturePath}" );

		if ( !ModelRenderer.IsValid() )
		{
			Log.Error( "ModelRenderer is not valid" );
			return;
		}

		// var baseMaterial = Material.Load( "items/seasonal/halloween/pumpkin_01/pumpkin_body.vmat" );
		/*var baseMaterial = PumpkinMaterial;

		if ( baseMaterial == null )
		{
			Log.Error( "Failed to load pumpkin material" );
			return;
		}

		var pumpkinMaterial = baseMaterial.CreateCopy();
		if ( pumpkinMaterial == null )
		{
			Log.Error( "Failed to create pumpkin material copy" );
			return;
		}*/

		var pumpkinMaterial = Material.Create( $"pumpkin_{TexturePath}.vmat", "shaders/pumpkin.shader" );

		/*pumpkinMaterial.Set( "F_SELF_ILLUM", true );
		pumpkinMaterial.Set( "S_SELF_ILLUM", true );
		pumpkinMaterial.Set( "SELF_ILLUM", true );
		pumpkinMaterial.Set( "SELFILLUM", true );
		pumpkinMaterial.Set( "SelfIllum", true );
		pumpkinMaterial.Set( "g_bF_SELF_ILLUM", true );
		pumpkinMaterial.Set( "g_bSELF_ILLUM", true );
		pumpkinMaterial.Set( "g_bSELFILLUM", true );
		pumpkinMaterial.Set( "g_bSelfIllum", true );
		pumpkinMaterial.Set( "selfillum", true );
		pumpkinMaterial.Set( "self_illum", true );
		pumpkinMaterial.Attributes.SetCombo( "F_SELF_ILLUM", true );
		pumpkinMaterial.Attributes.SetCombo( "SELF_ILLUM", true );
		pumpkinMaterial.Attributes.SetCombo( "SELFILLUM", true );*/

		/*pumpkinMaterial.Attributes.SetCombo( "PBR", 16 );
		pumpkinMaterial.Set( "PBR", 16 );

		pumpkinMaterial.Set( "AmbientOcclusion",
			Texture.Load( FileSystem.Mounted, "materials/default/default_ao.tga" ) );*/

		/*pumpkinMaterial.Set( "ModelTintAmount", 1f );
		pumpkinMaterial.Set( "ColorTint", new Color( 1, 1, 1, 0 ) );*/
		pumpkinMaterial.Set( "Color",
			Texture.Load( FileSystem.Mounted, "items/seasonal/halloween/pumpkin_01/pumpkin_body.png" ) );
		// pumpkinMaterial.Set( "Color", Texture.Load( FileSystem.Mounted, TexturePath ) );

		/*pumpkinMaterial.Set( "FadeExponent", 1f );

		pumpkinMaterial.Set( "FogEnabled", 1 );

		pumpkinMaterial.Set( "Metalness", 0f );
		*/

		/*pumpkinMaterial.Set( "Normal",
			Texture.Load( FileSystem.Mounted, "materials/default/default_normal.tga" ) );

		/*pumpkinMaterial.Set( "RoughnessScaleFactor", 1f );#1#
		pumpkinMaterial.Set( "Roughness",
			Texture.Load( FileSystem.Mounted, "materials/default/default_rough.tga" ) );

		pumpkinMaterial.Set( "SelfIllumAlbedoFactor", 1f );
		pumpkinMaterial.Set( "SelfIllumBrightness", 6f );
		pumpkinMaterial.Set( "SelfIllumScale", 1f );
		pumpkinMaterial.Set( "SelfIllumScrollSpeed", new Vector2( 0, 0 ) );
		pumpkinMaterial.Set( "SelfIllumTint", new Vector4( 1, 1, 1, 0 ) );
		pumpkinMaterial.Set( "SelfIllumMask", DecalTexture );
		pumpkinMaterial.Set( "Self_Illum_Mask", DecalTexture );
		*/

		pumpkinMaterial.Set( "Emission", DecalTexture );

		// pumpkinMaterial.Set( "ScaleTexCoordUByModelScaleAxis", 0 );
		// pumpkinMaterial.Set( "ScaleTexCoordVByModelScaleAxis", 0 );
		// pumpkinMaterial.Set( "TexCoordOffset", new Vector2( 0, 0 ) );
		// pumpkinMaterial.Set( "TexCoordScale", new Vector2( 1, 1 ) );
		// pumpkinMaterial.Set( "TexCoordScrollSpeed", new Vector2( 0, 0 ) );

		pumpkinMaterial.Attributes.Set( "paint", 1 );


		ModelRenderer.SetMaterialOverride( pumpkinMaterial, "paint" );

		if ( SpotLight.IsValid() )
		{
			SpotLight.Cookie = DecalTexture;
		}

		Log.Info( $"Pumpkin material updated, mat: {pumpkinMaterial.ResourcePath}" );
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
