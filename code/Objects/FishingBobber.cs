using Clover.Animals;
using Clover.Carriable;

namespace Clover.Objects;

public class FishingBobber : Component
{
	
	[Property] public SoundEvent SplashSound { get; set; }
	
	[Property] public SkinnedModelRenderer Bobber { get; set; }
	
	[Property] public GameObject Tip { get; set; }

	public CatchableFish Fish;

	public FishingRod Rod;
	
	public bool IsInWater { get; set; }
	
	// private TimeSince _lastNibble;

	public void OnHitWater()
	{
		// GetNode<AudioStreamPlayer3D>( "BobberWater" ).Play();
		// GetNode<AnimationPlayer>( "fish_bobber/AnimationPlayer" ).Play( "bobbing" );
		GameObject.PlaySound( SplashSound );
		// Bobber.Set("anim", 0);
		// Bobber.Set("bobbing", true);
		IsInWater = true;
		
		// Bobber.SceneModel.DirectPlayback.Play( "bobbing" );
		// Log.Info( string.Join(",", Bobber.SceneModel.DirectPlayback.Animations ));
	}

	public void OnNibble()
	{
		Bobber.Set("nibble", true);
	}
	
	public void OnFight()
	{
		Bobber.Set("fight", true);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		
		CameraMan.Instance?.Targets.Remove( GameObject );
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		
		if ( !IsInWater )
			return;

		/*if ( _lastNibble > 1 && Bobber.SceneModel.DirectPlayback.Name == "nibble" )
		{
			Bobber.SceneModel.DirectPlayback.Play( "bobbing" );
		}*/
	}
}
