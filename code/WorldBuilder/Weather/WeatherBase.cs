namespace Clover.WorldBuilder.Weather;

public class WeatherBase : Component
{
	
	// protected bool _enabled = false;
	// protected const float _fadeTime = 30.0f;

	private TimeSince _lastFade;

	public virtual void SetEnabled( bool state, bool smooth = false )
	{
		Log.Info( $"Setting weather {GameObject.Name} enabled to {state}" );
		_lastFade = 0;
	}
	
	public virtual void SetLevel( int level, bool smooth = false )
	{
		Log.Info( $"Setting weather {GameObject.Name} level to {level}" );
		_lastFade = 0;
	}
	
}
