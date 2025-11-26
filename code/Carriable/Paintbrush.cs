using System.IO;
using System.Text;
using Braxnet;
using Clover.Items;
using Clover.Persistence;
using Clover.Player;
using Clover.Ui;
using Clover.Utilities;

namespace Clover.Carriable;

[Category( "Clover/Carriable" )]
public class Paintbrush : BaseCarriable
{
	[Property] public SoundEvent PaintSound { get; set; }
	[Property] public SoundEvent TextureChangeSound { get; set; }

	public string CurrentTextureName { get; set; }
	public string CurrentTexturePath => $"decals/{CurrentTextureName}.decal";

	public override void OnUseDown()
	{
		var itemColliders = Player.PlayerInteract.InteractCollider.Touching;
		foreach ( var itemCollider in itemColliders )
		{
			if ( itemCollider.GameObject.Components.TryGet<PictureFrame>( out var pictureFrame ) )
			{
				if ( string.IsNullOrWhiteSpace( CurrentTexturePath ) )
				{
					Player.Notify( Notifications.NotificationType.Error, "No texture selected" );
					return;
				}

				pictureFrame.TexturePath = CurrentTexturePath;

				SoundEx.Play( PaintSound, Player.WorldPosition );
				ParticleManager.PoofAt( pictureFrame.WorldPosition );

				return;
			}

			if ( itemCollider.GameObject.Components.TryGet<Pumpkin>( out var pumpkin ) )
			{
				if ( string.IsNullOrWhiteSpace( CurrentTexturePath ) )
				{
					Player.Notify( Notifications.NotificationType.Error, "No texture selected" );
					return;
				}

				pumpkin.TexturePath = CurrentTexturePath;

				SoundEx.Play( PaintSound, Player.WorldPosition );
				ParticleManager.PoofAt( pumpkin.WorldPosition );

				return;
			}

			if ( itemCollider.GameObject.Components.TryGet<SnowmanPiece>( out var snowmanPiece ) )
			{
				if ( string.IsNullOrWhiteSpace( CurrentTexturePath ) )
				{
					Player.Notify( Notifications.NotificationType.Error, "No texture selected" );
					return;
				}

				snowmanPiece.TexturePath = CurrentTexturePath;

				SoundEx.Play( PaintSound, Player.WorldPosition );
				ParticleManager.PoofAt( snowmanPiece.WorldPosition );

				return;
			}
		}

		var pos = Player.GetAimingGridPosition();

		if ( pos == Vector2Int.Zero )
		{
			Log.Error( "Invalid position" );
			return;
		}

		var item = Player.World.GetItem<FloorDecal>( pos, 16f );

		if ( item != null )
		{
			if ( string.IsNullOrWhiteSpace( CurrentTexturePath ) || CurrentTexturePath == item.TexturePath )
			{
				Log.Info( $"Removing decal {item.TexturePath}" );
				item.WorldItem.NodeLink.Remove();
			}
			else
			{
				Log.Info( $"Updating decal {item.TexturePath} -> {CurrentTexturePath}" );
				item.TexturePath = CurrentTexturePath;
				item.UpdateDecal();
			}

			SoundEx.Play( PaintSound, Player.WorldPosition );
			ParticleManager.PoofAt( item.WorldPosition );
		}
		else
		{
			if ( string.IsNullOrWhiteSpace( CurrentTexturePath ) )
			{
				Player.Notify( Notifications.NotificationType.Error, "No texture selected" );
				return;
			}

			var playerRotation = World.GetItemRotationFromDirection(
				World.Get4Direction( Player.PlayerController.Yaw ) );

			Log.Info( $"Spawning decal at {pos} with texture {CurrentTexturePath}" );

			/*var newItem = PersistentItem.Create<Persistence.FloorDecal>( Data.ItemData.GetById( "floor_decal" ) );
			if ( newItem == null ) throw new System.Exception( "Failed to create floor decal" );

			newItem.TexturePath = CurrentTexturePath;

			var node = World.SpawnPersistentNode( newItem, pos, playerRotation, World.ItemPlacement.FloorDecal, false );*/

			var newPItem = PersistentItem.Create( Data.ItemData.Get( "floor_decal" ) );

			newPItem.SetSaveData( "TexturePath", CurrentTexturePath );

			WorldNodeLink node;

			try
			{
				node = Player.World.SpawnPlacedItem( newPItem, pos, playerRotation );
			}
			catch ( System.Exception e )
			{
				Player.Notify( Notifications.NotificationType.Error, $"Failed to spawn decal: {e.Message}" );
				return;
			}

			SoundEx.Play( PaintSound, Player.WorldPosition );

			ParticleManager.PoofAt( node.WorldPosition );

			// fade in the decal
			/* if ( node is Items.FloorDecal decal2 )
			{
				decal2.Decal.Modulate = new Godot.Color( 1, 1, 1, 0f );
				var tween = GetTree().CreateTween();
				tween.TweenProperty( decal2.Decal, "modulate:a", 1f, 0.1f );
			} */
		}
	}

	public override string GetUseName()
	{
		return "Paint";
	}

	public override IEnumerable<MainUi.InputData> GetInputs()
	{
		yield return new MainUi.InputData( "WheelUp", "Next texture" );
		yield return new MainUi.InputData( "WheelDown", "Previous texture" );
	}

	protected override void OnStart()
	{
		CurrentTextureName = Decals.GetAllDecals().FirstOrDefault();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		if ( Input.MouseWheel.y != 0 )
		{
			var decals = Decals.GetAllDecals();

			if ( !decals.Any() )
			{
				Player.Notify( Notifications.NotificationType.Error, "No decals found" );
				return;
			}

			var index = decals.IndexOf( CurrentTextureName );

			index += Input.MouseWheel.y > 0 ? 1 : -1;

			if ( index < 0 ) index = decals.Count - 1;
			if ( index >= decals.Count ) index = 0;

			CurrentTextureName = decals[index];

			if ( string.IsNullOrEmpty( CurrentTextureName ) )
			{
				Log.Warning( "No texture selected" );
				return;
			}

			Sound.Play( TextureChangeSound );

			Log.Info( $"Selected texture: {CurrentTextureName}" );

			// Player.Notify( Notifications.NotificationType.Info, $"Selected texture: {CurrentTexture}" );
		}
	}
}
