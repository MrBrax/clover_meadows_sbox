using Sandbox;

namespace Clover;

public sealed class WorldLayerObject : Component
{
	
	[Property] public int Layer { get; private set; }
	
	public void SetLayer( int layer, bool rebuildVisibility = false )
	{
		Layer = layer;
		
		if ( rebuildVisibility )
		{
			RebuildVisibility();
		}
	}

	public void SetLayerWithTransform( int layer )
	{
		var currentLayer = Layer;
		var currentHeight = Transform.Position.z;
		var layerHeight = WorldManager.WorldOffset;
		
		Layer = layer;
		Transform.Position = new Vector3( Transform.Position.x, Transform.Position.y, currentHeight + ( layerHeight * ( layer - currentLayer ) ) );

	}

	public void RebuildVisibility()
	{
		Tags.Remove( "worldlayer_invisible" );
		Tags.Remove( "worldlayer_visible" );
			
		if ( Layer == WorldManager.Instance.ActiveWorldIndex )
		{
			Tags.Add( "worldlayer_visible" );
		}
		else
		{
			Tags.Add( "worldlayer_invisible" );
		}
	}
}
