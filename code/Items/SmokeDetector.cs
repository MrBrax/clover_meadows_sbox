using System;

namespace Clover.Items;

public class SmokeDetector : Component
{
	[Property] public SoundEvent Sound { get; set; }

	[Property] public float TimeBetweenSounds { get; set; }

	private TimeSince _timeSinceLastSound;

	protected override void OnStart()
	{
		_timeSinceLastSound = TimeBetweenSounds + Random.Shared.Float( 0, 10 );
	}

	protected override void OnFixedUpdate()
	{
		if ( _timeSinceLastSound >= TimeBetweenSounds )
		{
			GameObject.PlaySound( Sound );
			_timeSinceLastSound = 0;
		}
	}
}
