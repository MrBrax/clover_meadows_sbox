using System.Threading.Tasks;
using Clover.Items;
using Clover.Player;

namespace Clover.Carriable;

public sealed class WateringCan : BaseCarriable
{
	[Property] public ParticleConeEmitter WaterParticles { get; set; }

	[Property] public SoundEvent WateringSound { get; set; }

	private SoundHandle _wateringSoundHandle;

	protected override void OnAwake()
	{
		base.OnAwake();
		if ( WaterParticles != null )
		{
			WaterParticles.Enabled = false;
		}
	}

	public override bool ShouldDisableMovement()
	{
		return WaterParticles?.Enabled ?? false;
	}


	public override void OnUseDown()
	{
		if ( !CanUse() )
		{
			return;
		}

		base.OnUseDown();

		NextUse = UseTime;
		
		if ( !Networking.IsHost )
		{
			Log.Error( "Only the host can use world altering items for now." );
			return;
		}

		var pos = Player.GetAimingGridPosition();

		var worldItems = Player.World.GetItems( pos ).ToList();

		if ( worldItems.Count == 0 )
		{
			_ = PourWaterAsync();
			return;
		}

		var floorItem = worldItems.FirstOrDefault( x => x.GridPlacement == World.ItemPlacement.Floor );

		if ( floorItem != null )
		{
			WaterItem( floorItem );
			return;
		}

		_ = PourWaterAsync();
	}

	private async void WaterItem( WorldNodeLink floorItem )
	{
		Log.Info( "Watering item." );
		// (floorItem.Node as IWaterable)?.OnWater( this );

		if ( !floorItem.Node.Components.TryGet<IWaterable>( out var waterable ) )
		{
			Log.Warning( "Item is not waterable." );
			return;
		}
		
		waterable.OnWater( this );

		Durability--;

		await PourWaterAsync();
		Log.Info( "Item watered." );
	}

	[Broadcast]
	public void StartEmitting()
	{
		if ( WaterParticles.IsValid() )
		{
			WaterParticles.Enabled = true;
		}
	}

	[Broadcast]
	public void StopEmitting()
	{
		if ( WaterParticles.IsValid() )
		{
			WaterParticles.Enabled = false;
		}
	}

	private async Task PourWaterAsync()
	{
		Log.Info( "Wasting water." );

		// GetNode<AnimationPlayer>( "AnimationPlayer" ).Play( "watering" );
		// GetNode<AudioStreamPlayer3D>( "Watering" ).Play();
		StartEmitting();

		Model.LocalRotation = Rotation.FromPitch( -20 );
		
		_wateringSoundHandle = Sound.Play( WateringSound, WorldPosition );
		// await ToSignal( GetTree().CreateTimer( UseTime ), Timer.SignalName.Timeout );
		await Task.DelayRealtimeSeconds( UseTime );

		// GetNode<AudioStreamPlayer3D>( "Watering" ).Stop();
		_wateringSoundHandle?.Stop();
		
		StopEmitting();
		
		Model.LocalRotation = Rotation.Identity;
		
		Log.Info( "Water wasted." );
	}
}
