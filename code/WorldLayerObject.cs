using System.Text.Json.Serialization;
using Sandbox;

namespace Clover;

[Category( "Clover" )]
[Icon( "visibility" )]
[Description( "Handles the visibility of the object based on the world layer." )]
public sealed class WorldLayerObject : Component, IWorldEvent
{
	
	private int _layer = -1;

	/// <summary>
	///  The layer of the world this object is in. It's just the index of the world in the WorldManager.
	/// </summary>
	[Property, Sync, JsonIgnore]
	public int Layer
	{
		get => _layer;
		set
		{
			_layer = value;
			Log.Info( $"Setting layer property to {value} for {GameObject.Name}, rebuilding visibility." );
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
		Log.Info( $"Setting layer {layer} for {GameObject.Name}" );
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
		if ( Scene.IsEditor ) return;
		Tags.Remove( "worldlayer_invisible" );
		Tags.Remove( "worldlayer_visible" );

		if ( !WorldManager.Instance.IsValid() )
		{
			Log.Error( "WorldManager is not valid." );
			return;
		}
			
		if ( layer == WorldManager.Instance.ActiveWorldIndex )
		{
			Log.Info( $"Setting {GameObject.Name} to visible" );
			Tags.Add( "worldlayer_visible" );
		}
		else
		{
			Log.Info( $"Setting {GameObject.Name} to invisible" );
			Tags.Add( "worldlayer_invisible" );
		}
	}

	public void OnWorldChanged( World world )
	{
		Log.Info( $"World changed to {world.Layer}, rebuilding visibility for {GameObject.Name}" );
		RebuildVisibility( Layer );
	}
}
