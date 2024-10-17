namespace Clover.Data;

[GameResource( "Catalogue", "cata", "Catalogue" )]
public class CatalogueData : GameResource
{
	[Property] public string Name { get; set; } = "Catalogue";

	[Property] public string Description { get; set; } = "A collection of items.";

	[Property] public List<ItemData> Items { get; set; } = new();

	[Property] public bool AlwaysAvailable { get; set; } = true;

	[Property, ShowIf( nameof(AlwaysAvailable), false )]
	public List<DatePeriod> AvailablePeriods { get; set; } = new();
}

public class DatePeriod
{
	public int StartMonth;
	public int StartDay;

	public int EndMonth;
	public int EndDay;
}
