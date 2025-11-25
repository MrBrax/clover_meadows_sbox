using System;
using Clover.Carriable;
using Clover.Components;
using Clover.Persistence;
using Clover.Player;
using Clover.Player.Clover;
using Sandbox.UI;

namespace Clover.Ui;

public partial class InventoryUiCloverBalance
{
	private CloverBalanceController CloverBalance => PlayerCharacter.Local?.CloverBalanceController;

	protected override int BuildHash()
	{
		StateHasChanged();
		return HashCode.Combine( CloverBalance );
	}

	private Texture GetCloverIcon()
	{
		return Texture.LoadFromFileSystem( "ui/icons/default_item.png", FileSystem.Mounted );
	}
}
