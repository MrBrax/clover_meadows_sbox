using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Clover.Data;
using Clover.Items;

namespace Clover.WorldBuilder;

public class ShopManager : Component
{
	[Property] public string StoreId { get; set; }

	[Property] public List<ShopDisplay> Displays { get; set; }

	// [Property] public Shopkeeper Shopkeeper { get; set; }

	[Property] public List<CatalogueData> Catalogues { get; set; }

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
				.FindAll( i => !HasItem( i.Id ) )
				.Where( i => i.GetMaxBounds() <= display.Size ).ToList();

			if ( !nonAddedItems.Any() )
			{
				Log.Error( $"Shop: No items found for display {display}." );
				continue;
			}

			var item = Random.Shared.FromList( nonAddedItems );

			Items.Add( new ShopItem
			{
				ItemId = item.Id, Price = item.BaseBuyPrice, Stock = 1, Display = Displays.IndexOf( display )
			} );

			Log.Info( $"Shop: Added item {item} to display {display}." );
		}
	}

	private string GetStatePath() => $"{GameManager.Realm.Path}/shops/{StoreId}.json";

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
			Log.Info( "Shop: Regenerating shop items." );
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
		FileSystem.Data.CreateDirectory( $"{GameManager.Realm.Path}/shops" );
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
}
