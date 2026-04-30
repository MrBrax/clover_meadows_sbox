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

	public override Bitmap RenderThumbnail( ThumbnailOptions options )
	{
		var bmp = new Bitmap( options.Width, options.Height );

		int i = 0;
		foreach ( var node in Nodes )
		{
			var speakerRect = new Rect( 10, i * 50, options.Width - 20, 20 );
			var textRect = new Rect( 10, i * 50 + 20, options.Width - 20, 20 );

			var speaker = string.IsNullOrEmpty( node.Speaker ) ? "NPC" : node.Speaker;
			bmp.DrawText( TextRendering.Scope.Default with
			{
				Text = speaker,
				TextColor = Color.Parse( "#0077b6" )!.Value,
				FontSize = 16,
				FontWeight = 700
			}, speakerRect, TextFlag.LeftTop );

			var text = node.Text.Truncate( 20 );
			bmp.DrawText( TextRendering.Scope.Default with
			{
				Text = text,
				TextColor = Color.White,
				FontSize = 14
			}, textRect, TextFlag.LeftTop );
			i++;
		}

		return bmp;

	}

}
