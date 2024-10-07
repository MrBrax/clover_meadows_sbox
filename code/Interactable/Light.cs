using Clover.Persistence;
using Clover.Player;

namespace Clover.Interactable;

[Category( "Clover/Interactable" )]
[Icon( "lightbulb" )]
public class Light : Component, IInteract, IPersistent
{

	[Property] public bool IsOn { get; set; } = true;
	
	[Property] public List<GameObject> Targets { get; set; } = new();
	
	[Property] public SoundEvent LightSwitchSound { get; set; }


	public void StartInteract( PlayerCharacter player )
	{
		Toggle();
		Sound.Play( LightSwitchSound, WorldPosition );
	}
	
	public void FinishInteract( PlayerCharacter player )
	{
		
	}

	public void Toggle()
	{
		IsOn = !IsOn;
		UpdateState();
	}
	
	public void TurnOn()
	{
		IsOn = true;
		UpdateState();
	}
	
	public void TurnOff()
	{
		IsOn = false;
		UpdateState();
	}

	private void UpdateState()
	{
		foreach ( var target in Targets )
		{
			/*if ( target.Components.TryGet<Sandbox.Light>( out var light ) )
			{
				light.
			}*/
			target.Enabled = IsOn;
		}
	}

	public void OnSave( PersistentItem item )
	{
		item.SetArbitraryData( "IsOn", IsOn );
		
	}

	public void OnLoad( PersistentItem item )
	{
		IsOn = item.GetArbitraryData<bool>( "IsOn" );
		UpdateState();
	}
}
