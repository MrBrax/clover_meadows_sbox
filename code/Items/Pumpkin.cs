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

		if ( DecalTexture == null )
		{
			Log.Error( "DecalTexture is null" );
			return;
		}

		var pumpkinMaterial = Material.Create( $"pumpkin_{TexturePath}.vmat", "shaders/pumpkin.shader" );

		pumpkinMaterial.Set( "Color",
			Texture.Load( FileSystem.Mounted, "items/seasonal/halloween/pumpkin_01/pumpkin_body.png" ) );

		pumpkinMaterial.Set( "Emission", DecalTexture );

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
