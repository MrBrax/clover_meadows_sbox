using System;
using Clover.Inventory;
using Clover.Persistence;
using Clover.Player;
using Sandbox.UI;

namespace Clover.Ui;

public partial class InventoryUi : IInventoryEvent
{
	public static InventoryUi Instance => Game.ActiveScene.GetAllComponents<InventoryUi>().FirstOrDefault();

	private Inventory.Inventory Inventory => PlayerCharacter.Local?.Inventory;
	private IEnumerable<InventorySlot> Slots => Inventory?.Container.Slots;

	public bool Show;

	private Panel SlotContainer { get; set; }

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
			// Log.Error( "SlotContainer is null" );
			return;
		}

		SlotContainer.DeleteChildren();

		if ( Inventory == null )
		{
			Log.Error( "Inventory is null" );
			return;
		}

		Panel row = null;

		foreach ( var entry in Inventory.Container.QuerySlots() )
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

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( Input.Pressed( "Inventory" ) )
		{
			Show = !Show;
			Panel.Style.Display = Show ? DisplayMode.Flex : DisplayMode.None;
			if ( Show ) UpdateInventory();
			StateHasChanged();
			Sound.Play( "sounds/ui/inventory_toggle.sound" );
		}
	}

	public void Close()
	{
		Show = false;
		Panel.Style.Display = DisplayMode.None;
		StateHasChanged();
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Show, Inventory );
	}

	public void OnInventoryChanged( InventoryContainer container )
	{
		if ( container.Owner == Inventory?.GameObject )
		{
			UpdateInventory();
		}
	}
}
