namespace Clover.WorldBuilder;

public class InteriorPiece : Component
{
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }

	[Property] public List<string> BelongsToRooms { get; set; } = new();

	public void Hide()
	{
		GetComponent<ModelRenderer>( true ).Enabled = false;
	}

	public void Show()
	{
		GetComponent<ModelRenderer>( true ).Enabled = true;
	}

	public bool BelongsToRoom( string room )
	{
		return BelongsToRooms.Contains( room );
	}
}
