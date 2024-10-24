using System;
using Clover.Carriable;
using Clover.Components;
using Clover.Items;
using Clover.Player;

namespace Clover.Ui;

public partial class PaintbrushUi : IPaintEvent, IEquipChanged
{
	private List<PaintUi.DecalEntry> Decals = new();
	private Paintbrush Paintbrush => PlayerCharacter.Local?.Equips.GetEquippedItem<Paintbrush>( Equips.EquipSlot.Tool );

	protected override void OnStart()
	{
		Refresh();
	}

	private void Refresh()
	{
		if ( !Paintbrush.IsValid() )
		{
			Log.Warning( "Paintbrush is not valid" );
			return;
		}

		Decals.Clear();

		foreach ( var path in Paintbrush.GetTextures() )
		{
			Log.Info( $"Loading decal {path}" );
			Decals.Add( new PaintUi.DecalEntry()
			{
				Decal = Utilities.Decals.ReadDecal( $"decals/{path}" ), ResourcePath = $"decals/{path}"
			} );
		}

		Log.Info( $"Paintbrush has {Decals.Count} textures" );
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Decals, Paintbrush?.CurrentTexturePath );
	}

	void IPaintEvent.OnFileSaved( string path )
	{
		Refresh();
	}

	public void OnEquippedItemChanged( GameObject owner, Equips.EquipSlot slot, GameObject item )
	{
		if ( owner == PlayerCharacter.Local.GameObject && item.Components.TryGet<Paintbrush>( out var paintbrush ) )
		{
			Refresh();
		}
	}

	public void OnEquippedItemRemoved( GameObject owner, Equips.EquipSlot slot )
	{
	}
}
