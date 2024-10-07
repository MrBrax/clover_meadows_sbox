using Clover.Player;
using Sandbox.UI;

namespace Clover;

public partial class MainUi : IPlayerSaved, IWorldSaved
{
	
	private Panel IsSavingPanel { get; set; }

	public void PrePlayerSave( PlayerCharacter player )
	{
		IsSavingPanel.FlashClass( "saving", 2 );
		
	}

	public void PostPlayerSave( PlayerCharacter player )
	{
		
	}

	public void PreWorldSaved( World world )
	{
		IsSavingPanel.FlashClass( "saving", 2 );
	}

	public void PostWorldSaved( World world )
	{
		
	}
}
