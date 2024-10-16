using System;
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

	public struct InputData
	{
		public string Action;
		public string Name;

		public InputData( string action, string name )
		{
			Action = action;
			Name = name;
		}
	}

	private IEnumerable<InputData> GetCurrentInputs()
	{
		// yield return new InputData( "use", "Use" );
		var player = PlayerCharacter.Local;

		if ( player.Equips.TryGetEquippedItem<BaseCarriable>( Equips.EquipSlot.Tool, out var tool ) )
		{
			yield return new InputData( "UseTool", tool.GetUseName() );

			foreach ( var input in tool.GetInputs() )
			{
				yield return input;
			}
		}

		var interactable = player.PlayerInteract.FindInteractable();
		if ( interactable != null )
		{
			yield return new InputData( "Use", interactable.GetInteractName() );
		}

		var pickupable = player.PlayerInteract.GetPickupableNode();
		if ( pickupable != null && pickupable.CanPickup( player ) )
		{
			yield return new InputData( "Pickup", $"Pick up {pickupable.GetPickupName()}" );
		}
	}
}
