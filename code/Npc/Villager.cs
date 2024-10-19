using System;

namespace Clover.Npc;

public class Villager : BaseNpc
{
	private Vector3 _targetPos;

	public bool IsCloseToTarget => WorldPosition.Distance( _targetPos ) < 32f;

	public void WalkToRandomTarget()
	{
		var pos = Scene.NavMesh.GetClosestPoint( WorldPosition +
		                                         new Vector3( Random.Shared.Float( -512, 512 ),
			                                         Random.Shared.Float( -512, 512 ), 0 ) );

		if ( !pos.HasValue )
		{
			Log.Error( "Villager WalkToRandomTarget: No valid position found" );
			return;
		}

		_targetPos = pos.Value;

		Log.Info( "Villager WalkToRandomTarget: " + _targetPos );
		Agent.MoveTo( _targetPos );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		Gizmo.Draw.LineSphere( _targetPos, 32f );
	}
}
