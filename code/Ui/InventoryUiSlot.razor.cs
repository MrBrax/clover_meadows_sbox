using Clover.Inventory;
using Clover.Persistence;

namespace Clover.Ui;

public partial class InventoryUiSlot
{
	public Inventory.Inventory Inventory;
	public int Index;
	
	private InventorySlot<PersistentItem> _slot;
	public InventorySlot<PersistentItem> Slot
	{
		get => _slot;
		set
		{
			_slot = value;
			StateHasChanged();
			// UpdateSlot();
		}
	}
	
}
