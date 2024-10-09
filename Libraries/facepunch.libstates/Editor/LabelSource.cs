namespace Sandbox.States.Editor;

/// <summary>
/// Something that is represented as a label on a state or transition.
/// </summary>
public interface ILabelSource : IDeletable, IDoubleClickable, IValid
{
	string Title { get; }
	string? Description { get; }

	string? Icon { get; }
	string? Text { get; }

	public Color? Color => null;

	void BuildAddContextMenu( global::Editor.Menu menu );
	void BuildModifyContextMenu( global::Editor.Menu menu );
}
