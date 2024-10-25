using System.IO;
using System.Text;
using Clover.Persistence;
using Clover.Ui;
using Clover.Utilities;
using Sandbox.Diagnostics;
using Sandbox.Utility;

namespace Clover.Items;

[Category( "Clover/Items" )]
public class FloorDecal : DecalItem
{
	[RequireComponent] public WorldItem WorldItem { get; private set; }

	[Property] public DecalRenderer DecalRenderer { get; set; }

	[Property] public ModelRenderer ModelRenderer { get; set; }

	// private static HashSet<string> _decalCache = new();

	public override void OnMaterialUpdate( Material material )
	{
		base.OnMaterialUpdate( material );

		ModelRenderer.MaterialOverride = material;
	}

	/*protected override void OnUpdate()
	{
		DebugOverlay.Text( WorldPosition + Vector3.Up * 8f, _texturePath, 16f );
	}*/
}
