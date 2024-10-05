using System;
using Clover.Components;
using Clover.Inventory;

namespace Clover.Persistence;

public class PlayerSaveData
{

	public string PlayerId = Guid.NewGuid().ToString();
	public string Name;
	
	public List<InventorySlot<PersistentItem>> InventorySlots = new();
	public Dictionary<Equips.EquipSlot, PersistentItem> EquippedItems = new();
	
	public DateTime LastSave = DateTime.Now;
	
	public int Clovers;
	
	public PlayerSaveData()
	{
	}
	
	public PlayerSaveData( string playerId )
	{
		PlayerId = playerId;
	}

}
