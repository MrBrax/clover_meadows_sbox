using Clover.Animals;
using Clover.Carriable;

namespace Clover.Objects;

public class FishingBobber : Component
{

	public CatchableFish Fish;

	public FishingRod Rod;
	
	public void OnHitWater()
	{
		// GetNode<AudioStreamPlayer3D>( "BobberWater" ).Play();
		// GetNode<AnimationPlayer>( "fish_bobber/AnimationPlayer" ).Play( "bobbing" );
	}
}
