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
	private int _currentIndex;
	private int _currentLine;

	public void Load( string text )
	{
		_text = text;
		_currentIndex = 0;
		ParseMeta();
	}
	
	
	
	public DialogueLine Next()
	{
		if ( _currentIndex >= _text.Length )
			return null;

		var line = new DialogueLine();
		line.Index = _currentIndex;

		// Parse speaker
		if ( _text[_currentIndex] == '$' )
		{
			_currentIndex++;
			var speaker = "";
			while ( _text[_currentIndex] != ':' )
			{
				speaker += _text[_currentIndex];
				_currentIndex++;
			}
			line.Speaker = speaker;
			_currentIndex++;
		}

		// Parse text
		var text = "";
		while ( _text[_currentIndex] != '\n' )
		{
			text += _text[_currentIndex];
			_currentIndex++;
		}
		line.Text = text;
		_currentIndex++;

		// Parse node
		if ( _text[_currentIndex] == '-' )
		{
			_currentIndex += 2;
			var node = new BaseNode();
			while ( _text[_currentIndex] != '\n' )
			{
				// Parse label
				if ( _text[_currentIndex] == '[' )
				{
					_currentIndex++;
					var label = "";
					while ( _text[_currentIndex] != ']' )
					{
						label += _text[_currentIndex];
						_currentIndex++;
					}
					node.Label = label;
					_currentIndex++;
				}

				// Parse speaker
				if ( _text[_currentIndex] == '$' )
				{
					_currentIndex++;
					var speaker = "";
					while ( _text[_currentIndex] != ':' )
					{
						speaker += _text[_currentIndex];
						_currentIndex++;
					}
					node.Speaker = speaker;
					_currentIndex++;
				}

				// Parse body
				if ( _text[_currentIndex] == '{' )
				{
					_currentIndex++;
					var body = "";
					while ( _text[_currentIndex] != '}' )
					{
						body += _text[_currentIndex];
						_currentIndex++;
					}
					_currentIndex++;
				}
			}
			line.Node = node;
		}

		return line;
	}
	
	[Button("Test")]
	public void Test()
	{
		Load( SampleDialogue );
		/*while ( true )
		{*/
			var line = Next();
			/*if ( line == null )
				break;*/
			Log.Info( $"[{line.Speaker}] {line.Text}" );
		/*}*/
	}
	
}
