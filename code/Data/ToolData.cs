﻿using System;
using Clover.Carriable;
using Clover.Inventory;
using Clover.Persistence;

namespace Clover.Data;

[GameResource("Tool", "tool", "tool", Icon = "build")]
public class ToolData : ItemData
{
	
	[Property, Group("Tool")] public GameObject CarryScene { get; set; }
	
	
	public BaseCarriable SpawnCarriable()
	{
		if ( !CarryScene.IsValid() ) throw new Exception( $"{ResourceName} has no CarryScene" );
		return CarryScene.Clone().GetComponent<BaseCarriable>();
	}

	public override string GetIcon()
	{
		return Icon ?? "ui/icons/default_tool.png";
	}
	
	public override IEnumerable<ItemAction> GetActions( InventorySlot<PersistentItem> slot )
	{
		yield return new ItemAction
		{
			Name = "Equip", 
			Icon = "build",
			Action = slot.Equip
		};
	}
}
