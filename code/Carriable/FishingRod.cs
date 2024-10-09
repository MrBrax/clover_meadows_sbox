using System;
using Braxnet;
using Clover.Animals;
using Clover.Objects;
using Clover.Persistence;

namespace Clover.Carriable;

public class FishingRod : BaseCarriable
{
	[Property] public GameObject BobberPrefab { get; set; }
	// [Property] public GameObject 

	[Property] public GameObject LineStartPoint { get; set; }
	
	[Property] public SoundEvent CastSound { get; set; }
	[Property] public SoundEvent ReelSound { get; set; }
	[Property] public SoundEvent HookSound { get; set; }

	private FishingBobber Bobber;

	private bool _hasCasted = false;
	private bool _isCasting = false;
	private bool _isBusy = false;

	private float _trashChance = 0.1f; // TODO: base this on luck?
	
	private SoundHandle _reelSound;

	public override void OnUseDown()
	{
		if ( !CanUse() )
		{
			Log.Warning( "Cannot use." );
			return;
		}
		
		NextUse = 1f;

		if ( _isCasting )
		{
			Log.Warning( "Already casting." );
			return;
		}

		if ( _hasCasted && !Bobber.IsValid() )
		{
			Log.Warning( "Bobber is not valid." );
			_hasCasted = false;
		}

		if ( _hasCasted )
		{
			if ( Bobber.Fish.IsValid() && Bobber.Fish.State == CatchableFish.FishState.FoundBobber )
			{
				Bobber.Fish.TryHook();
			}
			else if ( Bobber.Fish.IsValid() && Bobber.Fish.State == CatchableFish.FishState.Fighting )
			{
				Bobber.Fish.Pull();
				_reelSound = GameObject.PlaySound( ReelSound );
				_reelSound.Pitch = 0.8f + Random.Shared.Float() * 0.4f;
			}
			else
			{
				ReelIn();
			}
		}
		else
		{
			Cast();
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		_reelSound?.Stop();
	}

	protected override void OnFixedUpdate()
	{
		/*if ( _bobber.IsValid() )
		{

			if ( IsInstanceValid( Bobber.Fish ) && Bobber.Fish.State == CatchableFish.FishState.Fighting )
			{
				GetNode<AnimationPlayer>( "AnimationPlayer" ).Play( "fight" );
				CreateLine( false );
			}
			else
			{

				CreateLine( true );
			}
		}*/

		if ( Bobber.IsValid() )
		{
			if ( Bobber.Fish.IsValid() && Bobber.Fish.State == CatchableFish.FishState.Fighting )
			{
				// GetNode<AnimationPlayer>( "AnimationPlayer" ).Play( "fight" );
				// CreateLine( false );
			}
			else
			{
				// CreateLine( true );
			}
		}
	}

	private Vector3 GetCastPosition()
	{
		return Player.WorldPosition + Player.Model.WorldRotation.Forward * 128f;
	}

	public override bool ShouldDisableMovement()
	{
		return _hasCasted || _isCasting;
	}

	private async void Cast()
	{
		Log.Info( "Casting." );
		if ( Player == null ) throw new Exception( "Player is null." );

		if ( !CheckForWater( GetCastPosition() ) )
		{
			Log.Warning( $"CAST: No water found at {GetCastPosition()}." );
			return;
		}

		_isCasting = true;

		// play the cast animation
		// GetNode<AnimationPlayer>( "AnimationPlayer" ).Play( "cast" );
		// Model.LocalRotation = Rotation.FromPitch( 90f );

		// wait for the animation to finish
		// await ToSignal( GetNode<AnimationPlayer>( "AnimationPlayer" ), AnimationPlayer.SignalName.AnimationFinished );
		// await Task.DelayRealtimeSeconds( 0.5f );
		
		var tween1 = TweenManager.CreateTween();
		tween1.AddLocalRotation( Model, Rotation.FromPitch( 90f ), 0.5f ).SetEasing( Sandbox.Utility.Easing.QuadraticOut );
		tween1.AddLocalRotation( Model, Rotation.FromPitch( 0f ), 0.3f ).SetEasing( Sandbox.Utility.Easing.QuadraticIn );
		await tween1.Wait();
		
		GameObject.PlaySound( CastSound );
		
		var tween2 = TweenManager.CreateTween();
		tween2.AddLocalRotation( Model, Rotation.FromPitch( -20f ), 0.3f ).SetEasing( Sandbox.Utility.Easing.QuadraticOut );
		await tween2.Wait();
		
		var tween3 = TweenManager.CreateTween();
		tween3.AddLocalRotation( Model, Rotation.FromPitch( 0f ), 0.3f ).SetEasing( Sandbox.Utility.Easing.QuadraticIn );
		
		// Model.LocalRotation = Rotation.FromPitch( 0f );

		if ( !Bobber.IsValid() )
		{
			/* var waterPosition = GetWaterSurface( GetCastPosition() );
			Bobber = BobberScene.Instantiate<FishingBobber>();
			Bobber.Rod = this;
			Player.World.AddChild( Bobber );
			Bobber.GlobalPosition = waterPosition; */

			var waterPosition = GetWaterSurface( GetCastPosition() );

			Bobber = BobberPrefab.Clone().Components.Get<FishingBobber>();
			Bobber.Rod = this;
			// Player.World.AddChild( Bobber );

			Bobber.WorldPosition = LineStartPoint.WorldPosition;
			
			CameraMan.Instance.Targets.Add( Bobber.GameObject );

			// tween the bobber to the water in an arc
			/*var tween = GetTree().CreateTween();
			tween.TweenMethod( Callable.From<float>( ( float i ) =>
			{
				Bobber.GlobalPosition = LinePoint.GlobalPosition.CubicInterpolate( waterPosition,
					LinePoint.GlobalPosition + Vector3.Down * 1f, waterPosition + Vector3.Down * 10f, i );
			} ), 0f, 1f, 0.5f ).SetEase( Tween.EaseType.Out );

			await ToSignal( tween, Tween.SignalName.Finished );*/
			
			var tween = TweenManager.CreateTween();
			tween.AddPosition( Bobber.GameObject, waterPosition, 0.5f ).SetEasing( Sandbox.Utility.Easing.QuadraticIn );
			await tween.Wait();

			Bobber.OnHitWater();

			/*var splash = SplashScene.Instantiate<Node3D>();
			Player.World.AddChild( splash );
			splash.GlobalPosition = waterPosition;*/
		}
		else
		{
			Log.Error( "Bobber is already valid." );
		}

		// CreateLine();

		_isCasting = false;

		_hasCasted = true;
	}

	private const float WaterCheckHeight = 64f;

	private bool CheckForWater( Vector3 position )
	{
		/*var spaceState = GetWorld3D().DirectSpaceState;

		var traceWater =
			new Trace( spaceState ).CastRay(
				PhysicsRayQueryParameters3D.Create( position, position + (Vector3.Down * WaterCheckHeight),
					World.WaterLayer ) );

		if ( traceWater == null )
		{
			Log.Warning( $"No water found at {position}." );
			return false;
		}

		var traceTerrain =
			new Trace( spaceState ).CastRay(
				PhysicsRayQueryParameters3D.Create( traceWater.Position, traceWater.Position + (Vector3.Down * 1f),
					World.TerrainLayer ) );

		if ( traceTerrain != null )
		{
			Log.Warning( $"Terrain found at {position}." );
			return false;
		}*/

		var traceWater = Scene.Trace.Ray( position, position + Vector3.Down * WaterCheckHeight )
			.WithTag( "water" )
			.Run();

		if ( !traceWater.Hit )
		{
			Log.Warning( $"No water found at {position}." );
			return false;
		}

		var traceTerrain = Scene.Trace.Ray( traceWater.HitPosition, traceWater.HitPosition + Vector3.Down * 1f )
			.WithTag( "terrain" )
			.Run();

		if ( traceTerrain.Hit )
		{
			Log.Warning( $"Terrain found at {position}." );
			return false;
		}


		// TODO: check if it's the waterfall or something
		/* if ( traceWater.Normal != Vector3.Up )
		{
			Log.Warning( $"Water normal is not up ({traceWater.Normal})." );
			return false;
		} */

		return true;
	}

	public Vector3 GetWaterSurface( Vector3 position )
	{
		/*var spaceState = GetWorld3D().DirectSpaceState;

		var traceWater =
			new Trace( spaceState ).CastRay(
				PhysicsRayQueryParameters3D.Create( position + Vector3.Up * WaterCheckHeight,
					position + Vector3.Down * (WaterCheckHeight * 2), World.WaterLayer ) );

		if ( traceWater == null )
		{
			Log.Warning( $"No water found at {position}." );
			return Vector3.Zero;
		}*/
		
		var traceWater = Scene.Trace.Ray( position + Vector3.Up * WaterCheckHeight, position + Vector3.Down * (WaterCheckHeight * 2) )
			.WithTag( "water" )
			.Run();

		if ( !traceWater.Hit )
		{
			Log.Warning( $"No water found at {position}." );
			return Vector3.Zero;
		}

		return traceWater.HitPosition;
	}

	private void ReelIn()
	{
		Log.Info( "Reeling in." );
		if ( Bobber.IsValid() )
		{
			Log.Info( "Freeing bobber." );
			Bobber.DestroyGameObject();
			Bobber = null;
		}

		_hasCasted = false;

		// ((ImmediateMesh)LineMesh.Mesh).ClearSurfaces();
		// ClearLine();

		// GetNode<AnimationPlayer>( "AnimationPlayer" ).Play( "RESET" );
		
		_reelSound?.Stop();
	}

	public async void CatchFish( CatchableFish fishInWater )
	{
		if ( !fishInWater.IsValid() )
		{
			Log.Warning( "Fish is not valid." );
			return;
		}

		Log.Info( "Caught fish." );
		NextUse = 3f;
		// GetNode<AudioStreamPlayer3D>( "Splash" ).Play();
		// GameObject.PlaySound( 
		// GetNode<AnimationPlayer>( "AnimationPlayer" ).Play( "catch" );

		// var isTrash = GD.Randf() < _trashChance;
		var isTrash = Random.Shared.Float() < _trashChance;

		if ( !isTrash )
		{
			/*var carryableFish = fishInWater.Data.CarryScene.Instantiate<Fish>();
			Player.World.AddChild( carryableFish );
			carryableFish.GlobalTransform = fishInWater.GlobalTransform;

			fishInWater.QueueFree();

			// tween the fish to the player
			var tween = GetTree().CreateTween();
			tween.TweenProperty( carryableFish, "position", Player.GlobalPosition + Vector3.Up * 0.5f, 0.5f )
				.SetTrans( Tween.TransitionType.Quad ).SetEase( Tween.EaseType.Out );
			// tween.TweenCallback( Callable.From( carryableFish.QueueFree ) );
			await ToSignal( tween, Tween.SignalName.Finished );
			GiveFish( carryableFish );*/
		}
		else
		{
			/*var trashItemData =
				Loader.LoadResource<ItemData>( ResourceManager.Instance.GetItemPathByName( "item:shoe" ) );
			var trash = trashItemData.DropScene.Instantiate<DroppedItem>();
			// Player.World.AddChild( trash );
			trash.GlobalTransform = fishInWater.GlobalTransform;
			trash.DisableCollisions();

			fishInWater.QueueFree();

			// tween the trash to the player
			var tween = GetTree().CreateTween();
			tween.TweenProperty( trash, "position", Player.GlobalPosition + Vector3.Up * 0.5f, 0.5f )
				.SetTrans( Tween.TransitionType.Quad ).SetEase( Tween.EaseType.Out );
			// tween.TweenCallback( Callable.From( carryableFish.QueueFree ) );
			await ToSignal( tween, Tween.SignalName.Finished );
			GiveTrash( trash );*/
		}

		ReelIn();
	}

	/*private void GiveFish( Fish fish )
	{
		var playerInventory = Player.Inventory;

		if ( playerInventory == null )
		{
			Log.Warning( "Player inventory is null." );
			return;
		}

		var carry = PersistentItem.Create( fish );
		// carry.ItemDataPath = fish.Data.ResourcePath;
		// carry.ItemScenePath = fish.Data.DropScene != null && !string.IsNullOrEmpty( fish.Data.DropScene.ResourcePath ) ? fish.Data.DropScene.ResourcePath : World.DefaultDropScene;
		// carry.PlacementType = World.ItemPlacementType.Dropped;

		playerInventory.PickUpItem( carry );

		fish.QueueFree();
	}*/

	/*private void GiveTrash( DroppedItem trash )
	{
		var playerInventory = Player.Inventory;

		if ( playerInventory == null )
		{
			Log.Warning( "Player inventory is null." );
			return;
		}

		var carry = PersistentItem.Create( trash );
		// carry.ItemDataPath = trash.Data.ResourcePath;
		// carry.ItemScenePath = trash.Data.DropScene != null && !string.IsNullOrEmpty( trash.Data.DropScene.ResourcePath ) ? trash.Data.DropScene.ResourcePath : World.DefaultDropScene;
		// carry.PlacementType = World.ItemPlacementType.Dropped;

		playerInventory.PickUpItem( carry );

		trash.QueueFree();
	}*/

	public void FishGotAway()
	{
		Log.Info( "Fish got away." );
		NextUse = 2f;
		ReelIn();
	}
}
