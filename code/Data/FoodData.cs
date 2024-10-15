﻿using Clover.Inventory;
using Clover.Persistence;

namespace Clover.Data;

[GameResource( "Food Data", "food", "Food Data" )]
public class FoodData : ItemData, IEdibleData
{
	
	[Property] public GameObject HoldScene { get; set; }
	
	public override IEnumerable<ItemAction> GetActions( InventorySlot<PersistentItem> slot )
	{
		yield return new ItemAction
		{
			Name = "Hold",
			Icon = "restaurant",
			Action = slot.HoldEdible
		};

		foreach ( var action in base.GetActions( slot ) )
		{
			yield return action;
		}
	}

	
}
