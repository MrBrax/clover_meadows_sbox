using Clover.Interactable;
using Clover.Persistence;
using Clover.Player;
using Clover.Ui;

namespace Clover.Items;

public class SoccerGoal : Component, IInteract, Component.ITriggerListener, IPersistent
{
	[RequireComponent] public WorldItem WorldItem { get; set; }

	[Property] public ModelRenderer GoalModel { get; set; }

	[Property] public SoundEvent ScoreSound { get; set; }

	public enum Team
	{
		Red,
		Blue,
		Yellow,
		Green
	}

	private Team _teamColor;

	private bool _isBusy;

	[Sync]
	public Team TeamColor
	{
		get => _teamColor;
		set
		{
			_teamColor = value;
			UpdateModel();
		}
	}

	private void UpdateModel()
	{
		if ( !GoalModel.IsValid() )
		{
			Log.Warning( "GoalModel is not valid" );
			return;
		}

		GoalModel.Tint = TeamColor switch
		{
			Team.Red => Color.Red,
			Team.Blue => Color.Blue,
			Team.Yellow => Color.Yellow,
			Team.Green => Color.Green,
			_ => Color.White
		};
	}

	[Sync] public int Score { get; set; }

	protected override void OnStart()
	{
		base.OnStart();
		UpdateModel();
	}

	private SoccerSpawn GetClosestSpawn()
	{
		var spawns = Game.ActiveScene.GetAllComponents<SoccerSpawn>()
			.Where( x => x.WorldPosition.Distance( WorldPosition ) < 500 ).ToList();

		if ( spawns.Count == 0 )
		{
			Log.Error( "No spawns found" );
			return null;
		}

		var closest = spawns.OrderBy( x => x.WorldPosition.Distance( WorldPosition ) ).FirstOrDefault();

		return closest;
	}

	private SoccerBall _ball;

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( IsProxy ) return;

		Log.Info( $"SoccerGoal triggered by {other.GameObject}" );

		if ( other.GameObject.Components.Get<SoccerBall>() == null )
			return;

		_ball = other.GameObject.Components.Get<SoccerBall>();

		BallInGoal();
	}

	private async void BallInGoal()
	{
		if ( _isBusy )
			return;

		var spawn = GetClosestSpawn();
		if ( spawn == null )
		{
			Log.Error( "No spawn found" );
			return;
		}

		// PlayerCharacter.NotifyAll( Notifications.NotificationType.Info, $"{TeamColor} scored!" );

		SoundEx.Play( ScoreSound, WorldPosition );

		Score++;

		GetClosestSpawn()?.OnBallScored( TeamColor );

		_isBusy = true;

		_ball.Tags.Add( "invisible" );
		_ball.Tags.Add( "nocollideplayer" );
		_ball.Components.Get<Rigidbody>().MotionEnabled = false;

		await GameTask.DelaySeconds( 3f );

		if ( !_ball.IsValid() )
			return;

		_ball.Tags.Remove( "invisible" );
		_ball.Tags.Remove( "nocollideplayer" );
		_ball.Components.Get<Rigidbody>().MotionEnabled = true;

		RespawnBall( _ball, spawn );

		_isBusy = false;
	}

	private static void RespawnBall( SoccerBall ball, SoccerSpawn spawn )
	{
		var playersNearby = Game.ActiveScene.GetAllComponents<PlayerCharacter>()
			.Where( x => x.WorldPosition.Distance( spawn.WorldPosition ) < 32 ).ToList();

		foreach ( var player in playersNearby )
		{
			var playerPos = player.WorldPosition;
			var spawnPos = spawn.WorldPosition;
			var norm = (playerPos - spawnPos).Normal;
			player.TeleportTo( spawnPos + norm * 32, Rotation.Identity );
		}


		ball.WorldPosition = spawn.WorldPosition + Vector3.Up * 16f;
		ball.Components.Get<Rigidbody>().Velocity = Vector3.Zero;
		ball.Components.Get<Rigidbody>().AngularVelocity = Vector3.Zero;
	}

	/*void ITriggerListener.OnTriggerExit( Collider other )
	{

	}*/

	public void StartInteract( PlayerCharacter player )
	{
		if ( IsProxy )
		{
			player.Notify( Notifications.NotificationType.Error, "Only the host can change the team color" );
			return;
		}

		TeamColor = TeamColor switch
		{
			Team.Red => Team.Blue,
			Team.Blue => Team.Yellow,
			Team.Yellow => Team.Green,
			Team.Green => Team.Red,
			_ => Team.Red
		};
		Score = 0;
		_isBusy = false;
	}

	public string GetInteractName()
	{
		return "Change team";
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( Gizmo.Camera == null ) return;
		Gizmo.Draw.Text( Score.ToString(), new Transform( WorldPosition + Vector3.Up * 32f ), "Roboto", 36f );
	}

	public void OnSave( PersistentItem item )
	{
		item.SetSaveData( "TeamColor", TeamColor );
	}

	public void OnLoad( PersistentItem item )
	{
		TeamColor = item.GetSaveData<Team>( "TeamColor" );
		UpdateModel();
	}
}
