using Clover.Components;
using Clover.Player.Clover;

namespace Clover.Player;

public sealed partial class PlayerCharacter
{
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }
	[RequireComponent] public CharacterController CharacterController { get; set; }
	[RequireComponent] public PlayerController PlayerController { get; set; }
	[RequireComponent] public PlayerInteract PlayerInteract { get; set; }

	[RequireComponent, Icon( "inventory" )]
	public Inventory.Inventory Inventory { get; set; }

	[RequireComponent] public Equips Equips { get; set; }
	[RequireComponent] public CameraController CameraController { get; set; }
	[RequireComponent] public CloverBalanceController CloverBalanceController { get; set; }
	[RequireComponent] public VehicleRider VehicleRider { get; set; }
	[RequireComponent] public ItemPlacer ItemPlacer { get; set; }
}
