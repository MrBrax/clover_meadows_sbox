namespace Clover.Dialogue;

[AssetType( Name = "Dialogue Data", Extension = "dialogue" )]
public class DialogueData : GameResource
{
	// [Property] public string Name { get; set; } = "";
	// [Property] public string Description { get; set; } = "";

	[Property, Hide] public List<BaseDialogueNode> Nodes { get; set; } = new();

	/*public DialogueNode GetNodeById( string id )
	{
		return Nodes.FirstOrDefault( node => node.Id == id );
	}*/

	protected override Bitmap CreateAssetTypeIcon( int width, int height )
	{
		return CreateSimpleAssetTypeIcon( "message", width, height, (Color)"#0077b6", (Color)"white" );
	}
}
