using System.Threading.Tasks;

namespace Clover.Components;

public class Fader : Component
{
	
	public static Fader Instance => Game.ActiveScene.GetAllComponents<Fader>().FirstOrDefault();
	
	[Property] public float FadeTime { get; set; } = 0.5f;
	
	[Property] public SoundEvent FadeFromBlackSound { get; set; }
	[Property] public SoundEvent FadeToBlackSound { get; set; }

	private bool _isFading = false;
	private bool _targetState = false;
	private TimeSince _fadeStartTime;

	protected override void OnStart()
	{
		base.OnStart();
		FadeFromBlack();
	}
	
	
	public async Task FadeFromBlack( bool playSound = false )
	{
		Log.Info( "Fading out." );
		// FixResolution();
		_targetState = false;
		_fadeStartTime = 0;
		_isFading = true;
		// Logger.Debug( "Fader", "Fading out." );
		// await ToSignal( this, SignalName.FadeOutComplete );
		
		if ( playSound )
		{
			Sound.Play( FadeFromBlackSound );
		}
		
		await Task.DelayRealtimeSeconds( FadeTime );
		// Modulate = new Color( 0, 0, 0, 0 );
	}

	public async Task FadeToBlack( bool playSound = false )
	{
		Log.Info( "Fading in." );
		// FixResolution();
		_targetState = true;
		_fadeStartTime = 0;
		_isFading = true;
		// Modulate = new Color( 0, 0, 0, 1 );
		// Logger.Debug( "Fader", "Fading in." );
		// await ToSignal( this, SignalName.FadeInComplete );
		
		if ( playSound )
		{
			Sound.Play( FadeToBlackSound );
		}
		
		await Task.DelayRealtimeSeconds( FadeTime );
	}
	
	[Broadcast]
	public void FadeToBlackRpc( bool playSound = false )
	{
		FadeToBlack( playSound );
	}
	
	[Broadcast]
	public void FadeFromBlackRpc( bool playSound = false )
	{
		FadeFromBlack( playSound );
	}
	

	protected override void OnUpdate()
	{
		base.OnUpdate();
		
		if ( !_isFading ) return;
		var time = _fadeStartTime;
		var progress = time / (FadeTime);
		// if ( Material is not ShaderMaterial material ) return;
		// material.SetShaderParameter( "progress", !_targetState ? progress : 1 - progress );
		
		var finalProgress = !_targetState ? progress : 1 - progress;
		
		finalProgress = Sandbox.Utility.Easing.EaseInOut( finalProgress );
		
		IrisTransition.Progress = finalProgress;

		if ( time >= FadeTime * 1000 )
		{
			_isFading = false;
			if ( !_targetState )
			{
				// EmitSignal( SignalName.FadeOutComplete );
			}
			else
			{
				// EmitSignal( SignalName.FadeInComplete );
			}
		}
		
	}
}
