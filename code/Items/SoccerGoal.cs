using Clover.Interactable;
using Clover.Player;
using Clover.Ui;

namespace Clover.Items;

public class SoccerGoal : Component, IInteract, Component.ITriggerListener
{
	[RequireComponent] public WorldItem WorldItem { get; set; }

	[Property] public ModelRenderer GoalModel { get; set; }

	public enum Team
	{
		Red,
		Blue,
		Yellow,
		Green
	}

	private Team _teamColor;

	[Sync]
	public Team TeamColor
	{
		get => _teamColor;
		set
		{
			_teamColor = value;
			if ( !GoalModel.IsValid() )
			{
				Log.Warning( "GoalModel is not valid" );
				return;
			}

			GoalModel.Tint = value switch
			{
				Team.Red => Color.Red,
				Team.Blue => Color.Blue,
				Team.Yellow => Color.Yellow,
				Team.Green => Color.Green,
				_ => Color.White
			};
		}
	}

	[Sync] public int Score { get; set; }


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


	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( IsProxy ) return;

		Log.Info( $"SoccerGoal triggered by {other.GameObject}" );

		if ( other.GameObject.Components.Get<SoccerBall>() == null )
			return;

		var spawn = GetClosestSpawn();
		if ( spawn == null )
		{
			Log.Error( "No spawn found" );
			return;
		}

		var ball = other.GameObject.Components.Get<SoccerBall>();

		ball.WorldPosition = spawn.WorldPosition + Vector3.Up * 16f;
		ball.Components.Get<Rigidbody>().Velocity = Vector3.Zero;
		ball.Components.Get<Rigidbody>().AngularVelocity = Vector3.Zero;

		Score++;

		GetClosestSpawn()?.OnBallScored( TeamColor );
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
	}

	public string GetInteractName()
	{
		return "Change team";
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		Gizmo.Draw.Text( Score.ToString(), new Transform( WorldPosition + Vector3.Up * 32f ), "Roboto", 36f );
	}
}
