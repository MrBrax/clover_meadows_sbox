using Sandbox;
namespace Clover;

public sealed class World : Component
{
	
	[Property] public Data.World Data { get; set; }

	public string WorldId => Data.ResourceName;
	
	protected override void OnUpdate()
	{

	}
}
