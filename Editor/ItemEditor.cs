using System.IO;
using Clover.Data;

namespace Clover;

public class ItemEditor : BaseResourceEditor<ItemData>
{
	
	SerializedObject Object;

	public ItemEditor()
	{
		Layout = Layout.Column();
	}
	
	protected override void Initialize( Asset asset, ItemData resource )
	{
		
		Layout.Clear( true );

		Object = resource.GetSerialized();

		var sheet = new ControlSheet();
		sheet.AddObject( Object );
		Layout.Add( sheet );
		
		var createPlaceSceneButton = new Button( "Create Place Scene" );
		createPlaceSceneButton.Clicked += () =>
		{
			var template = "prefabs/worlditem_template.prefab";
			
			var assetTemplate = AssetSystem.FindByPath( template );
			
			if ( assetTemplate == null )
			{
				Log.Error( $"Could not find template {template}" );
				return;
			}

			var destinationPath = resource.ResourcePath;
			
			Log.Info( $"Creating place scene for {destinationPath}" );
			Log.Info( $"Using template {template}" );
			Log.Info( $"Destination path {destinationPath}" );

			// File.WriteAllText( s, File.ReadAllText( asset.GetSourceFile( true ) ) );

		};
		
		Layout.Add( createPlaceSceneButton );
		
		Object.OnPropertyChanged += ( p ) =>
		{
			// TryAutoFill( p );
			NoteChanged( p );

			// Cascade to all terrains, feel free to code a TerrainMaterial modified -> TerrainStorage -> Terrain event chain
			// UpdateSceneTerrain();
		};
		
	}
}
