using Clover.Interactable;
using Clover.Persistence;
using Clover.Player;
using Clover.Ui;

namespace Clover.Items;

public class SnowmanPiece : DecalItem, IInteract, IPersistent
{
	public enum SnowmanPieceType
	{
		Small,
		Medium,
		Large
	}

	[RequireComponent] public WorldItem WorldItem { get; private set; }

	[Property] public ModelRenderer ModelRenderer { get; set; }

	private SnowmanPieceType _type = SnowmanPieceType.Small;

	[Property]
	public SnowmanPieceType PieceType
	{
		get => _type;
		set
		{
			_type = value;
			if ( !ModelRenderer.IsValid() )
			{
				Log.Error( "ModelRenderer is not valid" );
				return;
			}

			var scale = _type switch
			{
				SnowmanPieceType.Small => 1f,
				SnowmanPieceType.Medium => 1.2f,
				SnowmanPieceType.Large => 1.4f,
				_ => 1f
			};

			ModelRenderer.LocalScale = Vector3.One * scale;
		}
	}

	public override void OnMaterialUpdate( Material material )
	{
		base.OnMaterialUpdate( material );

		if ( !ModelRenderer.IsValid() )
		{
			Log.Error( "ModelRenderer is not valid" );
			return;
		}

		// var baseMaterial = Material.Load( "items/seasonal/halloween/pumpkin_01/pumpkin_body.vmat" );
		/*var baseMaterial = PumpkinMaterial;

		if ( baseMaterial == null )
		{
			Log.Error( "Failed to load pumpkin material" );
			return;
		}

		var pumpkinMaterial = baseMaterial.CreateCopy();
		if ( pumpkinMaterial == null )
		{
			Log.Error( "Failed to create pumpkin material copy" );
			return;
		}*/

		var snowmanMaterial = Material.Create( $"snowman_{TexturePath}.vmat", "shaders/snowman.shader" );

		snowmanMaterial.Set( "Overlay", DecalTexture );


		snowmanMaterial.Attributes.Set( "paint", 1 );

		ModelRenderer.SetMaterialOverride( snowmanMaterial, "paint" );

		Log.Info( $"Snowman material updated, mat: {snowmanMaterial.ResourcePath}" );
	}

	public void StartInteract( PlayerCharacter player )
	{
		// MainUi.Instance.Components.Get<PaintUi>( true ).OpenPaint( PaintUi.PaintType.Pumpkin, 128, 64, true );
		if ( string.IsNullOrEmpty( TexturePath ) )
		{
			MainUi.Instance.Components.Get<PaintUi>( true ).OpenPaint( PaintUi.PaintType.Snowman, 128, 64, false );
		}
		else
		{
			MainUi.Instance.Components.Get<PaintUi>( true ).Open( TexturePath );
		}
	}

	public string GetInteractName()
	{
		return "Paint";
	}

	public override void OnSave( PersistentItem item )
	{
		base.OnSave( item );
		item.SetSaveData( "PieceType", (int)PieceType );
	}

	public override void OnLoad( PersistentItem item )
	{
		base.OnLoad( item );
		PieceType = (SnowmanPieceType)item.GetSaveData<int>( "PieceType" );
	}
}
