namespace Clover.Persistence;

public interface IPersistent
{
	/// <summary>
	///  Called when the item is saved to a file or otherwise serialized.
	///  Use <see cref="PersistentItem.SetSaveData"/> to store any data.
	/// </summary>
	/// <param name="item"></param>
	public void OnSave( PersistentItem item );

	/// <summary>
	///  Called when the item is loaded from a file or otherwise deserialized.
	///  Use <see cref="PersistentItem.GetSaveData{T}"/> or <see cref="PersistentItem.TryGetSaveData{T}" /> to retrieve stored data.
	/// </summary>
	/// <param name="item"></param>
	public void OnLoad( PersistentItem item );
}
