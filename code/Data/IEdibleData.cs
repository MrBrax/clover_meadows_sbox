namespace Clover.Data;

public interface IEdibleData
{
	
	public enum EdibleType
	{
		Unknown,
		Food,
		Drink,
	}
	
	public GameObject HoldScene { get; set; }
	
	public EdibleType Type { get; set; }
	
}
