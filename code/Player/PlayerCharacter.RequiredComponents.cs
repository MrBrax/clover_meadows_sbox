using Clover.Components;
using Clover.Player.Clover;

namespace Clover.Player;

public sealed partial class PlayerCharacter
{
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; private set; }
	[RequireComponent] public CharacterController CharacterController { get; private set; }
	[RequireComponent] public PlayerController PlayerController { get; private set; }
	[RequireComponent] public PlayerInteract PlayerInteract { get; private set; }

	[RequireComponent, Icon( "inventory" )]
	public Inventory.Inventory Inventory { get; private set; }

	[RequireComponent] public Equips Equips { get; private set; }
	[RequireComponent] public CameraController CameraController { get; private set; }
	[RequireComponent] public CloverBalanceController CloverBalanceController { get; private set; }
	[RequireComponent] public VehicleRider VehicleRider { get; private set; }
	[RequireComponent] public ItemPlacer ItemPlacer { get; private set; }
}
