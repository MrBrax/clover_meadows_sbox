using Clover.Items;

namespace Clover;

public class WorldItemSystem : GameObjectSystem
{
	public WorldItemSystem( Scene scene ) : base( scene )
	{
		// Listen( Stage.StartFixedUpdate, 10, OnStartFixedUpdate, "StartFixedUpdate" );
		Listen( Stage.StartFixedUpdate, 10, OnFinishFixedUpdate, "FinishFixedUpdate" );
	}

	private void OnFinishFixedUpdate()
	{
		var worldItems = Scene.GetAllComponents<WorldItem>();
		foreach ( var worldItem in worldItems )
		{
			worldItem.ItemHighlight.Enabled = false;
		}
	}
}
