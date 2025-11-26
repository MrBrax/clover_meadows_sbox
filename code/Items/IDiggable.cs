using Clover.Player;

namespace Clover.Items;

public interface IDiggable
{
	public bool CanDig();

	// public bool GiveItemWhenDug();

	bool OnDig( PlayerCharacter player, WorldItem item );
}
