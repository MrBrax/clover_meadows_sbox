using Clover.Items;
using Clover.Player;

namespace Clover.Inventory;

public interface IPickupable
{
	public bool CanPickup( PlayerCharacter player );

	/// <summary>
	///  Called when the player tries to pick up the item. By default it does nothing, but <see cref="WorldItem"/> has an implementation.
	///  Not every item is a
	/// </summary>
	/// <param name="player"></param>
	public void OnPickup( PlayerCharacter player );

	public string GetPickupName();
}
