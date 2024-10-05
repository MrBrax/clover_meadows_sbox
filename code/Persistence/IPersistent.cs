namespace Clover.Persistence;

public interface IPersistent
{
	
	public void OnSave( PersistentItem item );
	
	public void OnLoad( PersistentItem item );
	
}
