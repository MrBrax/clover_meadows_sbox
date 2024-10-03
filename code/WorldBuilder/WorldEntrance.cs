namespace Clover;

public class WorldEntrance : Component
{
	
	[Property] public string EntranceId { get; set; }
	
	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Arrow( Vector3.Zero, Vector3.Forward * 32f );
	}
}
