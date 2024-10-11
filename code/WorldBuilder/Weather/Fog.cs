namespace Clover.WorldBuilder.Weather;

public class Fog : WeatherBase
{
	
	[Property] public ParticleEmitter Emitter { get; set; }
	
	[Property] public GradientFog Effect { get; set; }
	
	[Property] public VolumetricFogVolume Volume { get; set; }

	public override void SetEnabled( bool state, bool smooth = false )
	{
		base.SetEnabled( state, smooth );
		
		Emitter.Enabled = state;
		Effect.Enabled = state;
		Volume.Enabled = state;
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
				Volume.Strength = 0.2f;
				break;
			case 2:
				Emitter.Enabled = true;
				Effect.Enabled = true;
				Emitter.Rate = 3;
				Volume.Strength = 0.3f;
				break;
			case 3:
				Emitter.Enabled = true;
				Effect.Enabled = true;
				Emitter.Rate = 5;
				Volume.Strength = 0.4f;
				break;
			case 4:
				Emitter.Enabled = true;
				Effect.Enabled = true;
				Emitter.Rate = 8;
				Volume.Strength = 0.6f;
				break;
			case 5:
				Emitter.Enabled = true;
				Effect.Enabled = true;
				Emitter.Rate = 12;
				Volume.Strength = 0.8f;
				break;
		}
	}

	protected override void OnFixedUpdate()
	{

		var col = NodeManager.TimeManager.CalculateFogColor();
		
		Effect.Color = col;

	}
}
