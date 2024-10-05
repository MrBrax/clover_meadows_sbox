using System;
using Clover.Carriable;
using Clover.Player;

namespace Clover.Components;

public class Equips : Component
{
	[Flags]
	public enum EquipSlot
	{
		None = 1 << 0, // Not a valid slot

		[Icon( "hat" )] Hat = 1 << 1,

		[Icon( "tshirt" )] Top = 1 << 2,

		[Icon( "jeans" )] Bottom = 1 << 3,

		[Icon( "ice_skating" )] Shoes = 1 << 4,

		[Icon( "build" )] Tool = 1 << 5,

		[Icon( "face_2" )] Hair = 1 << 6,
		// TODO: add more later?
	}

	[Property] public Dictionary<EquipSlot, GameObject> AttachPoints { get; set; } = new();

	public Dictionary<EquipSlot, GameObject> EquippedItems { get; set; } = new();

	[Property] public Action<EquipSlot, GameObject> OnEquippedItemChanged { get; set; }

	[Property] public Action<EquipSlot> OnEquippedItemRemoved { get; set; }

	public GameObject GetEquippedItem( EquipSlot slot )
	{
		return CollectionExtensions.GetValueOrDefault( EquippedItems, slot );
	}

	public T GetEquippedItem<T>( EquipSlot slot ) where T : Component
	{
		return EquippedItems.TryGetValue( slot, out var gameObject ) ? gameObject.GetComponent<T>() : null;
	}

	public bool IsEquippedItemType<T>( EquipSlot slot )
	{
		return EquippedItems.TryGetValue( slot, out var gameObject ) && gameObject.GetComponent<T>() != null;
	}

	public bool HasEquippedItem( EquipSlot slot )
	{
		return EquippedItems.ContainsKey( slot ) && EquippedItems[slot].IsValid();
	}

	public bool TryGetEquippedItem( EquipSlot slot, out GameObject item )
	{
		return EquippedItems.TryGetValue( slot, out item );
	}

	public void SetEquippedItem( EquipSlot slot, GameObject item )
	{
		if ( slot == 0 ) throw new ArgumentException( "Invalid slot" );
		if ( !item.IsValid() ) throw new ArgumentException( "Item is not valid" );

		if ( !AttachPoints.TryGetValue( slot, out var attachPoint ) )
		{
			throw new ArgumentException( $"No attach point for slot {slot}" );
		}

		if ( EquippedItems.ContainsKey( slot ) )
		{
			RemoveEquippedItem( slot, true );
		}

		EquippedItems[slot] = item;

		item.SetParent( attachPoint );
		item.LocalPosition = Vector3.Zero;
		item.LocalRotation = Rotation.Identity;

		if ( item.Components.TryGet<BaseCarriable>( out var carriable ) )
		{
			carriable.SetHolder( GameObject );
			carriable.OnEquip( GameObject ); // TODO: don't hardcode player, since NPCs can use items too
		}

		OnEquippedItemChanged?.Invoke( slot, item );
		Scene.RunEvent<IEquipChanged>( x => x.OnEquippedItemChanged( GameObject, slot, item ) );
	}

	public void RemoveEquippedItem( EquipSlot slot, bool destroy = false )
	{
		if ( EquippedItems.ContainsKey( slot ) )
		{
			if ( destroy ) EquippedItems[slot].Destroy();
			EquippedItems.Remove( slot );
			OnEquippedItemRemoved?.Invoke( slot );
			Scene.RunEvent<IEquipChanged>( x => x.OnEquippedItemRemoved( GameObject, slot ) );
		}
	}

	public void SetEquippableVisibility( EquipSlot slot, bool visible )
	{
		if ( EquippedItems.ContainsKey( slot ) )
		{
			EquippedItems[slot].Enabled = visible;
			return;
		}

		throw new Exception( $"No item equipped in slot {slot}" );
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		if ( Input.Pressed( "UseTool" ) )
		{
			if ( HasEquippedItem( EquipSlot.Tool ) )
			{
				var tool = GetEquippedItem<BaseCarriable>( EquipSlot.Tool );
				if ( tool.CanUse() )
				{
					// tool.OnUse( GetComponent<PlayerCharacter>() );
					tool.OnUseDown();
				}
				else
				{
					Log.Info( "Can't use tool" );
				}
			}
			else
			{
				Log.Info( "No tool equipped" );
			}
		}
	}
}

public interface IEquipChanged
{
	void OnEquippedItemChanged( GameObject owner, Equips.EquipSlot slot, GameObject item );
	void OnEquippedItemRemoved( GameObject owner, Equips.EquipSlot slot );
}
