namespace Clover;

public static class SoundEx
{
	
	[Broadcast]
	public static void Play( SoundEvent soundEvent, Vector3 position )
	{
		Sound.Play( soundEvent, position );
	}
	
}
