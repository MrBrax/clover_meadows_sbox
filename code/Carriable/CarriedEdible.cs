using Clover.Components;
using Clover.Data;
using Clover.Persistence;

namespace Clover.Carriable;

public class CarriedEdible : BaseCarriable
{
	[Property] public ItemData EdibleData { get; set; }

	[Property] public SoundEvent EatSound { get; set; }
	[Property] public SoundEvent DrinkSound { get; set; }

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

	public override string GetUseName()
	{
		if ( EdibleData is IEdibleData iEdible )
		{
			return iEdible.Type switch
			{
				IEdibleData.EdibleType.Food => "Eat",
				IEdibleData.EdibleType.Drink => "Drink",
				IEdibleData.EdibleType.Unknown => "Use",
				_ => "Use"
			};
		}

		return "Use";
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

		if ( EdibleData is IEdibleData iEdible )
		{
			switch ( iEdible.Type )
			{
				case IEdibleData.EdibleType.Food:
					SoundEx.Play( EatSound, GameObject.WorldPosition );
					break;
				case IEdibleData.EdibleType.Drink:
					SoundEx.Play( DrinkSound, GameObject.WorldPosition );
					break;
				case IEdibleData.EdibleType.Unknown:
				default:
					Log.Error( "Unknown edible type" );
					SoundEx.Play( EatSound, GameObject.WorldPosition );
					break;
			}
		}
		else
		{
			SoundEx.Play( EatSound, GameObject.WorldPosition );
		}


		Player.Equips.RemoveEquippedItem( Equips.EquipSlot.Tool, true );
	}
}
