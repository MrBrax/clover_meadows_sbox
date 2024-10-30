using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clover;

public class RealmManager
{
	public class RealmInfo
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public DateTime Created { get; set; }
		public DateTime LastPlayed { get; set; }

		[JsonIgnore] public string Path => $"realms/{Id}";
		[JsonIgnore] public string SavePath => $"{Path}/.realminfo";

		// TODO: permissions

		public void Save()
		{
			FileSystem.Data.WriteAllText( SavePath, JsonSerializer.Serialize( this, GameManager.JsonOptions ) );
		}
	}

	public static RealmInfo CurrentRealm { get; set; }
}
