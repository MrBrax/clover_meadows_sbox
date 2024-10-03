namespace Clover.Player;

public sealed class PlayerCharacter : Component
{
	
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }

	protected override void OnStart()
	{
		base.OnStart();
		GameObject.BreakFromPrefab();
	}
}
