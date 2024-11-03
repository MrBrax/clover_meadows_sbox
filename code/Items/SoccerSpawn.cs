using Clover.Player;
using Clover.Ui;

namespace Clover.Items;

public class SoccerSpawn : Component
{
	[RequireComponent] public WorldItem WorldItem { get; set; }

	[Property] public SoundEvent WinSound { get; set; }

	public int MaxScore { get; set; } = 5;

	public List<SoccerGoal> GetGoals()
	{
		return Game.ActiveScene.GetAllComponents<SoccerGoal>()
			.Where( x => x.WorldPosition.Distance( WorldPosition ) < 500 ).ToList();
	}

	public void OnBallScored( SoccerGoal.Team teamColor )
	{
		var goals = GetGoals();

		foreach ( var goal in goals )
		{
			if ( goal.Score >= MaxScore )
			{
				Log.Info( $"{teamColor} wins!" );

				// PlayerCharacter.NotifyAll( Notifications.NotificationType.Info, $"{teamColor} wins!" );

				SoundEx.Play( WinSound, WorldPosition );
				ResetGame();
			}
		}
	}

	private void ResetGame()
	{
		var goals = GetGoals();
		foreach ( var goal in goals )
		{
			goal.Score = 0;
		}
	}
}
