namespace Clover.Persistence;

public interface ISaveData
{
	
	public void OnSave( WorldNodeLink nodeLink );
	
	public void OnLoad( WorldNodeLink nodeLink );
	
}
