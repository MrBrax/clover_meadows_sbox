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

		Data = new Dictionary<string, object> { { "test", 123 }, { "money", 100 }, { "price", 200 }, };

		CurrentNode = Dialogue.Nodes[0];
		CurrentNode.OnEnter?.Invoke( this, Data, null, null, CurrentNode, null );
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

	/// <summary>
	///  Searches for a node with the given id recursively and sets it as the current node.
	/// </summary>
	/// <param name="id"></param>
	public void JumpToId( string id )
	{
		Dialogue.DialogueNode FindNode( List<Dialogue.DialogueNode> nodes )
		{
			foreach ( var node in nodes )
			{
				if ( node.Id == id )
					return node;

				if ( node.Choices.Count > 0 )
				{
					foreach ( var choice in node.Choices )
					{
						var found = FindNode( choice.Nodes );
						if ( found != null )
							return found;
					}
				}
			}

			return null;
		}

		var node = FindNode( Dialogue.Nodes );
		if ( node != null )
		{
			CurrentNode?.OnExit?.Invoke( this, Data, null, null, CurrentNode, null );
			CurrentNode = node;
			CurrentNode.OnEnter?.Invoke( this, Data, null, null, CurrentNode, null );
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
		_textTarget = ParseVariables( CurrentNode.Text );
	}

	private string ParseVariables( string text )
	{
		var result = text;

		foreach ( var key in Data.Keys )
		{
			Log.Info( $"Replacing {{{{key}}}} with {Data[key]}" );
			result = result.Replace( "{{" + key + "}}", Data[key].ToString() );
		}

		return result;
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
			CurrentNode.OnExit?.Invoke( this, Data, null, null, CurrentNode, null );
			CurrentNode = choice.Nodes.FirstOrDefault();
			if ( CurrentNode != null )
			{
				CurrentNode.OnEnter?.Invoke( this, Data, null, null, CurrentNode, null );
				Read();
			}
			else
			{
				Log.Error( "No nodes found for choice" );
			}
		}
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Text );
	}
}
