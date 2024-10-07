using Clover.Data;
using Clover.Inventory;
using Clover.Persistence;
using Clover.Player;
using Sandbox.Diagnostics;

namespace Clover.Items;

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
	
	public bool CanPickup( PlayerCharacter player )
	{
		throw new System.NotImplementedException();
	}

	public void OnPickup( PlayerCharacter player )
	{
		throw new System.NotImplementedException();
	}

	public PersistentWorldObject OnObjectSave()
	{

		var persistence = PersistentItem.Create( GameObject );
		
		Assert.NotNull( ObjectData, "ObjectData is null" );
		Assert.NotNull( GameObject, "GameObject is null" );
		Assert.NotNull( WorldLayerObject, "WorldLayerObject is null" );
		
		return new PersistentWorldObject()
		{
			Position = GameObject.WorldPosition - WorldLayerObject.World.WorldPosition,
			Rotation = WorldRotation,
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
		
		foreach ( var persistent in Components.GetAll<IPersistent>( FindMode.EverythingInSelfAndAncestors ) )
		{
			persistent.OnLoad( worldObject.Item );
		}
		
		GameObject.Name = ObjectData.Name;
		
	}
}
