namespace Clover.WorldBuilder;

public class InteriorManager : Component
{
	
	[Property, InlineEditor] public Dictionary<string, Room> Rooms { get; set; } = new();

	public class Room
	{
		[Property] public GameObject Wall { get; set; }
		[Property] public GameObject Floor { get; set; }
	}

	protected override void OnStart()
	{
		base.OnStart();
		UpdateMaterials();
	}

	private void UpdateMaterials()
	{
		foreach ( var room in Rooms )
		{
			UpdateRoom( room.Key, room.Value );
		}
	}

	private void UpdateRoom( string roomId, Room roomData )
	{
		if ( roomData.Floor.IsValid() )
		{
			if ( roomData.Floor.Components.TryGet<MeshComponent>( out var mesh ) )
			{
				foreach ( var face in mesh.Mesh.FaceHandles )
				{
					mesh.Mesh.SetFaceMaterial( face, "materials/world/summer/cliff.vmat" );
				}
			}
			
			if ( roomData.Floor.Components.TryGet<ModelRenderer>( out var modelRenderer ) )
			{
				
			}
			
		}
		
		if ( roomData.Wall.IsValid() )
		{
			if ( roomData.Wall.Components.TryGet<MeshComponent>( out var mesh ) )
			{
				foreach ( var face in mesh.Mesh.FaceHandles )
				{
					mesh.Mesh.SetFaceMaterial( face, "materials/world/summer/cliff.vmat" );
				}
			}
		}
		
	}
}
