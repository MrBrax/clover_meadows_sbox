using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Clover.Components;
using Clover.Data;
using Clover.Items;
using Clover.Npc;
using Clover.Persistence;
using Clover.Player;
using Clover.Ui;
using Sandbox.Diagnostics;

namespace Clover.WorldBuilder;

[Category( "Clover/World" )]
public class ShopManager : Component
{
	[Property] public string StoreId { get; set; }

	[Property] public List<ShopDisplay> Displays { get; set; }

	[Property] public ShopClerk ShopClerk { get; set; }

	[Property] public List<CatalogueData> Catalogues { get; set; }

	[Property] public Dialogue BuyItemDialogue { get; set; }

	private CatalogueData PickRandomCatalogue()
	{
		var availableCatalogues = Catalogues.FindAll( c => c.IsCurrentlyInsideAvailablePeriod );
		if ( availableCatalogues.Count == 0 )
			return null;

		return Random.Shared.FromList( availableCatalogues );
	}

	private bool HasItem( string itemId )
	{
		return Items.Exists( i => i.ItemId == itemId );
	}

	private void GenerateItems()
	{
		Log.Info( "Generating shop items." );
		foreach ( var display in Displays )
		{
			if ( !display.IsValid() )
			{
				Log.Error( $"Shop: Display {display} is not valid." );
				continue;
			}

			Log.Info( $"Generating items for display {display}." );

			display.ShopManager = this;

			var catalogue = PickRandomCatalogue();
			if ( catalogue == null )
			{
				Log.Error( $"Shop: No available catalogues found for display {display}." );
				continue;
			}

			var nonAddedItems = catalogue.Items
				.Where( i => !HasItem( i.GetIdentifier() ) && i.GetMaxBounds() <= display.Size ).ToList();

			if ( !nonAddedItems.Any() )
			{
				Log.Error( $"Shop: No items found for display {display}." );
				continue;
			}

			Log.Info( $"Shop: Found {nonAddedItems.Count} items for display {display}." );

			var item = Random.Shared.FromList( nonAddedItems );

			if ( item == null )
			{
				Log.Error( $"Shop: Invalid item found for display {display}. This should not happen." );
				continue;
			}

			// WORKAROUND FOR WRONGLY CASTED ITEMDATA
			// TODO: Remove this when fixed
			// https://github.com/Facepunch/sbox-issues/issues/6630
			if ( string.IsNullOrEmpty( item.GetIdentifier() ) )
			{
				item = ResourceLibrary.Get<ItemData>( item.ResourcePath );
			}

			Items.Add( new ShopItem
			{
				ItemId = item.GetIdentifier(),
				Price = item.BaseBuyPrice,
				Stock = 1,
				Display = Displays.IndexOf( display )
			} );

			Log.Info( $"Shop: Added item {item} to display {display}." );
		}
	}

	private string GetStatePath() => $"{RealmManager.CurrentRealm.Path}/shops/{StoreId}.json";

	public void LoadState()
	{
		if ( !FileSystem.Data.FileExists( GetStatePath() ) )
		{
			Log.Info( "Shop: No shop state found, generating new items." );
			State = new ShopManagerState { Items = new List<ShopItem>(), LastGenerated = DateTime.MinValue };
			GenerateItems();
			SaveState();
			LoadItems();
			return;
		}

		State = JsonSerializer.Deserialize<ShopManagerState>( FileSystem.Data.ReadAllText( GetStatePath() ),
			GameManager.JsonOptions );

		if ( State.LastGenerated.Date != DateTime.Today )
		{
			Log.Info( $"Shop: Regenerating shop items (last: {State.LastGenerated}, today: {DateTime.Today})." );
			State.Items.Clear();
			State.LastGenerated = DateTime.Today;
			GenerateItems();
			SaveState();
		}

		LoadItems();
	}

	private void LoadItems()
	{
		foreach ( var display in Displays )
		{
			display.ShopManager = this;
			display.UpdateItem();
		}
	}

	public void SaveState()
	{
		Log.Info( "Shop: Saving shop state." );
		FileSystem.Data.CreateDirectory( $"{RealmManager.CurrentRealm.Path}/shops" );
		FileSystem.Data.WriteAllText( GetStatePath(), JsonSerializer.Serialize( State, GameManager.JsonOptions ) );
	}

	protected override void OnStart()
	{
		if ( IsProxy ) return;
		LoadState();
	}

	public class ShopItem
	{
		public string ItemId { get; set; }
		public int Price { get; set; }
		public int Stock { get; set; }
		public int Display { get; set; }
		[JsonIgnore] public ItemData ItemData => ItemData.Get( ItemId );
	}

	public class ShopManagerState
	{
		public List<ShopItem> Items { get; set; }
		public DateTime LastGenerated { get; set; }
	}

	public List<ShopItem> Items => State.Items;

	public ShopManagerState State { get; set; }

	public void DispatchBuyItem( PlayerCharacter player, ShopItem item )
	{
		if ( !Items.Contains( item ) )
		{
			player.Notify( Notifications.NotificationType.Error, "This item is not available in this shop." );
			return;
		}

		if ( item.Stock <= 0 )
		{
			player.Notify( Notifications.NotificationType.Error, "This item is out of stock." );
			return;
		}

		Assert.NotNull( ShopClerk, "ShopClerk is not set." );

		CameraMan.Instance?.AddTarget( ShopClerk.GameObject );

		var window = DialogueManager.Instance.DialogueWindow;

		window.SetData( "ItemName", item.ItemData.Name );
		window.SetData( "ItemPrice", item.Price );
		window.SetData( "PlayerClovers", player.CloverBalanceController.GetBalance() );

		window.SetTarget( 0, ShopClerk.GameObject );

		window.SetAction( "BuyItem", () =>
		{
			if ( !BuyItem( player, item ) )
				return;

			window.Enabled = false;
		} );

		ShopClerk.LoadDialogue( BuyItemDialogue );

		ShopClerk.SetState( BaseNpc.NpcState.Interacting );
		ShopClerk.LookAt( player.GameObject );

		window.OnDialogueEnd += () =>
		{
			ShopClerk.SetState( BaseNpc.NpcState.Idle );
			CameraMan.Instance.RemoveTarget( ShopClerk.GameObject );
		};
	}

	public bool BuyItem( PlayerCharacter player, ShopItem item )
	{
		if ( player.CloverBalanceController.GetBalance() < item.Price )
		{
			player.Notify( Notifications.NotificationType.Error, "You don't have enough clovers to buy this item." );
			return false;
		}

		player.CloverBalanceController.DeductClover( item.Price );

		item.Stock--;

		if ( item.Stock <= 0 )
		{
			Items.Remove( item );
		}

		SaveState();

		player.Inventory.PickUpItem( PersistentItem.Create( item.ItemId ) );

		player.Notify( Notifications.NotificationType.Success,
			$"You bought {item.ItemData.Name} for {item.Price} clovers." );

		return true;
	}
}
