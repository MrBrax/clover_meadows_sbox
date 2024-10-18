using System;
using Clover.Persistence;

namespace Clover.Objects;

public class GiftCarrier : Component
{
	[Property] public float Speed = 50f;

	[Property] public GameObject GiftVisual { get; set; }
	[Property] public GameObject GiftFallingScene { get; set; }
	[Property] public GameObject GiftModelSpawn { get; set; }

	public List<PersistentItem> Items { get; set; } = new();

	private bool _hasDroppedGift;

	protected override void OnFixedUpdate()
	{
		WorldPosition += WorldRotation.Forward * Speed * Time.Delta;

		if ( WorldPosition.x < -1000 || WorldPosition.x > 5000 || WorldPosition.z < -1000 || WorldPosition.z > 5000 )
		{
			DestroyGameObject();
		}
	}
	
	public static void SpawnRandom()
	{

		Log.Info( "Spawning random gift carrier" );

		var world = NodeManager.WorldManager.ActiveWorld;

		if ( world == null )
		{
			Log.Error( "No active world" );
			return;
		}

		var giftCarrier = SceneUtility.GetPrefabScene( 
				ResourceLibrary.Get<PrefabFile>( "objects/stork/stork.prefab" ) 
				)
			.Clone();

		var height = 256f;

		var westOrEast = Random.Shared.Next() % 2 == 0 ? -500 : 4500;
		var NorthOrSouth = Random.Shared.Next() % 2 == 0 ? -500 : 4500;

		giftCarrier.WorldPosition = new Vector3( westOrEast, NorthOrSouth, height );

		Log.Info( $"Spawned at {giftCarrier.WorldPosition}" );

		var midpoint = new Vector3( 2000, 2000, 0 );

		// face the midpoint
		giftCarrier.WorldRotation = Rotation.LookAt( midpoint - giftCarrier.WorldPosition, Vector3.Up );
		// giftCarrier.RotateObjectLocal( Vector3.Up, Mathf.Pi );

		Log.Info( $"Facing {midpoint} ({giftCarrier.WorldRotation}) ({giftCarrier.WorldRotation.Forward}) ({giftCarrier.WorldRotation.Yaw()})" );

		// giftCarrier.Items = GenerateRandomItems();

	}

	/*private static List<PersistentItem> GenerateRandomItems()
	{
		var category = Loader.LoadResource<ItemCategoryData>( "res://collections/gifts.tres" );
		var itemData = category.Items.PickRandom();
		return [itemData.CreateItem()]; // TODO: give maybe multiple items?
	}*/
}
