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

		Log.Info( $"UNDO | Size: {UndoStack.Count}" );

		PushRedo();

		DrawTextureData = UndoStack.Pop();
		PushByteDataToTexture();

		_isDrawing = false;
		_isMoving = false;
		_lastBrushPosition = null;
	}

	private void Redo()
	{
		if ( RedoStack.Count == 0 )
		{
			Log.Info( "Nothing to redo" );
			return;
		}

		Log.Info( $"REDO | Size: {RedoStack.Count}" );

		PushUndo();

		DrawTextureData = RedoStack.Pop();
		PushByteDataToTexture();

		_isDrawing = false;
		_isMoving = false;
		_lastBrushPosition = null;
	}

	private void PushUndo()
	{
		if ( DrawTextureData == null )
		{
			Log.Error( "DrawTextureData is null" );
			return;
		}

		if ( DrawTextureData.Length == 0 )
		{
			Log.Error( "DrawTextureData is empty" );
			return;
		}

		UndoStack.Push( DrawTextureData.ToArray() );
		Log.Info( $"PUSHED UNDO | Size: {UndoStack.Count}" );
		UndoStack.TrimExcess();
	}

	private void PushRedo()
	{
		RedoStack.Push( DrawTextureData.ToArray() );
		Log.Info( $"PUSHED REDO | Size: {RedoStack.Count}" );
		RedoStack.TrimExcess();
	}
}
