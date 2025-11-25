namespace Clover.Data;

[AssetType( Name = "World", Extension = "cmworld" )]
public class WorldData : GameResource
{
	[Property] public string Title { get; set; }

	[Property] public GameObject Prefab { get; set; }

	[Property, Group( "Weather" )] public bool IsInside { get; set; }

	[Property, Group( "Grid" )] public int Width { get; set; }

	[Property, Group( "Grid" )] public int Height { get; set; }


	[Property] public float MaxItemAltitude { get; set; } = 0;

	[Property] public bool DisableItemPlacement { get; set; }

	[Property] public bool CanRotateCamera { get; set; }
}
