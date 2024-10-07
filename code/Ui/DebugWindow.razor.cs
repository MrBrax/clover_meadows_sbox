using System;
using Clover.Data;
using Clover.Items;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Ui;

public partial class DebugWindow
{
	public bool IsVisible { get; set; }
	
	private void ToggleVisibility()
	{
		IsVisible = !IsVisible;
	}

	protected override void OnFixedUpdate()
	{
		
		if ( Input.Released( "DebugWindow" ) )
		{
			Log.Info( "Debug key pressed" );
			ToggleVisibility();
		}
		
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( IsVisible );
	}

	private void SpawnItem( ItemData item )
	{
		var pItem = item.CreatePersistentItem();
		
		PlayerCharacter.Local.Inventory.PickUpItem( pItem );
		
		Sound.Play( "sounds/interact/item_pickup.sound", PlayerCharacter.Local.WorldPosition );
		
	}
	
	private void SpawnObject( ObjectData item )
	{
		var gameObject = item.Prefab.Clone();
		
		var worldObject = gameObject.GetComponent<WorldObject>();
		worldObject.WorldLayerObject.SetLayer( PlayerCharacter.Local.WorldLayerObject.Layer, true );
		
		worldObject.WorldPosition = PlayerCharacter.Local.WorldPosition;
		
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
