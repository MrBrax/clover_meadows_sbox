using System.Text.Json;
using System.Text.Json.Serialization;
using Clover.Player;

namespace Clover;

public class GameManager : Component
{
	public static GameManager Instance;

	protected override void OnAwake()
	{
		base.OnAwake();
		Instance = this;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		Instance = null;
	}

	public static JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true, IncludeFields = true, Converters = { new JsonStringEnumConverter() }
	};
	
	public string SaveProfile = "default";

	private TimeSince _lastSave;
	
	public void SaveTimer()
	{
		if ( _lastSave < 60f )
		{
			return;
		}
		_lastSave = 0f;

		if ( !AutoSave ) return;
		
		foreach( var world in WorldManager.Instance.Worlds )
		{
			world.Value.Save();
		}

		PlayerCharacter.Local?.Save();
	}
	
	[ConVar("clover_autosave")]
	public static bool AutoSave { get; set; } = true;

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		SaveTimer();
	}
}
