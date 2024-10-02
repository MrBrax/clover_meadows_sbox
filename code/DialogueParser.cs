namespace Sandbox;

/*
Dialogue format, yarn-like:

title: BuyItem
tags:
---
{$NpcName}: That is a [blue]{$ItemName}[/blue]. It costs [green]{$ItemPrice}[/green] clovers. Do you want to buy it?
-> Yes
    <<if $PlayerClovers >= $ItemPrice>>
        <<DoBuyItem>>
        {$NpcName}: Thank you for your purchase!
    <<else>>
        {$NpcName}: Oh... You don't seem to have enough clovers to buy that. See anything else you like?
    <<endif>>
-> No
    {$NpcName}: Alright, let me know if you change your mind.
*/

public class DialogueParser : Component
{

	public const string SampleDialogue = @"title: BuyItem
tags:
---
{$NpcName}: That is a [blue]{$ItemName}[/blue]. It costs [green]{$ItemPrice}[/green] clovers. Do you want to buy it?
-> Yes
    <<if $PlayerClovers >= $ItemPrice>>
        <<DoBuyItem>>
        {$NpcName}: Thank you for your purchase!
    <<else>>
        {$NpcName}: Oh... You don't seem to have enough clovers to buy that. See anything else you like?
    <<endif>>
-> No
    {$NpcName}: Alright, let me know if you change your mind.";

	
	public class BaseNode
	{
		public string Label { get; set; }
		public string Speaker { get; set; }
	}

	public class TextNode : BaseNode
	{
		public string Body { get; set; }
	}

	public class ChoiceNode : BaseNode
	{
		public string Text { get; set; }
		public List<Choice> Choices { get; set; }
	}


	public class Choice
	{
		public string Text { get; set; }
		public string Target { get; set; }
	}
	
	public class DialogueLine
	{
		public int Index { get; set; }
		public string Speaker { get; set; }
		public string Text { get; set; }
		public BaseNode Node { get; set; }
	}

	private string _text;
	private int _currentSymbolIndex;
	private int _currentLine;
	private Dictionary<string, string> _meta = new();
	
	public Dictionary<string, object> Variables { get; set; } = new();

	public void Load( string text )
	{
		_text = text;
		_currentSymbolIndex = 0;
		ParseMeta();
	}
	
	private void ParseMeta()
	{
		var meta = _text.Split( "---" );
		if ( meta.Length > 1 )
		{
			var metaLines = meta[0].Split( '\n' );
			foreach ( var line in metaLines )
			{
				var parts = line.Split( ':' );
				if ( parts.Length == 2 )
				{
					_meta[parts[0].Trim()] = parts[1].Trim();
					Log.Info( $"Meta: {parts[0].Trim()} = {parts[1].Trim()}" );
				}
			}
			_currentSymbolIndex = meta[0].Length + 3;
		}
	}
	
	public DialogueLine Next()
	{
		if ( _currentSymbolIndex >= _text.Length )
			return null;

		var line = new DialogueLine();
		line.Index = _currentLine;
		line.Speaker = "Narrator";
		line.Text = "";
		line.Node = new BaseNode();
		
		while ( _currentSymbolIndex < _text.Length )
		{
			var symbol = _text[_currentSymbolIndex];
			if ( symbol == '\n' )
			{
				_currentSymbolIndex++;
				break;
			}

			if ( symbol == '{' )
			{
				var end = _text.IndexOf( '}', _currentSymbolIndex );
				if ( end == -1 )
				{
					Log.Error( $"DialogueParser: Missing '}}' at {_currentSymbolIndex}" );
					break;
				}
				
				var variable = _text.Substring( _currentSymbolIndex + 2, end - _currentSymbolIndex - 3 );
				
				if ( Variables.TryGetValue( variable, out var value ) )
				{
					line.Text += value.ToString();
				}
				else
				{
					Log.Error( $"DialogueParser: Variable '{variable}' not found" );
				}
				
				_currentSymbolIndex = end + 1;
				
			}
			// skip logic for now
			/*else if ( symbol == '[' )
			{
				var end = _text.IndexOf( ']', _currentSymbolIndex );
				if ( end == -1 )
				{
					Log.Error( $"DialogueParser: Missing ']' at {_currentSymbolIndex}" );
					break;
				}
				
				var color = _text.Substring( _currentSymbolIndex + 1, end - _currentSymbolIndex - 1 );
				line.Text += $"<color={color}>";
				_currentSymbolIndex = end + 1;
			}
			
			else if ( symbol == '<' && _text[_currentSymbolIndex + 1] == '<' )
			{
				var end = _text.IndexOf( ">>", _currentSymbolIndex );
				if ( end == -1 )
				{
					Log.Error( $"DialogueParser: Missing '>>' at {_currentSymbolIndex}" );
					break;
				}
				
				var command = _text.Substring( _currentSymbolIndex + 2, end - _currentSymbolIndex - 2 );
				var parts = command.Split( ' ' );
				if ( parts.Length == 1 )
				{
					line.Text += $"[{command}]";
				}
				else
				{
					switch ( parts[0] )
					{
						case "if":
							if ( Variables.TryGetValue( parts[1], out var value ) )
							{
								if ( (bool)value )
								{
									_currentSymbolIndex = end + 2;
								}
								else
								{
									_currentSymbolIndex = _text.IndexOf( "<<endif>>", end );
									if ( _currentSymbolIndex == -1 )
									{
										Log.Error( $"DialogueParser: Missing <<endif>> after {command}" );
										break;
									}
									_currentSymbolIndex += 9;
								}
							}
							else
							{
								Log.Error( $"DialogueParser: Variable '{parts[1]}' not found" );
							}
							break;
						case "endif":
							_currentSymbolIndex = end + 9;
							break;
						default:
							Log.Error( $"DialogueParser: Unknown command '{parts[0]}'" );
							break;
					}
				}
			}
			
			else
			{
				line.Text += symbol;
				_currentSymbolIndex++;
			}*/
			
			else
			{
				line.Text += symbol;
				_currentSymbolIndex++;
			}
		}
		
		_currentLine++;
		return line;
	}
	
	[Button("Test")]
	public void Test()
	{
		Load( SampleDialogue );
		while ( true )
		{
			var line = Next();
			if ( line == null )
				break;
			Log.Info( $"[{line.Speaker}] {line.Text}" );
		}
	}
	
}
