using Clover.Items;
using Clover.Player;

namespace Clover.WorldBuilder;

[Category( "Clover/World" )]
public class InteriorManager : Component
{
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }

	[Property, InlineEditor] public Dictionary<string, Room> Rooms { get; set; } = new();

	public string CurrentRoom { get; set; }
	public List<GameObject> CurrentRoomPieces { get; set; } = new();

	[Property] public string DefaultRoom { get; set; }


	public IEnumerable<InteriorPiece> InteriorPieces =>
		WorldLayerObject.World?.Components.GetAll<InteriorPiece>( FindMode.EverythingInSelfAndDescendants );

	public class Room
	{
		[Property] public GameObject Wall { get; set; }
		[Property] public GameObject Floor { get; set; }
		[Property] public RoomTrigger[] Triggers { get; set; }
	}

	public bool IsInCurrentRoom( Vector3 position )
	{
		var room = CollectionExtensions.GetValueOrDefault( Rooms, CurrentRoom );

		if ( room == null )
		{
			Log.Warning( $"Room {CurrentRoom} not found" );
			return false;
		}

		foreach ( var trigger in room.Triggers )
		{
			var collider = trigger.Components.Get<BoxCollider>();
			var bbox = BBox.FromPositionAndSize( collider.WorldPosition + collider.Center, collider.Scale );
			if ( bbox.Contains( position ) )
			{
				return true;
			}
		}

		return false;
	}

	protected override void OnStart()
	{
		base.OnStart();
		UpdateMaterials();

		Log.Info( $"InteriorManager started, found {InteriorPieces.Count()} interior pieces" );
		foreach ( var piece in InteriorPieces )
		{
			if ( piece.BelongsToRoom( DefaultRoom ) )
			{
				piece.Show();
			}
			else
			{
				piece.Hide();
			}
		}

		CurrentRoom = DefaultRoom;

		RebuildVisibility();
	}

	private void RebuildVisibility()
	{
		foreach ( var room in Rooms )
		{
			foreach ( var trigger in room.Value.Triggers )
			{
				var collider = trigger.Components.Get<Collider>();
				foreach ( var touching in collider.Touching )
				{
					if ( touching.Components.Get<WorldItem>() == null )
						continue;

					touching.Tags.Remove( "room_invisible" );

					if ( CurrentRoom != room.Key )
					{
						touching.Tags.Add( "room_invisible" );
					}
				}
			}
		}
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

	public void EnterRoom( PlayerCharacter player, RoomTrigger trigger )
	{
		var roomToEnter = trigger.RoomId;

		if ( roomToEnter == CurrentRoom )
			return;

		var piecesToShow = InteriorPieces.Where( x => x.BelongsToRoom( roomToEnter ) );
		var piecesToHide = InteriorPieces.Where( x => !x.BelongsToRoom( roomToEnter ) );

		foreach ( var piece in piecesToShow )
		{
			piece.Show();
		}

		foreach ( var piece in piecesToHide )
		{
			piece.Hide();
		}

		CurrentRoom = roomToEnter;

		RebuildVisibility();
	}

	public void ExitRoom( PlayerCharacter player, RoomTrigger trigger )
	{
	}
}
