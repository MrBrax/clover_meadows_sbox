using Clover.Persistence;

namespace Clover.Items;

public class PictureFrame : DecalItem, IPersistent
{
	[RequireComponent] public WorldItem WorldItem { get; private set; }
	[Property] public ModelRenderer ModelRenderer { get; set; }


	public override void OnMaterialUpdate( Material material )
	{
		base.OnMaterialUpdate( material );

		ModelRenderer.SetMaterialOverride( material, "image" );
	}
}
