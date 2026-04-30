using System;
using Editor.NodeEditor;
namespace Clover.Dialogue
{
	public class DialogueNodeData : INode
	{
		public event Action Changed;

		public BaseDialogueNode DialogueNode { get; }

		private readonly DialogueInputPlug _inputPlug;
		private int _lastChoiceCount = -1;

		public DialogueOutputPlug NextOutput { get; }
		public List<DialogueOutputPlug> ChoiceOutputs { get; } = new();

		public string Identifier => DialogueNode.Id;

		public DisplayInfo DisplayInfo => new()
		{
			Name = (DialogueNode.IsYou ? "You" : DialogueNode.Speaker) ?? "NPC", Icon = DialogueNode.IsYou ? "person" : "smart_toy"
		};

		public bool CanClone => true;
		public bool CanRemove => true;

		public Vector2 Position
		{
			get => DialogueNode.EditorPosition;
			set => DialogueNode.EditorPosition = value;
		}

		public Vector2 ExpandSize => new( 200, string.IsNullOrEmpty( DialogueNode.Text ) ? 0 : 64 );
		public bool AutoSize => true;
		public bool HasTitleBar => true;

		public IEnumerable<IPlugIn> Inputs => new IPlugIn[]
		{
			_inputPlug
		};

		public IEnumerable<IPlugOut> Outputs
		{
			get
			{
				var choiceCount = DialogueNode.Choices.Count;
				if ( choiceCount != _lastChoiceCount )
					RebuildOutputs();

				if ( DialogueNode.ExitsDialogue )
					return Enumerable.Empty<IPlugOut>();

				if ( choiceCount > 0 )
					return ChoiceOutputs;

				return new IPlugOut[]
				{
					NextOutput
				};
			}
		}

		public string ErrorMessage => null;
		public bool IsReachable => true;
		public Pixmap Thumbnail => null;

		public void OnPaint( Rect rect )
		{
			var text = DialogueNode.Text;
			if ( string.IsNullOrEmpty( text ) ) return;

			// Plug handles start at y=24 (title height) and each takes 24px.
			// Draw the text below all handles to avoid overlapping choice labels.
			const float titleHeight = 24f;
			var handlesHeight = MathF.Max( 24f, DialogueNode.Choices.Count * 24f );
			var textTop = titleHeight + handlesHeight + 6f;
			var textRect = new Rect( 10f, textTop, rect.Width - 20f, rect.Height - textTop - 8f );

			if ( textRect.Height <= 0 ) return;

			global::Editor.Paint.SetDefaultFont( 7 );
			global::Editor.Paint.SetPen( Theme.TextControl.WithAlpha( 0.65f ) );
			global::Editor.Paint.DrawText( textRect, text, TextFlag.LeftTop | TextFlag.WordWrap );
		}

		public void OnDoubleClick( MouseEvent e ) {}

		public NodeUI CreateUI( GraphView view ) => new NodeUI( view, this );

		public Color GetPrimaryColor( GraphView view ) =>
			DialogueNode.IsYou
				? Color.Parse( "#2563eb" )!.Value
				: Color.Parse( "#16a34a" )!.Value;

		public Menu CreateContextMenu( NodeUI nodeUi )
		{
			var menu = new Menu();

			menu.AddOption( "Add Choice", "add", () =>
			{
				DialogueNode.Choices.Add( new DialogueChoice
				{
					Text = "..."
				} );
				FireChanged();
			} );

			if ( DialogueNode.Choices.Count > 0 )
			{
				menu.AddOption( "Remove Last Choice", "remove", () =>
				{
					DialogueNode.Choices.RemoveAt( DialogueNode.Choices.Count - 1 );
					FireChanged();
				} );
			}

			menu.AddSeparator();

			if ( DialogueNode.IsYou )
			{
				menu.AddOption( "Make NPC Line", "smart_toy", () =>
				{
					DialogueNode.IsYou = false;
					FireChanged();
				} );
			}
			else
			{
				menu.AddOption( "Make Player Line", "person", () =>
				{
					DialogueNode.IsYou = true;
					FireChanged();
				} );
			}

			menu.AddSeparator();

			var exitsLabel = DialogueNode.ExitsDialogue ? "Clear Exit Flag" : "Mark as Exit Node";
			menu.AddOption( exitsLabel, "logout", () =>
			{
				DialogueNode.ExitsDialogue = !DialogueNode.ExitsDialogue;
				FireChanged();
			} );

			return menu;
		}

		public void FireChanged()
		{
			RebuildOutputs();
			Changed?.Invoke();
		}

		private void RebuildOutputs()
		{
			var choiceCount = DialogueNode.Choices.Count;

			while ( ChoiceOutputs.Count < choiceCount )
				ChoiceOutputs.Add( new DialogueOutputPlug( this, ChoiceOutputs.Count ) );

			while ( ChoiceOutputs.Count > choiceCount )
				ChoiceOutputs.RemoveAt( ChoiceOutputs.Count - 1 );

			_lastChoiceCount = choiceCount;
		}

		public DialogueNodeData( BaseDialogueNode node, DialogueGraph graph )
		{
			DialogueNode = node;
			_inputPlug = new DialogueInputPlug( this, graph );
			NextOutput = new DialogueOutputPlug( this, -1 );
			RebuildOutputs();
		}
	}
}
