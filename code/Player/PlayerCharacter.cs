using System;
using Clover.Components;
using Clover.Data;

namespace Clover.Player;

public sealed class PlayerCharacter : Component
{
	
	public static PlayerCharacter Local => Game.ActiveScene.GetAllComponents<PlayerCharacter>().FirstOrDefault( x => !x.IsProxy );
	
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }
	[RequireComponent] public PlayerController Controller { get; set; }

	[Property] public GameObject Model { get; set; }

	[Property] public GameObject InteractPoint { get; set; }

	[Property] public SittableNode Seat { get; set; }

	public Vector3 EnterPosition { get; set; }
	public bool IsSitting => Seat.IsValid();


	protected override void OnStart()
	{
		base.OnStart();
		GameObject.BreakFromPrefab();
	}

	public void ModelLookAt( Vector3 position )
	{
		var dir = (position - WorldPosition).Normal;
		dir.y = 0;
		Model.WorldRotation = Rotation.LookAt( dir, Vector3.Up );
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( Input.Released( "Duck" ) )
		{
			try
			{
				WorldManager.Instance.ActiveWorld.SpawnPlacedNode(
					ResourceLibrary.GetAll<ItemData>().FirstOrDefault(),
					GetAimingGridPosition(),
					World.ItemRotation.North,
					World.ItemPlacement.Floor
				);
			}
			catch ( Exception e )
			{
				Log.Error( e.Message );
			}
		}

		if ( Input.Pressed( "use" ) && IsSitting )
		{
			Seat.Occupant = null;
			Seat = null;
			WorldPosition = EnterPosition;
			Input.Clear( "use" );
			Input.ReleaseAction( "use" );
		}
	}

	public Vector2Int GetAimingGridPosition()
	{
		var boxPos = InteractPoint.WorldPosition;

		var world = WorldManager.Instance.ActiveWorld;

		var gridPosition = world.WorldToItemGrid( boxPos );
		var worldPosition = world.ItemGridToWorld( gridPosition );

		if ( MathF.Abs( boxPos.z - worldPosition.z ) > 16f )
		{
			// throw new System.Exception( $"Aiming at a higher position: {boxPos} -> {worldPosition}" );
			// Log.Warning( $"Aiming at a higher position: {boxPos} -> {worldPosition}" );
			// return default;
		}

		return gridPosition;
	}

	/*protected override void OnUpdate()
	{
		Gizmo.Draw.Arrow( WorldPosition + Vector3.Up * 16f, WorldPosition + Vector3.Up * 16 + Model.WorldRotation.Forward * 32f );
	}*/

	[Authority]
	public void TeleportTo( Vector3 pos, Rotation rot )
	{
		WorldPosition = pos;
		// WorldRotation = rot;
		Transform.ClearInterpolation();
		ModelLookAt( pos + rot.Forward );
		GetComponent<CameraController>().SnapCamera();
		GetComponent<CharacterController>().Velocity = Vector3.Zero;
	}

	[Authority]
	public void SetLayer( int layer )
	{
		WorldLayerObject.SetLayer( layer, true );
		WorldManager.Instance.SetActiveWorld( layer );
	}

	public bool ShouldMove()
	{
		return !IsSitting;
	}
}
