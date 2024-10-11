namespace Clover.WorldBuilder.Weather;

public class Fog : WeatherBase
{
	
	[Property] public ParticleEmitter Emitter { get; set; }
	
	[Property] public GradientFog Effect { get; set; }

	public override void SetEnabled( bool state, bool smooth = false )
	{
		base.SetEnabled( state, smooth );
		
		Emitter.Enabled = state;
		Effect.Enabled = state;
	}

	public override void SetLevel( int level, bool smooth = false )
	{
		base.SetLevel( level, smooth );
		
		switch ( level )
		{
			case 0:
				Emitter.Enabled = false;
				Effect.Enabled = false;
				break;
			case 1:
				Emitter.Enabled = true;
				Effect.Enabled = true;
				Emitter.Rate = 1;
				break;
			case 2:
				Emitter.Enabled = true;
				Effect.Enabled = true;
				Emitter.Rate = 3;
				break;
			case 3:
				Emitter.Enabled = true;
				Effect.Enabled = true;
				Emitter.Rate = 5;
				break;
			case 4:
				Emitter.Enabled = true;
				Effect.Enabled = true;
				Emitter.Rate = 8;
				break;
			case 5:
				Emitter.Enabled = true;
				Effect.Enabled = true;
				Emitter.Rate = 12;
				break;
		}
	}
}
