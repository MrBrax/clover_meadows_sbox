using System;
using System.Collections.Immutable;
using Clover.Objects;
using Sandbox.Audio;

namespace Clover.WorldBuilder;

public class TimeManager : Component, IWorldEvent
{
	public static TimeManager Instance =>
		Game.ActiveScene.GetAllComponents<TimeManager>().FirstOrDefault(); // TODO: optimize

	// public static DirectionalLight Sun =>
	// 	Game.ActiveScene.GetAllComponents<DirectionalLight>().FirstOrDefault( x => x.Tags.Has( "sun" ) );
	[Property] public DirectionalLight Sun { get; set; }
	[Property] public SkyBox2D SkyBox { get; set; }
	
	[Property] public Gradient SunlightGradient { get; set; }
	[Property] public Gradient AmbientGradient { get; set; }

	[ConVar( "clover_time_scale" )] public static int Speed { get; set; } = 1;
	
	[Property] public Curve SunPitchCurve { get; set; }
	[Property] public Curve SunYawCurve { get; set; }

	// public static DateTime Time => Speed != 1 ? DateTime.Now.AddSeconds( Sandbox.Time.Now * Speed ) : DateTime.Now;
	
	[Sync] public double GlobalTime { get; set; }
	
	public static DateTime Time => Instance.TimeOverride > -1 ? DateTime.Today.AddSeconds( Instance.TimeOverride ) : DateTime.FromOADate( Instance.GlobalTime );
	
	[Property, Range( -1, 86400 )]
	public float TimeOverride { get; set; } = -1;
	

	private const float SecondsPerDay = 86400f;

	public delegate void OnNewHourEventHandler( int hour );

	[Property] public OnNewHourEventHandler OnNewHour { get; set; }

	public delegate void OnNewMinuteEventHandler( int minute );

	[Property] public OnNewMinuteEventHandler OnNewMinute { get; set; }

	[Property] public SoundEvent HourChime { get; set; }

	public static bool IsNight => Time.Hour is < 6 or > 18;
	public static bool IsDay => !IsNight;
	
	public static int DayStartHour => 6;
	public static int DayEndHour => 18;

	protected override void OnStart()
	{
		OnNewHour += PlayChime;
		
		_lastHour = Time.Hour;

		OnNewMinute += ( minute ) =>
		{
			if ( Random.Shared.NextSingle() > 0.95f )
			{
				GiftCarrier.SpawnRandom();
			}
		};
	}
	
	private void PlayChime( int hour )
	{
		if ( TimeOverride > -1 ) return;
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

		Sound.Play( HourChime.ResourcePath, Mixer.FindMixerByName( "UI" ) );
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
		if ( !IsProxy )
		{
			if ( GlobalTime == 0 ) _lastHour = DateTime.Now.Hour;
			GlobalTime = DateTime.Now.ToOADate();
		}
		
		if ( Sun.IsValid() )
		{
			Sun.WorldRotation = CalculateSunRotation( Sun );
			// Sun.LightEnergy = CalculateSunEnergy( Sun );
			Sun.LightColor = CalculateSunLightColor();
			// Sun.SkyColor = (CalculateSunColor() * (IsDay ? 0.4f : 1f)).WithAlpha( 1 );
			Sun.SkyColor = CalculateSunAmbientColor();
			// GetTree().CallGroup( "debugdraw", "add_line", Sun.GlobalTransform.Origin, Sun.GlobalTransform.Origin + Sun.GlobalTransform.Basis.Z * 0.5f, new Color( 1, 1, 1 ), 0.2f );
		}
		else
		{
			// Log.Warning( "Sun not found." );
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
			Scene.RunEvent<ITimeEvent>( x => x.OnNewHour( hour ) );
		}

		var minute = Time.Minute;
		if ( minute != _lastMinute )
		{
			_lastMinute = minute;
			OnNewMinute?.Invoke( minute );
			Scene.RunEvent<ITimeEvent>( x => x.OnNewMinute( minute ) );
		}
	}

	private float DayFraction => (Time.Hour * 3600 + Time.Minute * 60 + Time.Second) / SecondsPerDay;

	public Color GetComputedSkyColor()
	{
		var baseIndex = MathF.Floor( DayFraction * _skyColors.Count );
		var nextIndex = (int)Math.Ceiling( DayFraction * _skyColors.Count ) % _skyColors.Count;
		var baseColor = _skyColors[(int)baseIndex];
		var nextColor = _skyColors[nextIndex];
		var lerp = DayFraction * _skyColors.Count - baseIndex;
		var color = Color.Lerp( baseColor, nextColor, lerp );
		return color;
	}

	public Color CalculateSunLightColor()
	{
		// return GetComputedSkyColor();
		
		return SunlightGradient.Evaluate( DayFraction );
	}
	
	public Color CalculateSunAmbientColor()
	{
		
		return AmbientGradient.Evaluate( DayFraction );
		
		/*// daytime is *1, nighttime is *3. lerp between *1 and *3 based on time of day
		
		var baseAmbientColor = GetComputedSkyColor();
		
		var lerp = MathF.Sin( MathF.PI * DayFraction );
		var color = Color.Lerp( baseAmbientColor * 1f, baseAmbientColor * 1f, lerp );
		
		return color.WithAlpha( 1f );*/
		

	}
	
	public Color CalculateFogColor()
	{
		return AmbientGradient.Evaluate( DayFraction ).WithAlpha( 0.2f );
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

		// var pitch = MathX.Lerp( 15, 50, frac );
		// var yaw = MathX.Lerp( 0, 180, frac );
		
		var pitch = SunPitchCurve.Evaluate( frac );
		var yaw = SunYawCurve.Evaluate( frac );

		var rotation = Rotation.FromYaw( yaw ) * Rotation.FromPitch( pitch );


		return rotation;
	}
	
	[Property]
	private Rotation _SunRotation => CalculateSunRotation( Sun );

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

	public void OnWorldChanged( World world )
	{
		if ( Sun.IsValid() )
		{
			Sun.Enabled = !world.Data.IsInside;
		}
		if ( SkyBox.IsValid() )
		{
			SkyBox.Enabled = !world.Data.IsInside;
		}
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( !Gizmo.IsSelected ) return;
		
		for ( var i = 0; i < 48; i++ )
		{
			var frac = i / 48f;
			var pitch = SunPitchCurve.Evaluate( frac );
			var yaw = SunYawCurve.Evaluate( frac );
			var rotation = Rotation.FromYaw( yaw ) * Rotation.FromPitch( pitch );
			var direction = rotation.Backward;
			Gizmo.Draw.LineSphere( Gizmo.Camera.Position + direction * 512f, 8f );
		}
	}
}

public interface ITimeEvent
{
	public void OnNewHour( int hour );
	public void OnNewMinute( int minute );
}
