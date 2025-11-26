using System;
using Braxnet;
using Clover.Items;
using Clover.Ui;

namespace Clover.Carriable;

[Category( "Clover/Carriable" )]
public class Axe : BaseCarriable
{
	[Property] public SoundEvent SwingSound { get; set; }

	[Property] public SoundEvent HitTreeSound { get; set; }

	public override void OnUseDown()
	{
		base.OnUseDown();

		if ( !CanUse() ) return;

		NextUse = 1f;

		if ( !Networking.IsHost )
		{
			Log.Error( "Only the host can use world altering items for now." );
			return;
		}

		var pos = Player.GetAimingGridPosition();

		List<WorldItem> worldItems;

		try
		{
			worldItems = Player.World.GetItems( pos ).ToList();
		}
		catch ( Exception e )
		{
			Player.Notify( Notifications.NotificationType.Error, "Error getting items to axe" );
			return;
		}

		if ( worldItems.Count == 0 )
		{
			Log.Info( "No items to axe" );
			Player.PlayerController.Model.Set( "attack", true );
			Task.DelayRealtimeSeconds( 0.3f ).ContinueWith( _ => GameObject?.PlaySound( SwingSound ) );
			return;
		}

		foreach ( var floorItem in worldItems )
		{
			if ( floorItem.Components.TryGet<Tree>( out var tree ) )
			{
				if ( tree.IsFalling || tree.IsDroppingFruit )
				{
					Log.Info( "Tree is falling or dropping fruit." );
					GameObject.PlaySound( SwingSound );
					return;
				}

				ChopTree( pos, floorItem, tree );
				return;
			}
			else
			{
				HitItem( pos, floorItem );
				return;
			}
		}

		Log.Info( "No floor item to interact with." );
	}

	private void HitItem( Vector2Int pos, WorldItem floorItem )
	{
		Log.Info( "Hitting item." );
	}

	private async void ChopTree( Vector2Int pos, WorldItem nodeLink, Items.Tree tree )
	{
		// Logger.Info( "Chopping tree." );
		// GetNode<AudioStreamPlayer3D>( "TreeHit" ).Play();
		GameObject.PlaySound( HitTreeSound );
		Log.Info( "Chopping tree" );

		await tree.DropFruitAsync();

		Log.Info( "Dropped fruit" );

		tree.StumpModel.Enabled = true;

		var model = tree.TreeModel;

		var tween = TweenManager.CreateTween();
		var treePositionTween = tween.AddRotation( model, Rotation.FromRoll( 90f ), 1f );
		treePositionTween.SetEasing( Sandbox.Utility.Easing.ExpoIn );

		// var treeSizeTween = tween.Parallel().TweenProperty( model, "scale", Vector3.Zero, 0.1f ).SetDelay( 0.9f );
		// var treeOpacityTween = tween.Parallel().TweenProperty( model, "modulate:a", 0, 2f );

		Sound.Play( tree.FallSound );

		await Task.DelayRealtimeSeconds( 1 );

		Sound.Play( tree.FallGroundSound );

		/*var particle = Loader.LoadResource<PackedScene>( "res://particles/poof.tscn" ).Instantiate<Node3D>();
		GetTree().Root.AddChild( particle );
		particle.GlobalPosition = tree.GlobalPosition + Vector3.Left * 1f;*/
		ParticleManager.PoofAt( tree.WorldPosition + Vector3.Right * 64f, 4 );

		// model.Hide();
		model.Enabled = false;

		// await ToSignal( GetTree().CreateTimer( 1f ), Timer.SignalName.Timeout );
		await Task.DelayRealtimeSeconds( 1 );

		nodeLink.RemoveFromWorld();

		// var stump = Loader.LoadResource<ItemData>( ResourceManager.Instance.GetItemPathByName( "item:tree_stump" ) );
		// var stumpNode = World.SpawnNode( stump, pos, World.ItemRotation.North, World.ItemPlacement.Floor );

		var stumpNode = tree.WorldItem.WorldLayerObject.World.SpawnPlacedItem( tree.StumpData, pos,
			World.ItemRotation.North );
	}
}
