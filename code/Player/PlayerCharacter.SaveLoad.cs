using System;
using System.IO;
using System.Text.Json;
using Clover.Carriable;
using Clover.Data;
using Clover.Persistence;
using Clover.Player.Clover;

namespace Clover.Player;

public sealed partial class PlayerCharacter
{
	public static string SpawnPlayerId { get; set; }
	
	public string SaveFilePath => $"players/{PlayerId}.json";
	public PlayerSaveData SaveData { get; set; }

	private DateTime _lastPlayTimeUpdate;

	public void Save()
	{
		if ( IsProxy )
		{
			Log.Error( "Cannot save proxy player. Fix this call." );
			return;
		}

		Log.Info( $"Saving player {PlayerId}" );

		Scene.RunEvent<IPlayerSaved>( x => x.PrePlayerSave( this ) );

		SaveData ??= new PlayerSaveData( PlayerId );

		SaveData.Name = PlayerName ?? Network.Owner.DisplayName;

		SaveData.InventorySlots.Clear();
		foreach ( var slot in Inventory.Container.GetUsedSlots() )
		{
			SaveData.InventorySlots.Add( slot );
		}

		Log.Info( $"Saved {SaveData.InventorySlots.Count} inventory slots" );

		SaveData.EquippedItems.Clear();
		foreach ( var (slot, item) in Equips.EquippedItems )
		{
			var persistentItem = PersistentItem.Create( item );
			SaveData.EquippedItems.Add( slot, persistentItem );
		}

		Log.Info( $"Saved {SaveData.EquippedItems.Count} equipped items" );

		SaveData.Clovers = CloverBalanceController.GetBalance();

		if ( _lastPlayTimeUpdate != default )
		{
			SaveData.PlayTime += (DateTime.Now - _lastPlayTimeUpdate).TotalSeconds;
		}

		_lastPlayTimeUpdate = DateTime.Now;

		SaveData.LastSave = DateTime.Now;

		var json = JsonSerializer.Serialize( SaveData, GameManager.JsonOptions );

		FileSystem.Data.CreateDirectory( "players" );

		FileSystem.Data.WriteAllText( SaveFilePath, json );

		Scene.RunEvent<IPlayerSaved>( x => x.PostPlayerSave( this ) );
	}

	/*public void NewGame()
	{
		SaveData = new PlayerSaveData( PlayerId );
		PlayerName = Network.Owner.DisplayName;
		CloverBalanceController.SetStartingClovers();
		SaveData.LastLoad = DateTime.Now;
		Save();
	}*/

	public void Load()
	{
		if ( IsProxy )
		{
			Log.Error( "Cannot load proxy player. Fix this call." );
			return;
		}

		// TODO: temporary fix for player id
		if ( string.IsNullOrEmpty( PlayerId ) )
		{
			/*var save = FileSystem.Data.FindFile( "players", "*.json" );
			if ( save != null && save.Any() )
			{
				PlayerId = Path.GetFileNameWithoutExtension( save.First() );
				Log.Info( $"PlayerId found: {PlayerId}" );
			}
			else
			{
				PlayerId = Guid.NewGuid().ToString();
				Log.Info( $"PlayerId generated: {PlayerId}" );
				NewGame();
				return;
			}*/
			PlayerId = SpawnPlayerId;
			
			if ( string.IsNullOrEmpty( PlayerId ) )
			{
				throw new Exception( "PlayerId is null" );
			}
		}

		if ( !FileSystem.Data.FileExists( SaveFilePath ) )
		{
			Log.Warning( $"File {SaveFilePath} does not exist" );
			return;
		}

		var json = FileSystem.Data.ReadAllText( SaveFilePath );
		SaveData = JsonSerializer.Deserialize<PlayerSaveData>( json, GameManager.JsonOptions );

		PlayerName = SaveData.Name;

		CloverBalanceController.SetClovers( SaveData.Clovers );
		
		SaveData.LastLoad = DateTime.Now;

		// limit inventory slots if for some reason it exceeds max items
		if ( SaveData.InventorySlots.Count > Inventory.Container.MaxItems )
		{
			Log.Error( "Inventory slots count exceeds max items" );
			SaveData.InventorySlots = SaveData.InventorySlots.Take( Inventory.Container.MaxItems ).ToList();
		}

		Inventory.Container.RemoveSlots();

		foreach ( var slot in SaveData.InventorySlots )
		{
			if ( slot.GetItem() == null )
			{
				Log.Error( "Item is null" );
				continue;
			}

			if ( slot.GetItem().ItemData == null )
			{
				Log.Error( "ItemData is null" );
				continue;
			}

			Inventory.Container.ImportSlot( slot );
		}

		Inventory.Container.RecalculateIndexes();

		foreach ( var (slot, item) in SaveData.EquippedItems )
		{
			if ( item.ItemData is ToolData toolData )
			{
				BaseCarriable carriableNode;
				// var carriableNode = item.SpawnCarriable();

				try
				{
					carriableNode = item.SpawnCarriable();
				}
				catch ( Exception e )
				{
					Log.Error( $"Failed to spawn carriable: {e.Message}" );
					continue;
				}

				if ( carriableNode == null )
				{
					Log.Error( $"Item is not a carriable" );
					continue;
				}

				Equips.SetEquippedItem( slot, carriableNode.GameObject );
			}
		}
	}
}
