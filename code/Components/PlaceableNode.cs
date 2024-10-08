namespace Clover.Components;

public class PlaceableNode : Component
{
	
	protected override void DrawGizmos()
	{
		base.DrawGizmos();
		Gizmo.Draw.Color = Color.White.WithAlpha( 0.4f );
		Gizmo.Draw.Model( "items/misc/gift/gift.vmdl");
		Gizmo.Draw.Color = Color.White;
	}
	
}
