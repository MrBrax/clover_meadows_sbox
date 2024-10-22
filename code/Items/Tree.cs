using System;
using System.Threading.Tasks;
using Braxnet;
using Clover.Data;
using Clover.Interactable;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Items;

[Category( "Clover/Items" )]
public class Tree : Component, IPersistent, IInteract
{
	[RequireComponent] public WorldItem WorldItem { get; set; }

	[Property] public List<GameObject> GrowSpawnPoints { get; set; } = new();
	[Property] public List<GameObject> ShakeSpawnPoints { get; set; } = new();

	[Property] public GameObject TreeModel { get; set; }
	[Property] public GameObject StumpModel { get; set; }

	[Property] public FruitData FruitData { get; set; }

	[Property] public SoundEvent ShakeSound { get; set; }
	[Property] public SoundEvent FallSound { get; set; }
	[Property] public SoundEvent FallGroundSound { get; set; }
	[Property] public SoundEvent FruitHitGroundSound { get; set; }

	[Property] public ItemData StumpData { get; set; }

	public DateTime LastFruitDrop { get; set; } = DateTime.UnixEpoch;

	public bool IsDroppingFruit;
	public bool IsFalling;
	public bool IsShaking;

	public const float FruitGrowTime = 10f;

	private bool _hasFruit;


	protected override void OnAwake()
	{
		if ( IsProxy ) return;
		base.OnAwake();
		if ( StumpModel.IsValid() ) StumpModel.Enabled = false;
	}

	private void SpawnFruit()
	{
		if ( _hasFruit ) return;
		if ( !FruitData.IsValid() ) throw new Exception( "FruitData is null" );
		foreach ( var spawnPoint in GrowSpawnPoints )
		{
			var scene = FruitData.InTreeScene;
			if ( scene == null )
			{
				throw new Exception( "FruitData.InTreeScene is null" );
			}

			// var fruit = scene.Instantiate<Node3D>();
			// spawnPoint.AddChild( fruit );
			// Logger.Debug( "Tree", "Added fruit to tree" );
			var fruit = FruitData.InTreeScene.Clone();
			fruit.SetParent( spawnPoint );
			fruit.LocalPosition = Vector3.Zero;
		}

		_hasFruit = true;
	}

	private async void Shake()
	{
		if ( IsShaking ) return;

		IsShaking = true;

		/*var tween = GetTree().CreateTween();
		tween.TweenProperty( Model, "rotation_degrees", new Vector3( 0, 0, 5 ), 0.2f ).SetTrans( Tween.TransitionType.Quad ).SetEase( Tween.EaseType.In );
		tween.TweenProperty( Model, "rotation_degrees", new Vector3( 0, 0, -5 ), 0.2f ).SetTrans( Tween.TransitionType.Quad ).SetEase( Tween.EaseType.InOut );
		tween.TweenProperty( Model, "rotation_degrees", new Vector3( 0, 0, 5 ), 0.2f ).SetTrans( Tween.TransitionType.Quad ).SetEase( Tween.EaseType.InOut );
		tween.TweenProperty( Model, "rotation_degrees", new Vector3( 0, 0, 0 ), 0.2f ).SetTrans( Tween.TransitionType.Quad ).SetEase( Tween.EaseType.Out );
		await ToSignal( tween, Tween.SignalName.Finished );*/

		var tween = TweenManager.CreateTween();
		tween.AddRotation( TreeModel, Rotation.FromRoll( 5 ), 0.2f ).SetEasing( Sandbox.Utility.Easing.QuadraticOut );
		tween.AddRotation( TreeModel, Rotation.FromRoll( -5 ), 0.2f )
			.SetEasing( Sandbox.Utility.Easing.QuadraticInOut );
		tween.AddRotation( TreeModel, Rotation.FromRoll( 5 ), 0.2f ).SetEasing( Sandbox.Utility.Easing.QuadraticInOut );
		tween.AddRotation( TreeModel, Rotation.FromRoll( 0 ), 0.2f ).SetEasing( Sandbox.Utility.Easing.QuadraticOut );
		await tween.Wait();

		await DropFruitAsync();

		IsShaking = false;
	}

	public async Task DropFruitAsync()
	{
		if ( IsDroppingFruit ) return;
		IsDroppingFruit = true;
		for ( var i = 0; i < GrowSpawnPoints.Count; i++ )
		{
			var growPoint = GrowSpawnPoints[i];
			var shakePoint = ShakeSpawnPoints[i];

			/* foreach ( var child in growPoint.GetChildren() )
			{ */

			// Log.Info( $"Checking growPoint: {growPoint.Name}" );

			var growNodeRaw = growPoint.Children.FirstOrDefault();
			var shakeNodeRaw = shakePoint;

			var endPos = shakeNodeRaw.WorldPosition + Vector3.Up * 8f;

			// if ( child is not Node3D growNode ) continue;
			/*var tween = GetTree().CreateTween();
			var p = tween.TweenProperty( growNode, "global_position", endPos, 0.7f + GD.Randf() * 0.5f );
			p.SetTrans( Tween.TransitionType.Bounce );
			p.SetEase( Tween.EaseType.Out );
			p.SetDelay( GD.Randf() * 0.5f );

			tween.TweenCallback( Callable.From( () =>
			{
				growNode.QueueFree();
				var pos = World.WorldToItemGrid( shakePoint.GlobalTransform.Origin );
				World.SpawnNode( FruitData, pos, World.ItemRotation.North, World.ItemPlacement.Floor, true );

				GetNode<AudioStreamPlayer3D>( "Drop" ).Play();
			} ) );*/

			var tween = TweenManager.CreateTween();
			var p = tween.AddPosition( growNodeRaw, endPos, 0.7f + Random.Shared.Float() * 0.5f );
			p.SetEasing( Sandbox.Utility.Easing.BounceOut );
			p.SetDelay( Random.Shared.Float() * 0.5f );

			p.OnFinish += () =>
			{
				Sound.Play( FruitHitGroundSound, shakePoint.WorldPosition );
			};

			p.OnBounce += () =>
			{
				Log.Info( "Bounce" );
				Sound.Play( FruitHitGroundSound, shakePoint.WorldPosition );
			};

			/* await ToSignal( tween, Tween.SignalName.Finished );

			growNode.QueueFree();
			var pos = World.WorldToItemGrid( shakePoint.GlobalTransform.Origin );
			World.SpawnNode( FruitData, pos, World.ItemRotation.North, World.ItemPlacement.Floor );

			GetNode<AudioStreamPlayer3D>( "Drop" ).Play(); */

			/*tween.Wait().ContinueWith( _ =>
			{
				Log.Info( "Tween finished" );

				growNode?.Destroy();

				// TODO: check free space
				var pos = WorldItem.WorldLayerObject.World.WorldToItemGrid( shakePoint.WorldPosition );
				WorldItem.WorldLayerObject.World.SpawnDroppedNode( FruitData, pos, World.ItemRotation.North,
					World.ItemPlacement.Floor );

				Sound.Play( FruitHitGroundSound, shakePoint.WorldPosition );

			} );*/
		}

		await Task.DelayRealtimeSeconds( 1.5f );

		foreach ( var growPoint in GrowSpawnPoints )
		{
			foreach ( var child in growPoint.Children )
			{
				child.Destroy();
			}
		}

		foreach ( var shakePoint in ShakeSpawnPoints )
		{
			var pos = WorldItem.WorldLayerObject.World.WorldToItemGrid( shakePoint.WorldPosition );
			WorldItem.WorldLayerObject.World.SpawnDroppedNode( FruitData, pos, World.ItemRotation.North );
		}

		// await ToSignal( GetTree().CreateTimer( 1.5f ), Timer.SignalName.Timeout );
		// await Task.DelayRealtimeSeconds( 1.5f );
		IsDroppingFruit = false;
		LastFruitDrop = DateTime.Now;
		_hasFruit = false;
	}

	private void CheckGrowth()
	{
		if ( IsFalling ) return;
		if ( GrowSpawnPoints == null || GrowSpawnPoints.Count == 0 ) return;
		if ( ShakeSpawnPoints == null || ShakeSpawnPoints.Count == 0 ) return;

		if ( !_hasFruit && DateTime.Now - LastFruitDrop > TimeSpan.FromSeconds( FruitGrowTime ) )
		{
			SpawnFruit();
			// LastFruitDrop = DateTime.Now;
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;
		if ( FruitData == null ) return;
		CheckGrowth();
	}


	public void OnSave( PersistentItem item )
	{
		item.SetArbitraryData( "LastFruitDrop", LastFruitDrop );
	}

	public void OnLoad( PersistentItem item )
	{
		LastFruitDrop = item.GetArbitraryData<DateTime>( "LastFruitDrop" );
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		foreach ( var growPoint in GrowSpawnPoints )
		{
			if ( !growPoint.IsValid() ) continue;
			Gizmo.Draw.LineSphere( growPoint.LocalPosition, 8f );
		}

		foreach ( var shakePoint in ShakeSpawnPoints )
		{
			if ( !shakePoint.IsValid() ) continue;
			Gizmo.Draw.LineSphere( shakePoint.LocalPosition, 8f );
		}
	}

	public void StartInteract( PlayerCharacter player )
	{
		if ( FruitData == null ) return;
		Shake();
	}

	public void FinishInteract( PlayerCharacter player )
	{
	}

	public string GetInteractName()
	{
		return "Shake";
	}
}
