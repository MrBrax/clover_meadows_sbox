using System.Text.Json.Serialization;
using Sandbox;

namespace Clover;

[Category( "Clover" )]
[Icon( "visibility" )]
[Description( "Handles the visibility of the object based on the world layer." )]
public sealed class WorldLayerObject : Component
{
	
	private int _layer = -1;

	/// <summary>
	///  The layer of the world this object is in. It's just the index of the world in the WorldManager.
	/// </summary>
	[Property, Sync]
	public int Layer
	{
		get => _layer;
		set
		{
			_layer = value;
			RebuildVisibility( value );
		}
	}
	
	[JsonIgnore] public World World => WorldManager.Instance?.GetWorld( Layer );

	protected override void OnStart()
	{
		
		GameObject parentCheck = GameObject;
		
		while ( parentCheck != null )
		{
			if ( parentCheck.Components.TryGet<World>( out var world ) )
			{
				Layer = world.Layer;
				// Log.Info( $"Found world layer {Layer} for {GameObject.Name}" );
				break;
			}

			parentCheck = parentCheck.Parent;
		}
		
	}

	public void SetLayer( int layer, bool rebuildVisibility = false )
	{
		Layer = layer;
		
		if ( rebuildVisibility )
		{
			RebuildVisibility( layer );
		}
	}

	public void SetLayerWithTransform( int layer )
	{
		var currentLayer = Layer;
		var currentHeight = WorldPosition.z;
		var layerHeight = WorldManager.WorldOffset;
		
		Layer = layer;
		WorldPosition = new Vector3( WorldPosition.x, WorldPosition.y, currentHeight + ( layerHeight * ( layer - currentLayer ) ) );

	}

	/// <summary>
	///  Visibility is based on render tags on the camera. This method adds or removes the tags based on the layer.
	/// </summary>
	[Broadcast]
	public void RebuildVisibility( int layer )
	{
		Tags.Remove( "worldlayer_invisible" );
		Tags.Remove( "worldlayer_visible" );
			
		if ( layer == WorldManager.Instance.ActiveWorldIndex )
		{
			Tags.Add( "worldlayer_visible" );
		}
		else
		{
			Tags.Add( "worldlayer_invisible" );
		}
	}
}
