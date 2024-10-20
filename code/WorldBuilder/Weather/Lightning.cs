using System;
using Sandbox.Audio;

namespace Clover.WorldBuilder.Weather;

[Category( "Clover/Weather" )]
public class Lightning : WeatherBase
{
	[Property] public Light LightningLight { get; set; }

	[Property] public SoundEvent LightningSound { get; set; }

	private bool _lightningEnabled;

	private int _lightningLevel;

	private TimeUntil _nextLightning;

	// in meters
	private float _lightningDistance;

	private bool _lightningActive;

	protected override void OnAwake()
	{
		LightningLight.Enabled = false;
	}

	public override void SetEnabled( bool state, bool smooth = false )
	{
		base.SetEnabled( state, smooth );
		_lightningEnabled = state;
		if ( !_lightningActive && state )
		{
			RandomizeLightning();
		}
		else if ( !state )
		{
			LightningLight.Enabled = false;
			_lightningActive = false;
		}
	}

	public override void SetLevel( int level, bool smooth = false )
	{
		base.SetLevel( level, smooth );
		_lightningLevel = level;
		if ( !_lightningActive ) RandomizeLightning();
	}

	private async void RandomizeLightning()
	{
		// _nextLightning = Random.Shared.Float( 10, 20 );
		// _lightningDistance = Random.Shared.Float( 1000, 15000 );
		if ( !Scene.IsValid() || !_lightningEnabled )
		{
			Log.Info( "Lightning disabled" );
			return;
		}

		_lightningActive = true;

		Log.Info( "Randomizing lightning" );
		/*Task.DelayRealtimeSeconds( Random.Shared.Float( 10, 20 ) ).ContinueWith( _ =>
		{
			Log.Info( "Lightning!" );
			LightningLight.Enabled = true;
			Task.DelayRealtimeSeconds( 0.1f ).ContinueWith( __ =>
			{
				Log.Info( "Resetting lightning" );
				LightningLight.Enabled = false;
				RandomizeLightning();
			} );

			Task.DelayRealtimeSeconds( Random.Shared.Float( 0.5f, 5f ) ).ContinueWith( __ =>
			{
				Log.Info( "Playing sound" );
				Sound.Play( LightningSound );
			} );
		} );*/

		var nextFlash = Random.Shared.Float( 60, 120 );

		nextFlash /= _lightningLevel;

		await Task.DelaySeconds( nextFlash );

		if ( !NodeManager.WeatherManager.IsInside )
		{
			Log.Info( "Lightning!" );
			LightningLight.Enabled = true;

			await Task.DelaySeconds( 0.1f );

			Log.Info( "Flash done" );
			LightningLight.Enabled = false;
		}

		await Task.DelaySeconds( Random.Shared.Float( 0.5f, 5f ) );

		Log.Info( "Playing thunder" );
		var s = Sound.Play( LightningSound );
		s.TargetMixer = NodeManager.WeatherManager.IsInside
			? Mixer.FindMixerByName( "WeatherInside" )
			: Mixer.FindMixerByName( "WeatherOutside" );

		RandomizeLightning();
	}

	/*protected override void OnFixedUpdate()
	{
		if ( !_lightningEnabled ) return;

		if ( _nextLightning )
		{
			LightningLight.Enabled = _nextLightning.Relative - _nextLightning.Passed < 0.1f; // short flash
			if ( _nextLightning.Passed + 1f > _nextLightning.Relative )
			{
				RandomizeLightning();
			}
		}
	}*/
}
