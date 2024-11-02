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

		var pumpkinMaterial = Material
			.Load( "items/seasonal/halloween/pumpkin_01/pumpkin_body.vmat" ).CreateCopy();

		pumpkinMaterial.Set( "SelfIllumMask", DecalTexture );
		pumpkinMaterial.Set( "Self_Illum_Mask", DecalTexture );

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
