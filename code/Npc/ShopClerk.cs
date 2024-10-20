namespace Clover.Npc;

[Title( "Shop Clerk" )]
[Icon( "face" )]
[Category( "Clover/Npc" )]
public class ShopClerk : BaseNpc
{
	protected override void StateLogic()
	{
	}

	protected override void OnFixedUpdate()
	{
		WorldRotation = Rotation.Slerp( WorldRotation, TargetLookAt, Time.Delta * 5f );

		var players = Components.Get<WorldLayerObject>().World.PlayersInWorld.ToList();

		if ( !players.Any() )
		{
			LookReset();
			return;
		}

		var player = players.Where( x => IsVisible( x.WorldPosition ) )
			.MinBy( p => p.WorldPosition.Distance( WorldPosition ) );

		if ( !player.IsValid() )
		{
			LookReset();
			return;
		}

		LookAt( player.WorldPosition );
	}

	private void LookReset()
	{
		TargetLookAt = Rotation.Identity;
	}

	private bool IsVisible( Vector3 pos )
	{
		var tr = Scene.Trace.Ray( WorldPosition + Vector3.Up * 32f, pos + Vector3.Up * 32f )
			.WithTag( "terrain" ).Run();
		// Gizmo.Draw.Line( WorldPosition + Vector3.Up * 32f, pos + Vector3.Up * 32f );
		return !tr.Hit;
	}
}
