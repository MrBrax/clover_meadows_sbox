using System;
using Clover.Data;
using Clover.Npc;
using Clover.Player;
using Sandbox.UI;

namespace Clover;

public partial class DialogueWindow
{
	[Property] public Dialogue Dialogue { get; set; }

	[Property] public Dictionary<string, object> Data { get; set; } = new();

	[Property] public Dictionary<string, Action> Actions { get; set; } = new();

	[Property, ReadOnly] public List<Dialogue.DialogueNode> CurrentNodeList { get; set; }

	// [Property, ReadOnly] public Dialogue.DialogueChoice CurrentChoice { get; set; }
	[Property, ReadOnly] public List<GameObject> CurrentTargets { get; set; } = new();

	public int CurrentNodeIndex;

	public Dialogue.DialogueNode CurrentNode
	{
		get => CurrentNodeList.ElementAtOrDefault( CurrentNodeIndex );
		// set => CurrentNodeIndex = Dialogue.Nodes.IndexOf( value );
	}

	public bool IsOnLastNode => CurrentNodeIndex == CurrentNodeList.Count - 1;

	public string Text { get; set; } = "";
	public string Name { get; set; } = "";

	private int _textIndex;
	private string _textTarget;
	private TimeSince _lastLetter;
	// private bool _skipped;

	public Action OnDialogueEnd { get; set; }


	protected override void OnStart()
	{
		base.OnStart();

		/*Data = new Dictionary<string, object> { { "test", 123 }, { "money", 100 }, { "price", 200 }, };

		CurrentNodeList = Dialogue.Nodes;
		CurrentNodeIndex = 0;
		CurrentNode.OnEnter?.Invoke( this, null, null, CurrentNode, null );
		Read();*/

		// LoadDialogue( ResourceLibrary.GetAll<Dialogue>().First() );

		Panel.ButtonInput = PanelInputType.UI;
	}

	public void LoadDialogue( Dialogue dialogue )
	{
		Dialogue = dialogue;
		CurrentNodeList = Dialogue.Nodes;
		CurrentNodeIndex = 0;
		CurrentNode.OnEnter?.Invoke( this, PlayerCharacter.Local, CurrentTargets, CurrentNode, null );
		Read();
	}

	[Pure]
	[Icon( "description" )]
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
	[Icon( "description" )]
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
	[Icon( "description" )]
	public float GetDataFloat( string key )
	{
		if ( Data.TryGetValue( key, out var value ) )
		{
			if ( value is float f )
				return f;
		}

		return 0;
	}

	[Pure]
	[Icon( "description" )]
	public bool GetDataBool( string key )
	{
		if ( Data.TryGetValue( key, out var value ) )
		{
			if ( value is bool b )
				return b;
		}

		return false;
	}

	[Pure]
	public void SetData( string key, object value )
	{
		Data[key] = value;
	}

	public void AddTarget( GameObject target )
	{
		CurrentTargets ??= new List<GameObject>();
		CurrentTargets.Add( target );
	}

	public void SetTarget( int index, GameObject target )
	{
		CurrentTargets ??= new List<GameObject>();

		if ( index >= CurrentTargets.Count )
		{
			CurrentTargets.Add( target );
		}
		else
		{
			CurrentTargets[index] = target;
		}
	}

	public BaseNpc GetTarget( int index )
	{
		return CurrentTargets.ElementAtOrDefault( index )?.Components.Get<BaseNpc>();
	}

	public void ClearTargets()
	{
		CurrentTargets.Clear();
	}

	public void SetAction( string key, Action action )
	{
		Actions[key] = action;
	}

	/// <summary>
	///  Runs an action by key. Add actions with <see cref="SetAction"/>.
	/// </summary>
	/// <param name="key"></param>
	[Property]
	[Icon( "rocket_launch" )]
	public void RunAction( string key )
	{
		if ( Actions.TryGetValue( key, out var action ) )
		{
			action();
		}
		else
		{
			Log.Warning( $"Could not find action {key}" );
		}
	}

	/// <summary>
	///  Searches for a node with the given id recursively and sets it as the current node.
	/// </summary>
	/// <param name="id"></param>
	[Property]
	[Icon( "search" )]
	public void JumpToId( string id )
	{
		/*Dialogue.DialogueNode FindNode( List<Dialogue.DialogueNode> nodes )
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
			CurrentNodeList = null;
			CurrentNode.OnEnter?.Invoke( this, Data, null, null, CurrentNode, null );
			Read();
		}
		else
		{
			Log.Warning( $"Could not find node with id {id}" );
		}*/

		( Dialogue.DialogueNode node, List<Dialogue.DialogueNode> list ) FindNode( List<Dialogue.DialogueNode> nodes )
		{
			foreach ( var node in nodes )
			{
				if ( node.Id == id )
					return (node, nodes);

				if ( node.Choices.Count > 0 )
				{
					foreach ( var choice in node.Choices )
					{
						var found = FindNode( choice.Nodes );
						if ( found.node != null )
							return found;
					}
				}
			}

			return (null, null);
		}

		var (node, list) = FindNode( Dialogue.Nodes );

		if ( node != null )
		{
			CurrentNode?.OnExit?.Invoke( this, PlayerCharacter.Local, CurrentTargets, CurrentNode, null );
			CurrentNodeList = list;
			CurrentNodeIndex = list.IndexOf( node );
			Log.Info( $"Jumped to node {node.Id}, index {CurrentNodeIndex}/{list.Count}" );
			if ( CurrentNode != null )
			{
				CurrentNode.OnEnter?.Invoke( this, PlayerCharacter.Local, CurrentTargets, CurrentNode, null );
				Read();
			}
			else
			{
				Log.Error( "JumpToId: No nodes found for choice" );
			}
		}
		else
		{
			Log.Warning( $"Could not find node with id {id}" );
		}
	}

	public void Advance()
	{
		if ( IsOnLastNode && CurrentNode.Choices.Count == 0 )
		{
			Log.Info( "Closing window" );
			End();
			return;
		}

		Log.Info( $"Choices: {CurrentNode.Choices.Count}, index: {CurrentNodeIndex}/{CurrentNodeList.Count}" );

		// go to the next node if there are no choices

		if ( CurrentNode.Choices.Count == 0 )
		{
			CurrentNode.OnExit?.Invoke( this, PlayerCharacter.Local, CurrentTargets, CurrentNode, null );
			CurrentNodeIndex++;
			if ( CurrentNode != null )
			{
				// CurrentNode.OnEnter?.Invoke( this, null, null, CurrentNode, null );
				// Read();
				if ( CurrentNode.IsHidden )
				{
					Advance();
					return;
				}

				Read();
			}
			else
			{
				Log.Error( "Advance: No nodes found for choice" );
			}

			return;
		}

		Log.Error( "Choices found" );
	}

	private void Read()
	{
		Log.Info( $"Reading {CurrentNode}" );
		Text = "";
		_textIndex = 0;
		_textTarget = ParseVariables( CurrentNode.Text );

		if ( CurrentNode.IsPlayer )
		{
			Name = "Player";
		}
		else if ( CurrentTargets.Count > 0 )
		{
			// var speaker = CurrentTargets.ElementAtOrDefault( CurrentNode.Speaker );
			// Name = speaker?.Name ?? "Unknown";

			var speaker = CurrentTargets.ElementAtOrDefault( CurrentNode.Speaker );

			if ( !speaker.IsValid() )
			{
				Log.Error( "Speaker is not valid" );
				Name = "UNKNOWN";
			}
			else if ( speaker.Components.TryGet<BaseNpc>( out var npc ) )
			{
				Name = npc.Name;
			}
			else if ( speaker.Components.TryGet<PlayerCharacter>( out var player ) )
			{
				Name = player.PlayerName;
			}
			else
			{
				Name = speaker.Name;
			}
		}
		else
		{
			Log.Error( "No targets found" );
			Name = "NO TARGETS";
		}

		// _skipped = false;
		// Panel.FlashClass( "noclick", 0.1f );
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
		if ( _textTarget == null ) return;
		if ( _lastLetter <= 0.05f )
		{
			return;
		}

		_lastLetter = 0;
		if ( _textIndex >= _textTarget.Length )
		{
			return;
		}

		Text += _textTarget[_textIndex];

		OnLetterTyped( _textTarget[_textIndex] );
		_textIndex++;
	}

	private char[] _letters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();

	private void OnLetterTyped( char letter )
	{
		switch ( letter )
		{
			case '1':
				letter = 'o';
				break;
			case '2':
				letter = 't';
				break;
			case '3':
				letter = 't';
				break;
			case '4':
				letter = 'f';
				break;
			case '5':
				letter = 'f';
				break;
			case '6':
				letter = 's';
				break;
			case '7':
				letter = 's';
				break;
			case '8':
				letter = 'e';
				break;
			case '9':
				letter = 'n';
				break;
			case '0':
				letter = 'z';
				break;
		}

		if ( !char.IsLetter( letter ) ) return;

		var s = SoundFile.Load( "sounds/speech/alphabet/" + letter.ToString().ToUpper() + ".vsnd" );

		var h = Sound.PlayFile( s );
		h.ListenLocal = true;
		h.Pitch = Random.Shared.Float( 1.9f, 2.1f );
	}

	private void OnClick( PanelEvent e )
	{
		// if ( _textIndex < 2 ) return;

		// Input.ReleaseActions();

		e.StopPropagation();

		// If we're still typing, finish the text
		if ( Text.Length < _textTarget.Length )
		{
			Log.Info( "Skipping text" );
			// _skipped = true;
			_textIndex = _textTarget.Length;
			Text = _textTarget;
			return;
		}

		// if we're at the last node, close the window
		if ( IsOnLastNode && CurrentNode.Choices.Count == 0 )
		{
			Log.Info( "Closing window" );
			End();
			return;
		}

		Advance();
	}

	private void End()
	{
		Enabled = false;
		ClearTargets();
		CurrentNodeList = null;
		CurrentNodeIndex = 0;
		// CurrentNode.OnExit?.Invoke( this, null, null, CurrentNode, null );
		OnDialogueEnd?.Invoke();
		OnDialogueEnd = null;
	}

	private void OnChoice( Dialogue.DialogueChoice choice )
	{
		Log.Info( $"Selected {choice.Label}" );

		if ( choice.OnSelect != null )
		{
			Log.Info( $"Running custom action for {choice.Label}" );
			choice.OnSelect( this, PlayerCharacter.Local, CurrentTargets, CurrentNode, choice );
		}
		else
		{
			if ( choice.Nodes.Count == 0 && string.IsNullOrEmpty( choice.JumpToId ) )
			{
				Log.Error( "OnChoice1: No nodes found for choice" );
				End();
				return;
			}

			CurrentNode.OnExit?.Invoke( this, PlayerCharacter.Local, CurrentTargets, CurrentNode, null );

			if ( !string.IsNullOrEmpty( choice.JumpToId ) )
			{
				JumpToId( choice.JumpToId );
				return;
			}

			CurrentNodeList = choice.Nodes;
			CurrentNodeIndex = 0;
			// CurrentChoice = choice;

			if ( CurrentNode != null )
			{
				CurrentNode.OnEnter?.Invoke( this, PlayerCharacter.Local, CurrentTargets, CurrentNode, null );
				Read();
			}
			else
			{
				Log.Error( "OnChoice2: No nodes found for choice" );
			}
		}
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Text );
	}
}
