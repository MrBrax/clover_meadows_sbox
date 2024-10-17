using System;
using System.Text.Json.Serialization;

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

	[JsonIgnore, Hide]
	public bool IsCurrentlyInsideAvailablePeriod
	{
		get
		{
			if ( AlwaysAvailable )
				return true;

			var now = DateTime.Now;
			foreach ( var period in AvailablePeriods )
			{
				if ( period.StartMonth <= now.Month && now.Month <= period.EndMonth )
				{
					if ( period.StartDay <= now.Day && now.Day <= period.EndDay )
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}

public struct DatePeriod
{
	public DatePeriod()
	{
	}

	[JsonInclude] public int StartMonth { get; set; } = 1;
	[JsonInclude] public int StartDay { get; set; } = 1;

	[JsonInclude] public int EndMonth { get; set; } = 1;
	[JsonInclude] public int EndDay { get; set; } = 1;
}
