using Clover.Inventory;
using Clover.Persistence;
using Clover.Player;
using Sandbox.UI;

namespace Clover.Ui;

public partial class InventoryUi : IPlayerSpawned
{
	
	private Inventory.Inventory Inventory => PlayerCharacter.Local.Inventory;
	private IEnumerable<InventorySlot<PersistentItem>> Slots => Inventory.Container.Slots;

	public bool Show;
	
	private Panel SlotContainer { get; set; }

	/*protected override void OnAwake()
	{
		base.OnAwake();
		
		// PlayerCharacter.Local.Inventory.Container.InventoryChanged += UpdateInventory;
		
		UpdateInventory();
	}*/

	protected override void OnEnabled()
	{
		base.OnEnabled();
		UpdateInventory();
		Panel.Style.Display = DisplayMode.None;
	}
	
	private const int SlotsPerRow = 5;

	private void UpdateInventory()
	{
		if ( !SlotContainer.IsValid() )
		{
			Log.Error( "SlotContainer is null" );
			return;
		}
		
		SlotContainer.DeleteChildren();

		if ( Inventory == null )
		{
			Log.Error( "Inventory is null" );
			return;
		}

		Panel row = null;
		
		foreach ( var entry in Inventory.Container.GetEnumerator() )
		{
			var rowNumber = entry.Index / SlotsPerRow;
			var columnNumber = entry.Index % SlotsPerRow;
			if ( row == null || columnNumber == 0 )
			{
				row = new Panel();
				row.AddClass( "inventory-row" );
				SlotContainer.AddChild( row );
			}
			
			var slotButton = new InventoryUiSlot();
			slotButton.Index = entry.Index;
			slotButton.Slot = entry.HasSlot ? entry.Slot : null;
			slotButton.Inventory = Inventory;
			slotButton.AddClass( entry.HasSlot ? "has-item" : "empty" );
			
			// SlotContainer.AddChild( slotButton );
			row.AddChild( slotButton );

		}
		
		StateHasChanged();
		
	}

	void IPlayerSpawned.OnPlayerSpawned( PlayerCharacter player )
	{
		if ( player == PlayerCharacter.Local )
		{
			player.Inventory.Container.InventoryChanged += UpdateInventory;
			UpdateInventory();
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		
		if ( Input.Pressed( "Inventory") )
		{
			Show = !Show;
			Panel.Style.Display = Show ? DisplayMode.Flex : DisplayMode.None;
			if ( Show ) UpdateInventory();
			StateHasChanged();
			Sound.Play( "sounds/ui/inventory_toggle.sound" );
		}
	}

	/// <summary>
	/// the hash determines if the system should be rebuilt. If it changes, it will be rebuilt
	/// </summary>
	// protected override int BuildHash() => System.HashCode.Combine( MyStringValue );
}
