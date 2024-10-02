namespace Sandbox;

public class DialogueParser
{
	
	Dialogue dialogue = new();

	public Dialogue Parse( string text )
	{
		
		dialogue = new();
		
		var lines = text.Split( '\n' );
		
		int characterIndex = 0;
		int lineIndex = 0;
		int indent = 0;
		int lastIndent = 0;
		
		var stack = new Stack<Dialogue.BaseNode>();
		
		foreach ( var line in lines )
		{
			// detect indent with 4 spaces
			indent = 0;
			while ( line[indent] == ' ' )
			{
				indent++;
			}
			
			if ( indent < lastIndent )
			{
				// pop stack
				while ( indent < lastIndent )
				{
					stack.Pop();
					lastIndent -= 4;
				}
			}
			
			// remove indent
			var lineText = line.Substring( indent );
			
			// start parsing
			if ( line)
			
			
			
		}
		
		
	}
	
}
