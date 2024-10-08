using System;
using System.IO;
using System.Text.Json;
using Clover.Carriable;
using Clover.Components;
using Clover.Data;
using Clover.Inventory;
using Clover.Persistence;

namespace Clover.Player;

public sealed class PlayerCharacter : Component
{
	[ActionGraphNode( "player.local" ), Title( "Local Player" ), Icon( "face" ), Category( "Clover" )]
	public static PlayerCharacter Local =>
		Game.ActiveScene.GetAllComponents<PlayerCharacter>().FirstOrDefault( x => !x.IsProxy );

	[Sync] public string PlayerId { get; set; }
	[Sync] public string PlayerName { get; set; }

	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }
	[RequireComponent] public CharacterController CharacterController { get; set; }
	[RequireComponent] public PlayerController PlayerController { get; set; }
	[RequireComponent] public Inventory.Inventory Inventory { get; set; }
	[RequireComponent] public Equips Equips { get; set; }

	[Property] public int Clovers { get; set; }

	[Property] public GameObject Model { get; set; }

	[Property] public GameObject InteractPoint { get; set; }

	[Property] public SittableNode Seat { get; set; }

	public Vector3 EnterPosition { get; set; }
	public bool IsSitting => Seat.IsValid();

	public World World => WorldLayerObject.World;

	public Action<World> OnWorldChanged { get; set; }

	public bool InCutscene { get; set; }
	public Vector3? CutsceneTarget { get; set; }

	public string LastEntrance { get; set; }

	protected override void OnStart()
	{
		base.OnStart();
		GameObject.BreakFromPrefab();

		Load();

		OnWorldChanged += ( world ) =>
		{
			Save();
		};
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

		if ( WorldPosition.z < -500f )
		{
			Log.Error( $"Player fell off the world: {WorldPosition} in world {World}" );
			TeleportTo( LastEntrance );
		}
		else if ( World != null && WorldPosition.z < World.WorldPosition.z - 500f )
		{
			Log.Error( $"Player fell off the world: {WorldPosition} in world {World}" );
			TeleportTo( LastEntrance );
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

	public void TeleportTo( string entrance )
	{
		var spawnPoint = World.GetEntrance( entrance );
		if ( spawnPoint.IsValid() )
		{
			TeleportTo( spawnPoint.WorldPosition, spawnPoint.WorldRotation );
			LastEntrance = entrance;
		}
		else
		{
			Log.Error( $"No spawn point found in the world for entrance {entrance}" );
		}
	}

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
		// return !IsSitting && !InCutscene;
		if ( IsSitting ) return false;
		if ( InCutscene ) return false;
		if ( Components.TryGet<VehicleRider>( out var rider ) && rider.Vehicle.IsValid() ) return false;
		return true;
	}


	public string SaveFilePath => $"players/{PlayerId}.json";

	public PlayerSaveData SaveData { get; set; }


	public void Save()
	{
		Scene.RunEvent<IPlayerSaved>( x => x.PrePlayerSave( this ) );

		SaveData ??= new PlayerSaveData( PlayerId );

		SaveData.Name = PlayerName ?? Network.Owner.DisplayName;

		SaveData.InventorySlots.Clear();
		foreach ( var slot in Inventory.Container.GetUsedSlots() )
		{
			SaveData.InventorySlots.Add( slot );
		}

		SaveData.EquippedItems.Clear();
		foreach ( var (slot, item) in Equips.EquippedItems )
		{
			SaveData.EquippedItems.Add( slot, PersistentItem.Create( item ) );
		}

		SaveData.Clovers = Clovers;

		SaveData.LastSave = DateTime.Now;

		var json = JsonSerializer.Serialize( SaveData, GameManager.JsonOptions );

		FileSystem.Data.CreateDirectory( "players" );

		FileSystem.Data.WriteAllText( SaveFilePath, json );

		Scene.RunEvent<IPlayerSaved>( x => x.PostPlayerSave( this ) );
	}


	public void Load()
	{
		// TODO: temporary fix for player id
		if ( string.IsNullOrEmpty( PlayerId ) )
		{
			var save = FileSystem.Data.FindFile( "players", "*.json" );
			if ( save != null && save.Any() )
			{
				PlayerId = Path.GetFileNameWithoutExtension( save.First() );
				Log.Info( $"PlayerId found: {PlayerId}" );
			}
			else
			{
				PlayerId = Guid.NewGuid().ToString();
				Log.Info( $"PlayerId generated: {PlayerId}" );
			}
		}

		if ( !FileSystem.Data.FileExists( SaveFilePath ) )
		{
			Log.Warning( $"File {SaveFilePath} does not exist" );
			return;
		}

		var json = FileSystem.Data.ReadAllText( SaveFilePath );
		SaveData = JsonSerializer.Deserialize<PlayerSaveData>( json, GameManager.JsonOptions );

		PlayerName = SaveData.Name;
		Clovers = SaveData.Clovers;

		// limit inventory slots if for some reason it exceeds max items
		if ( SaveData.InventorySlots.Count > Inventory.Container.MaxItems )
		{
			Log.Error( "Inventory slots count exceeds max items" );
			SaveData.InventorySlots = SaveData.InventorySlots.Take( Inventory.Container.MaxItems ).ToList();
		}

		Inventory.Container.RemoveSlots();

		foreach ( var slot in SaveData.InventorySlots )
		{
			if ( slot.GetItem() == null )
			{
				Log.Error( "Item is null" );
				continue;
			}

			if ( slot.GetItem().ItemData == null )
			{
				Log.Error( "ItemData is null" );
				continue;
			}

			Inventory.Container.ImportSlot( slot );
		}

		Inventory.Container.RecalculateIndexes();

		foreach ( var (slot, item) in SaveData.EquippedItems )
		{
			if ( item.ItemData is ToolData toolData )
			{
				var carriableNode = toolData.SpawnCarriable();

				if ( carriableNode == null )
				{
					Log.Error( $"Item is not a carriable" );
					continue;
				}

				Equips.SetEquippedItem( slot, carriableNode.GameObject );
			}
		}
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
}

public interface IPlayerSaved
{
	void PrePlayerSave( PlayerCharacter player );
	void PostPlayerSave( PlayerCharacter player );
}
