using System;
using Clover.Carriable;
using Clover.Data;
using Clover.Interactable;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Items;

[Category( "Clover/Items" )]
public class Plant : Component, IInteract, IWaterable, IDiggable, IPersistent
{
	[RequireComponent] public WorldItem WorldItem { get; set; }
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }

	[Property] public GameObject Model { get; set; }
	[Property] public GameObject SeedHole { get; set; } // TODO: better name

	[Property] public GameObject WateredParticles { get; set; }

	// [Property] public PlantData PlantData { get; set; }
	public PlantData PlantData => WorldItem.ItemData as PlantData;

	public DateTime LastWatered { get; set; }

	public DateTime LastProcess { get; set; }
	public float Growth { get; set; } = 0f;
	public float Wilt { get; set; } = 0f;
	public float Water { get; set; } = 0f;
	public bool WantsToSpread { get; set; } = false;

	// flower grows fully in 3 days
	public const float GrowthPerHour = 1f / 72f;

	// wilting takes a day
	public const float WiltPerHour = 1f / 24f;

	// watered once per day
	public const float WaterUsedPerHour = 1f / 24f;

	public enum GrowthStage
	{
		Seed = 0,
		Sprout = 1,
		Stem = 2,
		Budding = 3,
		Flowering = 4,
	}

	public GrowthStage Stage
	{
		get
		{
			if ( Growth < 0.2f )
			{
				return GrowthStage.Seed;
			}
			else if ( Growth < 0.4f )
			{
				return GrowthStage.Sprout;
			}
			else if ( Growth < 0.6f )
			{
				return GrowthStage.Stem;
			}
			else if ( Growth < 0.8f )
			{
				return GrowthStage.Budding;
			}
			else
			{
				return GrowthStage.Flowering;
			}
		}
	}

	public void StartInteract( PlayerCharacter player )
	{
		throw new System.NotImplementedException();
	}

	public void FinishInteract( PlayerCharacter player )
	{
		throw new System.NotImplementedException();
	}

	public string GetInteractName()
	{
		return "?";
	}

	public void OnWater( WateringCan wateringCan )
	{
		Log.Info( "Watering plant" );
		LastWatered = DateTime.Now;
		Water = 1f;
		UpdateVisuals();
	}

	public bool CanDig()
	{
		return Growth >= 1f;
	}

	public bool OnDig( PlayerCharacter player, WorldItem item )
	{
		// TODO: give item
		return false;
	}

	public bool GiveItemWhenDug()
	{
		throw new System.NotImplementedException();
	}

	public void OnSave( PersistentItem item )
	{
		item.SetSaveData( "LastWatered", LastWatered );
		item.SetSaveData( "LastProcess", LastProcess );
		item.SetSaveData( "Growth", Growth );
		item.SetSaveData( "Wilt", Wilt );
		item.SetSaveData( "Water", Water );
	}

	public void OnLoad( PersistentItem item )
	{
		LastWatered = item.GetSaveData<DateTime>( "LastWatered" );
		LastProcess = item.GetSaveData<DateTime>( "LastProcess" );
		Growth = item.GetSaveData<float>( "Growth" );
		Wilt = item.GetSaveData<float>( "Wilt" );
		Water = item.GetSaveData<float>( "Water" );

		UpdateVisuals();
	}

	protected override void OnFixedUpdate()
	{
		if ( DateTime.Now - LastProcess > TimeSpan.FromHours( 1 ) )
		{
			SimulateHour( DateTime.Now );
			LastProcess = DateTime.Now;
			UpdateVisuals();
		}
	}

	private void UpdateVisuals()
	{
		if ( Model.IsValid() )
		{
			var growth = Math.Clamp( Growth, 0.2f, 1f );
			Model.LocalScale = new Vector3( growth, growth, growth );
		}

		if ( SeedHole.IsValid() )
		{
			SeedHole.Enabled = Growth < 0.2f;
		}

		if ( WateredParticles.IsValid() )
		{
			WateredParticles.Enabled = Water > 0.9f;
		}
	}

	public void SimulateHour( DateTime time )
	{
		/*var lastRain = NodeManager.WeatherManager.GetLastPrecipitation( time );
		var hoursSinceLastRain = (time - lastRain.Time).TotalHours;
		if ( hoursSinceLastRain <= 1 )
		{
			Water = 1f;
			LastWatered = lastRain.Time;
		}*/

		// growth
		if ( Growth < 1f && Wilt == 0 )
		{
			// Growth += GrowthPerHour;
			Growth = Math.Clamp( Growth + GrowthPerHour, 0f, 1f );
		}

		// wilt
		if ( Water > 0 )
		{
			Water = Math.Clamp( Water - WaterUsedPerHour, 0f, 1f );
			Wilt = Math.Clamp( Wilt - WiltPerHour, 0f, 1f );
		}
		else
		{
			Wilt = Math.Clamp( Wilt + WiltPerHour, 0f, 1f );
		}

		if ( Stage == GrowthStage.Flowering && Wilt == 0 )
		{
			if ( Random.Shared.Float() > 0.9 )
			{
				WantsToSpread = true;
			}
		}

		if ( WantsToSpread )
		{
			Spread();
			WantsToSpread = false;
		}

		Log.Info( $"Simulated hour for plant: Growth: {Growth}, Wilt: {Wilt}, Water: {Water}" );
	}

	public void Spread()
	{
		/*var nodeLink = WorldLayerObject.World.GetItem( GameObject );
		if ( nodeLink == null )
		{
			Log.Warning( "No node link found for plant" );
			return;
		}

		var neighbors = WorldLayerObject.World.GetNeighbors( nodeLink.GridPosition );
		var emptyNeighbors =
			neighbors.Where( n => WorldLayerObject.World.GetItem( n, World.ItemPlacement.Floor ) == null );
		var validNeighbors = emptyNeighbors.Where( n => !WorldLayerObject.World.IsBlockedGridPosition( n ) ).ToList();

		if ( !validNeighbors.Any() )
		{
			Log.Warning( "No valid neighbors found for plant" );
			return;
		}

		// var randomNeighbor = validNeighbors.ToList().PickRandom();
		var randomNeighbor = Random.Shared.FromList( validNeighbors.ToList() );

		// clone self
		// World.SpawnNode( ItemData, (ItemData as PlantData).PlantedScene, randomNeighbor, World.ItemRotation.North, World.ItemPlacement.Floor );
		WorldLayerObject.World.SpawnCustomNode(
			PlantData,
			PlantData.PlantedScene,
			randomNeighbor,
			World.ItemRotation.North,
			World.ItemPlacement.Floor
		);*/
	}
}
