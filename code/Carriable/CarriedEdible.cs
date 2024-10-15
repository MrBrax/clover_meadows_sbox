using Clover.Components;
using Clover.Data;
using Clover.Persistence;

namespace Clover.Carriable;

public class CarriedEdible : BaseCarriable
{
	[Property] public ItemData EdibleData { get; set; }

	[Property] public SoundEvent EatSound { get; set; }

	// public override bool DestroyOnUnequip => true;

	private GameObject _edibleModel;

	public override bool CanUse()
	{
		return true;
	}

	public override void OnSave( PersistentItem item )
	{
		base.OnSave( item );
		item.SetArbitraryData( "EdibleData", EdibleData.Id );
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
	}

	public override void OnLoad( PersistentItem item )
	{
		base.OnLoad( item );
		if ( item.TryGetArbitraryData( "EdibleData", out string edibleId ) )
		{
			EdibleData = Data.ItemData.Get( edibleId );
			UpdateModel();
		}
	}

	public void UpdateModel()
	{
		_edibleModel?.Destroy();

		if ( EdibleData is IEdibleData edibleData )
		{
			_edibleModel = edibleData.HoldScene.Clone();
		}
		else
		{
			_edibleModel = EdibleData.ModelScene.Clone();
			_edibleModel.LocalScale = Vector3.One * 0.3f;
		}

		_edibleModel.SetParent( GameObject );
		_edibleModel.LocalPosition = Vector3.Zero;
		_edibleModel.LocalRotation = Rotation.Identity;
	}

	public override void OnUseDown()
	{
		base.OnUseDown();

		SoundEx.Play( EatSound, GameObject.WorldPosition );

		Player.Equips.RemoveEquippedItem( Equips.EquipSlot.Tool, true );
	}
}
