using System;
using Clover.Components;
using Clover.Data;
using Clover.Interactable;
using Clover.Inventory;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Items;

public class CoffeeMachine : Component, IInteract
{
	[Property] public SoundEvent GrindingSound { get; set; }
	[Property] public SoundEvent BrewingSound { get; set; }
	[Property] public SoundEvent PouringSound { get; set; }
	[Property] public SoundEvent CupSound { get; set; }
	[Property] public SoundEvent FinishSound { get; set; }

	[Property] public GameObject Cup { get; set; }

	[Property] public ItemData ReceivedItem { get; set; }

	[Property] public ParticleEmitter ParticleEmitter { get; set; }

	[Property] public GameObject Model { get; set; }

	private bool _hasCup;
	private bool _isBrewing;
	private bool _isGrinding;

	protected override void OnStart()
	{
		base.OnStart();
		Cup.Enabled = false;
		ParticleEmitter.Enabled = false;
	}

	void IInteract.StartInteract( PlayerCharacter player )
	{
		if ( _isBrewing ) return;
		if ( !_hasCup )
		{
			BrewAsync();
		}
		else
		{
			TakeCup( player );
		}
	}

	private void TakeCup( PlayerCharacter player )
	{
		var carriedPersistentItem = new PersistentItem( "carried_edible:4023053997083351548" );
		carriedPersistentItem.SetArbitraryData( "EdibleData", ReceivedItem.Id );

		var carriedEdible = carriedPersistentItem.SpawnCarriable();

		player.Equips.SetEquippedItem( Equips.EquipSlot.Tool, carriedEdible.GameObject );

		_hasCup = false;
		Cup.Enabled = false;

		SoundEx.Play( CupSound, WorldPosition );
	}

	private async void BrewAsync()
	{
		_isBrewing = true;

		Cup.Enabled = true;
		SoundEx.Play( CupSound, WorldPosition );
		await Task.DelayRealtimeSeconds( 1f );

		SoundEx.Play( GrindingSound, WorldPosition );
		_isGrinding = true;
		await Task.DelayRealtimeSeconds( GrindingSound.Sounds.FirstOrDefault()?.Duration ?? 1f );
		_isGrinding = false;

		SoundEx.Play( BrewingSound, WorldPosition );
		await Task.DelayRealtimeSeconds( 2f );

		SoundEx.Play( PouringSound, WorldPosition );
		ParticleEmitter.Enabled = true;
		await Task.DelayRealtimeSeconds( PouringSound.Sounds.FirstOrDefault()?.Duration ?? 1f );
		ParticleEmitter.Enabled = false;

		SoundEx.Play( FinishSound, WorldPosition );

		_isBrewing = false;
		_hasCup = true;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( _isGrinding )
		{
			Model.LocalScale = new Vector3( 0.5f + (Random.Shared.Float() * 0.1f),
				0.5f + (Random.Shared.Float() * 0.1f), 0.5f + (Random.Shared.Float() * 0.1f) );
		}
		else
		{
			Model.LocalScale = new Vector3( 0.5f, 0.5f, 0.5f );
		}
	}

	void IInteract.FinishInteract( PlayerCharacter player )
	{
	}
}
