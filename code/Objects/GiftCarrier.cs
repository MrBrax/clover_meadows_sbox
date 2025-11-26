using System;
using Braxnet;
using Clover.Data;
using Clover.Interfaces;
using Clover.Persistence;

namespace Clover.Objects;

public class GiftCarrier : Component, IShootable
{
	[Property] public float Speed = 50f;

	[Property] public GameObject GiftInHolderVisual { get; set; }

	// [Property] public GameObject GiftFallingScene { get; set; }
	[Property] public GameObject GiftModelSpawn { get; set; }

	[Property] public ItemData GiftItem { get; set; }

	[Property] public SoundEvent WingSound { get; set; }

	public List<PersistentItem> Items { get; set; } = new();

	private bool _hasDroppedGift;

	private SoundHandle _wingSoundHandle;

	protected override void OnStart()
	{
		_wingSoundHandle = GameObject.PlaySound( WingSound );
	}

	protected override void OnDestroy()
	{
		_wingSoundHandle?.Stop();
	}

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

		var world = NodeManager.WorldManager.GetWorld( "island" );

		if ( world == null )
		{
			Log.Error( "No active world" );
			return;
		}

		var giftCarrierGameObject = SceneUtility.GetPrefabScene(
				ResourceLibrary.Get<PrefabFile>( "objects/stork/stork.prefab" )
			)
			.Clone();

		giftCarrierGameObject.SetParent( world.GameObject );

		var worldLayerObject = giftCarrierGameObject.GetComponent<WorldLayerObject>();
		worldLayerObject.Layer = world.Layer;
		worldLayerObject.RebuildVisibility( world.Layer );

		var giftCarrier = giftCarrierGameObject.GetComponent<GiftCarrier>();

		var height = 256f;

		var westOrEast = Random.Shared.Next() % 2 == 0 ? -500 : 4500;
		var NorthOrSouth = Random.Shared.Next() % 2 == 0 ? -500 : 4500;

		giftCarrier.WorldPosition = new Vector3( westOrEast, NorthOrSouth, height );

		Log.Info( $"Spawned at {giftCarrier.WorldPosition}" );

		var midpoint = new Vector3( 2000, 2000, 0 );

		// face the midpoint
		giftCarrier.WorldRotation = Rotation.LookAt( midpoint - giftCarrier.WorldPosition, Vector3.Up );
		// giftCarrier.RotateObjectLocal( Vector3.Up, Mathf.Pi );

		Log.Info(
			$"Facing {midpoint} ({giftCarrier.WorldRotation}) ({giftCarrier.WorldRotation.Forward}) ({giftCarrier.WorldRotation.Yaw()})" );

		giftCarrier.Items = GenerateRandomItems();
	}

	public void OnShot( Pellet pellet )
	{
		if ( _hasDroppedGift ) return;

		Log.Info( "Shot" );

		var world = NodeManager.WorldManager.ActiveWorld;

		var endPosGrid = world.WorldToItemGrid( WorldPosition );
		var endPosWorld = world.ItemGridToWorld( endPosGrid );

		var giftModel = GiftModelSpawn.Clone( GiftInHolderVisual.WorldPosition, GiftInHolderVisual.WorldRotation );

		/*var tween = GetTree().CreateTween();
		var p = tween.TweenProperty( giftModel, "global_position", endPosWorld, 2f );
		p.SetTrans( Tween.TransitionType.Bounce );
		p.SetEase( Tween.EaseType.Out );

		tween.TweenCallback( Callable.From( () =>
		{
			giftModel.QueueFree();
			SpawnGift( endPosWorld );
		} ) );*/

		// SpawnGift( endPosWorld );

		CameraMan.Instance.AddTarget( giftModel );

		var tween = TweenManager.CreateTween();
		tween.AddPosition( giftModel, endPosWorld, 2f ).SetEasing( Sandbox.Utility.Easing.BounceOut );

		tween.OnFinish += () =>
		{
			giftModel.Destroy();
			SpawnGift( endPosWorld );
		};

		// LookAtWhenShotTarget = giftModel;

		// QueueFree();
		GiftInHolderVisual.Enabled = false;
		_hasDroppedGift = true;
		Speed *= 2f;
		// AnimationPlayer.SpeedScale = 2f;
	}

	public void SpawnGift( Vector3 position )
	{
		var world = NodeManager.WorldManager.ActiveWorld;

		var gridPos = world.WorldToItemGrid( position );


		var pItem = PersistentItem.Create( GiftItem );
		pItem.SetSaveData( "Items", Items );

		var nodeLink = world.SpawnPlacedItem( pItem, gridPos, World.ItemRotation.North );
	}

	private static List<PersistentItem> GenerateRandomItems()
	{
		var catalogue = ResourceLibrary.Get<CatalogueData>( "catalogues/stork_items.cata" );
		var itemData = Random.Shared.FromList( catalogue.Items );
		return new List<PersistentItem> { itemData.CreatePersistentItem() }; // TODO: give maybe multiple items?
	}
}
