namespace Clover.Items;

public interface IDiggable
{
	
	public bool CanDig();

	public bool GiveItemWhenDug();
	
	// TODO: add OnDig method?
	
}
