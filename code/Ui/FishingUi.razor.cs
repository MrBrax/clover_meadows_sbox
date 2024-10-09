using System;
using Clover.Carriable;
using Clover.Components;
using Clover.Player;

namespace Clover.Ui;

public partial class FishingUi
{

	private FishingRod Rod => PlayerCharacter.Local?.Equips.GetEquippedItem<FishingRod>( Equips.EquipSlot.Tool );

	private bool IsFishing => Rod?.HasCasted ?? false;

	protected override int BuildHash()
	{
		return HashCode.Combine( IsFishing, Rod?.Stamina, Rod?.LineStrength );
	}
}
