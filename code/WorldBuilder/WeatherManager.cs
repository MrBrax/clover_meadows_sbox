namespace Clover.WorldBuilder;

public class WeatherManager : Component
{
	
	public enum WeatherEffects
	{
		None = 0,
		Rain = 1,
		Lightning = 2,
		Wind = 4,
		Fog = 8
	}

	public static DirectionalLight Sun =>
		Game.ActiveScene.GetAllComponents<DirectionalLight>().FirstOrDefault( x => x.Tags.Has( "sun" ) );

}
