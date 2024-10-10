using System;

namespace Clover.Components;


public sealed class IrisTransition : Component
{
	
	[Property] public ModelRenderer IrisModel { get; set; }

	public static float Progress = 1f;

	protected override void OnUpdate()
	{
		if ( !IrisModel.IsValid() ) return;
		IrisModel.SceneObject.Attributes.Set( "prog", Progress );
	}
}
