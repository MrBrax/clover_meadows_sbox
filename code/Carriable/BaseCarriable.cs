using System;
using Clover.Data;
using Clover.Inventory;
using Clover.Items;
using Clover.Persistence;
using Clover.Player;

namespace Clover.Carriable;

[Category( "Clover/Carriable" )]
[Icon( "build" )]
public class BaseCarriable : Component, IPersistent, IPickupable
{
	[Sync] public int Durability { get; set; } = 100;

	public TimeUntil NextUse;

	[Sync] public GameObject Holder { get; set; }

	public PlayerCharacter Player => Holder.GetComponent<PlayerCharacter>();

	[Property] public GameObject Model { get; set; }

	[Property] public ToolData ItemData { get; set; }

	[Property] public float UseTime { get; set; } = 1f;

	// public virtual bool DestroyOnUnequip => false;

	public delegate void OnEquipActionEvent( GameObject holder );

	[Property, Group( "Actions" )] public OnEquipActionEvent OnEquipAction { get; set; }

	public delegate void OnUnequipActionEvent();

	[Property, Group( "Actions" )] public OnUnequipActionEvent OnUnequipAction { get; set; }

	public delegate void OnUseDownActionEvent();

	[Property, Group( "Actions" )] public OnUseDownActionEvent OnUseDownAction { get; set; }

	public delegate void OnUseUpActionEvent();

	[Property, Group( "Actions" )] public OnUseUpActionEvent OnUseUpAction { get; set; }

	public void SetHolder( GameObject holder )
	{
		Holder = holder;
	}

	public virtual string GetUseName()
	{
		return "Use";
	}

	public virtual IEnumerable<MainUi.InputData> GetInputs()
	{
		yield break;
	}

	public virtual bool CanUse()
	{
		return NextUse <= 0 && Durability > 0;
	}

	public virtual float CustomPlayerSpeed()
	{
		return 1;
	}

	public virtual bool ShouldDisableMovement()
	{
		return false;
	}

	public virtual void OnEquip( GameObject holder )
	{
		SetHolder( holder );
	}

	public virtual void OnUnequip()
	{
		Holder = null;
	}

	/// <summary>
	///  Called when you press down on the use button. Useful for holding an action like drawing a net or aiming a bow.
	/// </summary>
	public virtual void OnUseDown()
	{
	}

	/// <summary>
	///  Called when you release the use button. Useful for firing a bow or swinging a tool.
	/// </summary>
	public virtual void OnUseUp()
	{
	}

	public virtual void OnUseDownHost()
	{
	}

	public virtual void OnBreak()
	{
	}

	/*public void OnSave( WorldNodeLink nodeLink )
	{
		nodeLink.Persistence?.SetArbitraryData( "Durability", Durability );
	}

	public void OnLoad( WorldNodeLink nodeLink )
	{
		if ( nodeLink.Persistence == null ) return;
		if ( nodeLink.Persistence.TryGetArbitraryData<int>( "Durability", out var durability ) )
		{
			Durability = durability;
		}
	}*/

	public bool CanPickup( PlayerCharacter player )
	{
		return true;
	}

	public void OnPickup( PlayerCharacter player )
	{
		player.Inventory.PickUpItem( GetComponent<WorldItem>() );
	}

	public string GetPickupName()
	{
		return ItemData.Name;
	}

	public virtual void OnSave( PersistentItem item )
	{
		item.SetSaveData( "Durability", Durability );
	}

	public virtual void OnLoad( PersistentItem item )
	{
		if ( item.TryGetSaveData<int>( "Durability", out var durability ) )
		{
			Durability = durability;
		}
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.Arrow( Vector3.Zero, Vector3.Forward * 40f );
		if ( WorldRotation != Rotation.Identity )
		{
			Gizmo.Draw.Text( "WRONG ROTATION", new Transform(), "Roboto", 24f );
		}

		if ( Model.IsValid() && Model.WorldPosition != Vector3.Zero )
		{
			Gizmo.Draw.Text( "MODEL WRONG POSITION", new Transform(), "Roboto", 24f );
		}

		if ( Model.IsValid() && Model.WorldRotation != Rotation.Identity )
		{
			Gizmo.Draw.Text( "MODEL WRONG ROTATION", new Transform(), "Roboto", 24f );
		}
	}
}
