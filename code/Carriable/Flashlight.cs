namespace Clover.Carriable;

[Category( "Clover/Carriable" )]
public class Flashlight : BaseCarriable
{
	// private bool _enabled;

	[Property] public Light Light { get; set; }

	public override void OnUseDown()
	{
		base.OnUseDown();

		if ( !CanUse() ) return;

		NextUse = 0.1f;

		// _enabled = !_enabled;
		Light.Enabled = !Light.Enabled;
	}
}
