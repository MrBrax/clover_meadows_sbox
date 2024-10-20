namespace Clover.Interactable;

public class ItemHighlight : HighlightOutline
{
	protected override void OnStart()
	{
		Color = Color.White;
		Width = 0.1f;
		Enabled = false;
	}
}
