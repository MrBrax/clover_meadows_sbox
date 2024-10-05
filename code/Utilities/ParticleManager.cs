using Sandbox;

namespace Braxnet;

public sealed class ParticleManager : Component
{
	
	public static ParticleManager Instance => Game.ActiveScene.GetAllComponents<ParticleManager>().FirstOrDefault();

	[Property] public GameObject Poof { get; set; }
	[Property] public GameObject Confetti { get; set; }

	[Broadcast]
	public static void PoofAt( Vector3 position, float scale = 1 )
	{
		var poof = Instance.Poof.Clone();
		poof.WorldPosition = position;
		
		poof.GetComponent<ParticleSphereEmitter>().Radius = 20 * scale;
		poof.GetComponent<ParticleSpriteRenderer>().Scale = scale;
	}
	
	[Broadcast]
	public static void ConfettiAt( Vector3 position )
	{
		var confetti = Instance.Confetti.Clone();
		confetti.WorldPosition = position;
		confetti.WorldRotation = Rotation.FromPitch( -90 );
		// return confetti;
	}
}
