using System;
using System.Text.Json.Serialization;

namespace Clover.Items;

public class WorldItem : Component
{
	
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }
	
	private Vector2Int _tilePosition { get; set; }

	[Property, ReadOnly, Sync]
	public Vector2Int TilePosition
	{
		get => _tilePosition;
		set
		{
			if ( Scene.IsEditor || value == Vector2Int.Zero ) // TODO: don't make zero a special case
			{
				_tilePosition = value;
				return;
			}
			
			var oldPosition = _tilePosition;

			// Log.Info( $"Setting tile position of {GameObject.Name} to {value}" );
			_tilePosition = value;
			// WorldPosition = new Vector3( value.x * 32, value.y * 32, 0 );
			WorldPosition = WorldManager.Instance.GetWorld( WorldLayerObject.Layer ).ItemGridToWorld( value );
			// LevelManager.Instance?.OnWorldObjectMoved( this, oldPosition, value );
			// OnTilePositionChanged?.Invoke( _tilePosition, value );
		}
	}
	
	[Property, ReadOnly, Sync]
	public Vector2Int Size { get; set; } = new(1, 1);

	public delegate void TilePositionChanged( Vector2Int oldPosition, Vector2Int newPosition );
	[JsonIgnore] public TilePositionChanged OnTilePositionChanged;
	
	private string _prefab;

	[Property, ResourceType( "prefab" )]
	public string Prefab
	{
		get { return !string.IsNullOrEmpty( _prefab ) ? _prefab : GameObject.PrefabInstanceSource; }
		set { _prefab = value; }
	}
	
	protected override void OnAwake()
	{
		if ( Scene.IsEditor ) return;
		
		GameObject.Transform.OnTransformChanged += GameObjectTransformChanged;

		base.OnEnabled();

		/*BackupPrefabSource();

		if ( Transform.Position != Vector3.Zero )
		{
			// Log.Warning( $"WorldObject {GameObject.Name} has no position set" );
			SetPosition( Transform.Position );
		}*/
	}

	private void GameObjectTransformChanged()
	{
		Log.Info( $"WorldItem {GameObject.Name} transform changed" );
	}
}
