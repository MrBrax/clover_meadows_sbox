using System;
using Clover.Interactable;
using Clover.Player;
using Clover.Ui;

namespace Clover.Components;

public class HideAndSeek : Component, IInteract
{
	public static HideAndSeek Leader =>
		Game.ActiveScene?.GetAllComponents<HideAndSeek>()
			.FirstOrDefault( x => x.Network.Owner != null && x.Network.Owner.IsHost );

	[Sync, HostSync] public bool IsSeeker { get; set; }

	[Sync, Property] public TimeSince RoundStart { get; set; }

	[Sync, Property] public bool IsRoundActive { get; set; }

	[Property] public SoundEvent RoundStartSound { get; set; }
	[Property] public SoundEvent RoundEndSound { get; set; }
	[Property] public SoundEvent CatchSound { get; set; }
	[Property] public SoundEvent BlindCountdownSound { get; set; }
	[Property] public SoundEvent BlindEndSound { get; set; }

	private bool IsLeader => Networking.IsHost;

	private const int RoundDuration = 120;
	private const int BlindDuration = 20;

	public bool IsBlind => IsSeeker && Leader.RoundStart < BlindDuration;

	public float BlindSecondsLeft => BlindDuration - Leader.RoundStart;
	public float RoundSecondsLeft => RoundDuration - Leader.RoundStart;


	[ConCmd( "clover_hns_start" )]
	public static void Start()
	{
		if ( !Networking.IsHost )
		{
			PlayerCharacter.Local.Notify( Notifications.NotificationType.Error,
				"Only the host can start a game of hide and seek." );
			return;
		}

		if ( !Leader.IsValid() )
		{
			PlayerCharacter.Local.Notify( Notifications.NotificationType.Error,
				"Only the host can start a game of hide and seek." );
			return;
		}

		var players = Game.ActiveScene.GetAllComponents<HideAndSeek>().ToList();

		if ( players.Count < 2 )
		{
			PlayerCharacter.Local.Notify( Notifications.NotificationType.Error,
				"Not enough players to start a game of hide and seek." );
			return;
		}

		// reset all players
		foreach ( var player in players )
		{
			player.SetSeeker( false );
		}

		// pick a random seeker
		var seeker = Random.Shared.FromList( players );
		seeker.SetSeeker( true );

		// start the round
		Leader.RoundStart = 0;
		Leader.IsRoundActive = true;

		Log.Info( "Started a game of hide and seek" );

		foreach ( var player in players )
		{
			player.GetComponent<PlayerCharacter>()
				.Notify( Notifications.NotificationType.Info, "A new round of hide and seek has started!" );
		}

		SoundEx.Play( Leader.RoundStartSound );
	}

	[Authority]
	public void SetSeeker( bool isSeeker )
	{
		IsSeeker = isSeeker;
		Log.Info( $"Player {GameObject.Name} is now a {(isSeeker ? "seeker" : "hider")}" );
	}


	private TimeSince _lastBlindCountdownSound;

	protected override void OnFixedUpdate()
	{
		if ( !IsRoundActive )
			return;

		if ( IsLeader && RoundStart > RoundDuration )
		{
			IsRoundActive = false;
			foreach ( var player in Scene.GetAllComponents<HideAndSeek>() )
			{
				player.GetComponent<PlayerCharacter>()
					.Notify( Notifications.NotificationType.Info, "The round has ended." );
			}

			SoundEx.Play( RoundEndSound );

			return;
		}

		if ( IsSeeker )
		{
			if ( IsBlind && _lastBlindCountdownSound > 1 )
			{
				Sound.Play( BlindCountdownSound );
				_lastBlindCountdownSound = 0;
			}
		}
		else
		{
		}
	}

	public bool CanInteract( PlayerCharacter player )
	{
		var hns = player.GetComponent<HideAndSeek>();
		// Log.Info(
		// 	$"HnS:{GameObject.Name} - IsSeeker: {IsSeeker}, IsRoundActive: {Leader.IsRoundActive}, IsSelf: {player != Components.Get<PlayerCharacter>()}/{IsProxy}" );
		return hns.IsSeeker && Leader.IsValid() && Leader.IsRoundActive && IsProxy;
	}

	public void StartInteract( PlayerCharacter player )
	{
		Log.Info( $"{player.GameObject.Name} is trying to catch {GameObject.Name}" );
		using ( Rpc.FilterInclude( Connection.Host ) )
		{
			player.GetComponent<HideAndSeek>().Catch( this );
		}
	}

	private bool AreAllCaught => Scene.GetAllComponents<HideAndSeek>().All( x => x.IsSeeker );

	// Broadcasted but only called on the host
	[Broadcast]
	public void Catch( HideAndSeek target )
	{
		var catcher = PlayerCharacter.Get( Rpc.Caller );
		var caught = target.Components.Get<PlayerCharacter>();

		caught.Notify( Notifications.NotificationType.Warning, "You have been caught! You are now a seeker." );
		catcher.Notify( Notifications.NotificationType.Success, $"You caught {caught.GameObject.Name}!" );
		Log.Info( $"{catcher.GameObject.Name} caught {caught.GameObject.Name}" );

		target.IsSeeker = true;
		target.SetSeeker( true );

		SoundEx.Play( CatchSound, target.WorldPosition );

		CheckRoundEnd();
	}

	private void CheckRoundEnd()
	{
		if ( !AreAllCaught )
		{
			return;
		}

		Leader.RoundStart = 0;
		Leader.IsRoundActive = false;
		Log.Info( "All players have been caught. Round ended." );

		foreach ( var player in Scene.GetAllComponents<HideAndSeek>() )
		{
			player.GetComponent<PlayerCharacter>()
				.Notify( Notifications.NotificationType.Info,
					"All players have been caught. The round has ended." );
		}

		SoundEx.Play( RoundEndSound );
	}

	public string GetInteractName()
	{
		return "Catch";
	}
}
