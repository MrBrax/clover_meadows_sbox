using System;

namespace Sandbox;

public partial class DialogueWindow
{
	
	[Property] public Dialogue Dialogue { get; set; }
	
	public Dialogue.DialogueChoice CurrentChoice { get; set; }
	public Dialogue.DialogueNode CurrentNode { get; set; }
	public int CurrentNodeIndex { get; set; }
	
	public string Text { get; set; } = "";
	public string Name => CurrentNode?.Speaker;
	
	private int _textIndex;
	private string _textTarget;
	private TimeSince _lastLetter;
	

	protected override void OnStart()
	{
		base.OnStart();
		
		CurrentNode = Dialogue.Nodes[0];
		Read();
	}

	private void Read()
	{
		Text = "";
		_textIndex = 0;
		_textTarget = CurrentNode.Text;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		Typewriter();
	}

	private void Typewriter()
	{
		if ( _lastLetter > 0.05f )
		{
			_lastLetter = 0;
			if ( _textIndex < _textTarget.Length )
			{
				Text += _textTarget[_textIndex];
				_textIndex++;
			}
		}
	}

	private void OnClick()
	{
		// If we're still typing, finish the text
		/*if ( Text.Length < _textTarget.Length )
		{
			Log.Info( "Finish the text" );
			Text = _textTarget;
			return;
		}*/
		
	}
	
	private void OnChoice( Dialogue.DialogueChoice choice )
	{
		Log.Info( $"Selected {choice.Label}" );
		
		CurrentChoice = choice;
		CurrentNode = choice.Nodes.FirstOrDefault();
		Read();
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Text );
	}
}
