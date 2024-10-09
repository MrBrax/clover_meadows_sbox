using Clover.Animals;
using Clover.Carriable;

namespace Clover.Objects;

public class FishingBobber : Component
{
	
	[Property] public SoundEvent SplashSound { get; set; }
	
	[Property] public SkinnedModelRenderer Bobber { get; set; }

	public CatchableFish Fish;

	public FishingRod Rod;

	public void OnHitWater()
	{
		// GetNode<AudioStreamPlayer3D>( "BobberWater" ).Play();
		// GetNode<AnimationPlayer>( "fish_bobber/AnimationPlayer" ).Play( "bobbing" );
		GameObject.PlaySound( SplashSound );
		Bobber.Set("bobbing", true);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		
		CameraMan.Instance?.Targets.Remove( GameObject );
	}
}
