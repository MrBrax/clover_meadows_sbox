using System;
using Clover.Components;
using Clover.Inventory;

namespace Clover.Persistence;

public class PlayerSaveData
{
	public string PlayerId = Guid.NewGuid().ToString();
	public string Name;

	public List<InventorySlot> InventorySlots = new();
	public Dictionary<Equips.EquipSlot, PersistentItem> EquippedItems = new();

	public DateTime Created = DateTime.Now;
	public DateTime LastSave = DateTime.Now;
	public DateTime LastLoad = DateTime.Now;
	public double PlayTime;

	public int Clovers;

	public PlayerSaveData()
	{
	}

	public PlayerSaveData( string playerId )
	{
		PlayerId = playerId;
	}
}
