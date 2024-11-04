namespace Clover.Carriable;

public partial class BaseInstrument
{
	private TimeUntil _nextPlaybackEntry;
	private bool _isPlayingBack;
	private Queue<InstrumentPlaybackEntry> _playbackQueue = new();

	public class InstrumentPlaybackEntry
	{
	}

	public class InstrumentPlaybackNote : InstrumentPlaybackEntry
	{
		public int Octave { get; set; }
		public Note Note { get; set; }
		public float Duration { get; set; }
	}

	public class InstrumentPlaybackPause : InstrumentPlaybackEntry
	{
		public float Duration { get; set; }
	}

	/*public class InstrumentPlaybackLoop : InstrumentPlaybackEntry
	{
		public int LoopCount { get; set; }
		public Queue<InstrumentPlaybackEntry> Entries { get; set; }
	}*/

	public void AddPlaybackEntry( InstrumentPlaybackEntry entry )
	{
		_playbackQueue.Enqueue( entry );
	}

	public void ClearPlayback()
	{
		_playbackQueue.Clear();
	}

	private void Playback()
	{
		if ( _playbackQueue.Count == 0 )
		{
			_isPlayingBack = false;
			return;
		}

		if ( !_nextPlaybackEntry ) return;

		var entry = _playbackQueue.Dequeue();
		if ( entry is InstrumentPlaybackNote note )
		{
			PlayNote( note.Octave, note.Note );
			_nextPlaybackEntry = note.Duration;
		}
		else if ( entry is InstrumentPlaybackPause pause )
		{
			_nextPlaybackEntry = pause.Duration;
		}
		else
		{
			Log.Warning( "Unknown playback entry" );
		}
	}
}
