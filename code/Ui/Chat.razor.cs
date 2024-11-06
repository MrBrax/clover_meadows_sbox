using System;
using Clover.Player;
using Sandbox.UI;

namespace Clover;

public partial class Chat
{
	public static Chat Instance { get; private set; }

	public List<ChatMessage> Messages { get; set; } = new();

	public List<ChatMessage> DistantMessages => Messages.Where( x => !x.IsNearby ).ToList();

	private bool ShouldShowMessages => DistantMessages.Count > 0 || ShouldShowInput;

	private bool ShouldShowInput;

	public record ChatMessage
	{
		public string Text { get; init; }
		public ulong Author { get; init; }
		public string PlayerId { get; init; }
		public DateTime Time { get; init; }

		public string Name => IsYou ? "You" : (Player.IsValid() ? Player.PlayerName : "Unknown");

		public bool IsYou => PlayerId == PlayerCharacter.Local?.PlayerId;

		public PlayerCharacter Player => Game.ActiveScene.GetAllComponents<PlayerCharacter>()
			.FirstOrDefault( x => x.PlayerId == PlayerId /*|| x.Network.Owner.SteamId == Author*/ );

		public bool IsNearby => Player.IsValid() && PlayerCharacter.Local.IsValid() &&
		                        PlayerCharacter.Local.WorldPosition.Distance( Player.WorldPosition ) < 300;

		public string Location { get; set; } = "Unknown";
	}


	private string MessageText { get; set; } = "";

	private TextEntry InputBox;


	protected override int BuildHash() =>
		System.HashCode.Combine( Messages, DistantMessages, ShouldShowInput, MessageText );

	protected override void OnAwake()
	{
		Instance = this;
	}

	protected override void OnDestroy()
	{
		Instance = null;
	}

	protected override void OnFixedUpdate()
	{
		if ( Input.EscapePressed )
		{
			ShouldShowInput = false;
			StateHasChanged();
		}

		if ( Input.Pressed( "Chat" ) )
		{
			if ( !ShouldShowInput )
			{
				Log.Info( "Enable Chat" );
				ShouldShowInput = true;
				StateHasChanged();
				InputBox?.Focus();
				return;
			}

			SendMessage();
		}

		if ( Messages.RemoveAll( x => x.Time < DateTime.Now.AddSeconds( -30 ) ) > 0 )
		{
			StateHasChanged();
		}
	}

	private void HideInput()
	{
		ShouldShowInput = false;
		StateHasChanged();
	}

	private void SendMessage()
	{
		if ( !string.IsNullOrWhiteSpace( MessageText ) )
		{
			var location = PlayerCharacter.Local.World.Title;
			AddMessage( MessageText, PlayerCharacter.Local.PlayerId, location );
			MessageText = "";
		}

		ShouldShowInput = false;
		StateHasChanged();
	}

	[Broadcast]
	public void AddMessage( string message, string playerId, string location )
	{
		var msg = new ChatMessage()
		{
			Text = message,
			Author = Rpc.Caller.SteamId,
			Time = DateTime.Now,
			PlayerId = playerId,
			Location = location
		};
		Messages.Add( msg );
		Log.Info( $"> {msg.Name} ({Rpc.Caller.DisplayName}): {msg.Text}" );
		StateHasChanged();
	}

	[ConCmd( "say" )]
	public static void Say( string message )
	{
		Instance.AddMessage( message, PlayerCharacter.Local.PlayerId, PlayerCharacter.Local.World.Title );
	}
}
