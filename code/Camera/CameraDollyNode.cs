namespace Clover.Camera;

[Category( "Clover/Camera" )]
[EditorHandle( "materials/gizmo/tracked_object.png" )]
public class CameraDollyNode : Component
{
	[Property] public CameraDollyNode Next { get; set; }

	public List<CameraDollyNode> GetPath()
	{
		var path = new List<CameraDollyNode>();
		var current = this;
		while ( current.IsValid() )
		{
			path.Add( current );
			if ( !current.Next.IsValid() ) break;
			if ( path.Contains( current.Next ) ) break;
			current = current.Next;
		}

		return path;
	}


	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( Next.IsValid() )
		{
			Gizmo.Transform = global::Transform.Zero;
			Gizmo.Draw.Line( WorldPosition, Next.WorldPosition );
		}
	}
}
