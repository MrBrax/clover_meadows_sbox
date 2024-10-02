using System;

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
		public DialogueLine Line { get; set; }
		public int Indent { get; set; }
		public string Label { get; set; }
		public string Speaker { get; set; }
		
		public BaseNode( DialogueLine line )
		{
			Line = line;
		}
	}

	public class TextNode : BaseNode
	{
		public string Body { get; set; }

		public TextNode(DialogueLine line) : base(line)
		{
		}
	}

	public class ChoiceNode : BaseNode
	{
		public string Text { get; set; }
		public List<Choice> Choices { get; set; } = new();

		public ChoiceNode(DialogueLine line) : base(line)
		{
		}
	}

	public class LogicNode : BaseNode
	{
		// public string Logic { get; set; }
		public List<string> Logic { get; set; } = new();

		/// <summary>
		///  Run the logic and return the line index to jump to
		/// </summary>
		/// <returns></returns>
		public int Run()
		{
			var result = 0;
			
			foreach ( var logic in Logic )
			{
				var parts = logic.Split( ' ' );
				if ( parts.Length < 3 )
				{
					Log.Error( $"Invalid logic: {logic}" );
					continue;
				}

				var variable = parts[1].Substring( 1 );
				var value = parts[2];
				var comparison = parts[0];

				if ( !Line.Parser.Variables.ContainsKey( variable ) )
				{
					Log.Error( $"Variable not found: {variable}" );
					continue;
				}

				var variableValue = Line.Parser.Variables[variable];
				var compareValue = Convert.ChangeType( value, variableValue.GetType() );

				switch ( comparison )
				{
					case "==":
						if ( variableValue.Equals( compareValue ) )
						{
							result = int.Parse( parts[3] );
						}
						break;
					case "!=":
						if ( !variableValue.Equals( compareValue ) )
						{
							result = int.Parse( parts[3] );
						}
						break;
					case ">":
						if ( (int)variableValue > (int)compareValue )
						{
							result = int.Parse( parts[3] );
						}
						break;
					case "<":
						if ( (int)variableValue < (int)compareValue )
						{
							result = int.Parse( parts[3] );
						}
						break;
					case ">=":
						if ( (int)variableValue >= (int)compareValue )
						{
							result = int.Parse( parts[3] );
						}
						break;
					case "<=":
						if ( (int)variableValue <= (int)compareValue )
						{
							result = int.Parse( parts[3] );
						}
						break;
				}
			}
			
			return result;
			
		}

		public LogicNode(DialogueLine line) : base(line)
		{
		}
	}


	public class Choice
	{
		public string Text { get; set; }
		public int TargetIndex { get; set; }
		public string TargetLabel { get; set; }
	}

	public class DialogueLine
	{
		public DialogueParser Parser { get; set; }
		public int Index { get; set; }
		// public string Speaker { get; set; }
		// public string Text { get; set; }
		public BaseNode Node { get; set; }
		
		public DialogueLine()
		{
			
		}
		
		public DialogueLine( DialogueParser parser )
		{
			Parser = parser;
		}
		
		public DialogueLine( DialogueParser parser, BaseNode node )
		{
			Parser = parser;
			Node = node;
		}

		public string Print()
		{
			if ( Node is TextNode textNode )
			{
				return $"{textNode.Speaker}: {textNode.Body}";
			}
			else if ( Node is ChoiceNode choiceNode )
			{
				return $"{choiceNode.Speaker}: {choiceNode.Text}";
			}
			else if ( Node is LogicNode logicNode )
			{
				return $"Logic: {string.Join( ", ", logicNode.Logic )}";
			}
			else
			{
				return "Unknown node";
			}
		}
	}

	private string _text;
	private string[] _lines;
	private int _currentSymbolIndex;
	private int _currentLine;
	private int _currentIndent;
	private Dictionary<string, string> _meta = new();

	public Dictionary<string, object> Variables { get; set; } = new();

	private readonly string[] _comparisonOperators = { "==", "!=", ">", "<", ">=", "<=" };
	private readonly string[] _statements = { "if", "else", "elseif", "endif" };


	public void Load( string text )
	{
		_text = text;
		// _lines = _text.Split( '\n' );
		_lines = _text.Split( new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None );
		_currentSymbolIndex = 0;
		ParseMeta();
	}

	private void ParseMeta()
	{
		var meta = new Dictionary<string, string>();
		var i = 0;
		foreach ( var line in _lines )
		{
			Log.Info( $"Parsing meta: {line} ({line.Length})" );
			if ( line == "---" )
				break;

			var parts = line.Split( ':' );
			if ( parts.Length == 2 )
			{
				meta[parts[0].Trim()] = parts[1].Trim();
			}

			i++;
		}

		_meta = meta;
		_currentLine = i + 1;
	}
	
	public DialogueLine JumpTo( int index )
	{
		Log.Info( $"Jumping to line {index}" );
		_currentLine = index;
		return Next();
	}

	public DialogueLine Next()
	{
		_currentSymbolIndex = 0;

		var line = new DialogueLine( this );
		line.Index = _currentLine;

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
		{
			Log.Info( $"End of dialogue reached" );
			return null;
		}

		Log.Info( $"Checking line {_currentLine}: {activeLine}" );

		// check for indent
		_currentIndent = GetIndent( activeLine );
		
		activeLine = activeLine.Substring( _currentIndent );

		// check for choices
		if ( GetString( activeLine, 0, 2 ) == "->" )
		{
			Log.Info( $"Checking for choices at line {_currentLine} with indent {_currentIndent}" );
			line.Node = new ChoiceNode( line );
			line.Node.Indent = _currentIndent;
			_currentSymbolIndex += 2;

			// add first option
			/*var text = "";
			while ( activeLine[_currentSymbolIndex] != '\n' && _currentSymbolIndex < activeLine.Length )
			{
				text += activeLine[_currentSymbolIndex];
				_currentSymbolIndex++;
			}*/
			var text = GetInterpolatedString( activeLine, _currentSymbolIndex, activeLine.Length - _currentSymbolIndex );
			_currentSymbolIndex += text.Length;

			var choice = new Choice();
			choice.Text = text.Trim();
			choice.TargetIndex = _currentLine + 1;
			((ChoiceNode)line.Node).Choices.Add( choice );

			// find the rest of the options
			/*var lineSearch = _currentLine + 1;
			while ( true )
			{
				var nextLine = _lines.ElementAtOrDefault( lineSearch );
				if ( nextLine == null )
					break;

				var nextIndent = GetIndent( nextLine );
				if ( nextIndent != _currentIndent )
					break;

				if ( GetString( nextLine, nextIndent, 2 ) != "->" )
				{
					break;
				}

				_currentSymbolIndex = nextIndent + 2;
				text = GetInterpolatedString( nextLine, _currentSymbolIndex, nextLine.Length - _currentSymbolIndex );
				_currentSymbolIndex += text.Length;

				choice = new Choice();
				choice.Text = text.Trim();
				choice.TargetIndex = lineSearch + 1;
				((ChoiceNode)line.Node).Choices.Add( choice );
			}*/
			
			// find next line with same indent
			var searchLine = _currentLine + 1;
			while ( true )
			{
				var nextLine = GetNextLineWithIndent( searchLine, _currentIndent );
				if ( nextLine == -1 )
					break;

				var nextLineText = _lines[nextLine];
				if ( GetString( nextLineText, _currentIndent, 2 ) != "->" )
					break;

				_currentSymbolIndex = _currentIndent + 2;
				text = GetInterpolatedString( nextLineText, _currentSymbolIndex, nextLineText.Length - _currentSymbolIndex );
				_currentSymbolIndex += text.Length;

				choice = new Choice();
				choice.Text = text.Trim();
				choice.TargetIndex = nextLine + 1;
				((ChoiceNode)line.Node).Choices.Add( choice );
				
				searchLine = nextLine + 1;
			}
			
			foreach ( var c in ((ChoiceNode)line.Node).Choices )
			{
				Log.Info( $"Parsed choice: {c.Text} -> {c.TargetIndex}" );
			}

			return line;
		}


		// check for logic
		if ( GetString( activeLine, 0, 2 ) == "<<" )
		{
			Log.Info( $"Checking for logic at line {_currentLine} with indent {_currentIndent}" );
			_currentSymbolIndex += 2;
			
			// find the end of the logic comparison
			var logic = "";
			while ( GetString( activeLine, _currentSymbolIndex, 2 ) != ">>" )
			{
				logic += activeLine[_currentSymbolIndex];
				_currentSymbolIndex++;
			}

			_currentSymbolIndex += 2;
			
			Log.Info( $"Found logic: {logic}" );

			line.Node = new LogicNode( line );
			((LogicNode)line.Node).Logic.Add( logic.Trim() ); // parse later
			
			if ( logic.Trim().StartsWith( "if " ) )
			{
				Log.Info( $"Found if statement: {logic}" );
				
				var searchLine = _currentLine + 1;
				while ( true )
				{
					var nextLine = GetNextLineWithIndent( searchLine, _currentIndent );
					if ( nextLine == -1 )
						break;

					var nextLineText = _lines[nextLine];

					if ( GetString( nextLineText, _currentIndent, 2 ) == "<<" )
					{
						Log.Info( $"Found nested logic at line {nextLine}" );
						_currentSymbolIndex = _currentIndent + 2;
						var nestedLogic = "";
						while ( GetString( nextLineText, _currentSymbolIndex, 2 ) != ">>" )
						{
							nestedLogic += nextLineText[_currentSymbolIndex];
							_currentSymbolIndex++;
						}

						_currentSymbolIndex += 2;
						((LogicNode)line.Node).Logic.Add( nestedLogic.Trim() );
					}
					else
					{
						break;
					}

					searchLine = nextLine + 1;
				}
				
			}
			else
			{
				Log.Error( $"Unknown logic statement: {logic}" );
			}
			
			foreach ( var l in ((LogicNode)line.Node).Logic )
			{
				Log.Info( $"Parsed logic: {l}" );
			}

			var result = ((LogicNode)line.Node).Run();
			
			JumpTo( result );
			
			return line;
		}
		
		line.Node = new TextNode( line );

		// check for speaker
		Log.Info( $"Checking for speaker at line {_currentLine} with indent {_currentIndent}" );
		var speaker = "";
		while ( activeLine[_currentSymbolIndex] != ':' && _currentSymbolIndex < activeLine.Length )
		{
			speaker += activeLine[_currentSymbolIndex];
			_currentSymbolIndex++;
		}
		Log.Info( $"Found speaker: {speaker}" );

		_currentSymbolIndex++; // skip ':'
		// line.Speaker = ParseVariables( speaker.Trim() );
		// Log.Info( $"Parsed speaker: {line.Speaker}" );
		line.Node.Speaker = ParseVariables( speaker.Trim() );

		// check for text
		Log.Info( $"Checking for text at line {_currentLine} with indent {_currentIndent}" );
		var linetext = "";
		while ( _currentSymbolIndex < activeLine.Length )
		{
			linetext += activeLine[_currentSymbolIndex];
			_currentSymbolIndex++;
		}

		// line.Text = ParseVariables( linetext.Trim() );
		// Log.Info( $"Parsed line {_currentLine}: {line.Speaker} - {line.Text}" );
		((TextNode)line.Node).Body = ParseVariables( linetext.Trim() );

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

	private int GetNextLineWithIndent( int start, int indent )
	{
		for ( var i = start; i < _lines.Length; i++ )
		{
			if ( GetIndent( _lines[i] ) == indent )
				return i;

			// if we go back to a lower indent, we've gone too far
			if ( GetIndent( _lines[i] ) < indent )
				return -1;
		}

		return -1;
	}

	private string GetString( string line, int start, int length )
	{
		return new string( line.Skip( start ).Take( length ).ToArray() );
	}
	
	private string GetInterpolatedString( string line, int start, int length )
	{
		var str = new string( line.Skip( start ).Take( length ).ToArray() );
		return ParseVariables( str );
	}

	private string ParseVariables( string text )
	{
		foreach ( var variable in Variables )
		{
			text = text.Replace( "{$" + variable.Key + "}", variable.Value.ToString() );
		}
		
		// warn for missing variables
		if ( text.Contains( "{$" ) )
		{
			Log.Warning( $"Missing variables in text: {text}" );
		}
		
		return text;
	}

	[Button( "Test" )]
	public void Test()
	{
		Load( SampleDialogue );
		Variables["NpcName"] = "Bob";
		Variables["ItemName"] = "Sword";
		Variables["ItemPrice"] = 10;
		/*while ( true )
		{
			var line = Next();
			if ( line == null )
				break;
			Log.Info( $"[{line.Speaker}] {line.Text}" );
		}*/
		
		var line = Next();
		Log.Info( $"Result1: {line.Print()}" );
		
		var line2 = Next();
		Log.Info( $"Result2: {line2.Print()}" );
		
		var choice = ((ChoiceNode)line2.Node).Choices[0];
		Log.Info( $"Choice: {choice.Text} -> {choice.TargetIndex}" );
		
		var line3 = JumpTo( choice.TargetIndex );
		Log.Info( $"Result3: {line3.Print()}" );
		
	}
}
