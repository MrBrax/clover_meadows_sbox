using System;
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

	private Panel Ghost;

	public override bool WantsDrag => true;

	protected override void OnDragStart( DragEvent e )
	{
		if ( Slot == null || !Slot.HasItem ) return;
		Log.Info( "OnDragStart" );

		AddClass( "dragging" );

		Ghost = new Panel();
		Ghost.Style.Position = PositionMode.Absolute;
		Ghost.Style.Width = 130;
		Ghost.Style.Height = 130;
		Ghost.Style.BackgroundColor = new Color( 0, 0, 0, 0.8f );
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
		icon.Style.Width = 80;
		icon.Style.Height = 80;
		Ghost.AddChild( icon );

		FindRootPanel().AddChild( Ghost );
	}

	protected override void OnDragEnd( DragEvent e )
	{
		// Log.Info( "OnDragEnd" );
		RemoveClass( "dragging" );

		if ( Ghost.IsValid() )
		{
			Ghost.Delete();
		}

		if ( Slot == null || !Slot.HasItem ) return;

		var slot = FindRootPanel().Descendants.OfType<InventoryUiSlot>()
			.FirstOrDefault( x => x.IsInside( e.ScreenPosition ) );
		if ( slot == null ) return;

		Log.Info( $"Dropped on {slot.Index}" );

		if ( Inventory.Id == slot.Inventory.Id )
		{
			Inventory.Container.MoveSlot( Slot.Index, slot.Index );
		}
		else
		{
			throw new NotImplementedException();
		}
	}

	protected override void OnDragSelect( SelectionEvent e )
	{
		// Log.Info( "OnDragSelect" );
	}


	protected override void OnDrag( DragEvent e )
	{
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
		if ( !Slot.HasItem ) return;

		if ( _contextMenu.IsValid() )
		{
			Log.Info( "Deleting context menu" );
			_contextMenu.Delete();
			return;
		}

		Log.Info( "Creating context menu" );
		_contextMenu = new ContextMenu( this, Mouse.Position * ScaleFromScreen );
		_contextMenu.Title = Slot.GetName();

		if ( Slot.GetItem().ItemData.PlaceScene != null )
		{
			_contextMenu.AddItem( "Place", () =>
			{
				Slot.Place();
			} );
		}

		if ( Slot.GetItem().ItemData.DropScene != null )
		{
			_contextMenu.AddItem( "Drop", () =>
			{
				Slot.Drop();
			} );
		}
	}
}
