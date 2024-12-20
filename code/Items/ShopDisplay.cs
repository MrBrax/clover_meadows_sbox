﻿using Clover.Interactable;
using Clover.Player;
using Clover.WorldBuilder;

namespace Clover.Items;

[Category( "Clover/Items" )]
public class ShopDisplay : Component, IInteract
{
	[Property] public GameObject Container { get; set; }
	[Property] public int Size { get; set; } = 1;

	[Property] public GameObject SoldOutSign { get; set; }

	public ShopManager ShopManager { get; set; }

	public ShopManager.ShopItem Item =>
		ShopManager.Items.Find( i => i.Display == ShopManager.Displays.IndexOf( this ) );

	protected override void OnAwake()
	{
		SoldOutSign.Enabled = true;
	}

	public void UpdateItem()
	{
		Container.Children.ForEach( c => c.Destroy() );

		if ( Item == null )
		{
			Log.Error( "ShopDisplay: Item is null" );
			SoldOutSign.Enabled = true;
			return;
		}

		var itemData = Item.ItemData;

		if ( !itemData.IsValid() )
		{
			Log.Error( $"ShopDisplay: Item data is invalid for {Item}" );
			SoldOutSign.Enabled = true;
			return;
		}

		// this is the easiest, no need to remove stuff
		if ( itemData.ModelScene.IsValid() )
		{
			var gameObject = itemData.ModelScene.Clone();
			gameObject.SetParent( Container );
			gameObject.LocalPosition = Vector3.Zero;
			gameObject.LocalRotation = Rotation.Identity;

			SoldOutSign.Enabled = false;

			return;
		}

		if ( itemData.PlaceScene.IsValid() )
		{
			var gameObject = itemData.PlaceScene.Clone();
			gameObject.SetParent( Container );
			gameObject.LocalPosition = Vector3.Zero;
			gameObject.LocalRotation = Rotation.Identity;

			/*if ( gameObject.Components.TryGet<WorldItem>( out var worldItem ) )
			{
				worldItem.Destroy();
			}

			// kill all interacts
			foreach ( var c in gameObject.Components.GetAll<IInteract>().ToList() )
			{
				(c as Component)?.Destroy();
			}

			// kill all sittables
			foreach ( var c in gameObject.Components.GetAll<Sittable>().ToList() )
			{
				c.Destroy();
			}*/

			// destroy all root components
			foreach ( var c in gameObject.Components.GetAll().ToList() )
			{
				c.Destroy();
			}

			// destroy all particles
			foreach ( var p in gameObject.GetComponentsInChildren<ParticleEffect>().ToList() )
			{
				p.Destroy();
			}

			SoldOutSign.Enabled = false;

			return;
		}

		if ( itemData.DropScene.IsValid() )
		{
			var gameObject = itemData.DropScene.Clone();
			gameObject.SetParent( Container );
			gameObject.LocalPosition = Vector3.Zero;
			gameObject.LocalRotation = Rotation.Identity;

			// destroy all components
			foreach ( var c in gameObject.Components.GetAll().ToList() )
			{
				c.Destroy();
			}

			SoldOutSign.Enabled = false;

			return;
		}

		Log.Error( $"ShopDisplay: No model or place scene found for {itemData.GetIdentifier()}" );
	}

	public void StartInteract( PlayerCharacter player )
	{
		ShopManager.DispatchBuyItem( player, Item );
	}

	public string GetInteractName()
	{
		if ( Item == null ) return "Sold Out";
		return $"Buy {Item.ItemData.Name}";
	}
}
