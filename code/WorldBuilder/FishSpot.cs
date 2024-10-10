using System;
using Clover.Animals;
using Clover.Carriable;
using Clover.Data;
using Clover.Player;

namespace Clover.WorldBuilder;

public class FishSpot : Component
{
	
	[Property] public List<FishData> SpecialFish { get; set; }
	
	[Property] public FishData.FishLocation Location { get; set; } = FishData.FishLocation.River;

	[Property] public float SpawnRadius { get; set; } = 100f;

	protected override void OnStart()
	{
		if ( Random.Shared.Float() > 0.5f ) SpawnFish();
	}
	
	private TimeSince _lastSpawn;

	protected override void OnFixedUpdate()
	{
		if ( _lastSpawn > 60 )
		{
			_lastSpawn = 0;
			CheckSpawn();
		}
		
	}

	private void CheckSpawn()
	{
		var allFish = Scene.GetAllComponents<CatchableFish>();
		var fishNearby = allFish.Where( f => f.WorldPosition.Distance( WorldPosition ) < SpawnRadius * 1.5f ).ToList();
		if ( fishNearby.Count > 0 ) return;
		if ( PlayerNearby() ) return;
		if ( Random.Shared.Float() > 0.5f ) return;
		SpawnFish();
	}

	private bool PlayerNearby()
	{
		var players = Scene.GetAllComponents<PlayerCharacter>();
		return players.Any( p => p.WorldPosition.Distance( WorldPosition ) < SpawnRadius * 2f );
	}

	private void SpawnFish()
	{
		
		var findPositionTry = 0;
		var basePosition = WorldPosition;

		Log.Trace( $"Trying to spawn fish at {basePosition}." );

		FishData fishData;

		if ( SpecialFish != null && SpecialFish.Count > 0 )
		{
			// fishData = SpecialFish.PickRandom();
			fishData = Random.Shared.FromList( SpecialFish );
		}
		else
		{
			// var resources = Resources.LoadAllResources( "res://items/fish/" );
			// var fish = resources.Where( r => r is FishData ).Select( r => r as FishData ).ToList();
			// fishData = fish.PickRandom();
			
			var fishes = ResourceLibrary.GetAll<FishData>().Where( f => f.Location == Location ).ToList();
			
			fishData = Random.Shared.FromList( fishes );
			
			if ( !fishData.IsValid() )
			{
				Log.Warning( $"No fish data found for location {Location}." );
				return;
			}
			
		}

		if ( fishData == null )
		{
			Log.Warning( $"No fish data found." );
			return;
		}

		while ( findPositionTry < 10 )
		{
			
			var randomPosition = basePosition;
			
			randomPosition += new Vector3(
				Random.Shared.Float() * SpawnRadius * 2 - SpawnRadius,
				Random.Shared.Float() * SpawnRadius * 2 - SpawnRadius,
				0
			);

			if ( FishingRod.CheckForWater( randomPosition ) )
			{
				
				var world = WorldManager.Instance?.ActiveWorld;
				
				if ( world == null )
				{
					Log.Warning( "No active world found." );
					return;
				}

				var prefab =
					SceneUtility.GetPrefabScene(
						ResourceLibrary.Get<PrefabFile>( "animals/fish/fish_shadow.prefab" ) );
				
				if ( prefab == null )
				{
					Log.Error( "Failed to load fish prefab." );
					return;
				}

				var gameObject = prefab.Clone();
				gameObject.SetParent( world.GameObject );
				
				var fish = gameObject.GetComponent<CatchableFish>();
				
				if ( fish == null )
				{
					Log.Error( "Failed to get CatchableFish component." );
					gameObject.Destroy();
					return;
				}
				
				fish.Data = fishData;
				fish.WorldPosition = randomPosition;
				fish.SetSize( fishData.Size );
				fish.Weight = fishData.Weight.GetValue();

				gameObject.NetworkSpawn();
				
				Log.Info( $"Spawned fish {fishData.Name} at {randomPosition}." );
				
				return;
			}

			findPositionTry++;
		}

		Log.Warning( $"Failed to find a valid position to spawn fish on {this}." );

	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Hitbox.Sphere( new Sphere( Vector3.Zero, SpawnRadius ) );
		
		Gizmo.Draw.LineSphere( Vector3.Zero, SpawnRadius );

		Gizmo.Draw.Text( Location.ToString(), global::Transform.Zero );
	}
}
