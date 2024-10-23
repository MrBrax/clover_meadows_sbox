using System;
using Clover.Carriable;
using Clover.Player;

namespace Clover.Components;

/// <summary>
///  A component that manages equipping items to players and NPCs.
/// </summary>
[Title( "Equips" )]
[Icon( "build" )]
[Category( "Clover/Components" )]
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

	[Sync] public NetDictionary<EquipSlot, bool> EquippedSlots { get; set; } = new();
	[Sync] public NetDictionary<EquipSlot, bool> VisibleSlots { get; set; } = new();
	
	private PlayerCharacter Player => GetComponent<PlayerCharacter>();

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

	public bool HasEquippedItem<T>( EquipSlot slot ) where T : Component
	{
		return EquippedItems.TryGetValue( slot, out var gameObject ) && gameObject.GetComponent<T>() != null;
	}

	public bool TryGetEquippedItem( EquipSlot slot, out GameObject item )
	{
		return EquippedItems.TryGetValue( slot, out item );
	}

	public bool TryGetEquippedItem<T>( EquipSlot slot, out T item ) where T : Component
	{
		if ( EquippedItems.TryGetValue( slot, out var gameObject ) )
		{
			item = gameObject.GetComponent<T>();
			return item.IsValid();
		}

		item = null;
		return false;
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

		EquippedSlots[slot] = true;

		item.SetParent( attachPoint );
		item.LocalPosition = Vector3.Zero;
		item.LocalRotation = Rotation.Identity;

		item.NetworkSpawn();

		if ( item.Components.TryGet<BaseCarriable>( out var carriable ) )
		{
			carriable.SetHolder( GameObject );
			carriable.OnEquip( GameObject ); // TODO: don't hardcode player, since NPCs can use items too
			carriable.OnEquipAction?.Invoke( GameObject );
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
			EquippedSlots[slot] = false;
			OnEquippedItemRemoved?.Invoke( slot );
			Scene.RunEvent<IEquipChanged>( x => x.OnEquippedItemRemoved( GameObject, slot ) );
		}
	}

	public void SetEquippableVisibility( EquipSlot slot, bool visible )
	{
		if ( EquippedItems.ContainsKey( slot ) )
		{
			EquippedItems[slot].Enabled = visible;
			VisibleSlots[slot] = visible;
			return;
		}

		throw new Exception( $"No item equipped in slot {slot}" );
	}
	
	public bool CanUseTool()
	{
		if ( Player.ItemPlacer.IsPlacing || Player.ItemPlacer.IsMoving ) return false;
		return true;
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		if ( !CanUseTool() ) return;

		if ( Input.Pressed( "UseTool" ) )
		{
			if ( HasEquippedItem( EquipSlot.Tool ) )
			{
				var tool = GetEquippedItem<BaseCarriable>( EquipSlot.Tool );
				if ( tool.CanUse() )
				{
					// tool.OnUse( GetComponent<PlayerCharacter>() );
					tool.OnUseDown();
					tool.OnUseDownAction?.Invoke();

					using ( Rpc.FilterInclude( Connection.Host ) )
					{
						tool.OnUseDownHost();
					}
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
		else if ( Input.Released( "UseTool" ) )
		{
			if ( HasEquippedItem( EquipSlot.Tool ) )
			{
				var tool = GetEquippedItem<BaseCarriable>( EquipSlot.Tool );
				tool.OnUseUp();
				tool.OnUseUpAction?.Invoke();
			}
		}
	}

	protected override void DrawGizmos()
	{
		foreach ( var pair in AttachPoints )
		{
			if ( pair.Value.IsValid() )
			{
				Gizmo.Draw.LineSphere( pair.Value.WorldPosition, 4f );
				Gizmo.Draw.Arrow( pair.Value.WorldPosition,
					pair.Value.WorldPosition + pair.Value.WorldRotation.Up * 32f );
				Gizmo.Draw.Arrow( pair.Value.WorldPosition,
					pair.Value.WorldPosition + pair.Value.WorldRotation.Forward * 32f );
			}
		}
	}
}

public interface IEquipChanged
{
	void OnEquippedItemChanged( GameObject owner, Equips.EquipSlot slot, GameObject item );
	void OnEquippedItemRemoved( GameObject owner, Equips.EquipSlot slot );
}
