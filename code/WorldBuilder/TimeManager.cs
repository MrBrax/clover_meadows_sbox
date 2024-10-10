using System;
using System.Collections.Immutable;

namespace Clover.WorldBuilder;

public class TimeManager : Component
{
	
	public static TimeManager Instance => Game.ActiveScene.GetAllComponents<TimeManager>().FirstOrDefault(); // TODO: optimize
	
	public static DirectionalLight Sun =>
		Game.ActiveScene.GetAllComponents<DirectionalLight>().FirstOrDefault( x => x.Tags.Has( "sun" ) );

	[ConVar( "clover_time_scale")]
	public static int Speed { get; set; } = 1;
	
	public static DateTime Time => Speed != 1 ? DateTime.Now.AddSeconds( Sandbox.Time.Now * Speed ) : DateTime.Now;

	private const float SecondsPerDay = 86400f;

	public delegate void OnNewHourEventHandler( int hour );

	[Property] public OnNewHourEventHandler OnNewHour { get; set; }

	public delegate void OnNewMinuteEventHandler( int minute );

	[Property] public OnNewMinuteEventHandler OnNewMinute { get; set; }

	[Property] public SoundEvent HourChime { get; set; }

	public static bool IsNight => Time.Hour is < 6 or > 18;
	public static bool IsDay => !IsNight;

	protected override void OnStart()
	{
		/* if ( Sun == null )
		{
			throw new System.Exception( "Sun not set." );
		} */

		OnNewHour += PlayChime;

		/*NodeManager.WorldManager.WorldLoaded += ( world ) =>
		{
			FindSun();
			FindEnvironment();
		};

		FindSun();*/

		_lastHour = Time.Hour;

		/*OnNewMinute += ( minute ) =>
		{
			if ( GD.Randf() > 0.95f )
			{
				GiftCarrier.SpawnRandom();
			}
		};*/
	}

	/*private void FindEnvironment()
	{
		if ( NodeManager.WorldManager.ActiveWorld == null ) return;

		var worldEnvironment = GetTree().GetNodesInGroup( "worldenvironment" );
		if ( worldEnvironment.Count == 0 )
		{
			Logger.Warn( "DayNightCycle", "No WorldEnvironment found." );
			return;
		}

		if ( worldEnvironment.Count > 1 )
		{
			Logger.Warn( "DayNightCycle", $"Multiple WorldEnvironments found: {worldEnvironment.Count}" );
		}

		_environment = worldEnvironment[0] as WorldEnvironment;
	}*/

	/*private void FindSun()
	{
		if ( NodeManager.WorldManager.ActiveWorld == null ) return;

		var suns = GetTree().GetNodesInGroup( "sunlight" );
		if ( suns.Count == 0 )
		{
			Logger.Warn( "DayNightCycle", "No sun found in sunlight group." );
			return;
		}

		if ( suns.Count > 1 )
		{
			Logger.Warn( "DayNightCycle", $"Multiple suns found in sunlight group: {suns.Count}" );
		}

		Sun = suns[0] as DirectionalLight3D;
	}*/

	private void PlayChime( int hour )
	{
		Log.Info( $"Playing chime for hours {hour}" );
		// var chime = GetNode<AudioStreamPlayer>( "Chime" );
		/* for ( int i = 0; i < hour; i++ )
		{
			ToSignal( GetTree().CreateTimer( i * 1f ), Timer.SignalName.Timeout ).OnCompleted( () =>
			{
				Logger.Info( "DayNightCycle", $"Playing chime for hour {i}" );
				chime.Play();
			} );
		} */
		// chime.Play();

		Sound.Play( HourChime );
	}

	private readonly List<Color> _skyColors = new()
	{
		Color.FromBytes( 1, 1, 3 ), // 00:00
		Color.FromBytes( 1, 1, 3 ), // 01:00
		Color.FromBytes( 1, 1, 3 ), // 02:00
		Color.FromBytes( 1, 1, 3 ), // 03:00
		Color.FromBytes( 1, 1, 3 ), // 04:00
		Color.FromBytes( 1, 1, 3 ), // 05:00
		Color.FromBytes( 1, 1, 3 ), // 06:00
		Color.FromBytes( 255, 100, 100 ), // 07:00 -- sunrise
		Color.FromBytes( 255, 255, 255 ), // 08:00
		Color.FromBytes( 255, 255, 255 ), // 09:00
		Color.FromBytes( 255, 255, 255 ), // 10:00
		Color.FromBytes( 255, 255, 255 ), // 11:00
		Color.FromBytes( 255, 255, 255 ), // 12:00
		Color.FromBytes( 255, 255, 255 ), // 13:00
		Color.FromBytes( 255, 255, 255 ), // 14:00
		Color.FromBytes( 255, 255, 255 ), // 15:00
		Color.FromBytes( 250, 240, 240 ), // 16:00
		Color.FromBytes( 220, 100, 100 ), // 17:00 -- sunset
		Color.FromBytes( 1, 1, 3 ), // 18:00
		Color.FromBytes( 1, 1, 3 ), // 19:00
		Color.FromBytes( 1, 1, 3 ), // 20:00
		Color.FromBytes( 1, 1, 3 ), // 21:00
		Color.FromBytes( 1, 1, 3 ), // 22:00
		Color.FromBytes( 1, 1, 3 ), // 23:00
	};

	private int _lastHour = -1;
	private int _lastMinute = -1;

	protected override void OnFixedUpdate()
	{
		if ( Sun.IsValid() )
		{
			Sun.WorldRotation = CalculateSunRotation( Sun );
			// Sun.LightEnergy = CalculateSunEnergy( Sun );
			Sun.LightColor = CalculateSunColor();
			Sun.SkyColor = CalculateSunColor() * 0.4f;
			// GetTree().CallGroup( "debugdraw", "add_line", Sun.GlobalTransform.Origin, Sun.GlobalTransform.Origin + Sun.GlobalTransform.Basis.Z * 0.5f, new Color( 1, 1, 1 ), 0.2f );
		}
		else
		{
			Log.Warning( "Sun not found." );
		}
		/*
		if ( IsInstanceValid( _environment ) )
		{
			_environment.Environment.BackgroundEnergyMultiplier = CalculateSunEnergy( Sun );
		}*/

		var hour = Time.Hour;
		if ( hour != _lastHour )
		{
			_lastHour = hour;
			OnNewHour?.Invoke( hour );
		}

		var minute = Time.Minute;
		if ( minute != _lastMinute )
		{
			_lastMinute = minute;
			OnNewMinute?.Invoke( minute );
		}
	}

	private float DayFraction => (float)(Time.Hour * 3600 + Time.Minute * 60 + Time.Second) / SecondsPerDay;

	private Color GetComputedSkyColor()
	{
		var baseIndex = MathF.Floor( DayFraction * _skyColors.Count );
		var nextIndex = (int)Math.Ceiling( DayFraction * _skyColors.Count ) % _skyColors.Count;
		var baseColor = _skyColors[(int)baseIndex];
		var nextColor = _skyColors[nextIndex];
		var lerp = DayFraction * _skyColors.Count - baseIndex;
		var color = Color.Lerp( baseColor, nextColor, lerp );
		return color;
	}

	private Color CalculateSunColor()
	{
		return GetComputedSkyColor();
	}

	private Rotation CalculateSunRotation( DirectionalLight sun )
	{
		var time = Time;
		var hours = time.Hour;
		var minutes = time.Minute;
		var seconds = time.Second;
		var msec = time.Millisecond;

		var totalSeconds = hours * 3600 + minutes * 60 + seconds + msec / 1000f;
		var totalSecondsInDay = 24 * 3600;
		
		var frac = totalSeconds / totalSecondsInDay;

		var pitch = MathX.Lerp( -3, 183, frac );
		var yaw = MathX.Lerp( 0, 180, frac );

		var rotation = Rotation.From( pitch, yaw, 0 );
		

		return rotation;
	}

	/// <summary>
	///  A value between 0 and 1 used to calculate the sun's energy (0 at night, 1 at midday).
	/// </summary>
	/// <param name="sun"></param>
	/// <returns></returns>
	/*private float CalculateSunEnergy( DirectionalLight3D sun )
	{
		var time = Time;
		var hours = time.Hour;
		var minutes = time.Minute;
		var seconds = time.Second;
		var msec = time.Millisecond;

		var totalSeconds = hours * 3600 + minutes * 60 + seconds + msec / 1000f;
		var totalSecondsInDay = 24 * 3600;

		var energy = Mathf.Abs( Mathf.Sin( Mathf.Pi * 2 * totalSeconds / totalSecondsInDay ) );

		return energy;

	}*/
	internal string GetDate()
	{
		return Time.ToString( "yyyy-MM-dd HH:mm:ss" );
	}

	internal string GetTime()
	{
		return Time.ToString( "h:mm tt" );
	}
}
