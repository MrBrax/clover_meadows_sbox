using Clover.Player;

namespace Clover.WorldBuilder;

[Category( "Clover/World" )]
public class InteriorPiece : Component
{
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }

	[Property] public List<string> BelongsToRooms { get; set; } = new();

	[Property] public bool IsWall { get; set; }
	[Property] public bool IsFloor { get; set; }
	[Property] public bool IsDoorway { get; set; }
	[Property] public bool IsDarkness { get; set; }

	public void Hide()
	{
		Tags.Add( "room_invisible" );
		var renderer = GetComponent<ModelRenderer>( true );
		/*if ( renderer != null )
		{
			renderer.Enabled = false;
		}*/
	}

	public void Show()
	{
		Tags.Remove( "room_invisible" );
		/*var renderer = GetComponent<ModelRenderer>( true );
		if ( renderer != null )
		{
			renderer.Enabled = true;
		}*/
	}

	public bool BelongsToRoom( string room )
	{
		return BelongsToRooms.Contains( room );
	}

	/*protected override void OnUpdate()
	{
		var camera = Scene.Camera;

		var cameraPosition = camera.WorldPosition;
		var cameraRotation = camera.WorldRotation.Angles().WithPitch( 0 );
		var cameraForward = cameraRotation.Forward;

		var pieceForward = WorldRotation.Left;

		// Gizmo.Draw.Arrow( WorldPosition, WorldPosition + pieceForward * 32f );

		var dot = Vector3.Dot( cameraForward.Normal, pieceForward.Normal );

		if ( dot > 0.4f && IsWall )
		{
			Gizmo.Draw.Text( "HIDE ME", new Transform( WorldPosition ) );
		}
	}*/

	protected override void OnFixedUpdate()
	{
		if ( IsWall || IsDarkness )
		{
			if ( IsDarkness && PlayerCharacter.Local.WorldPosition.Distance( WorldPosition ) < 32f )
			{
				Tags.Set( "invisiblewall", false );
				return;
			}

			var camera = Scene.Camera;

			var cameraRotation = camera.WorldRotation.Angles().WithPitch( 0 );
			var cameraForward = cameraRotation.Forward;

			var pieceForward = WorldRotation.Left;

			var dot = Vector3.Dot( cameraForward.Normal, pieceForward.Normal );

			/*if ( dot > 0.4f )
			{
				Gizmo.Draw.Text( "HIDE ME", new Transform( WorldPosition ) );
			}*/

			if ( IsWall )
			{
				Tags.Set( "invisiblewall", dot > 0.3f );
			}
			else
			{
				var renderer = GetComponent<ModelRenderer>( true );
				renderer.Tint = dot > 0.3f ? Color.White.WithAlpha( 0.02f ) : Color.White;
			}
		}
		else if ( IsDoorway )
		{
			var camera = Scene.Camera;

			var cameraRotation = camera.WorldRotation.Angles().WithPitch( 0 );
			var cameraForward = cameraRotation.Forward;

			var pieceForward = WorldRotation.Left;

			var playerPosition = PlayerCharacter.Local.WorldPosition;

			if ( playerPosition.Distance( WorldPosition ) < 32f )
			{
				Tags.Set( "invisiblewall", false );
				return;
			}

			// check if player is in front or behind the doorway to flip the final dot check, doorway should always face into the room
			var isInFront = Vector3.Dot( pieceForward.Normal, playerPosition - WorldPosition ) > 0;

			// Gizmo.Draw.Text( isInFront ? "IN FRONT" : "BEHIND", new Transform( WorldPosition ) );

			var dot = Vector3.Dot( cameraForward.Normal, pieceForward.Normal );

			// Gizmo.Draw.Text( dot.ToString(), new Transform( WorldPosition ) );

			Tags.Set( "invisiblewall", isInFront ? dot > 0.3f : dot < -0.3f );
		}
	}

	protected override void DrawGizmos()
	{
		Gizmo.Transform = global::Transform.Zero;
		Gizmo.Draw.Text( $"Rooms: {string.Join( ", ", BelongsToRooms )}", new Transform( WorldPosition ) );

		Gizmo.Draw.Arrow( WorldPosition, WorldPosition + WorldRotation.Left * 32f );
	}
}
