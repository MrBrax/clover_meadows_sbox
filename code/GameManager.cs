using System.Text.Json;
using System.Text.Json.Serialization;

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
	
}
