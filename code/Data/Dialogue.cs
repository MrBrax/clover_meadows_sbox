using System;
using System.Threading.Tasks;
using Clover.Components;
using Clover.Inventory;
using Clover.Persistence;
using Clover.Player;
using Clover.Ui;
using Clover.WorldBuilder;
using Sandbox.Diagnostics;
using Sandbox.UI;

namespace Clover.Data;

[AssetType( Name = "Dialogue", Extension = "dlg" )]
public class Dialogue : GameResource
{
	/*public class DialogueAction
	{
		[Property, ActionGraphInclude] public GameObject Player { get; set; }
		[Property] public List<GameObject> Targets { get; set; }
		[Property] public DialogueNode Node { get; set; }
		[Property] public DialogueChoice Choice { get; set; }
	}*/

	public DialogueAction Tree { get; set; } = null;

	[ActionGraphNode( "clover.dialogue.textnode" )]
	[Title( "Dialogue Text Node" ), Group( "Dialogue" ), Icon( "chat" )]
	public static async Task DialogueTextNode( GameObject speaker, [TextArea] string text )
	{
		Log.Info( "New DialogueTextNode" );
		if ( DialogueManager.Instance.DialogueWindow.DialogueNodeCompletedTaskSource != null )
		{
			Log.Error( "DialogueTextNode: DialogueNodeCompletedTaskSource is not null." );
			return;
		}

		DialogueManager.Instance.DialogueWindow.Enabled = true;

		DialogueManager.Instance.DialogueWindow.ChoicesPanel?.DeleteChildren();

		DialogueManager.Instance.DialogueWindow.DispatchText( speaker, text );

		DialogueManager.Instance.DialogueWindow.IsCurrentNodeChoice = false;

		Log.Info( "Creating new dialogue node completed task source." );
		DialogueManager.Instance.DialogueWindow.DialogueNodeCompletedTaskSource = new TaskCompletionSource();

		Log.Info( "Waiting for dialogue node to complete." );
		await DialogueManager.Instance.DialogueWindow.DialogueNodeCompletedTaskSource.Task;

		Log.Info( "Dialogue node completed." );
		DialogueManager.Instance.DialogueWindow.DialogueNodeCompletedTaskSource = null;
	}

	[ActionGraphNode( "clover.dialogue.choicenode" )]
	[Title( "Dialogue Choice Node" ), Group( "Dialogue" ), Icon( "chat" )]
	public static async Task DialogueChoiceNode( GameObject speaker, [TextArea] string text, string[] choices,
		Action onChoose0, Action onChoose1, Action onChoose2, Action onChoose3, Action onChoose4, Action onChoose5 )
	{
		if ( choices.Length == 0 )
		{
			Log.Error( "DialogueChoiceNode: No choices provided." );
			return;
		}

		if ( choices.Length > 6 )
		{
			Log.Error( "DialogueChoiceNode: Too many choices provided." );
			return;
		}

		Log.Info( "DialogueChoiceNode" );

		DialogueManager.Instance.DialogueWindow.Enabled = true;
		DialogueManager.Instance.DialogueWindow.DispatchText( speaker, text );
		DialogueManager.Instance.DialogueWindow.IsCurrentNodeChoice = true;

		await Task.Delay( 100 );

		if ( !DialogueManager.Instance.DialogueWindow.ChoicesPanel.IsValid() )
		{
			Log.Error( "DialogueChoiceNode: ChoicesPanel is null." );
			return;
		}

		DialogueManager.Instance.DialogueWindow.ChoicesPanel.DeleteChildren();

		int index = 0;
		foreach ( var choice in choices )
		{
			if ( string.IsNullOrEmpty( choice ) )
			{
				Log.Error( "DialogueChoiceNode: Empty choice provided." );
				return;
			}

			var _index = index;

			var button = new Button( choice );
			button.AddEventListener( "onclick", ( PanelEvent e ) =>
			{
				switch ( _index )
				{
					case 0:
						onChoose0();
						break;
					case 1:
						onChoose1();
						break;
					case 2:
						onChoose2();
						break;
					case 3:
						onChoose3();
						break;
					case 4:
						onChoose4();
						break;
					case 5:
						onChoose5();
						break;
				}

				e.StopPropagation();
			} );

			DialogueManager.Instance.DialogueWindow.ChoicesPanel.AddChild( button );

			index++;
		}
	}

	[ActionGraphNode( "clover.dialogue.itemselectnode" )]
	[Title( "Item Select Node" ), Group( "Dialogue" ), Icon( "chat" )]
	public static async Task<int[]> ItemSelectNode( int maxItems )
	{
		Log.Info( "ItemSelectNode" );

		if ( DialogueManager.Instance.DialogueWindow.ChoicesPanel.IsValid() )
		{
			DialogueManager.Instance.DialogueWindow.ChoicesPanel.DeleteChildren();
		}

		var result = await MainUi.Instance.Components.Get<InventorySelectUi>( true )
			.SelectItems( maxItems, ( item ) => item.GetItem().ItemData.CanSell );

		Log.Info( "ItemSelectNode: Selected items" );

		return result.ToArray();
	}

	[ActionGraphNode( "clover.dialogue.inventoryindexestoslots" )]
	[Title( "Inventory Indexes To Slots" ), Group( "Dialogue" ), Icon( "chat" )]
	public static List<InventorySlot<PersistentItem>> InventoryIndexesToSlots( InventoryContainer container,
		int[] indexes )
	{
		return indexes.Select( container.GetSlotByIndex ).Where( slot => slot != null ).ToList();
	}

	[ActionGraphNode( "clover.dialogue.end" )]
	[Title( "End Dialogue" ), Group( "Dialogue" ), Icon( "chat" )]
	public static void EndDialogue()
	{
		Log.Info( "EndDialogue" );
		DialogueManager.Instance.DialogueWindow.End();
	}

	[ActionGraphNode( "clover.dialogue.setdata" )]
	[Title( "Set Dialogue Data" ), Group( "Dialogue" ), Icon( "chat" )]
	public static void SetData( string key, object value )
	{
		Log.Info( $"SetData: {key} = {value}" );
		DialogueManager.Instance.DialogueWindow.SetData( key, value );
	}

	[ActionGraphNode( "clover.dialogue.getdatageneric" )]
	[Title( "Get Dialogue Data" ), Group( "Dialogue" ), Icon( "chat" )]
	public static T GetData<T>( string key )
	{
		Log.Info( $"GetData: {key}" );
		return DialogueManager.Instance.DialogueWindow.GetData<T>( key );
	}

	[ActionGraphNode( "clover.dialogue.getitemssellprice.itemdata" )]
	[Title( "Get Items Sell Price (ItemData)" ), Group( "Dialogue" ), Icon( "chat" )]
	public static int GetItemsSellPrice( List<ItemData> items )
	{
		return items.Sum( item => item.GetCustomSellPrice?.Invoke( TimeManager.Time ) ?? item.BaseSellPrice );
	}

	[ActionGraphNode( "clover.dialogue.getitemssellprice.slot" )]
	[Title( "Get Items Sell Price (Slot)" ), Group( "Dialogue" ), Icon( "chat" )]
	public static int GetItemsSellPrice( List<InventorySlot<PersistentItem>> slots )
	{
		return slots.Sum( slot =>
			slot.GetItem().ItemData.GetCustomSellPrice?.Invoke( TimeManager.Time ) ??
			slot.GetItem().ItemData.BaseSellPrice );
	}


	/*[ActionGraphNode( "clover.dialogue.quickcompare" )]
	public static Task<bool> QuickCompare( int a, int b )
	{
		if ( a == b )
		{
			return Task.FromResult( true );
		}
		else
		{
			return Task.FromResult( false );
		}

	}*/


	public delegate void DialogueAction(
		DialogueWindow window,
		PlayerCharacter player,
		List<GameObject> targets,
		DialogueNode node,
		DialogueChoice choice
	);

	public delegate bool DialogueCondition(
		DialogueWindow window,
		PlayerCharacter player,
		List<GameObject> targets,
		DialogueNode node,
		DialogueChoice choice
	);

	public class DialogueChoice
	{
		[Property] public string Label { get; set; } = null;

		// [Property] public string Id { get; set; }
		// [Property] public string IdTarget { get; set; }
		[Property] public DialogueAction OnSelect { get; set; } = null;
		[Property] public string JumpToId { get; set; } = null;

		[Property, Description( "Will only show if OnSelect is null" )]

		public List<DialogueNode> Nodes { get; set; } = new();

		public override string ToString()
		{
			if ( Nodes.Count == 0 && !string.IsNullOrEmpty( JumpToId ) )
			{
				return $"{Label} -> JUMP:{JumpToId}";
			}
			else if ( Nodes.Count == 0 && OnSelect != null )
			{
				return $"{Label} -> ACTION";
			}

			return $"{Label} -> {Nodes.Count} nodes";
		}
	}

	public class DialogueNode
	{
		[Property] public string Id { get; set; } = Guid.NewGuid().ToString()[..8];

		[Property, Description( "I don't think the player ever talks, but this is here just in case." )]
		public bool IsPlayer { get; set; } = false;


		[Property, HideIf( nameof(IsPlayer), true )]
		public int Speaker { get; set; } = 0;

		[Property, TextArea] public string Text { get; set; } = null;

		// [Property] public List<string> Choices { get; set; } = new();
		[Property] public List<DialogueChoice> Choices { get; set; } = new();

		[Property, Description( "This is the action that will be executed when this node is entered." )]
		public DialogueAction OnEnter { get; set; } = null;

		[Property] public DialogueAction OnExit { get; set; } = null;

		/// <summary>
		///   If true, this node will not be advanced to automatically, and must be jumped to manually.
		/// </summary>
		[Property]
		public bool IsHidden { get; set; } = false;

		public override string ToString()
		{
			if ( !string.IsNullOrEmpty( Id ) )
			{
				return $"#{Id} - {Speaker}: {Text}";
			}

			return $"{Speaker}: {Text}";
		}
	}

	[Property, InlineEditor] public List<DialogueNode> Nodes { get; set; } = new();
}
