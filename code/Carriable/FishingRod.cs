using System;
using Clover.Animals;
using Clover.Objects;

namespace Clover.Carriable;

public class FishingRod : BaseCarriable
{
	[Property] public GameObject BobberPrefab { get; set; }
	// [Property] public GameObject 

	[Property] public GameObject LineStartPoint { get; set; }

	private FishingBobber _bobber;

	private bool _hasCasted = false;
	private bool _isCasting = false;
	private bool _isBusy = false;

	private float _trashChance = 0.1f; // TODO: base this on luck?

	public override void OnUseDown()
	{
		if ( !CanUse() ) return;
		NextUse = 1f;

		if ( _isCasting ) return;

		if ( _hasCasted && !_bobber.IsValid() )
		{
			Log.Warning( "Bobber is not valid." );
			_hasCasted = false;
		}
		
		/*if ( _hasCasted )
		{
			if ( _bobber.Fish.IsValid() && _bobber.Fish.State == CatchableFish.FishState.FoundBobber )
			{
				_bobber.Fish.TryHook();
			}
			else if ( _bobber.Fish.IsValid() && _bobber.Fish.State == CatchableFish.FishState.Fighting )
			{
				_bobber.Fish.Pull();
				// GetNode<AudioStreamPlayer3D>( "Reel" ).Play();
				// GetNode<AudioStreamPlayer3D>( "Reel" ).PitchScale = 0.8f + GD.Randf() * 0.4f;
			}
			else
			{
				ReelIn();
			}
		}
		else
		{
			Cast();
		}*/
	}
	
	public override void _Process( double delta )
	{

		/*if ( IsInstanceValid( Bobber ) )
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
		
		if ( _bobber.IsValid() )
		{
			if ( _bobber.Fish.IsValid() && _bobber.Fish.State == CatchableFish.FishState.Fighting )
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
		return Player.WorldPosition + Player.Model.WorldRotation.Forward * 64f;
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
			Log.Warning( $"No water found at {GetCastPosition()}." );
			return;
		}

		_isCasting = true;

		// play the cast animation
		GetNode<AnimationPlayer>( "AnimationPlayer" ).Play( "cast" );

		// wait for the animation to finish
		await ToSignal( GetNode<AnimationPlayer>( "AnimationPlayer" ), AnimationPlayer.SignalName.AnimationFinished );

		if ( !IsInstanceValid( Bobber ) )
		{
			/* var waterPosition = GetWaterSurface( GetCastPosition() );
			Bobber = BobberScene.Instantiate<FishingBobber>();
			Bobber.Rod = this;
			Player.World.AddChild( Bobber );
			Bobber.GlobalPosition = waterPosition; */

			var waterPosition = GetWaterSurface( GetCastPosition() );

			Bobber = BobberScene.Instantiate<FishingBobber>();
			Bobber.Rod = this;
			Player.World.AddChild( Bobber );

			Bobber.GlobalPosition = LinePoint.GlobalPosition;

			// tween the bobber to the water in an arc
			var tween = GetTree().CreateTween();
			tween.TweenMethod( Callable.From<float>( ( float i ) =>
			{
				Bobber.GlobalPosition = LinePoint.GlobalPosition.CubicInterpolate( waterPosition, LinePoint.GlobalPosition + Vector3.Down * 1f, waterPosition + Vector3.Down * 10f, i );
			} ), 0f, 1f, 0.5f ).SetEase( Tween.EaseType.Out );

			await ToSignal( tween, Tween.SignalName.Finished );

			Bobber.OnHitWater();

			var splash = SplashScene.Instantiate<Node3D>();
			Player.World.AddChild( splash );
			splash.GlobalPosition = waterPosition;
		}

		// CreateLine();

		_isCasting = false;

		_hasCasted = true;

	}
	
	private const float WaterCheckHeight = 3f;

	private bool CheckForWater( Vector3 position )
	{

		var spaceState = GetWorld3D().DirectSpaceState;

		var traceWater =
			new Trace( spaceState ).CastRay(
				PhysicsRayQueryParameters3D.Create( position, position + (Vector3.Down * WaterCheckHeight), World.WaterLayer ) );

		if ( traceWater == null )
		{
			Log.Warning( $"No water found at {position}." );
			return false;
		}

		var traceTerrain =
			new Trace( spaceState ).CastRay(
				PhysicsRayQueryParameters3D.Create( traceWater.Position, traceWater.Position + (Vector3.Down * 1f), World.TerrainLayer ) );

		if ( traceTerrain != null )
		{
			Log.Warning( $"Terrain found at {position}." );
			return false;
		}

		/* var traceTerrain =
			new Trace( spaceState ).CastRay(
				PhysicsRayQueryParameters3D.Create( position, position + Vector3.Down * (WaterCheckHeight * 2), World.TerrainLayer ) );

		if ( traceTerrain != null )
		{
			Log.Warning( $"Terrain found at {position}." );
			return false;
		}

		var traceWater =
			new Trace( spaceState ).CastRay(
				PhysicsRayQueryParameters3D.Create( position, position + Vector3.Down * (WaterCheckHeight * 2), World.WaterLayer ) );

		if ( traceWater == null )
		{
			Log.Warning( $"No water found at {position}." );
			return false;
		} */

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
		var spaceState = GetWorld3D().DirectSpaceState;

		var traceWater =
			new Trace( spaceState ).CastRay(
				PhysicsRayQueryParameters3D.Create( position + Vector3.Up * WaterCheckHeight, position + Vector3.Down * (WaterCheckHeight * 2), World.WaterLayer ) );

		if ( traceWater == null )
		{
			Log.Warning( $"No water found at {position}." );
			return Vector3.Zero;
		}

		return traceWater.Position;
	}

	private void ReelIn()
	{
		Log.Info( "Reeling in." );
		if ( IsInstanceValid( Bobber ) )
		{
			Log.Info( "Freeing bobber." );
			Bobber.QueueFree();
			Bobber = null;
		}

		_hasCasted = false;

		// ((ImmediateMesh)LineMesh.Mesh).ClearSurfaces();
		ClearLine();

		GetNode<AnimationPlayer>( "AnimationPlayer" ).Play( "RESET" );
	}

	public async void CatchFish( CatchableFish fishInWater )
	{
		if ( !IsInstanceValid( fishInWater ) )
		{
			Log.Warning( "Fish is not valid." );
			return;
		}

		Log.Info( "Caught fish." );
		_timeUntilUse = 3f;
		GetNode<AudioStreamPlayer3D>( "Splash" ).Play();
		GetNode<AnimationPlayer>( "AnimationPlayer" ).Play( "catch" );

		var isTrash = GD.Randf() < _trashChance;

		if ( !isTrash )
		{

			var carryableFish = fishInWater.Data.CarryScene.Instantiate<Fish>();
			Player.World.AddChild( carryableFish );
			carryableFish.GlobalTransform = fishInWater.GlobalTransform;

			fishInWater.QueueFree();

			// tween the fish to the player
			var tween = GetTree().CreateTween();
			tween.TweenProperty( carryableFish, "position", Player.GlobalPosition + Vector3.Up * 0.5f, 0.5f ).SetTrans( Tween.TransitionType.Quad ).SetEase( Tween.EaseType.Out );
			// tween.TweenCallback( Callable.From( carryableFish.QueueFree ) );
			await ToSignal( tween, Tween.SignalName.Finished );
			GiveFish( carryableFish );

		}
		else
		{

			var trashItemData = Loader.LoadResource<ItemData>( ResourceManager.Instance.GetItemPathByName( "item:shoe" ) );
			var trash = trashItemData.DropScene.Instantiate<DroppedItem>();
			Player.World.AddChild( trash );
			trash.GlobalTransform = fishInWater.GlobalTransform;
			trash.DisableCollisions();

			fishInWater.QueueFree();

			// tween the trash to the player
			var tween = GetTree().CreateTween();
			tween.TweenProperty( trash, "position", Player.GlobalPosition + Vector3.Up * 0.5f, 0.5f ).SetTrans( Tween.TransitionType.Quad ).SetEase( Tween.EaseType.Out );
			// tween.TweenCallback( Callable.From( carryableFish.QueueFree ) );
			await ToSignal( tween, Tween.SignalName.Finished );
			GiveTrash( trash );

		}

		ReelIn();
	}

	private void GiveFish( Fish fish )
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

	}

	private void GiveTrash( DroppedItem trash )
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

	}

	public void FishGotAway()
	{
		Log.Info( "Fish got away." );
		_timeUntilUse = 2f;
		ReelIn();
	}
}
