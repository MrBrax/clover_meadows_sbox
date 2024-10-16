using System;
using Clover.Components;
using Clover.Data;
using Clover.Interactable;
using Clover.Inventory;
using Clover.Persistence;
using Clover.Player;
using Clover.Ui;

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

	[Property] public ParticleEmitter SteamParticleEmitter { get; set; }
	[Property] public ParticleEmitter LiquidParticleEmitter { get; set; }

	[Property] public GameObject Model { get; set; }

	[Sync] private bool _hasCup { get; set; }
	[Sync] private bool _isBrewing { get; set; }
	private bool _isGrinding;

	protected override void OnStart()
	{
		base.OnStart();
		Cup.Enabled = false;
		SteamParticleEmitter.Enabled = false;
		LiquidParticleEmitter.Enabled = false;
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
		if ( player.Equips.HasEquippedItem( Equips.EquipSlot.Tool ) )
		{
			// Log.Error( "Player already has an item equipped" );
			player.Notify( Notifications.NotificationType.Error, "You already have an item equipped" );
			return;
		}

		var carriedPersistentItem = new PersistentItem( "carried_edible:4023053997083351548" );
		carriedPersistentItem.SetArbitraryData( "EdibleData", ReceivedItem.Id );

		var carriedEdible = carriedPersistentItem.SpawnCarriable();

		player.Equips.SetEquippedItem( Equips.EquipSlot.Tool, carriedEdible.GameObject );

		TakeCupRpc();
		SetCupEnabled( false );

		SoundEx.Play( CupSound, WorldPosition );
	}

	[Authority]
	private void TakeCupRpc()
	{
		_hasCup = false;
	}

	[Broadcast]
	private void SetCupEnabled( bool enabled )
	{
		Cup.Enabled = enabled;
	}

	[Authority]
	private async void BrewAsync()
	{
		_isBrewing = true;

		SetCupEnabled( true );

		SoundEx.Play( CupSound, WorldPosition );

		await Task.DelayRealtimeSeconds( 0.5f );

		SoundEx.Play( FinishSound, WorldPosition );

		await Task.DelayRealtimeSeconds( 0.5f );

		SoundEx.Play( GrindingSound, WorldPosition );
		_isGrinding = true;
		await Task.DelayRealtimeSeconds( 6f );
		_isGrinding = false;

		SoundEx.Play( BrewingSound, WorldPosition );
		await Task.DelayRealtimeSeconds( 1f );

		SoundEx.Play( PouringSound, WorldPosition );
		SteamParticleEmitter.Enabled = true;
		LiquidParticleEmitter.Enabled = true;
		await Task.DelayRealtimeSeconds( 4f );
		LiquidParticleEmitter.Enabled = false;
		SteamParticleEmitter.Enabled = false;

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

	public string GetInteractName()
	{
		return !_isBrewing ? (_hasCup ? "Take cup" : "Brew coffee") : "Unavailable";
	}
}
