// Copyright Braxnet 2026 unless specified otherwise
// https://braxnet.online
namespace Clover.Dialogue;

[AssetType( Name = "Dialogue Collection", Extension = "diacol" )]
public class DialogueCollection : GameResource
{
	public List<DialogueData> Dialogues { get; set; } = new();

}
