using System;
using System.Threading.Tasks;
using Clover.Interactable;
using Clover.Player;

namespace Clover.WorldBuilder;

[Category( "Clover/World" )]
public class Door : Component, IInteract
{
	[Property] public GameObject DoorModel { get; set; }
	[Property] public GameObject FloorCollider { get; set; }

	[Property] public SoundEvent OpenSound { get; set; }
	[Property] public SoundEvent CloseSound { get; set; }
	[Property] public SoundEvent SqueakSound { get; set; }

	[Property] public bool IsLocked { get; set; } = false;

	[Property] public bool HasOpenHours { get; set; } = false;
	[Property] public int HourOpen { get; set; } = 0;
	[Property] public int HourClose { get; set; } = 24;

	[Property] public Curve DoorCurve { get; set; }

	private bool _isBusy;
	private bool _isBeingUsed;

	[Sync] private bool _openState { get; set; }
	private float _openAngle = -90;
	[Sync] private TimeSince _lastUse { get; set; }

	private float _doorSpeed = 0.5f;

	private SoundHandle _squeakSoundHandle;

	[Rpc.Owner]
	public void SetBusy( bool state )
	{
		_isBusy = state;
	}


	public void StartInteract( PlayerCharacter player )
	{
		if ( _isBusy ) return;
		// _ = PlayerEnter( player );
		PlayerEnterRpc();
	}

	public void FinishInteract( PlayerCharacter player )
	{
	}

	public string GetInteractName()
	{
		return "Open door";
	}

	public void SetState( bool state )
	{
		_openState = state;
		DoorModel.LocalRotation = Rotation.FromYaw( _openState ? _openAngle : 0 );
	}


	public void Open()
	{
		if ( _squeakSoundHandle.IsValid() ) _squeakSoundHandle.Stop();
		_openState = true;
		_lastUse = 0;
		_isBeingUsed = true;
		SetBusy( true );
		SetCollision( false );
		GameObject.PlaySound( OpenSound );
		_squeakSoundHandle = GameObject.PlaySound( SqueakSound );
	}

	public void Close()
	{
		if ( _squeakSoundHandle.IsValid() ) _squeakSoundHandle.Stop();
		// if ( !_openState ) return;
		_openState = false;
		_lastUse = 0;
		_isBeingUsed = true;
		SetBusy( true );
		SetCollision( true );
		_squeakSoundHandle = GameObject.PlaySound( SqueakSound );
		// Log.Info( "Door: Close" );
	}

	/*void ITriggerListener.OnTriggerEnter( Collider other )
	{

		var player = other.GetComponent<PlayerCharacter>();
		if ( !player.IsValid() )
		{
			return;
		}

		OnEnter( player );


	}*/

	[Rpc.Owner]
	private void PlayerEnterRpc()
	{
		_ = PlayerEnter( PlayerCharacter.Get( Rpc.Caller ) );
	}

	private async Task PlayerEnter( PlayerCharacter player )
	{
		if ( _isBeingUsed ) return;

		player.CharacterController.Velocity = Vector3.Zero;

		player.TeleportTo( WorldPosition + WorldRotation.Forward * 25f + WorldRotation.Left * 16f,
			WorldRotation * Rotation.FromYaw( 180 ) );

		Open();

		SetCollision( false );

		await Task.DelayRealtimeSeconds( _doorSpeed );

		player.StartCutscene( WorldPosition + WorldRotation.Forward * -32f + WorldRotation.Left * 16f );

		await Task.DelayRealtimeSeconds( 0.5f );

		Close();

		await Task.DelayRealtimeSeconds( _doorSpeed + 0.5f );

		SetCollision( true );

		player.EndCutscene();

		_isBeingUsed = false;
	}

	private void SetCollision( bool state )
	{
		var collider = Components.Get<Collider>( FindMode.EverythingInSelfAndDescendants );

		if ( collider.IsValid() )
		{
			collider.Enabled = state;
		}
		else
		{
			Log.Error( "Door: SetCollision: No collider found" );
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( !_isBeingUsed ) return;
		if ( _isBeingUsed && _lastUse > _doorSpeed )
		{
			Log.Info( "Door: OnFixedUpdate: Finish using" );
			_isBeingUsed = false;
			SetBusy( false );
			if ( !_openState )
			{
				SetCollision( true );
				// CloseSound?.Play();
				GameObject.PlaySound( CloseSound );
			}

			// SqueakSound?.Stop();
			if ( _squeakSoundHandle.IsValid() ) _squeakSoundHandle.Stop();
		}

		// animate door opening/closing by rotating it

		/*var sourceAngle = _openState ? 0 : _openAngle;
		var destinationAngle = _openState ? _openAngle : 0;*/

		var frac = Math.Clamp( _lastUse / _doorSpeed, 0, 1 );

		if ( !_openState ) frac = 1 - frac;

		// var angle = Mathf.CubicInterpolate( sourceAngle, destinationAngle, 0, 1, frac );

		var curveFrac = DoorCurve.Evaluate( frac );

		var yaw = MathX.LerpTo( 0, _openAngle, curveFrac );

		DoorModel.LocalRotation = Rotation.FromYaw( yaw );
	}


	public async void CloseAfter( float seconds )
	{
		DoorModel.LocalRotation = Rotation.FromYaw( _openAngle );
		SetBusy( true );
		await Task.DelayRealtimeSeconds( seconds );
		Close();
	}
}
