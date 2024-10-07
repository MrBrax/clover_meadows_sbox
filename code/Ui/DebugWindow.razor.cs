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
		
		Sound.Play( "sounds/interact/item_pickup.sound", PlayerCharacter.Local.WorldPosition );
		
	}

	[ConCmd( "spawn_cloud_item" )]
	public static async void SpawnCloudItem( string packageIdent )
	{
		var package = await Package.Fetch( packageIdent, false );
		
		if ( package == null )
		{
			Log.Error( $"Failed to fetch package {packageIdent}" );
			return;
		}
		
		Log.Info( package.Title );
		
		var pp = package.GetMeta<string>("ParentPackage");

		if ( pp == null || pp != Game.Ident )
		{
			Log.Error( $"Package {packageIdent} is not supported" );
			return;
		}

	}
}
