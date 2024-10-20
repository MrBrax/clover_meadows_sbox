using Clover.Player;

namespace Clover.Camera;

[Category( "Clover/Camera" )]
[Icon( "camera" )]
public class CameraTrigger : Component, Component.ITriggerListener
{
	[Property] public CameraNode Camera { get; set; }


	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		var player = other.GetComponent<PlayerCharacter>();
		if ( !player.IsValid() || player != PlayerCharacter.Local )
		{
			return;
		}

		Camera.Priority = 100;
	}

	void ITriggerListener.OnTriggerExit( Collider other )
	{
		var player = other.GetComponent<PlayerCharacter>();
		if ( !player.IsValid() || player != PlayerCharacter.Local )
		{
			return;
		}

		Camera.Priority = 0;
	}
}
