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

		foreach ( var fileName in Utilities.Decals.GetAllDecals() )
		{
			Log.Info( $"Loading decal {fileName}" );

			Utilities.Decals.DecalData decal;

			try
			{
				decal = Utilities.Decals.ReadDecal( $"decals/{fileName}.decal" );
			}
			catch ( Exception e )
			{
				Log.Error( e.Message );
				continue;
			}

			Decals.Add( new PaintUi.DecalEntry() { Decal = decal, FileName = fileName } );
		}

		Log.Info( $"Paintbrush has {Decals.Count} textures" );
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Decals, Paintbrush?.CurrentTexturePath, Paintbrush?.CurrentTextureName );
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

	private void OpenPaintMode()
	{
		MainUi.Instance.Components.Get<PaintUi>( true ).OpenPaint( PaintUi.PaintMode.Decal, 32, 32, false );
	}
}
