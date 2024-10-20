using Clover;
using Sandbox;

[Category( "Clover/Player" )]
[Icon( "camera" )]
public sealed class CameraController : Component
{
	[Property] public CameraNode MainCameraNode { get; set; }
	[Property] public CameraNode SkyCameraNode { get; set; }

	[Property] public SoundEvent CameraInSound { get; set; }
	[Property] public SoundEvent CameraOutSound { get; set; }
	[Property] public SoundEvent CameraErrorSound { get; set; }

	[Property] public GameObject CameraPivot { get; set; }

	private bool _playedInSound;
	private bool _playedOutSound;
	private bool _playedErrorSound;

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		if ( !SkyCameraNode.IsValid() )
		{
			Log.Error( "SkyCameraNode is not valid" );
			return;
		}

		// SkyCameraNode.Priority = Input.Down("View") ? 10 : 0;
		if ( Input.Down( "View" ) )
		{
			if ( (CameraMan.Instance?.MainCameraNode.IsValid() ?? false) && CameraMan.Instance.MainCameraNode.Static )
			{
				if ( !_playedErrorSound ) Sound.Play( CameraErrorSound );
				_playedErrorSound = true;
				return;
			}

			var trace = Scene.Trace.Ray( GameObject.WorldPosition, SkyCameraNode.WorldPosition )
				.WithTag( "terrain" )
				.Run();

			// Gizmo.Draw.Line( SkyCameraNode.WorldPosition, GameObject.WorldPosition );

			_playedOutSound = false;

			if ( trace.Hit )
			{
				if ( SkyCameraNode.Priority == 10 && !_playedInSound )
				{
					Sound.Play( CameraInSound );
					_playedInSound = true;
					_playedOutSound = false;
					_playedErrorSound = false;
				}
				else if ( SkyCameraNode.Priority == 0 && !_playedErrorSound )
				{
					Sound.Play( CameraErrorSound );
					_playedErrorSound = true;
					_playedInSound = false;
					_playedOutSound = false;
				}

				SkyCameraNode.Priority = 0;
				return;
			}

			if ( !_playedInSound )
			{
				Sound.Play( CameraInSound );
				_playedInSound = true;
			}

			SkyCameraNode.Priority = 10;
		}
		else
		{
			if ( !_playedOutSound && SkyCameraNode.Priority == 10 )
			{
				Sound.Play( CameraOutSound );
				_playedOutSound = true;
			}

			_playedInSound = false;
			_playedErrorSound = false;

			SkyCameraNode.Priority = 0;
		}

		if ( MainUi.Instance.IsValid() && MainUi.Instance.LastInput > MainUi.HideUiDelay * 3 )
		{
			SkyCameraNode.Priority = 10;
		}

		if ( Input.Pressed( "CameraLeft" ) )
		{
			CameraPivot.WorldRotation *= Rotation.FromYaw( 30 );
		}
		else if ( Input.Pressed( "CameraRight" ) )
		{
			CameraPivot.WorldRotation *= Rotation.FromYaw( -30 );
		}
	}

	public void SnapCamera()
	{
		MainCameraNode.SnapTo();
	}
}
