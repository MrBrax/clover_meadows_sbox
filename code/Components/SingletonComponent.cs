namespace Clover.Components;

/// <summary>
///  https://github.com/Facepunch/sbox-hc1/blob/main/Libraries/facepunch.singletons/Code/SingletonComponent.cs
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class SingletonComponent<T> : Component, IHotloadManaged
	where T : SingletonComponent<T>
{
	[SkipHotload]
	public static T Instance { get; private set; }

	void IHotloadManaged.Destroyed( Dictionary<string, object> state )
	{
		state["IsActive"] = Instance == this;
	}

	void IHotloadManaged.Created( IReadOnlyDictionary<string, object> state )
	{
		if ( state.GetValueOrDefault( "IsActive" ) is true )
		{
			Instance = (T)this;
		}
	}

	protected override void OnAwake()
	{
		if ( Active )
		{
			Instance = (T)this;
		}
	}

	protected override void OnDestroy()
	{
		if ( Instance == this )
		{
			Instance = null;
		}
	}
}
