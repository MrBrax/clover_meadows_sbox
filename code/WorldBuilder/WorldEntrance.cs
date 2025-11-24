using Clover.Player;
using Clover.WorldBuilder;

namespace Clover;

[Category( "Clover/World" )]
[Icon( "world" )]
public class WorldEntrance : Component
{
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }
	[Property] public string EntranceId { get; set; }

	[Property] public Door ExitDoor { get; set; }

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Arrow( Vector3.Up * 16f, Vector3.Up * 16 + Vector3.Forward * 32f );
		Gizmo.Draw.Model( "models/player/placeholder.vmdl" );
		Gizmo.Hitbox.BBox( BBox.FromPositionAndSize( Vector3.Up * 25, new Vector3( 16, 16, 50 ) ) );
		Gizmo.Draw.Text( EntranceId, new Transform( Vector3.Up * 50 ) );
	}

	[Rpc.Owner]
	public void OnTeleportTo( PlayerCharacter player )
	{
		if ( ExitDoor.IsValid() )
		{
			ExitDoor.CloseAfter( 1f );
		}
	}
}
