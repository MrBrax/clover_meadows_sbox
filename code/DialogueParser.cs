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

public class DialogueParser
{
	
	Dialogue dialogue = new();

	private string[] lines;
	private int lineIndex;
	private int currentIndentLevel;
	private int previousIndentLevel;
	// private Dialogue.BaseNode currentNode;
	private Stack<Dialogue.BaseNode> nodeStack = new();

	public Dialogue Parse( string text )
	{
		
		dialogue = new();
		
		lines = text.Split( '\n' );
		
		lineIndex = 0;
		
		foreach ( var line in lines )
		{
			ParseLine( line );
			lineIndex++;
		}
		
		return dialogue;
		
	}

	private void ParseLine( string line )
	{
		// Skip empty lines
		if ( string.IsNullOrWhiteSpace( line ) )
			return;
		
		// Skip comments
		if ( line.StartsWith( "//" ) )
			return;
		
		// Indent level
		currentIndentLevel = 0;
		while ( line[currentIndentLevel] == ' ' )
			currentIndentLevel++;
		
		// Remove indentation
		line = line.TrimStart();
		
		// Check if we are in a node
		if ( nodeStack.Count > 0 )
		{
			// Check if we are still in the same node
			if ( currentIndentLevel > previousIndentLevel )
			{
				// Still in the same node
				ParseNodeLine( line );
			}
			else
			{
				// End of node
				nodeStack.Pop();
			}
		}
		else
		{
			// Check if we are starting a new node
			if ( currentIndentLevel == 0 )
			{
				// Start a new node
				ParseNodeLine( line );
			}
		}
		
	}

	private void ParseNodeLine( string line )
	{
		// Check if we are starting a new node
		if ( line.StartsWith( "->" ) )
		{
			// Create a new node
			var node = new Dialogue.TextNode();
			node.Label = line.Substring( 2 ).Trim();
			nodeStack.Push( node );
			dialogue.Nodes.Add( node );
		}
		else if ( line.StartsWith( ">>" ) )
		{
			// Create a new node
			var node = new Dialogue.ChoiceNode();
			node.Text = line.Substring( 2 ).Trim();
			nodeStack.Push( node );
			dialogue.Nodes.Add( node );
		}
		else
		{
			// Add line to current node
			var node = nodeStack.Peek();
			if ( node is Dialogue.TextNode textNode )
			{
				textNode.Body += line + "\n";
			}
		}
	}
		
}
