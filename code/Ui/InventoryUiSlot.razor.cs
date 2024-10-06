using System;
using Clover.Data;
using Clover.Inventory;
using Clover.Persistence;
using Sandbox.UI;

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

	public override bool WantsDrag => true;

	protected override void OnDragStart( DragEvent e )
	{
		if ( Slot == null || !Slot.HasItem ) return;
		Log.Info( "OnDragStart" );

		AddClass( "dragging" );

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

		foreach ( var s in FindRootPanel().Descendants.OfType<InventoryUiSlot>() )
		{
			s.RemoveClass( "moving-to" );
		}
		
		foreach ( var s in FindRootPanel().Descendants.OfType<InventoryUiEquip>() )
		{
			s.RemoveClass( "moving-to" );
		}

		if ( Slot == null || !Slot.HasItem ) return;

		var slot = FindRootPanel().Descendants.OfType<InventoryUiSlot>()
			.FirstOrDefault( x => x.IsInside( e.ScreenPosition ) );
		if ( slot != null )
		{
			DropOnSlot( slot );
			return;
		}
		
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
		Log.Info( $"Dropped on {slot.Index}" );

		if ( Inventory.Id == slot.Inventory.Id )
		{
			Inventory.Container.MoveSlot( Slot.Index, slot.Index );
		}
		else
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
			_contextMenu.AddItem( action.Name, () =>
			{
				action.Action();
			} );
		}
		

		if ( item.ItemData.PlaceScene != null && item.ItemData.CanDrop )
		{
			_contextMenu.AddItem( "Place", () =>
			{
				Slot.Place();
			} );
		}

		if ( item.ItemData.DropScene != null && item.ItemData.CanDrop )
		{
			_contextMenu.AddItem( "Drop", () =>
			{
				Slot.Drop();
			} );
		}
	}
}
