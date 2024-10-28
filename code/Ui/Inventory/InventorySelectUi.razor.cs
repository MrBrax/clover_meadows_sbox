using System;
using System.Threading.Tasks;
using Clover.Inventory;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Ui;

public partial class InventorySelectUi
{
	private InventoryContainer Inventory => PlayerCharacter.Local?.Inventory.Container;
	private HashSet<int> SelectedItemIndexes = new();
	public int MaxItems { get; set; } = 1;

	public delegate bool CanSelectItemDelegate( InventorySlot<PersistentItem> slot );

	[Property] public CanSelectItemDelegate CanSelectItem { get; set; }


	public Action<HashSet<int>> OnSelect { get; set; }

	public Action OnCancel { get; set; }


	private TaskCompletionSource<HashSet<int>> _selectTaskCompletionSource;

	public async Task<HashSet<int>> SelectItems( int maxItems, CanSelectItemDelegate canSelectItem )
	{
		_selectTaskCompletionSource = new TaskCompletionSource<HashSet<int>>();
		Open( maxItems, canSelectItem, OnSelectTask, OnCancelTask );
		return await _selectTaskCompletionSource.Task;
	}

	private void OnSelectTask( HashSet<int> indexes )
	{
		_selectTaskCompletionSource.SetResult( indexes );
	}

	private void OnCancelTask()
	{
		_selectTaskCompletionSource.SetResult( null );
	}

	private void ToggleItem( int index )
	{
		if ( CanSelectItem != null && !CanSelectItem( Inventory.GetSlotByIndex( index ) ) )
		{
			PlayerCharacter.Local.Notify( Notifications.NotificationType.Error, "You can't select this item" );
			return;
		}

		if ( SelectedItemIndexes.Contains( index ) )
		{
			SelectedItemIndexes.Remove( index );
			return;
		}

		if ( SelectedItemIndexes.Count >= MaxItems )
		{
			if ( MaxItems == 1 )
			{
				SelectedItemIndexes.Clear();
				SelectedItemIndexes.Add( index );
			}
			else
			{
				PlayerCharacter.Local.Notify( Notifications.NotificationType.Error, "You can't select more items" );
			}

			return;
		}

		if ( !SelectedItemIndexes.Add( index ) )
		{
			SelectedItemIndexes.Remove( index );
		}
	}

	private void Select()
	{
		if ( SelectedItemIndexes.Count == 0 )
		{
			PlayerCharacter.Local.Notify( Notifications.NotificationType.Error, "You must select at least one item" );
			return;
		}

		OnSelect?.Invoke( SelectedItemIndexes );
		ResetSelection();
		Enabled = false;
	}

	private void Cancel()
	{
		OnCancel?.Invoke();
		ResetSelection();
		Enabled = false;
	}

	private void ResetSelection()
	{
		SelectedItemIndexes.Clear();
		OnSelect = null;
		OnCancel = null;
	}

	public async void Open( int maxItems, CanSelectItemDelegate canSelectItem, Action<HashSet<int>> onSelect,
		Action onCancel )
	{
		MaxItems = maxItems;
		CanSelectItem = canSelectItem;
		OnSelect = onSelect;
		OnCancel = onCancel;
		await Task.Delay( 1 );
		StateHasChanged();
	}

	/// <summary>
	/// the hash determines if the system should be rebuilt. If it changes, it will be rebuilt
	/// </summary>
	protected override int BuildHash() =>
		System.HashCode.Combine( MaxItems, CanSelectItem, OnSelect, OnCancel, SelectedItemIndexes );
}
