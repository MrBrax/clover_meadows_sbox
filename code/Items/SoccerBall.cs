using Clover.Player;

namespace Clover.Items;

public class SoccerBall : Component, Component.ICollisionListener
{
	[Property] public SoundEvent KickSound { get; set; }

	public void OnCollisionStart( Collision collision )
	{

		if ( collision.Other.GameObject == null ||
		     collision.Other.GameObject.Components.Get<PlayerCharacter>() == null )
		{
			return;
		}
		
		// Log.Info( $"SoccerBall collided with {collision.Other.GameObject}" );
		
		Components.Get<Rigidbody>().ApplyImpulseAt( collision.Contact.Point, collision.Contact.Normal * 5000.0f );

		GameObject.PlaySound( KickSound );
	}

	/*public void OnCollisionUpdate( Collision collision )
	{
		Log.Info( "SoccerBall is colliding with something" );
	}*/
}
