using System;
using Clover.Player;
using Clover.WorldBuilder.Weather;

namespace Clover.WorldBuilder;

[Category( "Clover/World" )]
public class WeatherManager : Component, IWorldEvent, ITimeEvent
{
	public enum WeatherEffects
	{
		None = 0,
		Rain = 1,
		Lightning = 2,
		Wind = 4,
		Fog = 8
	}

	public static DirectionalLight Sun =>
		Game.ActiveScene.GetAllComponents<DirectionalLight>().FirstOrDefault( x => x.Tags.Has( "sun" ) );

	private int StringToInt( string input )
	{
		var hash = 0;
		for ( int i = 0; i < input.Length; i++ )
		{
			hash = input[i] + (hash << 6) + (hash << 16) - hash;
		}

		return hash;
	}

	[Property] public Rain RainComponent { get; set; }
	[Property] public Fog FogComponent { get; set; }
	[Property] public Wind WindComponent { get; set; }
	[Property] public Lightning LightningComponent { get; set; }

	public bool IsInside { get; set; } = false;
	public bool PrecipitationEnabled { get; private set; }
	public bool LightningEnabled { get; private set; }
	public bool WindEnabled { get; private set; }
	public bool FogEnabled { get; private set; }

	protected float GetStaticFloat( string seed )
	{
		return GetStaticFloat( StringToInt( seed ) );
	}

	protected float GetStaticFloat( int seed )
	{
		var random = new Random( seed );
		return (float)random.NextSingle();
	}

	protected float GetPrecipitationChance( DateTime time )
	{
		var input = $"{time.DayOfYear}{time.Hour}-precipitation";
		return GetStaticFloat( input );
	}

	protected int GetPrecipitationLevel( DateTime time )
	{
		var input = $"{time.DayOfYear}{time.Hour}-precipitation-level";
		var value = GetStaticFloat( input );
		if ( value < 0.2f )
		{
			return 1;
		}
		else if ( value < 0.5f )
		{
			return 2;
		}
		else
		{
			return 3;
		}
	}

	/// <summary>
	/// Returns a float between -180 and 180 representing the wind direction.
	/// </summary>
	protected float GetWindDirection( DateTime time )
	{
		var input = $"{time.DayOfYear}{time.Hour}-wind-direction";
		return GetStaticFloat( input ) * 360f - 180f;
	}

	protected float GetLightningChance( DateTime time )
	{
		var input = $"{time.DayOfYear}{time.Hour}-lightning";
		return GetStaticFloat( input );
	}

	protected int GetLightningLevel( DateTime time )
	{
		var input = $"{time.DayOfYear}{time.Hour}-lightning-level";
		var value = GetStaticFloat( input );
		if ( value < 0.2f )
		{
			return 1;
		}
		else if ( value < 0.5f )
		{
			return 2;
		}
		else
		{
			return 3;
		}
	}

	protected float GetFogChance( DateTime time )
	{
		var input = $"{time.DayOfYear}{time.Hour}-fog";
		return GetStaticFloat( input );
	}

	protected float GetCloudDensity( DateTime time )
	{
		var input = $"{time.DayOfYear}{time.Hour}-cloud-density";
		return GetStaticFloat( input );
	}

	protected override void OnStart()
	{
		// debug check today's weather
		/* for ( int i = 0; i < 24; i++ )
		{
			var time = new DateTime( 2024, 6, 14, i, 0, 0 );
			var weather = GetWeather( time );
			Logger.Info( "WeatherManager", $"Weather @ {time.ToString( "h tt" )}: Rain: {weather.RainLevel}, Lightning: {weather.Lightning}, Wind: {weather.WindLevel}, Fog: {weather.Fog}, CloudDensity: {weather.CloudDensity}" );
		} */

		Setup();

		/*NodeManager.TimeManager.OnNewHour += ( hour ) =>
		{
			Setup();
		};

		NodeManager.WorldManager.WorldLoaded += ( world ) =>
		{
			if ( IsProxy || !Networking.IsHost ) return;
			IsInside = world.Data.IsInside;
			Setup( true );
		};*/
	}

	protected override void OnAwake()
	{
		base.OnAwake();
		SetPrecipitation( 0, 0, 0, true );
		SetLightning( 0, true );
		SetWind( 0, true );
		SetFog( 0, true );
		SetCloudDensity( 0.0f, true );
	}

	public class WeatherReport
	{
		public DateTime Time;

		public int RainLevel;
		public int LightningLevel;
		public int WindLevel;
		public int FogLevel;

		public float CloudDensity;

		public float WindDirection;
		// public float WindSpeed;

		public bool Rain => RainLevel > 0;
		public bool Wind => WindLevel > 0;
		public bool Lightning => LightningLevel > 0;

		public WeatherReport( DateTime time )
		{
			Time = time;
			RainLevel = 0;
			LightningLevel = 0;
			WindLevel = 0;
			FogLevel = 0;
			CloudDensity = 0.0f;
			WindDirection = 0.0f;
		}
	}

	public WeatherReport GetWeather( DateTime time )
	{
		var weather = new WeatherReport( time );
		var precipitationChance = GetPrecipitationChance( time );
		var lightningChance = GetLightningChance( time );
		var fogChance = GetFogChance( time );

		if ( precipitationChance > 0.8f )
		{
			weather.RainLevel = GetPrecipitationLevel( time );
			weather.LightningLevel = GetLightningLevel( time );
			// weather.Fog = fogChance > 0.6f;
			weather.FogLevel = fogChance > 0.6f ? 1 : 0;
			weather.WindLevel = 1;
			weather.CloudDensity = 0.5f + GetCloudDensity( time ) * 0.5f;
		}
		else
		{
			weather.RainLevel = 0;
			weather.LightningLevel = 0;

			// higher chance of fog in the morning
			// weather.Fog = time.Hour > 3 && time.Hour < 7 ? fogChance > 0.2f : fogChance > 0.8f;
			weather.FogLevel = time.Hour > 3 && time.Hour < 7 ? fogChance > 0.2f ? 1 : 0 : fogChance > 0.8f ? 1 : 0;

			weather.WindLevel = 0;
			weather.CloudDensity = GetCloudDensity( time ) * 0.5f;
		}

		weather.WindDirection = GetWindDirection( time );

		return weather;
	}

	public WeatherReport GetCurrentWeather()
	{
		return GetWeather( TimeManager.Time );
	}

	public WeatherReport GetLastPrecipitation( DateTime time )
	{
		var weather = new WeatherReport( time );

		if ( !weather.Rain )
		{
			while ( weather.Time > time.AddHours( -168 ) )
			{
				weather.Time = weather.Time.AddHours( -1 );
				var report = GetWeather( weather.Time );
				if ( report.Rain )
				{
					return report;
				}
			}
		}

		if ( !weather.Rain )
		{
			return null;
		}

		return weather;
	}

	private void Setup( bool instant = false )
	{
		Log.Info( "Setting up weather" );

		var now = TimeManager.Time;


		var weather = GetWeather( now );

		SetPrecipitation( weather.RainLevel, weather.WindDirection, weather.WindLevel * 3f, instant );
		SetLightning( weather.LightningLevel, instant );
		SetWind( weather.WindLevel, instant );
		SetFog( weather.FogLevel, instant );
		SetCloudDensity( weather.CloudDensity, instant );

		Log.Info(
			$"Weather {now.Hour}: Rain: {weather.RainLevel}, Lightning: {weather.LightningLevel}, Wind: {weather.WindLevel}, Fog: {weather.FogLevel}, CloudDensity: {weather.CloudDensity}" );
	}

	protected override void OnFixedUpdate()
	{
		if ( !PlayerCharacter.Local.IsValid() ) return;
		WorldPosition = PlayerCharacter.Local.WorldPosition;
	}

	private void SetPrecipitation( int level, float direction, float windSpeed, bool instant = false )
	{
		PrecipitationEnabled = level > 0;
		// var rainInside = GetNode<Rain>( "RainInside" );
		// var rainOutside = GetNode<Rain>( "RainOutside" );

		if ( !PrecipitationEnabled )
		{
			RainComponent.SetEnabled( false );
			return;
		}

		if ( IsInside )
		{
			RainComponent.SetEnabled( false );
			// fade in rain sound
		}
		else
		{
			RainComponent.SetEnabled( true );
			RainComponent.SetLevel( 0 );
			if ( instant )
			{
				RainComponent.SetLevel( level );
			}
			else
			{
				RainComponent.SetLevel( level, true );
			}

			RainComponent.SetWind( direction, windSpeed );
		}
	}

	private void SetLightning( int level, bool instant = false )
	{
		LightningComponent.SetEnabled( level > 0, !instant );
		LightningComponent.SetLevel( level, !instant );
	}

	private void SetWind( int level, bool instant = false )
	{
		WindEnabled = level > 0;
		if ( WindEnabled && IsInside )
		{
			WindComponent.SetEnabled( false );
			return; // no wind inside
		}
		// GetNode<Wind>( "Wind" ).SetEnabledSmooth( state );
		/*if ( instant )
		{
			// GetNode<Wind>( "Wind" ).SetEnabled( state );
		}
		else
		{
			// GetNode<Wind>( "Wind" ).SetEnabledSmooth( state );
		}*/

		WindComponent.SetEnabled( level > 0, !instant );
	}

	private void SetFog( int level, bool instant = false )
	{
		FogEnabled = level > 0;
		// if ( Environment == null ) return;
		// Environment.Environment.FogDensity = state ? 0.04f : 0.0f;
		// GetNode<Fog>( "Fog" ).SetEnabledSmooth( state );
		/* if ( PrecipitationEnabled )
		{
			GetNode<Rain>( "RainOutside" )?.SetFogState( state );
		} */

		/*if ( instant )
		{
			// GetNode<Fog>( "Fog" ).SetEnabled( state );
		}
		else
		{
			// GetNode<Fog>( "Fog" ).SetEnabledSmooth( state );
		}*/

		if ( IsInside )
		{
			FogComponent.SetEnabled( false );
			return;
		}

		FogComponent.SetEnabled( level > 0, !instant );
		FogComponent.SetLevel( level, !instant );
	}

	private void SetCloudDensity( float density, bool instant = false )
	{
		/*if ( SunLight == null )
		{
			Logger.LogError( "WeatherManager", "SunLight is null" );
			return;
		}
		Logger.Info( "WeatherManager", $"Setting cloud density to {density}" );
		SunLight.ShadowBlur = density * 2f;*/
	}

	public Texture GetWeatherIcon()
	{
		var now = TimeManager.Time;
		var weather = GetWeather( now );

		var isDay = TimeManager.IsDay;

		if ( isDay )
		{
			if ( weather.RainLevel > 0 )
			{
				return Texture.LoadFromFileSystem( "ui/icons/weather/partly-cloudy-day-rain.png", FileSystem.Mounted );
			}

			if ( weather.FogLevel > 0 )
			{
				return Texture.LoadFromFileSystem( "ui/icons/weather/fog-day.png", FileSystem.Mounted );
			}

			return Texture.LoadFromFileSystem( "ui/icons/weather/clear-day.png", FileSystem.Mounted );
		}
		else
		{
			if ( weather.Rain && weather.Lightning )
			{
				return Texture.LoadFromFileSystem( "ui/icons/weather/thunderstorms-night-overcast-rain.png",
					FileSystem.Mounted );
			}

			if ( weather.Rain )
			{
				return Texture.LoadFromFileSystem( "ui/icons/weather/partly-cloudy-night-rain.png",
					FileSystem.Mounted );
			}

			if ( weather.FogLevel > 0 )
			{
				return Texture.LoadFromFileSystem( "ui/icons/weather/fog-night.png", FileSystem.Mounted );
			}

			return Texture.LoadFromFileSystem( "ui/icons/weather/clear-night.png", FileSystem.Mounted );
		}

		/* if ( weather.RainLevel > 0 )
		{
			return Texture.Load( FileSystem.Mounted, "ui/icons/weather/rainy.png" );
		}
		else if ( weather.Lightning )
		{
			return Texture.Load( FileSystem.Mounted, "ui/icons/weather/thunderstorm.png" );
		}
		else if ( weather.Fog )
		{
			// return Texture.Load( FileSystem.Mounted, "res://assets/weather/fog.png" );
		}*/

		// return Texture.Load( FileSystem.Mounted, "ui/icons/weather/barometer.png" );
	}

	public void OnWorldLoaded( World world )
	{
	}

	public void OnWorldUnloaded( World world )
	{
	}

	public void OnWorldChanged( World world )
	{
		Log.Info( "IS INSIDE: " + world.Data.IsInside );
		IsInside = world.Data.IsInside;
		Setup( true );
	}

	public void OnNewHour( int hour )
	{
		Setup();
	}

	public void OnNewMinute( int minute )
	{
	}
}
