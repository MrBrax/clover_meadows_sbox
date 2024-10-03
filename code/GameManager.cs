using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clover;

public class GameManager
{
	public static JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true, IncludeFields = true, Converters = { new JsonStringEnumConverter() }
	};
}
