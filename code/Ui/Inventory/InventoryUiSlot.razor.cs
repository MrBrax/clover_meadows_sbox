using System;
using Clover.Data;
using Clover.Inventory;
using Clover.Persistence;
using Sandbox.UI;

namespace Clover.Ui;

public partial class InventoryUiSlot
{
	[Parameter] public Inventory.Inventory Inventory;
	[Parameter] public int Index;

	// public override bool HasTooltip => true;

	private InventorySlot _slot;

	public InventorySlot Slot
	{
		get => _slot;
		set
		{
			_slot = value;
			StateHasChanged();
			// UpdateSlot();
		}
	}

	protected override void OnAfterTreeRender( bool firstTime )
	{
		base.OnAfterTreeRender( firstTime );
		// Tooltip = Slot != null && Slot.HasItem ? Slot.GetItem().GetName() : null;
		SetClass( "has-item", Slot != null && Slot.HasItem );
		SetClass( "empty", Slot == null || !Slot.HasItem );
	}

	protected override void OnDoubleClick( MousePanelEvent e )
	{
		base.OnDoubleClick( e );

		if ( Slot == null || !Slot.HasItem ) return;

		if ( Slot.GetItem().ItemData is ToolData toolData )
		{
			Slot.Equip();
		}
	}

	private Panel Ghost;

	/// <summary>
	///  This has to be true to receive drag events, otherwise it just tries to scroll the panel.
	/// </summary>
	public override bool WantsDrag => true;

	protected override void OnDragStart( DragEvent e )
	{
		if ( Slot == null || !Slot.HasItem ) return;
		Log.Info( "OnDragStart" );

		AddClass( "dragging" );

		// I make a "ghost" panel that follows the mouse cursor instead of dragging the actual panel, because
		// dragging the actual panel causes all sorts of issues with layout. The ghost panel is added to the root panel,
		// which means that the stylesheet for this panel does not apply to it, so the style has to be set manually.

		Ghost = new Panel();
		Ghost.Style.Position = PositionMode.Absolute;
		Ghost.Style.Width = 100;
		Ghost.Style.Height = 100;
		Ghost.Style.BackgroundColor = new Color( 255, 255, 255, 0.8f );
		Ghost.Style.AlignContent = Align.Center;
		Ghost.Style.JustifyContent = Justify.Center;
		Ghost.Style.AlignItems = Align.Center;
		Ghost.Style.BorderBottomLeftRadius = Ghost.Style.BorderBottomRightRadius =
			Ghost.Style.BorderTopLeftRadius = Ghost.Style.BorderTopRightRadius = 10;
		Ghost.Style.ZIndex = 1000;
		// Ghost.Style.BackgroundImage = Slot.GetItem().GetIconTexture();
		// Ghost.Style.BackgroundRepeat = BackgroundRepeat.NoRepeat;
		// Ghost.Style.BackgroundPositionX = Length.Cover;
		// Ghost.Style.BackgroundPositionY = Length.Cover;

		var icon = new Image();
		icon.Texture = Slot.GetItem().GetIconTexture();
		icon.Style.Width = Ghost.Style.Width.GetValueOrDefault().Value * 0.8f;
		icon.Style.Height = Ghost.Style.Height.GetValueOrDefault().Value * 0.8f;
		Ghost.AddChild( icon );

		FindRootPanel().AddChild( Ghost );

		Sound.Play( "sounds/ui/inventory_start_drag.sound" );
	}

	protected override void OnDragEnd( DragEvent e )
	{
		Log.Info( "OnDragEnd" );
		RemoveClass( "dragging" );

		if ( Ghost.IsValid() )
		{
			Ghost.Delete();
		}

		// Clear all the "moving-to" classes from all slots and equips

		foreach ( var s in FindRootPanel().Descendants.OfType<InventoryUiSlot>() )
		{
			s.RemoveClass( "moving-to" );
		}

		foreach ( var s in FindRootPanel().Descendants.OfType<InventoryUiEquip>() )
		{
			s.RemoveClass( "moving-to" );
		}

		if ( Slot == null || !Slot.HasItem ) return;

		// Check if we are over a slot or equip, IsInside is the only way to do this I think, but if there's a better way please tell me.
		var slot = FindRootPanel().Descendants.OfType<InventoryUiSlot>()
			.FirstOrDefault( x => x.IsInside( e.ScreenPosition ) );
		if ( slot != null )
		{
			DropOnSlot( slot );
			return;
		}

		// Check equips
		var equip = FindRootPanel().Descendants.OfType<InventoryUiEquip>()
			.FirstOrDefault( x => x.IsInside( e.ScreenPosition ) );

		if ( equip != null )
		{
			DropOnEquip( equip );
			return;
		}
	}

	private void DropOnSlot( InventoryUiSlot slot )
	{
		Log.Info( $"Dropped on slot #{slot.Index}" );

		if ( Inventory.Id == slot.Inventory.Id ) // Same inventory
		{
			Inventory.Container.MoveSlot( Slot.Index, slot.Index );
		}
		else // Different inventory, there are no storage containers yet, so this is not implemented
		{
			throw new NotImplementedException();
		}

		Sound.Play( "sounds/ui/inventory_stop_drag.sound" );
	}

	private void DropOnEquip( InventoryUiEquip equip )
	{
		Log.Info( $"Dropped on {equip.Slot}" );

		if ( Inventory.Id == equip.Inventory.Id )
		{
			Slot.Equip();
		}
		else
		{
			throw new NotImplementedException();
		}

		Sound.Play( "sounds/ui/inventory_stop_drag.sound" );
	}


	private Panel _lastHovered;

	// This is called continuously while dragging, so we can use it to highlight the slot/equip we are hovering over
	protected override void OnDragSelect( SelectionEvent e )
	{
		if ( Slot == null || !Slot.HasItem ) return;

		var slot = FindRootPanel().Descendants.OfType<InventoryUiSlot>()
			.FirstOrDefault( x => x.IsInside( e.EndPoint ) );

		var equip = FindRootPanel().Descendants.OfType<InventoryUiEquip>()
			.FirstOrDefault( x => x.IsInside( e.EndPoint ) );

		if ( slot == null && equip == null ) return;

		// Log.Info( $"Selected on {slot.Index}" );

		foreach ( var s in FindRootPanel().Descendants.OfType<InventoryUiSlot>() )
		{
			s.RemoveClass( "moving-to" );
		}

		foreach ( var s in FindRootPanel().Descendants.OfType<InventoryUiEquip>() )
		{
			s.RemoveClass( "moving-to" );
		}

		slot?.AddClass( "moving-to" );
		equip?.AddClass( "moving-to" );

		Panel currentHovered = slot != null ? slot : equip;
		if ( _lastHovered != currentHovered )
		{
			Sound.Play( "sounds/ui/inventory_hover_drag.sound" );
			_lastHovered = currentHovered;
		}
	}

	// This is also called continuously while dragging, we use it to move the ghost panel.
	// I don't know the difference between OnDrag and OnDragSelect, but both seem to be called.
	protected override void OnDrag( DragEvent e )
	{
		if ( Slot == null || !Slot.HasItem ) return;
		if ( !Ghost.IsValid() ) return;
		Ghost.Style.Top = (e.ScreenPosition.y - e.LocalGrabPosition.y) * ScaleFromScreen;
		Ghost.Style.Left = (e.ScreenPosition.x - e.LocalGrabPosition.x) * ScaleFromScreen;
	}

	public override void OnDeleted()
	{
		base.OnDeleted();
		if ( Ghost.IsValid() )
		{
			Ghost.Delete();
		}
	}

	private ContextMenu _contextMenu;

	// Custom context menu on right click, there's also Popup, but for easier styling I made my own ContextMenu class.
	// The context menu shows different options depending on the item in the slot.
	protected override void OnRightClick( MousePanelEvent e )
	{
		if ( Slot == null || !Slot.HasItem ) return;

		if ( _contextMenu.IsValid() )
		{
			Log.Info( "Deleting context menu" );
			_contextMenu.Delete();
			return;
		}

		Log.Info( "Creating context menu" );
		_contextMenu = new ContextMenu( this, Mouse.Position * ScaleFromScreen );
		_contextMenu.Title = Slot.GetName();

		var item = Slot.GetItem();

		/*if ( item.ItemData is ToolData toolData )
		{
			_contextMenu.AddItem( "Equip", () =>
			{
				Slot.Equip();
			} );
		}*/

		foreach ( var action in item.ItemData.GetActions( Slot ) )
		{
			_contextMenu.AddItem( action.Name, action.Icon, () =>
			{
				action.Action();
				_contextMenu.Delete();
			} );
		}


		if ( item.ItemData.PlaceScene != null && item.ItemData.CanPlace )
		{
			_contextMenu.AddItem( "Place", "file_download", () =>
			{
				Slot.Place();
				_contextMenu.Delete();
			} );
		}

		if ( item.ItemData.CanDrop )
		{
			_contextMenu.AddItem( "Drop", "work", () =>
			{
				Slot.Drop();
				_contextMenu.Delete();
			} );
		}

		_contextMenu.AddItem( "Destroy", "delete", () =>
		{
			Slot.Destroy();
			_contextMenu.Delete();
		} );
	}
}
