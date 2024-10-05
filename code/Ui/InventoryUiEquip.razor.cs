using System;
using Clover.Carriable;
using Clover.Components;
using Clover.Persistence;
using Sandbox.UI;

namespace Clover.Ui;

public partial class InventoryUiEquip : IEquipChanged
{
	public Inventory.Inventory Inventory { get; set; }

	public Equips.EquipSlot Slot { get; set; }

	private BaseCarriable Tool => Inventory.Player.Equips.GetEquippedItem<BaseCarriable>( Slot );
	
	private bool HasItem => Inventory.Player.Equips.HasEquippedItem( Slot );

	public void OnEquippedItemChanged( GameObject owner, Equips.EquipSlot slot, GameObject item )
	{
		if ( Inventory.GameObject != owner || slot != Slot )
		{
			Log.Info( "InventoryUiEquip: OnEquippedItemChanged: Not this slot" );
			return;
		}

		StateHasChanged();
	}

	public void OnEquippedItemRemoved( GameObject owner, Equips.EquipSlot slot )
	{
		if ( Inventory.GameObject != owner || slot != Slot )
		{
			Log.Info( "InventoryUiEquip: OnEquippedItemRemoved: Not this slot" );
			return;
		}

		StateHasChanged();
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Inventory, Slot, Tool );
	}

	protected override void OnRightClick( MousePanelEvent e )
	{
		base.OnRightClick( e );
		if ( Tool == null ) return;
		
		Unequip();
		
	}

	protected override void OnDoubleClick( MousePanelEvent e )
	{
		base.OnDoubleClick( e );
		if ( Tool == null ) return;
		
		Unequip();
	}

	public void Unequip( int targetSlot = -1 )
	{
		// Inventory.Player.Equips.RemoveEquippedItem( Slot );

		var freeIndex = Inventory.Container.GetFirstFreeEmptyIndex();
		if ( freeIndex == -1 )
		{
			Log.Error( "No free slots available" );
			return;
		}
		
		targetSlot = targetSlot == -1 ? freeIndex : targetSlot;
		
		var item = Inventory.Player.Equips.GetEquippedItem( Slot );
		if ( item == null )
		{
			Log.Error( "No item equipped" );
			return;
		}

		var persistentItem = PersistentItem.Create( item );
		
		Inventory.Container.AddItemToIndex( persistentItem, targetSlot );
		
		Inventory.Player.Equips.RemoveEquippedItem( Slot, true );
		
		Inventory.Player.Save();
		
		StateHasChanged();

	}
	
	private Panel Ghost;
	public override bool WantsDrag => true;
	
	protected override void OnDragStart( DragEvent e )
	{
		if ( !HasItem ) return;

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

		var icon = new Image();
		
		var texture = Tool?.ItemData.GetIconTexture();
		
		icon.Texture = texture;
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

		if ( !HasItem ) return;

		var slot = FindRootPanel().Descendants.OfType<InventoryUiSlot>()
			.FirstOrDefault( x => x.IsInside( e.ScreenPosition ) );
		if ( slot != null )
		{
			DropOnSlot( slot );
			return;
		}
		
	}

	private void DropOnSlot( InventoryUiSlot slot )
	{
		Log.Info( $"Dropped on {slot.Index}" );

		if ( Inventory.Id == slot.Inventory.Id )
		{
			Unequip( slot.Index );
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
		if ( !HasItem ) return;

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
		if (!HasItem ) return;
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
	
}
