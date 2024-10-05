using Clover.Carriable;

namespace Clover.Data;

[GameResource("Tool", "tool", "tool")]
public class ToolData : ItemData
{
	
	[Property] public GameObject CarryScene { get; set; }
	
	
	public BaseCarriable SpawnCarriable()
	{
		return CarryScene.Clone().GetComponent<BaseCarriable>();
	}

	public override string GetIcon()
	{
		return Icon ?? "ui/icons/default_tool.png";
	}
}
