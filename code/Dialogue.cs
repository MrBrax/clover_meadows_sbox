using System.Text.Json.Serialization;

namespace Sandbox;

[GameResource("Dialogue", "dlg", "dialogue")]
public class Dialogue : GameResource
{
	
	
	
	public class BaseNode
	{
		[Property, ReadOnly, JsonIgnore] public string Type => GetType().Name;
		[Property] public string Label { get; set; }
		[Property] public string Speaker { get; set; }
	}
	
	public class TextNode : BaseNode
	{
		[Property, TextArea] public string Body { get; set; }
	}
	
	public class ChoiceNode : BaseNode
	{
		[Property] public string Text { get; set; }
		[Property] public List<Choice> Choices { get; set; }
	}
	
	
	public class Choice
	{
		[Property] public string Text { get; set; }
		[Property] public string Target { get; set; }
	}
	
	
	[Property, InlineEditor] public List<BaseNode> Nodes { get; set; } = new();
	
}
