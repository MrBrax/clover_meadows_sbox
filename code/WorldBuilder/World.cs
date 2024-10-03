using Clover.Player;
using Sandbox;
namespace Clover;

public sealed class World : Component
{
	
	[Property] public Data.World Data { get; set; }

	public int Layer;

	public string WorldId => Data.ResourceName;

	public bool ShouldUnloadOnExit
	{
		get
		{
			var playersInWorld = Scene.GetAllComponents<PlayerCharacter>().Count( p => p.WorldLayerObject.Layer == Layer );
			return playersInWorld == 0;
		}
	}

	public void Setup()
	{
		var layerObjects = GetComponentsInChildren<WorldLayerObject>( true );
		foreach ( var layerObject in layerObjects )
		{
			layerObject.SetLayer( Layer );
		}
		
		var modelPhysics = GetComponentsInChildren<ModelPhysics>( true );
		foreach ( var model in modelPhysics )
		{
			model.Enabled = false;
			Invoke( 0.01f, () => model.Enabled = true );
		}
		
	}
	
	public WorldEntrance GetEntrance( string entranceId )
	{
		var entrances = GetComponentsInChildren<WorldEntrance>( true );
		
		foreach ( var entrance in entrances )
		{
			if ( entrance.EntranceId == entranceId )
			{
				return entrance;
			}
		}
		
		return null;
	}
	
}
