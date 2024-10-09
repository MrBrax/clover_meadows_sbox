using System;

namespace Clover.Data;

[GameResource( "Fish Data", "fish", "Fish", Icon = "phishing" )]
public class FishData : AnimalData
{
	
	[Flags]
	public enum FishLocation
	{
		Sea = 1,
		Pond = 1 << 1,
		River = 1 << 2,
	}
	
	public enum FishSize
	{
		Tiny,
		Small,
		Medium,
		Large,
	}
	
	[Property] public RangedFloat Weight { get; set; } = new( 1, 1 );
	
	[Property] public FishLocation Location { get; set; } = FishLocation.River;

	[Property] public FishSize Size { get; set; } = FishSize.Small;
	
	public override string GetIcon()
	{
		return Icon ?? "ui/icons/default_fish.png";
	}
	
}
