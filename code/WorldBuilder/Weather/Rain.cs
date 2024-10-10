using System;

namespace Clover.WorldBuilder.Weather;

public class Rain : WeatherBase
{
	[Property] public ParticleEmitter Emitter { get; set; }

	[Property] public SoundPointComponent Sound { get; set; }

	public override void SetEnabled( bool state, bool smooth = false )
	{
		base.SetEnabled( state, smooth );
		Emitter.Enabled = state;
		Sound.Enabled = state;
	}

	public override void SetLevel( int level, bool smooth = false )
	{
		base.SetLevel( level, smooth );

		switch ( level )
		{
			case 0:
				Emitter.Enabled = false;
				break;
			case 1:
				Emitter.Enabled = true;
				Emitter.Rate = 100;
				break;
			case 2:
				Emitter.Enabled = true;
				Emitter.Rate = 200;
				break;
			case 3:
				Emitter.Enabled = true;
				Emitter.Rate = 300;
				break;
			case 4:
				Emitter.Enabled = true;
				Emitter.Rate = 400;
				break;
			case 5:
				Emitter.Enabled = true;
				Emitter.Rate = 500;
				break;
		}
	}

	public void SetWind( float direction, float windSpeed )
	{
		Emitter.GetComponent<ParticleEffect>().ForceDirection = new Vector3( 
			MathF.Cos( direction ) * ( windSpeed * 50f ),
			MathF.Sin( direction ) * ( windSpeed * 50f ), 
			-400f );
	}
}
