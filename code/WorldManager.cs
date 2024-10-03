using Sandbox;

public sealed class WorldManager : Component
{
	protected override void DrawGizmos()
	{
		base.DrawGizmos();
		
		Gizmo.Draw.Grid( Gizmo.GridAxis.XY, 32f );
	}
	
}
