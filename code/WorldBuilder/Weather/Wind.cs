﻿using Sandbox.Audio;

namespace Clover.WorldBuilder.Weather;

public class Wind : WeatherBase
{
	[Property] public SoundPointComponent Sound { get; set; }

	public override void SetEnabled( bool state, bool smooth = false )
	{
		base.SetEnabled( state, smooth );

		Sound.Enabled = state;

		Sound.TargetMixer = NodeManager.WeatherManager.IsInside
			? Mixer.FindMixerByName( "InsideWeather" )
			: Mixer.FindMixerByName( "OutsideWeather" );
	}
}
