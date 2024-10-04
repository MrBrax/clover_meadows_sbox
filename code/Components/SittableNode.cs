using Clover.Interactable;

namespace Clover.Components;

public class SittableNode : Component
{
	
	public Sittable Sittable { get; set; }
	public GameObject Occupant { get; set; }
	
	public bool IsOccupied => Occupant.IsValid();
	
	protected override void DrawGizmos()
	{
		base.DrawGizmos();
		Gizmo.Draw.Model( "models/player/placeholder.vmdl");
	}
	
}
