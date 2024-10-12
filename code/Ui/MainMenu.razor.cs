using System;
using System.Collections;
using System.IO;
using System.Text.Json;

namespace Clover.Ui;

public partial class MainMenu
{
	public enum MenuState
	{
		Title,
		SelectRealm,
		CreateRealm,
		Settings,
		About,
	}

	public MenuState State { get; set; } = MenuState.Title;

	public List<RealmInfo> Realms { get; set; } = new();

	public void SetState( MenuState state )
	{
		State = state;
	}

	public struct RealmInfo
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public DateTime Created { get; set; }
		public string Path => $"realms/{Id}";
	}

	protected override void OnStart()
	{
		FileSystem.Data.CreateDirectory( "realms" );
		var folders = FileSystem.Data.FindDirectory( "realms" );

		foreach ( var folder in folders )
		{
			var path = $"realms/{folder}";

			if ( !FileSystem.Data.FileExists( $"{path}/.realminfo" ) )
			{
				Log.Warning( $"Realm folder {folder} is missing .realminfo file" );
				var json1 = new RealmInfo { Id = folder, Name = folder, Created = DateTime.Now };
				FileSystem.Data.WriteAllText( $"{path}/.realminfo", JsonSerializer.Serialize( json1,
					GameManager.JsonOptions ) );
			}

			var json = JsonSerializer.Deserialize<RealmInfo>( FileSystem.Data.ReadAllText( $"{path}/.realminfo" ),
				GameManager.JsonOptions );

			Realms.Add( json );
		}
	}

	private string _newRealmName = "";

	private void CreateRealm()
	{
		var invalid = Path.GetInvalidPathChars();
		var folder_name = invalid.Aggregate( _newRealmName, ( current, c ) => current.Replace( c, '_' ) );

		if ( string.IsNullOrWhiteSpace( folder_name ) )
		{
			Log.Error( "Realm name cannot be empty" );
			return;
		}

		var path = $"realms/{folder_name}";

		if ( FileSystem.Data.DirectoryExists( path ) )
		{
			Log.Error( $"Realm {folder_name} already exists" );
			return;
		}

		FileSystem.Data.CreateDirectory( path );

		var json = new RealmInfo { Id = folder_name, Name = _newRealmName, Created = DateTime.Now };
		FileSystem.Data.WriteJson( $"{path}/.realminfo", json );

		Realms.Add( json );

		_newRealmName = "";

		SetState( MenuState.SelectRealm );
	}

	private void SelectRealm( RealmInfo realm )
	{
		GameManager.Realm = realm;
		GameManager.LoadRealm();
		
	}
}
