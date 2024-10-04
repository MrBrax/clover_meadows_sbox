using Clover.Data;
using Clover.Player;

namespace Clover.Ui;

public partial class DebugWindow
{
	private void SpawnItem( ItemData item )
	{
		WorldManager.Instance.ActiveWorld.SpawnPlacedNode( item, PlayerCharacter.Local.GetAimingGridPosition(),
			World.ItemRotation.North, World.ItemPlacement.Floor );
	}
}
