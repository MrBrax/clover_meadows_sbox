namespace Clover.Components;

public class PlaceableNode : Component
{
	
	public WorldNodeLink PlacedNodeLink { get; set; }
	
	protected override void DrawGizmos()
	{
		base.DrawGizmos();
		Gizmo.Draw.Color = Color.White.WithAlpha( 0.4f );
		Gizmo.Draw.Model( "items/misc/gift/gift.vmdl");
		Gizmo.Draw.Color = Color.White;
	}
	
}
