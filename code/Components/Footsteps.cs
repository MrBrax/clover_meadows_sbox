namespace Clover.Components;

public class Footsteps : Component
{
	
	// [Property] SkinnedModelRenderer Source { get; set; }
	
	protected override void OnEnabled()
	{
		/*if ( !Source.IsValid() )
			return;

		Source.OnFootstepEvent += OnEvent;*/
	}

	protected override void OnDisabled()
	{
		/*if ( !Source.IsValid() )
			return;

		Source.OnFootstepEvent -= OnEvent;*/
	}

	private float _velocityTick;
	private int _lastFoot;
	
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		
		if ( Components.TryGet<CharacterController>( out var controller ) )
		{
			if ( controller.Velocity.Length > 0.1f )
			{
				// Source?.PlayAnimation( "Walk" );

				var dummyEvent = new SceneModel.FootstepEvent()
				{
					Transform = new Transform( WorldPosition ), Volume = 4, FootId = _lastFoot,
				};
				
				_lastFoot = _lastFoot == 0 ? 1 : 0;
				
				_velocityTick += controller.Velocity.Length * Time.Delta;
				
				if ( _velocityTick > 15 )
				{
					_velocityTick = 0;
					OnEvent( dummyEvent );
				}
				
				// OnEvent( dummyEvent );
			}
			else
			{
				// Source?.PlayAnimation( "Idle" );
			}
		}
		
	}

	TimeSince timeSinceStep;
	
	/*private void TempEvent( SceneModel.FootstepEvent e )
	{
		if ( timeSinceStep < 0.2f )
			return;

		var tr = Scene.Trace
			.Ray( e.Transform.Position + Vector3.Up * 20, e.Transform.Position + Vector3.Up * -20 )
			.Run();

		if ( !tr.Hit )
			return;

		if ( tr.Surface is null )
			return;

		timeSinceStep = 0;

		var sound = e.FootId == 0 ? tr.Surface.Sounds.FootLeft : tr.Surface.Sounds.FootRight;
		if ( sound is null ) return;

		var handle = Sound.Play( sound, tr.HitPosition + tr.Normal * 5 );
		handle.Volume *= e.Volume;
	}*/

	private void OnEvent( SceneModel.FootstepEvent e )
	{
		if ( timeSinceStep < 0.2f )
			return;

		var tr = Scene.Trace
			.Ray( e.Transform.Position + Vector3.Up * 20, e.Transform.Position + Vector3.Up * -20 )
			.Run();

		if ( !tr.Hit )
			return;

		if ( tr.Surface is null )
			return;

		timeSinceStep = 0;

		var sound = e.FootId == 0 ? tr.Surface.Sounds.FootLeft : tr.Surface.Sounds.FootRight;
		if ( sound is null ) return;

		var handle = Sound.Play( sound, tr.HitPosition + tr.Normal * 5 );
		handle.Volume *= e.Volume;
		
		Scene.RunEvent<IFootstepEvent>( x => x.OnFootstepEvent( e ) );
	}
	
}

public interface IFootstepEvent
{
	void OnFootstepEvent( SceneModel.FootstepEvent e );
}
