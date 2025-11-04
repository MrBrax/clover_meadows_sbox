using System;

namespace Clover.Components;

public sealed class IrisTransition : Component
{
	[Property] public ModelRenderer IrisModel { get; set; }

	public static float Progress = 1f;

	protected override void OnUpdate()
	{
		if ( !IrisModel.IsValid() ) return;
		IrisModel.Attributes.Set( "Progress", Progress );
		IrisModel.Materials.GetOriginal( 0 ).Set( "Progress", Progress );
	}
}
