using Clover.Persistence;

namespace Clover.Objects;

public class GiftCarrier : Component
{
	[Property] public float Speed = 50f;

	[Property] public GameObject GiftVisual { get; set; }
	[Property] public GameObject GiftFallingScene { get; set; }
	[Property] public GameObject GiftModelSpawn { get; set; }

	public List<PersistentItem> Items { get; set; } = new();

	private bool _hasDroppedGift;

	protected override void OnFixedUpdate()
	{
		WorldPosition += WorldRotation.Forward * Speed * Time.Delta;

		if ( WorldPosition.x < -1000 || WorldPosition.x > 5000 || WorldPosition.z < -1000 || WorldPosition.z > 5000 )
		{
			DestroyGameObject();
		}
	}
}
