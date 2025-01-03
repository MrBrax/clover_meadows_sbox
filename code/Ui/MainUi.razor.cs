﻿using System;
using System.Collections;
using Clover.Carriable;
using Clover.Components;
using Clover.Data;
using Clover.Player;
using Clover.WorldBuilder;
using Sandbox.UI;

namespace Clover;

public partial class MainUi : IPlayerSaved, IWorldSaved
{
	public static MainUi Instance { get; private set; }

	public TimeSince LastInput;

	public bool ShouldShowUi => LastInput < HideUiDelay;

	public static bool IsIdle => !Instance?.ShouldShowUi ?? false;

	[ConVar( "clover_ui_hide_delay" )] public static float HideUiDelay { get; set; } = 5;

	protected override void OnAwake()
	{
		Instance = this;
	}

	protected override void OnDestroy()
	{
		Instance = null;
	}

	private Panel IsSavingPanel { get; set; }

	public void PrePlayerSave( PlayerCharacter player )
	{
		IsSavingPanel.FlashClass( "saving", 2 );
	}

	public void PostPlayerSave( PlayerCharacter player )
	{
	}

	public void PreWorldSaved( World world )
	{
		IsSavingPanel?.FlashClass( "saving", 2 );
	}

	public void PostWorldSaved( World world )
	{
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( TimeManager.Time );
	}

	protected override void OnFixedUpdate()
	{
		if ( Input.ActionNames.Any( x => Input.Down( x ) ) )
		{
			LastInput = 0;
		}

		if ( !PlayerCharacter.Local.IsValid() )
			return;

		if ( PlayerCharacter.Local.ItemPlacer.IsPlacing || PlayerCharacter.Local.ItemPlacer.IsMoving )
		{
			LastInput = 0;
		}
	}

	public struct InputData
	{
		public string Group;
		public string ActionPair;
		public string Action;
		public string Name;

		public InputData( string action, string name )
		{
			Action = action;
			Name = name;
		}

		public InputData( string group, string actionPair, string action, string name )
		{
			Group = group;
			ActionPair = actionPair;
			Action = action;
			Name = name;
		}
	}

	private IEnumerable<InputData> GetCurrentInputs()
	{
		// yield return new InputData( "use", "Use" );
		var player = PlayerCharacter.Local;

		if ( player.Equips.TryGetEquippedItem<BaseCarriable>( Equips.EquipSlot.Tool, out var tool ) &&
		     player.Equips.CanUseTool() )
		{
			yield return new InputData( "UseTool", tool.GetUseName() );

			foreach ( var input in tool.GetInputs() )
			{
				yield return input;
			}
		}

		if ( player.PlayerInteract.CanInteract() )
		{
			var interactable = player.PlayerInteract.FindInteractable();
			if ( interactable != null )
			{
				yield return new InputData( "Use", interactable.GetInteractName() );
			}

			var pickupable = player.PlayerInteract.GetPickupableNode();
			if ( pickupable != null && pickupable.CanPickup( player ) )
			{
				yield return new InputData( "Pickup", $"Pick up {pickupable.GetPickupName()}" );
				// yield return new InputData( "Move", $"Move" );
			}
		}

		if ( player.ItemPlacer.IsPlacing || player.ItemPlacer.IsMoving )
		{
			yield return new InputData( "Item Placer", "", "ItemPlacerConfirm", "Place" );
			yield return new InputData( "Item Placer", "", "ItemPlacerCancel", "Cancel" );

			yield return new InputData( "Item Placer", "Rotate", "RotateClockwise", "Rotate" );
			yield return new InputData( "Item Placer", "Rotate", "RotateCounterClockwise", "Rotate" );

			yield return new InputData( "Item Placer", "", "Snap", "Snap" );

			yield return new InputData( "Item Placer", "", "ItemPlacerHeight", "Height mode" );

			yield return new InputData( "Item Placer", "", "CameraAdjust", "Adjust Camera" );

			yield return new InputData( "Item Placer", "PositionSnap", "ItemPlacerPositionSnapDecrease",
				$"Change position snap ({ItemPlacer.PositionSnapDistance}u)" );
			yield return new InputData( "Item Placer", "PositionSnap", "ItemPlacerPositionSnapIncrease",
				$"Change position snap ({ItemPlacer.PositionSnapDistance}u)" );

			yield return new InputData( "Item Placer", "RotationSnap", "ItemPlacerRotationSnapDecrease",
				$"Change rotation snap ({ItemPlacer.RotationSnapDistance}°)" );
			yield return new InputData( "Item Placer", "RotationSnap", "ItemPlacerRotationSnapIncrease",
				$"Change rotation snap ({ItemPlacer.RotationSnapDistance}°)" );
		}
		else if ( player.ItemPlacer.CurrentHoveredItem.IsValid() )
		{
			yield return new InputData( "move", $"Move {player.ItemPlacer.CurrentHoveredItem.GetPickupName()}" );
		}

		if ( ShowInputsObvious )
		{
			yield return new InputData( "View", "Camera" );
			yield return new InputData( "Inventory", "Inventory" );
			yield return new InputData( "Run", "Run" );
			yield return new InputData( "Walk", "Walk" );
			yield return new InputData( "Chat", "Chat" );
		}

		yield return new InputData( "DebugWindow", "Debug" );
	}

	[ConVar( "clover_ui_show_inputs", Saved = true )]
	public static bool ShowInputs { get; set; } = true;

	// TODO: better name
	[ConVar( "clover_ui_show_inputs_obvious", Saved = true )]
	public static bool ShowInputsObvious { get; set; } = true;
}
