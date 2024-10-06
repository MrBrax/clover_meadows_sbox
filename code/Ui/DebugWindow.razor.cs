using Clover.Data;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Ui;

public partial class DebugWindow
{
	private void SpawnItem( ItemData item )
	{
		
		/*var direction = PlayerCharacter.Local.GetAimingDirection();
		
		WorldManager.Instance.ActiveWorld.SpawnPlacedNode( item, PlayerCharacter.Local.GetAimingGridPosition(),
			World.ItemRotation.North, World.ItemPlacement.Floor );*/

		var pItem = item.CreatePersistentItem();
		
		PlayerCharacter.Local.Inventory.PickUpItem( pItem );
		
	}
}
