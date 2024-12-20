﻿using System;
using Clover.Carriable;
using Clover.Components;
using Clover.Npc;
using Clover.Ui;

namespace Clover.Player;

[Title( "Player Character" )]
[Icon( "face" )]
[Category( "Clover/Player" )]
[Description( "The player character component." )]
public sealed partial class PlayerCharacter : Component
{
	private static PlayerCharacter _local;

	[ActionGraphNode( "player.local" ), Title( "Local Player" ), Icon( "face" ), Category( "Clover" )]
	public static PlayerCharacter Local
	{
		get
		{
			if ( _local.IsValid() ) return _local;
			_local = Game.ActiveScene.GetAllComponents<PlayerCharacter>().FirstOrDefault( x => !x.IsProxy );
			return _local;
		}
	}

	[Sync] public string PlayerId { get; set; }
	[Sync] public string PlayerName { get; set; }

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

	public bool IsLocalPlayer => Local == this;

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

		CameraMan.Instance.AddTarget( GameObject );

		MainUi.Instance.LastInput = 0;
	}

	// TODO: maybe stop using yaw
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
		var boxPos = InteractPoint.WorldPosition /*+ (Vector3.Down * WorldManager.WorldOffset)*/;
		// DebugOverlay.Sphere( new Sphere( boxPos, 8f ), Color.Red, 1f );

		var boxPosHeight = boxPos.z;

		var world = WorldManager.Instance.ActiveWorld;

		var gridPosition = world.WorldToItemGrid( boxPos );
		var worldPosition = world.ItemGridToWorld( gridPosition );

		// Log.Info( $"Aiming at {boxPos} -> {gridPosition} -> {worldPosition}" );

		// DebugOverlay.Sphere( new Sphere( worldPosition, 8f ), Color.Green, 1f );

		if ( MathF.Abs( boxPosHeight - worldPosition.z ) > 16f )
		{
			// throw new System.Exception( $"Aiming at a higher position: {boxPos} -> {worldPosition}" );
			Log.Warning(
				$"Aiming at a higher position: {boxPos} -> {worldPosition} ({MathF.Abs( boxPosHeight - worldPosition.z )})" );
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
		if ( IsSitting ) return false;
		if ( InCutscene ) return false;
		if ( VehicleRider.Vehicle.IsValid() ) return false;
		if ( ItemPlacer.IsPlacing || ItemPlacer.IsMoving ) return false;
		if ( Equips.TryGetEquippedItem<BaseCarriable>( Equips.EquipSlot.Tool, out var tool ) &&
		     tool.ShouldDisableMovement() ) return false;
		if ( PlayerInteract.InteractionTarget.IsValid() )
		{
			if ( PlayerInteract.InteractionTarget.GetComponent<BaseNpc>().IsValid() ) return false;
		}

		if ( Components.TryGet<HideAndSeek>( out var hideAndSeek ) )
		{
			if ( HideAndSeek.Leader.IsValid() && HideAndSeek.Leader.IsRoundActive && hideAndSeek.IsBlind )
			{
				return false;
			}
		}

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

	[Broadcast]
	public static void NotifyAll( Notifications.NotificationType type, string text, float duration = 5f )
	{
		foreach ( var player in Game.ActiveScene.GetAllComponents<PlayerCharacter>() )
		{
			player.Notify( type, text, duration );
		}
	}
}

public interface IPlayerSaved
{
	void PrePlayerSave( PlayerCharacter player );
	void PostPlayerSave( PlayerCharacter player );
}
