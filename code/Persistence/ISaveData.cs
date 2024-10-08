using System;

namespace Clover.Persistence;

[Obsolete( "Use IPersistent instead" )]
public interface ISaveData
{
	
	public void OnSave( WorldNodeLink nodeLink );
	
	public void OnLoad( WorldNodeLink nodeLink );
	
}
