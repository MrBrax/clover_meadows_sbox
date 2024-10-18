using System;
using System.IO;
using System.Text.Json;
using Clover.Carriable;
using Clover.Components;
using Clover.Data;
using Clover.Inventory;
using Clover.Persistence;
using Clover.Player.Clover;
using Clover.Ui;

namespace Clover.Player;

public sealed partial class PlayerCharacter : Component
{
	[ActionGraphNode( "player.local" ), Title( "Local Player" ), Icon( "face" ), Category( "Clover" )]
	public static PlayerCharacter Local =>
		Game.ActiveScene.GetAllComponents<PlayerCharacter>().FirstOrDefault( x => !x.IsProxy );

	[Sync] public string PlayerId { get; set; }
	[Sync] public string PlayerName { get; set; }

	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }
	[RequireComponent] public CharacterController CharacterController { get; set; }
	[RequireComponent] public PlayerController PlayerController { get; set; }
	[RequireComponent] public PlayerInteract PlayerInteract { get; set; }
	[RequireComponent] public Inventory.Inventory Inventory { get; set; }
	[RequireComponent] public Equips Equips { get; set; }
	[RequireComponent] public CameraController CameraController { get; set; }
	[RequireComponent] public CloverBalanceController CloverBalanceController { get; set; }
	[RequireComponent] public VehicleRider VehicleRider { get; set; }
	[RequireComponent] public ItemPlacer ItemPlacer { get; set; }

	[Property] public GameObject Model { get; set; }

	[Property] public GameObject InteractPoint { get; set; }

	[Property] public SittableNode Seat { get; set; }

	public Vector3 EnterPosition { get; set; }
	public bool IsSitting => Seat.IsValid();

	public World World => WorldLayerObject.World;

	public Action<World> OnWorldChanged { get; set; }

	[Sync] public bool InCutscene { get; set; }
	public Vector3? CutsceneTarget { get; set; }

	public string LastEntrance { get; set; }

	protected override void OnStart()
	{
		if ( IsProxy ) return;

		GameObject.BreakFromPrefab();

		Load();

		OnWorldChanged += ( world ) =>
		{
			if ( IsProxy ) return;
			Save();
		};

		_ = Fader.Instance.FadeFromBlack();

		CameraMan.Instance.Targets.Add( GameObject );
	}

	public void ModelLookAt( Vector3 position )
	{
		var dir = (position - WorldPosition).Normal;
		dir.z = 0;
		Model.WorldRotation = Rotation.LookAt( dir, Vector3.Up );
		PlayerController.Yaw = Model.WorldRotation.Yaw();
	}

	public void ModelLook( Rotation rotation )
	{
		Model.WorldRotation = rotation;
		PlayerController.Yaw = Model.WorldRotation.Yaw();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		base.OnFixedUpdate();

		if ( Input.Pressed( "use" ) && IsSitting )
		{
			Seat.Occupant = null;
			Seat = null;
			WorldPosition = EnterPosition;
			Input.Clear( "use" );
			Input.ReleaseAction( "use" );
		}

		if ( WorldPosition.z < -500f )
		{
			Log.Error( $"Player fell off the world: {WorldPosition} in world {World}" );
			if ( !string.IsNullOrEmpty( LastEntrance ) )
			{
				TeleportTo( LastEntrance );
			}
			else
			{
				Log.Error( "No last entrance found" );
			}
		}
		else if ( World != null && WorldPosition.z < World.WorldPosition.z - 500f )
		{
			Log.Error( $"Player fell off the world: {WorldPosition} in world {World}" );
			if ( !string.IsNullOrEmpty( LastEntrance ) )
			{
				TeleportTo( LastEntrance );
			}
			else
			{
				Log.Error( "No last entrance found" );
			}
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
			return default;
		}

		return gridPosition;
	}

	/*protected override void OnUpdate()
	{
		Gizmo.Draw.Arrow( WorldPosition + Vector3.Up * 16f, WorldPosition + Vector3.Up * 16 + Model.WorldRotation.Forward * 32f );
	}*/

	[Authority]
	public void TeleportTo( string entrance )
	{
		Log.Info( $"Teleporting to entrance: {entrance}" );

		if ( !World.IsValid() )
		{
			Log.Error( "World is not valid" );
			return;
		}

		var spawnPoint = World.GetEntrance( entrance );
		if ( spawnPoint.IsValid() )
		{
			TeleportTo( spawnPoint.WorldPosition, spawnPoint.WorldRotation );
			LastEntrance = entrance;
			spawnPoint.OnTeleportTo( this );
		}
		else
		{
			Log.Error( $"No spawn point found in the world for entrance {entrance}" );
		}
	}

	[Authority]
	public void TeleportTo( Vector3 pos, Rotation rot )
	{
		Log.Info( $"Teleporting to {pos} {rot}" );
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
		// return !IsSitting && !InCutscene;
		if ( IsSitting ) return false;
		if ( InCutscene ) return false;
		if ( VehicleRider.Vehicle.IsValid() ) return false;
		if ( ItemPlacer.IsPlacing ) return false;
		if ( Equips.TryGetEquippedItem<BaseCarriable>( Equips.EquipSlot.Tool, out var tool ) &&
		     tool.ShouldDisableMovement() ) return false;
		return true;
	}


	public void SnapToGrid()
	{
		WorldPosition = World.ItemGridToWorld( World.WorldToItemGrid( WorldPosition ) );
	}

	public void SetCollisionEnabled( bool state )
	{
		// CharacterController.Enabled = state;
	}

	public void SetCarriableVisibility( bool state )
	{
	}

	[Authority]
	public void StartCutscene( Vector3 target )
	{
		Log.Info( $"Starting cutscene to {target}" );
		CutsceneTarget = target;
		InCutscene = true;
	}

	[Authority]
	public void StartCutscene()
	{
		Log.Info( $"Starting cutscene (empty)" );
		CutsceneTarget = null;
		InCutscene = true;
	}


	[Authority]
	public void EndCutscene()
	{
		Log.Info( "Ending cutscene" );
		CutsceneTarget = null;
		InCutscene = false;
	}
	
	public void SetVisible( bool state )
	{
		foreach ( var renderer in Model.GetComponentsInChildren<ModelRenderer>( true ) )
		{
			renderer.Enabled = state;
		}
	}

	public static PlayerCharacter Get( Connection channel )
	{
		return Game.ActiveScene.GetAllComponents<PlayerCharacter>().FirstOrDefault( x => x.Network.Owner == channel );
	}

	[Authority]
	public void Notify( Notifications.NotificationType type, string text, float duration = 5f )
	{
		Notifications.Instance.AddNotification( type, text, duration );
	}
}

public interface IPlayerSaved
{
	void PrePlayerSave( PlayerCharacter player );
	void PostPlayerSave( PlayerCharacter player );
}
