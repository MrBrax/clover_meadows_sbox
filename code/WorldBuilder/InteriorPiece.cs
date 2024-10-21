﻿namespace Clover.WorldBuilder;

public class InteriorPiece : Component
{
	[RequireComponent] public WorldLayerObject WorldLayerObject { get; set; }

	[Property] public List<string> BelongsToRooms { get; set; } = new();

	[Property] public bool IsWall { get; set; }
	[Property] public bool IsFloor { get; set; }

	public void Hide()
	{
		Tags.Add( "room_invisible" );
		var renderer = GetComponent<ModelRenderer>( true );
		if ( renderer != null )
		{
			renderer.Enabled = false;
		}
	}

	public void Show()
	{
		Tags.Remove( "room_invisible" );
		var renderer = GetComponent<ModelRenderer>( true );
		if ( renderer != null )
		{
			renderer.Enabled = true;
		}
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
		if ( !IsWall ) return;
		var camera = Scene.Camera;

		var cameraRotation = camera.WorldRotation.Angles().WithPitch( 0 );
		var cameraForward = cameraRotation.Forward;

		var pieceForward = WorldRotation.Left;

		var dot = Vector3.Dot( cameraForward.Normal, pieceForward.Normal );

		/*if ( dot > 0.4f )
		{
			Gizmo.Draw.Text( "HIDE ME", new Transform( WorldPosition ) );
		}*/

		Tags.Set( "invisiblewall", dot > 0.4f );
	}

	protected override void DrawGizmos()
	{
		Gizmo.Transform = global::Transform.Zero;
		Gizmo.Draw.Text( $"Rooms: {string.Join( ", ", BelongsToRooms )}", new Transform( WorldPosition ) );
	}
}