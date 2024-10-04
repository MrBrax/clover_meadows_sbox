using Clover.Player;

namespace Clover.Carriable;

public class BaseCarriable : Component
{
	
	public int Durability { get; set; }

	public TimeUntil NextUse;
	
	public GameObject Holder { get; set; }
	public PlayerCharacter Player => Holder.GetComponent<PlayerCharacter>();
	
	// public ToolData ItemData { get; set; }
	
	public void SetHolder( GameObject holder )
	{
		Holder = holder;
	}
	
	public virtual bool CanUse()
	{
		return NextUse <= 0 && Durability > 0;
	}
	
	public virtual float CustomPlayerSpeed()
	{
		return 1;
	}
	
	public virtual void OnEquip( GameObject holder )
	{
		SetHolder( holder );
	}
	
	public virtual void OnUnequip()
	{
		Holder = null;
	}
	
	public virtual void OnUseDown()
	{
		
	}
	
	public virtual void OnUseUp()
	{
		
	}
	
	public virtual void OnBreak()
	{
		
	}

}
