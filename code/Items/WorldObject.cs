using Clover.Data;
using Clover.Inventory;
using Clover.Player;

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
}
