using System;

namespace Sandbox;

public partial class DialogueWindow
{
	[Property] public Dialogue Dialogue { get; set; }

	[Property]
	public Dictionary<string, object> Data { get; set; } =
		new() { { "test", 123 }, { "money", 100 }, { "price", 200 }, };

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
		
		Data = new Dictionary<string, object>
		{
			{ "test", 123 },
			{ "money", 100 },
			{ "price", 200 },
		};

		CurrentNode = Dialogue.Nodes[0];
		Read();
	}

	[Pure]
	public int GetDataInt( string key )
	{
		if ( Data.TryGetValue( key, out var value ) )
		{
			if ( value is int i )
				return i;
		}

		Log.Warning( $"Could not find key {key}" );

		return 0;
	}

	[Pure]
	public string GetDataString( string key )
	{
		if ( Data.TryGetValue( key, out var value ) )
		{
			if ( value is string s )
				return s;
		}

		return "";
	}

	[Pure]
	public float GetDataFloat( string key )
	{
		if ( Data.TryGetValue( key, out var value ) )
		{
			if ( value is float f )
				return f;
		}

		return 0;
	}

	public void JumpToId( string id )
	{
		var node = Dialogue.Nodes.Find( x => x.Id == id );
		if ( node != null )
		{
			Log.Info( $"Jumping to {id}" );
			CurrentNode = node;
			Read();
		}
		else
		{
			Log.Warning( $"Could not find node with id {id}" );
		}
	}

	private void Read()
	{
		Log.Info( $"Reading {CurrentNode}" );
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

		if ( choice.OnSelect != null )
		{
			Log.Info( $"Running custom action for {choice.Label}" );
			choice.OnSelect( this, Data, null, null, CurrentNode, choice );
		}
		else
		{
			CurrentChoice = choice;
			CurrentNode = choice.Nodes.FirstOrDefault();
			Read();
		}
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Text );
	}
}
