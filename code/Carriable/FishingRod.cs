using System;
using Braxnet;
using Clover.Animals;
using Clover.Data;
using Clover.Objects;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Carriable;

public class FishingRod : BaseCarriable
{
	public enum RodState
	{
		Idle,
		Casting,
		Reeling,
		Hooking,
		Fighting
	}

	[Property] public GameObject BobberPrefab { get; set; }
	// [Property] public GameObject 

	[Property] public GameObject LineStartPoint { get; set; }

	[Property] public SoundEvent CastSound { get; set; }
	[Property] public SoundEvent ReelSound { get; set; }
	[Property] public SoundEvent HookSound { get; set; }
	[Property] public SoundEvent SplashSound { get; set; }

	[Property] public GameObject SplashParticle { get; set; }

	[Property] public LineRenderer LineRenderer { get; set; }

	public float LineLength = 500f;


	public bool HasCasted = false;
	private bool _isWindup = false;
	private bool _isCasting = false;
	private bool _isBusy = false;
	private TimeSince _timeSinceWindup;
	private float _castDistance = 0f;

	private float _trashChance = 0.1f; // TODO: base this on luck?

	private SoundHandle _reelSound;

	private const float MinCastDistance = 32f;
	private const float MaxCastDistance = 192f;
	private const float WaterCheckHeight = 64f;
	
	private float LineSlackDistance => Sandbox.Utility.Noise.Perlin( Time.Now * 10f ) * 16f;

	private GameObject _lineSlackDummy;

	/*public class CurrentFishData
	{
		public FishData Data;
		public float Weight;
		public float Stamina;
		public float StaminaMax;
	}*/

	private FishingBobber Bobber;
	public CatchableFish CurrentFish;

	public float Stamina = 100f;
	public float LineStrength = 100f;

	protected override void OnStart()
	{
		base.OnStart();

		_reelSound = GameObject.PlaySound( ReelSound );
		_reelSound.Volume = 0f;
	}


	public override void OnUseDown()
	{
		if ( !CanUse() )
		{
			Log.Warning( "Cannot use." );
			return;
		}
		
		if ( _isCasting )
		{
			Log.Warning( "Already casting." );
			return;
		}

		if ( !HasCasted )
		{
			_isWindup = true;
			_timeSinceWindup = 0f;
			NextUse = 0f;
			return;
		}

		NextUse = 1f;
		
		if ( HasCasted && !Bobber.IsValid() )
		{
			Log.Warning( "Bobber is not valid." );
			HasCasted = false;
		}

		if ( HasCasted )
		{
			if ( Stamina <= 5 ) return;

			if ( Bobber.Fish.IsValid() )
			{
				Bobber.Fish.OnLinePull();
			}

			ReelIn( 32f );
		}
		
	}

	public override void OnUseUp()
	{
		base.OnUseUp();
		
		if ( _isWindup )
		{
			_isWindup = false;
			_isCasting = true;
			NextUse = 0.5f;
			Cast();
		}
		
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		_reelSound?.Stop();
		DestroyBobber();
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

		/*if ( Bobber.IsValid() )
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
		}*/

		if ( IsProxy ) return;
		
		if ( _lineSlackDummy.IsValid() )
		{
			_lineSlackDummy.WorldPosition = LineStartPoint.WorldPosition.LerpTo( Bobber.WorldPosition, 0.5f ) + Vector3.Down * LineSlackDistance;
		}
		
		if ( _isWindup )
		{
			var castDistance = Math.Clamp( 32f + ( _timeSinceWindup * 90f ), MinCastDistance, MaxCastDistance );
			// Log.Info( $"Windup: {castDistance}, {_timeSinceWindup}" );
			Model.LocalRotation = Rotation.FromPitch( (castDistance / MaxCastDistance) * 90f );
			return;
		}

		if ( HasCasted && CanUse() )
		{
			if ( Input.AnalogMove.x < 0 )
			{
				ReelIn( 8f, Vector3.Backward );
				NextUse = 0.3f;
			}
			else if ( Input.AnalogMove.y > 0 )
			{
				ReelIn( 4f, Vector3.Left );
				NextUse = 0.3f;
			}
			else if ( Input.AnalogMove.y < 0 )
			{
				ReelIn( 4f, Vector3.Right );
				NextUse = 0.3f;
			}
		}

		_reelSound.Volume = _reelSound.Volume.LerpTo( 0f, Sandbox.Utility.Easing.QuadraticIn( Time.Delta * 15f ) );
		// _reelSound.Pitch = _reelSound.Pitch.LerpTo( 1f, Time.Delta * 3f );

		if ( Stamina < 100f )
		{
			// Stamina = Stamina.LerpTo( 100f, Time.Delta * 5f );
			Stamina += Time.Delta * 7.5f;
			
			if ( Bobber.IsValid() && Bobber.Fish.IsValid() )
			{
				if ( Bobber.Fish.Stamina <= 0 )
				{
					Stamina += Time.Delta * 17f;
				}
			}
		}

		if ( LineStrength <= 0 )
		{
			ResetAll();
		}
	}

	private Vector3 GetCastPosition()
	{
		return Player.WorldPosition + Player.Model.WorldRotation.Forward * _castDistance;
	}

	public override bool ShouldDisableMovement()
	{
		return HasCasted || _isCasting;
	}

	private async void Cast()
	{
		Log.Info( "Casting." );
		if ( Player == null ) throw new Exception( "Player is null." );
		
		_castDistance = Math.Clamp( 32f + ( _timeSinceWindup * 90f ), MinCastDistance, MaxCastDistance );

		if ( !CheckForWater( GetCastPosition() ) )
		{
			Log.Warning( $"CAST: No water found at {GetCastPosition()}." );
			ResetAll();
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
		tween1.AddLocalRotation( Model, Rotation.FromPitch( 90f ), 0.5f )
			.SetEasing( Sandbox.Utility.Easing.QuadraticOut );
		tween1.AddLocalRotation( Model, Rotation.FromPitch( 0f ), 0.3f )
			.SetEasing( Sandbox.Utility.Easing.QuadraticIn );
		await tween1.Wait();

		GameObject.PlaySound( CastSound );

		var tween2 = TweenManager.CreateTween();
		tween2.AddLocalRotation( Model, Rotation.FromPitch( -20f ), 0.3f )
			.SetEasing( Sandbox.Utility.Easing.QuadraticOut );
		await tween2.Wait();

		var tween3 = TweenManager.CreateTween();
		tween3.AddLocalRotation( Model, Rotation.FromPitch( 0f ), 0.3f )
			.SetEasing( Sandbox.Utility.Easing.QuadraticIn );

		// Model.LocalRotation = Rotation.FromPitch( 0f );

		if ( !Bobber.IsValid() )
		{
			var waterPosition = GetWaterSurface( GetCastPosition() );

			Bobber = BobberPrefab.Clone().Components.Get<FishingBobber>();
			Bobber.Rod = this;

			LineRenderer?.Points.Add( Bobber.Tip );

			Bobber.WorldPosition = LineStartPoint.WorldPosition;
			Bobber.WorldRotation = Rotation.FromYaw( Player.Model.WorldRotation.Yaw() );

			CameraMan.Instance.Targets.Add( Bobber.GameObject );

			// place slack dummy between the line start and the bobber
			_lineSlackDummy = Scene.CreateObject();
			_lineSlackDummy.WorldPosition = LineStartPoint.WorldPosition.LerpTo( Bobber.WorldPosition, 0.5f ) + Vector3.Down * LineSlackDistance;
			
			LineRenderer?.Points.Insert( 1, _lineSlackDummy );

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
			SplashParticle.Clone( waterPosition, Rotation.FromPitch( -90f ) );
		}
		else
		{
			Log.Error( "Bobber is already valid." );
			ResetAll();
			return;
		}

		// CreateLine();

		_isCasting = false;

		HasCasted = true;

		Stamina = 100f;
	}


	public static bool CheckForWater( Vector3 position )
	{
		var traceWater = Game.ActiveScene.Trace.Ray( position + Vector3.Up * 32f, position + Vector3.Down * WaterCheckHeight )
			.WithTag( "water" )
			.Run();

		if ( !traceWater.Hit )
		{
			Log.Warning( $"No water found at {position}." );
			return false;
		}

		var traceTerrain = Game.ActiveScene.Trace
			.Ray( traceWater.HitPosition + Vector3.Up * 32f, traceWater.HitPosition + Vector3.Down * 16f )
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
		var traceWater = Scene.Trace.Ray( position + Vector3.Up * WaterCheckHeight,
				position + Vector3.Down * (WaterCheckHeight * 2) )
			.WithTag( "water" )
			.Run();

		if ( !traceWater.Hit )
		{
			Log.Warning( $"No water found at {position}." );
			return Vector3.Zero;
		}

		return traceWater.HitPosition;
	}

	private void DestroyBobber()
	{
		if ( Bobber.IsValid() ) LineRenderer?.Points.Remove( Bobber.Tip );
		if ( _lineSlackDummy.IsValid() ) LineRenderer?.Points.Remove( _lineSlackDummy );
		Bobber?.DestroyGameObject();
		Bobber = null;
		
		_lineSlackDummy?.Destroy();
		_lineSlackDummy = null;
	}

	private void ResetAll()
	{
		HasCasted = false;
		_isCasting = false;
		_isBusy = false;
		Stamina = 100f;
		LineStrength = 100f;
		Model.LocalRotation = Rotation.FromPitch( 0f );
		DestroyBobber();
	}

	private void ReelIn( float amount, Vector3 direction = default )
	{
		var forward = Player.Model.WorldRotation.Forward;

		var reelDirection = (Player.WorldPosition - Bobber.WorldPosition).Normal;

		if ( direction != Vector3.Zero )
		{
			if ( direction == Vector3.Right )
			{
				reelDirection += Player.Model.WorldRotation.Right;
			}
			else if ( direction == Vector3.Left )
			{
				reelDirection += Player.Model.WorldRotation.Left;
			}
			else if ( direction == Vector3.Backward )
			{
				reelDirection += Player.Model.WorldRotation.Backward;
			}
		}

		var reelPosition = Bobber.WorldPosition + reelDirection * amount;

		var dist = reelPosition.Distance( WorldPosition );

		if ( !CheckForWater( reelPosition ) || dist < 32f )
		{
			// Log.Info( "Reel position too close or not in water. Reeling in." );
			// ResetAll();

			if ( dist < 32 && Bobber.Fish.IsValid() && Bobber.Fish.State == CatchableFish.FishState.Fighting )
			{
				Log.Info( "Reeling in, fish is fighting." );
				CatchFish( Bobber.Fish );
				return;
			}
			
			Log.Info( "Reeling in, too close or not in water." );
			
			ResetAll();

			return;
		}

		var waterSurface = GetWaterSurface( reelPosition );

		reelPosition.z = waterSurface.z;

		var pitch = Math.Clamp( (dist * -0.5f) + 90f, -20f, 100f );
		// Log.Info( $"Reeling in. Distance: {dist}, Pitch: {pitch} ({(dist * -0.5f) + 90f})" );
		Model.LocalRotation = Rotation.FromPitch( pitch );

		var tween = TweenManager.CreateTween();
		tween.AddPosition( Bobber.GameObject, reelPosition, 0.4f ).SetEasing( Sandbox.Utility.Easing.QuadraticOut );

		_reelSound.Volume = 1f;
		// _reelSound.Pitch = 2f;

		NextUse = 0.5f;

		Stamina -= amount * 0.5f;
		LineStrength -= 2f;

		if ( Bobber.Fish.IsValid() )
		{
			Bobber.Fish.Stamina -= amount * 0.01f;
		}
	}

	public void Fight()
	{
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
		GameObject.PlaySound( SplashSound );
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

			var data = fishInWater.Data;

			var model = data.ModelScene.Clone();

			model.WorldPosition = fishInWater.WorldPosition;
			model.WorldRotation = fishInWater.WorldRotation;

			fishInWater.DestroyGameObject();

			var tween = TweenManager.CreateTween();
			tween.AddPosition( model, Player.WorldPosition + Vector3.Up * 16f, 0.5f )
				.SetEasing( Sandbox.Utility.Easing.QuadraticOut );
			await tween.Wait();

			model.Destroy();

			var item = PersistentItem.Create( data );

			Player.Inventory.PickUpItem( item );
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

		// ReelIn( 128f );
		ResetAll();
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
		NextUse = 1f;
		ReelIn( 128f );
	}

	/*protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( Bobber.IsValid() )
		{
			Gizmo.Draw.Line( LineStartPoint.WorldPosition, Bobber.WorldPosition );
		}
	}*/
}
