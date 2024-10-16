using System;

namespace Clover.Ui;

public partial class Notifications
{
	public static Notifications Instance { get; private set; }

	public enum NotificationType
	{
		Info,
		Warning,
		Error,
		Success
	}

	public record Notification
	{
		public NotificationType Type { get; init; }
		public string Text { get; init; }
		public float Duration { get; init; }
		public TimeSince CreatedAt { get; init; }

		public float Progress
		{
			get
			{
				if ( Duration == 0 )
					return 1;
				return Math.Clamp( CreatedAt / Duration, 0, 1 );
			}
		}

		public float Opacity
		{
			get
			{
				if ( Progress <= 0.1f )
					return Progress * 10;
				if ( Progress >= 0.9f )
					return 1 - Progress;
				return 1;
			}
		}

		public string Style
		{
			get
			{
				// translateY(-{Progress * 100}%)
				return
					$"opacity: {Opacity};";
				// return "";
			}
		}
	}

	public List<Notification> ScreenNotificationList = new();

	public void AddNotification( NotificationType type, string text, float duration = 5 )
	{
		Log.Info( $"[{type}] {text}" );
		Sound.Play( $"sounds/notifications/notification-{type.ToString().ToLower()}.sound" );
		ScreenNotificationList.Add( new Notification { Type = type, Text = text, Duration = duration, CreatedAt = 0 } );
	}

	protected override void OnStart()
	{
		Instance = this;
	}

	protected override void OnAwake()
	{
		Instance = this;
	}

	protected override void OnEnabled()
	{
		Instance = this;
	}

	protected override void OnDestroy()
	{
		Instance = null;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		for ( int i = ScreenNotificationList.Count - 1; i >= 0; i-- ) // Iterate backwards to allow removal
		{
			var notification = ScreenNotificationList[i];
			// Log.Info( $"Screen notification: {notification.Text}" );
			if ( notification.CreatedAt >= notification.Duration )
			{
				Log.Trace( $"Removing notification: {notification.Text}" );
				ScreenNotificationList.RemoveAt( i );
			}
		}
	}


	/// <summary>
	/// the hash determines if the system should be rebuilt. If it changes, it will be rebuilt
	/// </summary>
	protected override int BuildHash() => System.HashCode.Combine( ScreenNotificationList.Select( x => x.Duration ) );
}
