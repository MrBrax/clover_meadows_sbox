namespace Clover.Ui;

public partial class PaintUi
{
	private Stack<byte[]> UndoStack = new(30);
	private Stack<byte[]> RedoStack = new(30);

	private void Undo()
	{
		if ( UndoStack.Count == 0 )
		{
			Log.Info( "Nothing to undo" );
			return;
		}

		PushRedo();

		DrawTextureData = UndoStack.Pop();
		PushByteDataToTexture();
	}

	private void Redo()
	{
		if ( RedoStack.Count == 0 )
		{
			Log.Info( "Nothing to redo" );
			return;
		}

		DrawTextureData = RedoStack.Pop();
		PushByteDataToTexture();
	}

	private void PushUndo()
	{
		UndoStack.Push( DrawTextureData.ToArray() );
		Log.Info( $"Undo stack size: {UndoStack.Count}" );
		UndoStack.TrimExcess();
	}

	private void PushRedo()
	{
		RedoStack.Push( DrawTextureData.ToArray() );
		Log.Info( $"Redo stack size: {RedoStack.Count}" );
		RedoStack.TrimExcess();
	}
}
