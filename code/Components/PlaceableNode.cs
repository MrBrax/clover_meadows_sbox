namespace Clover.Components;

/// <summary>
///  To be able to place items on top of other items, we need to have a way to represent
///  the position of the "attachment point" on the floor item that the new item will be placed on.
///  These have to be added on top of the item, like the surface of a table. Only one node should be placed per tile.
///
///  <remarks>The rotation does not matter for the PlaceableNode, as it is only used to determine the position of the item on top.</remarks>
/// </summary>
public class PlaceableNode : Component
{
	public WorldNodeLink PlacedNodeLink { get; set; }

	protected override void DrawGizmos()
	{
		base.DrawGizmos();
		Gizmo.Draw.Color = Color.White.WithAlpha( 0.4f );
		Gizmo.Draw.Model( "items/misc/gift/gift.vmdl" );
		Gizmo.Draw.Color = Color.White;
	}
}
