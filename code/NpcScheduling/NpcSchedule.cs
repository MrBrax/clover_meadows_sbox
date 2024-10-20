using System;

namespace Clover.NpcScheduling;

public class NpcSchedule
{
	public class Activity
	{
		public int StartHour;
		public int StartMinute;
		public int EndHour;
		public int EndMinute;
	}

	public class SleepActivity : Activity
	{
	}

	public class WanderActivity : Activity
	{
	}

	public class ShopActivity : Activity
	{
	}

	public List<Activity> Activities { get; set; } = new();

	public void AddActivity( Activity activity )
	{
		Activities.Add( activity );
	}

	public int GetLastFreeTime()
	{
		if ( Activities.Count == 0 )
			return 0;

		Activity lastActivity = Activities[^1];
		return lastActivity.EndHour * 60 + lastActivity.EndMinute;
	}

	public void GenerateSchedule()
	{
		Activities.Clear();

		// Sleep
		SleepActivity sleep_morning = new()
		{
			StartHour = 0,
			StartMinute = 0,
			EndHour = 6 + Random.Shared.Int( 0, 2 ),
			EndMinute = Random.Shared.Int( 0, 59 )
		};
		AddActivity( sleep_morning );

		// random day activities
		int lastFreeTime = GetLastFreeTime();
		while ( lastFreeTime < 20 * 60 )
		{
			Activity activity = new()
			{
				StartHour = lastFreeTime / 60,
				StartMinute = lastFreeTime % 60,
				EndHour = lastFreeTime / 60 + Random.Shared.Int( 1, 3 ),
				EndMinute = Random.Shared.Int( 0, 59 )
			};
			AddActivity( activity );
			lastFreeTime = GetLastFreeTime();
		}


		// Sleep
		SleepActivity sleep_evening = new()
		{
			StartHour = 20 + Random.Shared.Int( 0, 2 ),
			StartMinute = Random.Shared.Int( 0, 59 ),
			EndHour = 24,
			EndMinute = 0
		};
		AddActivity( sleep_evening );
	}
}
