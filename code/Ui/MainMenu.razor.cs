using System.Collections;

namespace Clover.Ui;

public partial class MainMenu
{

	public enum MenuState
	{
		Title,
		SelectWorld,
		NewWorld,
		Settings,
		About,
	}
	
	public MenuState State { get; set; } = MenuState.Title;
	
	public List<WorldInfo> Worlds { get; set; }

	public void SetState( MenuState state )
	{
		State = state;
	}
	
	public struct WorldInfo
	{
		public string Id;
		public string Name;
	}

	protected override void OnStart()
	{
		FileSystem.Data.CreateDirectory( "worlds" );
		var folders = FileSystem.Data.FindDirectory( "worlds" );
		
		foreach ( var folder in folders )
		{
			var path = $"worlds/{folder}";
			
			if ( !FileSystem.Data.FileExists( $"{path}/.worldinfo" ) )
			{
				Log.Warning( $"World folder {folder} is missing .worldinfo file" );
				continue;
			}
			
			// var json = FileSystem.Data.ReadJson( $"{path}/world.json" );
		}
	}
}
