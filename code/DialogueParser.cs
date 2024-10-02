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
		public int Indent { get; set; }
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
		public List<Choice> Choices { get; set; } = new();
	}


	public class Choice
	{
		public string Text { get; set; }
		public int TargetIndex { get; set; }
		public string TargetLabel { get; set; }
	}
	
	public class DialogueLine
	{
		public int Index { get; set; }
		public string Speaker { get; set; }
		public string Text { get; set; }
		public BaseNode Node { get; set; }
	}

	private string _text;
	private string[] _lines;
	private int _currentSymbolIndex;
	private int _currentLine;
	private int _currentIndent;
	private Dictionary<string, string> _meta = new();
	
	public Dictionary<string, object> Variables { get; set; } = new();

	public void Load( string text )
	{
		_text = text;
		_lines = _text.Split( '\n' );
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
		_currentSymbolIndex = 0;

		var line = new DialogueLine();
		line.Index = _currentLine;
		line.Speaker = "Narrator";
		line.Text = "";
		line.Node = new BaseNode();
		
		var hasCheckedIndent = false;
		var indent = 0;
		
		
		/*while ( _currentSymbolIndex < _text.Length )
		{
			var symbol = _text[_currentSymbolIndex];
			
			// end of line
			if ( symbol == '\n' )
			{
				_currentSymbolIndex++;
				break;
			}
			
			// check for indent
			if ( !hasCheckedIndent )
			{
				if ( symbol == ' ' )
				{
					indent++;
					_currentSymbolIndex++;
					continue;
				}
				else
				{
					hasCheckedIndent = true;
				}
			}
			
			// check for choices
			if ( symbol == '-' && _text[_currentSymbolIndex + 1] == '>' )
			{
				line.Node = new ChoiceNode();
				line.Node.Indent = indent;
				_currentSymbolIndex += 2;
				
				// add first option
				var text = "";
				while ( _text[_currentSymbolIndex] != '\n' && _currentSymbolIndex < _text.Length )
				{
					text += _text[_currentSymbolIndex];
					_currentSymbolIndex++;
				}
				var choice = new Choice();
				choice.Text = text.Trim();
				((ChoiceNode)line.Node).Choices.Add( choice );
				
				// find next option
				
				
				
			}
			
			

			
		}*/
		
		var activeLine = _lines.ElementAtOrDefault( _currentLine );
		if ( activeLine == null )
			return null;
		
		// check for indent
		_currentIndent = GetIndent( activeLine );
		
		// check for choices
		if ( activeLine[_currentIndent] == '-' && activeLine[_currentIndent + 1] == '>' )
		{
			line.Node = new ChoiceNode();
			line.Node.Indent = _currentIndent;
			_currentSymbolIndex += 2;
			
			// add first option
			/*var text = "";
			while ( activeLine[_currentSymbolIndex] != '\n' && _currentSymbolIndex < activeLine.Length )
			{
				text += activeLine[_currentSymbolIndex];
				_currentSymbolIndex++;
			}*/
			var text = GetString( activeLine, _currentSymbolIndex, activeLine.Length - _currentSymbolIndex );
			_currentSymbolIndex += text.Length;
			
			var choice = new Choice();
			choice.Text = text.Trim();
			choice.TargetIndex = _currentLine + 1;
			((ChoiceNode)line.Node).Choices.Add( choice );
			
			// find next option
			while ( _currentLine < _lines.Length )
			{
				_currentLine++;
				activeLine = _lines.ElementAtOrDefault( _currentLine );
				if ( activeLine == null )
					break;
				
				// check for indent
				var indent = 0;
				while ( activeLine[indent] == ' ' )
				{
					indent++;
				}
				
				if ( indent <= _currentIndent )
				{
					_currentLine--;
					break;
				}
				
				// add option
				_currentSymbolIndex = indent;
				text = "";
				while ( activeLine[_currentSymbolIndex] != '\n' && _currentSymbolIndex < activeLine.Length )
				{
					text += activeLine[_currentSymbolIndex];
					_currentSymbolIndex++;
				}
				choice = new Choice();
				choice.Text = text.Trim();
				((ChoiceNode)line.Node).Choices.Add( choice );
			}
		}
		else
		{
			// find speaker
			var speaker = "";
			while ( activeLine[_currentSymbolIndex] != ':' && _currentSymbolIndex < activeLine.Length )
			{
				speaker += activeLine[_currentSymbolIndex];
				_currentSymbolIndex++;
			}
			line.Speaker = speaker.Trim();
			_currentSymbolIndex++;
			
			// find text
			while ( _currentSymbolIndex < activeLine.Length )
			{
				line.Text += activeLine[_currentSymbolIndex];
				_currentSymbolIndex++;
			}
		}
		
		
		
		_currentLine++;
		return line;
	}
	
	private int GetIndent( string line )
	{
		var indent = 0;
		while ( line[indent] == ' ' )
		{
			indent++;
		}
		return indent;
	}
	
	private string GetString( string line, int start, int length )
	{
		var str = "";
		for ( var i = start; i < start + length; i++ )
		{
			str += line[i];
		}
		return str;
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
