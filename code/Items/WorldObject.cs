using System;
using Clover.Data;
using Clover.Inventory;
using Clover.Persistence;
using Clover.Player;
using Sandbox.Diagnostics;

namespace Clover.Items;

[Category( "Clover/Items" )]
[Icon( "outlet" )]
[Description(
	"Has to be added to items placed not on the grid like physics objects, otherwise they will not be saved." )]
public class WorldObject : Component, IPickupable
{
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }

	private string _prefab;

	[Property, ResourceType( "prefab" )]
	public string Prefab
	{
		get { return !string.IsNullOrEmpty( _prefab ) ? _prefab : GameObject.PrefabInstanceSource; }
		set { _prefab = value; }
	}

	[Property] public ObjectData ObjectData { get; set; }

	public bool IsBeingPickedUp { get; set; }

	[Property] public Func<bool> CanPickupFunc { get; set; }

	public bool CanPickup( PlayerCharacter player )
	{
		if ( CanPickupFunc != null )
		{
			return CanPickupFunc();
		}

		return ObjectData.CanPickup;
	}

	public void OnPickup( PlayerCharacter player )
	{
		// player.Inventory.PickUpItem( this );

		Assert.NotNull( ObjectData, "ObjectData is null" );
		Assert.NotNull( ObjectData.PickupData, "ObjectData.PickupData is null" );

		var item = OnObjectSave().Item;
		item.ItemId = ObjectData.PickupData.GetIdentifier();

		if ( player.Inventory.PickUpItem( item ) )
		{
			RemoveFromWorld();
		}
	}

	public string GetPickupName()
	{
		return ObjectData?.Name ?? "Object";
	}

	public string GetName() => GetPickupName();

	protected override void OnFixedUpdate()
	{
		if ( Networking.IsHost )
		{
			if ( WorldPosition.z < -10000f )
			{
				Log.Warning( $"{GetName()} ({Id}, {GameObject.Name}) fell out of the world at {WorldPosition}." );
				RemoveFromWorld();
			}
		}
	}

	public delegate void OnObjectSaveActionEvent( PersistentItem item );

	[Property] public OnObjectSaveActionEvent OnObjectSaveAction { get; set; }

	public delegate void OnObjectLoadActionEvent( PersistentItem item );

	[Property] public OnObjectLoadActionEvent OnObjectLoadAction { get; set; }

	public PersistentWorldObject OnObjectSave()
	{
		var persistence = PersistentItem.Create( GameObject );

		Assert.NotNull( ObjectData, "ObjectData is null" );
		Assert.NotNull( GameObject, "GameObject is null" );
		Assert.NotNull( WorldLayerObject, "WorldLayerObject is null" );

		return new PersistentWorldObject()
		{
			Position = GetRelativeWorldPosition(),
			Rotation = GetRelativeWorldRotation(),
			PrefabPath = Prefab,
			ObjectId = ObjectData.ResourceName,
			Item = persistence,
		};
	}

	public void OnObjectLoad( PersistentWorldObject worldObject )
	{
		ObjectData = ObjectData.Get( worldObject.ObjectId );
		Prefab = worldObject.PrefabPath;

		WorldPosition = worldObject.Position + WorldLayerObject.World.WorldPosition;
		WorldRotation = worldObject.Rotation;

		/*foreach ( var persistent in Components.GetAll<IPersistent>( FindMode.EverythingInSelfAndAncestors ) )
		{
			persistent.OnLoad( worldObject.Item );
		}*/

		OnObjectLoadAction?.Invoke( worldObject.Item );

		GameObject.Name = ObjectData.Name;
	}

	public Vector3 GetRelativeWorldPosition()
	{
		if ( !WorldLayerObject.IsValid() || !WorldLayerObject.World.IsValid() )
		{
			Log.Error( $"WorldLayerObject or World is not valid for {this}" );
			return WorldPosition;
		}

		return WorldLayerObject.World.GetRelativePosition( WorldPosition );
	}

	public Angles GetRelativeWorldRotation()
	{
		if ( !WorldLayerObject.IsValid() || !WorldLayerObject.World.IsValid() )
		{
			Log.Error( $"WorldLayerObject or World is not valid for {this}" );
			return WorldRotation;
		}

		return WorldLayerObject.World.GetRelativeRotation( WorldRotation );
	}

	public void RemoveFromWorld()
	{
		if ( WorldLayerObject.IsValid() && WorldLayerObject.World.IsValid() )
		{
			WorldLayerObject.World.WorldObjects.Remove( this );
		}

		GameObject.Destroy();
	}

	protected override void OnDestroy()
	{
		if ( WorldLayerObject.IsValid() && WorldLayerObject.World.IsValid() &&
		     WorldLayerObject.World.WorldObjects.Contains( this ) )
		{
			Log.Warning( $"WorldObject {this} was not removed from world before destruction. Removing now." );
			WorldLayerObject.World.WorldObjects.Remove( this );
		}
	}
}
