using Clover.Player;

namespace Clover;

public partial class ChatBubbles
{
	private PlayerCharacter Player => GameObject.Parent.GetComponent<PlayerCharacter>();

	private List<Chat.ChatMessage> Messages =>
		Chat.Instance?.Messages.Where( x => x.PlayerId == Player.PlayerId ).ToList() ?? new();

	protected override int BuildHash() => System.HashCode.Combine( Messages );
}
