using System;
using System.Threading.Tasks;
using Clover.Inventory;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Ui;

public partial class InventorySelectUi
{
	private InventoryContainer Inventory => PlayerCharacter.Local?.Inventory.Container;
	private readonly HashSet<int> _selectedItemIndexes = new();
	public int MaxItems { get; set; } = 1;

	public delegate bool CanSelectItemDelegate( InventorySlot slot );

	[Property] public CanSelectItemDelegate CanSelectItem { get; set; }


	public Action<HashSet<int>> OnSelect { get; set; }

	public Action OnCancel { get; set; }


	private TaskCompletionSource<HashSet<int>> _selectTaskCompletionSource;

	public async Task<HashSet<int>> SelectItems( int maxItems, CanSelectItemDelegate canSelectItem )
	{
		Log.Info( $"SelectItems setup: {maxItems}" );
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
		_selectTaskCompletionSource.SetResult( new HashSet<int>() ); // TODO: null doesn't play nice with actiongraph
	}

	private void ToggleItem( int index )
	{
		if ( CanSelectItem != null && !CanSelectItem( Inventory.GetSlotByIndex( index ) ) )
		{
			PlayerCharacter.Local.Notify( Notifications.NotificationType.Error, "You can't select this item" );
			return;
		}

		if ( _selectedItemIndexes.Contains( index ) )
		{
			_selectedItemIndexes.Remove( index );
			return;
		}

		if ( _selectedItemIndexes.Count >= MaxItems )
		{
			if ( MaxItems == 1 )
			{
				_selectedItemIndexes.Clear();
				_selectedItemIndexes.Add( index );
			}
			else
			{
				PlayerCharacter.Local.Notify( Notifications.NotificationType.Error, "You can't select more items" );
			}

			return;
		}

		if ( !_selectedItemIndexes.Add( index ) )
		{
			_selectedItemIndexes.Remove( index );
		}
	}

	private void Select()
	{
		if ( _selectedItemIndexes.Count == 0 )
		{
			PlayerCharacter.Local.Notify( Notifications.NotificationType.Error, "You must select at least one item" );
			return;
		}

		OnSelect?.Invoke( _selectedItemIndexes );
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
		_selectedItemIndexes.Clear();
		OnSelect = null;
		OnCancel = null;
	}

	private void Open( int maxItems, CanSelectItemDelegate canSelectItem, Action<HashSet<int>> onSelect,
		Action onCancel )
	{
		Log.Info( $"Opening inventory select ui: {maxItems}" );
		_selectedItemIndexes.Clear();
		Enabled = true;
		MaxItems = maxItems;
		CanSelectItem = canSelectItem;
		OnSelect = onSelect;
		OnCancel = onCancel;
		// await Task.Delay( 1 );
		StateHasChanged();
	}

	/// <summary>
	/// the hash determines if the system should be rebuilt. If it changes, it will be rebuilt
	/// </summary>
	protected override int BuildHash() =>
		HashCode.Combine( Enabled, MaxItems, CanSelectItem, OnSelect, OnCancel, _selectedItemIndexes );
}
