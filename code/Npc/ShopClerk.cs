using Clover.Components;
using Clover.Player;
using Clover.Ui;
using Clover.WorldBuilder;

namespace Clover.Npc;

[Title( "Shop Clerk" )]
[Icon( "face" )]
[Category( "Clover/Npc" )]
public class ShopClerk : BaseNpc
{
	protected override void StateLogic()
	{
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		WorldRotation = Rotation.Slerp( WorldRotation, TargetLookAt, Time.Delta * 5f );

		var players = Components.Get<WorldLayerObject>().World.PlayersInWorld.ToList();

		if ( !players.Any() )
		{
			LookReset();
			return;
		}

		var player = players.Where( x => IsVisible( x.WorldPosition ) )
			.MinBy( p => p.WorldPosition.Distance( WorldPosition ) );

		if ( !player.IsValid() )
		{
			LookReset();
			return;
		}

		LookAt( player.WorldPosition );
	}

	private void LookReset()
	{
		TargetLookAt = Rotation.Identity;
	}

	private bool IsVisible( Vector3 pos )
	{
		var tr = Scene.Trace.Ray( WorldPosition + Vector3.Up * 32f, pos + Vector3.Up * 32f )
			.WithTag( "terrain" ).Run();
		// Gizmo.Draw.Line( WorldPosition + Vector3.Up * 32f, pos + Vector3.Up * 32f );
		return !tr.Hit;
	}

	public override void StartInteract( PlayerCharacter player )
	{
		Log.Info( "BaseNpc StartInteract" );

		if ( InteractionTarget.IsValid() )
		{
			Log.Error( "BaseNpc StartInteract: Busy" );
			return;
		}

		player.PlayerInteract.InteractionTarget = GameObject;
		player.ModelLookAt( WorldPosition );
		InteractionTarget = player.GameObject;
		SetState( NpcState.Interacting );

		var selectedItems = new HashSet<int>();
		var totalPrice = 0;

		/*DialogueManager.Instance.DialogueWindow.SetAction( "SelectItem", () =>
		{
			MainUi.Instance.Components.Get<InventorySelectUi>().Open( 10,
				( inventorySlot ) => inventorySlot.GetItem().ItemData.CanSell, ( items ) =>
				{
					Log.Info( "Selected item" );
					selectedItems = items;

					totalPrice = 0;
					foreach ( var index in selectedItems )
					{
						var slot = player.Inventory.Container.GetSlotByIndex( index );
						if ( slot == null || !slot.HasItem ) continue;

						var item = slot.GetItem();
						if ( item == null ) continue;

						totalPrice += item.ItemData.GetCustomSellPrice?.Invoke( TimeManager.Time ) ??
						              item.ItemData.BaseSellPrice;
					}
				}, () =>
				{
					Log.Info( "Cancelled" );
					DialogueManager.Instance.DialogueWindow.JumpToId( "canceled" );
				} );
		} );*/

		/*DialogueManager.Instance.DialogueWindow.SetAction( "SellItems", () =>
		{
			if ( selectedItems.Count == 0 )
			{
				DialogueManager.Instance.DialogueWindow.JumpToId( "no_items" );
				return;
			}

			if ( player.CloverBalanceController.GetBalance() < totalPrice )
			{
				DialogueManager.Instance.DialogueWindow.JumpToId( "no_money" );
				return;
			}

			player.CloverBalanceController.DeductClover( totalPrice );

			foreach ( var index in selectedItems )
			{
				var slot = player.Inventory.Container.GetSlotByIndex( index );
				if ( slot == null || !slot.HasItem ) continue;

				/*var item = slot.GetItem();
				if ( item == null ) continue;

				player.Inventory.Container.RemoveItem( item );#1#

				slot.TakeOneOrDelete();
			}

			DialogueManager.Instance.DialogueWindow.JumpToId( "sold" );
		} );*/

		DispatchDialogue();
	}
}
