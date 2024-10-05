using Braxnet;
using Clover.Items;

namespace Clover.Carriable;

public class Axe : BaseCarriable
{
	
	[Property] public SoundEvent SwingSound { get; set; }
	
	[Property] public SoundEvent HitTreeSound { get; set; }
	
	public override void OnUseDown()
	{
		base.OnUseDown();

		if ( !CanUse() ) return;

		NextUse = 1f;
		
		/*Log.Info( "Axe used down" );
		
		if ( SwingSound.IsValid() )
		{
			GameObject.PlaySound( SwingSound );
		}*/
		
		var pos = Player.GetAimingGridPosition();

		var worldItems = Player.World.GetItems( pos ).ToList();

		if ( worldItems.Count == 0 )
		{
			Log.Info( "No items to axe" );
			GameObject.PlaySound( SwingSound );
			return;
		}

		var floorItem = worldItems.FirstOrDefault( x => x.GridPlacement == World.ItemPlacement.Floor );
		if ( floorItem != null )
		{
			/*if ( floorItem.Node is Items.Tree tree )
			{
				if ( tree.IsFalling || tree.IsDroppingFruit )
				{
					Logger.Info( "Axe", "Tree is falling or dropping fruit." );
					return;
				}
				ChopTree( pos, floorItem, tree );
				return;
			}
			else
			{
				HitItem( pos, floorItem );
				return;
			}*/
			
			if ( floorItem.Node.Components.TryGet<Tree>( out var tree ) )
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
	
	private void HitItem( Vector2Int pos, WorldNodeLink floorItem )
	{
		Log.Info( "Hitting item." );
	}

	private async void ChopTree( Vector2Int pos, WorldNodeLink nodeLink, Items.Tree tree )
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
		treePositionTween.SetEasing( Sandbox.Utility.Easing.ExpoOut );

		// var treeSizeTween = tween.Parallel().TweenProperty( model, "scale", Vector3.Zero, 0.1f ).SetDelay( 0.9f );
		// var treeOpacityTween = tween.Parallel().TweenProperty( model, "modulate:a", 0, 2f );
		
		Sound.Play( tree.FallSound );
		
		await Task.DelayRealtimeSeconds( 1 );
		
		Sound.Play( tree.FallGroundSound );

		/*var particle = Loader.LoadResource<PackedScene>( "res://particles/poof.tscn" ).Instantiate<Node3D>();
		GetTree().Root.AddChild( particle );
		particle.GlobalPosition = tree.GlobalPosition + Vector3.Left * 1f;*/

		// model.Hide();
		model.Enabled = false;

		// await ToSignal( GetTree().CreateTimer( 1f ), Timer.SignalName.Timeout );
		await Task.DelayRealtimeSeconds( 1 );

		nodeLink.Remove();

		// var stump = Loader.LoadResource<ItemData>( ResourceManager.Instance.GetItemPathByName( "item:tree_stump" ) );
		// var stumpNode = World.SpawnNode( stump, pos, World.ItemRotation.North, World.ItemPlacement.Floor );
		
		var stumpNode = tree.WorldItem.WorldLayerObject.World.SpawnPlacedNode( tree.StumpData, pos, World.ItemRotation.North, World.ItemPlacement.Floor );
		
	}
	
}
