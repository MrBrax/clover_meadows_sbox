using System;
using Clover.Utilities;

namespace Clover.Carriable;

public partial class BaseInstrument
{
	// private TimeUntil _nextPlaybackEntry;
	private TimeSince _playbackStarted;

	// private int _playbackTimeSignature = 4;
	private int _playbackTempo = 0;

	private bool _isPlayingBack;

	private MidiFile _midiFile;

	// private int _currentIndex;
	private List<int> _currentTrackEventIndices = new();
	private int _transposePlayback = 0;

	// private List<bool> _tracksEnabled = new();
	private int _playbackTrackIndex;

	/*private Queue<InstrumentPlaybackEntry> _playbackQueue = new();

	public class InstrumentPlaybackEntry
	{
		public float Time { get; set; }
	}

	public class InstrumentPlaybackNote : InstrumentPlaybackEntry
	{

		public int Octave { get; set; }
		public Note Note { get; set; }
		public float Duration { get; set; }
	}*/

	/*public class InstrumentPlaybackPause : InstrumentPlaybackEntry
	{
		public float Duration { get; set; }
	}*/

	/*public class InstrumentPlaybackLoop : InstrumentPlaybackEntry
	{
		public int LoopCount { get; set; }
		public Queue<InstrumentPlaybackEntry> Entries { get; set; }
	}*/

	/*public void AddPlaybackEntry( InstrumentPlaybackEntry entry )
	{
		_playbackQueue.Enqueue( entry );
	}*/

	/*public void ClearPlayback()
	{
		_playbackQueue.Clear();
	}*/

	// private InstrumentPlaybackEntry _nextPlaybackEntry;

	private void Playback()
	{
		/*if ( _playbackQueue.Count == 0 )
		{
			_isPlayingBack = false;
			return;
		}*/

		/*if ( !_nextPlaybackEntry ) return;

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
		}*/

		/*if ( _nextPlaybackEntry == null )
		{
			_nextPlaybackEntry = _playbackQueue.Dequeue();
		}*/

		for ( var trackIndex = 0; trackIndex < _midiFile.Tracks.Length; trackIndex++ )
		{
			var track = _midiFile.Tracks[trackIndex];

			/*if ( !_tracksEnabled[trackIndex] )
			{
				continue;
			}*/

			// TODO: play multiple tracks at once or just one?
			/*if ( trackIndex != _playbackTrackIndex )
			{
				continue;
			}*/

			for ( var i = _currentTrackEventIndices[trackIndex]; i < track.MidiEvents.Count; i++ )
			{
				var midiEvent = track.MidiEvents[i];

				if ( midiEvent.MidiEventType != MidiEventType.NoteOn )
				{
					Log.Info( $"Event: {midiEvent.MidiEventType} {midiEvent.Time}" );

					if ( midiEvent.MidiEventType == MidiEventType.MetaEvent )
					{
						if ( midiEvent.MetaEventType == MetaEventType.Tempo )
						{
							_playbackTempo = midiEvent.Arg2;
							Log.Info( $"Tempo change: {midiEvent.Arg1}, {midiEvent.Arg2}, {midiEvent.Arg3}" );
						}
					}

					_currentTrackEventIndices[trackIndex] = i + 1;
					continue;
				}

				var bps = 60.0f / _playbackTempo;

				var time = ((float)midiEvent.Time / (float)_midiFile.TicksPerQuarterNote) * bps;


				if ( time > _playbackStarted )
				{
					break;
				}

				/*if ( i <= _currentIndex )
				{
					continue;
				}*/

				/*if ( i < _currentTrackEventIndices[trackIndex] )
				{
					continue;
				}*/

				if ( midiEvent.MidiEventType == MidiEventType.NoteOn )
				{
					var channel = midiEvent.Channel;
					var note = midiEvent.Note;
					var velocity = midiEvent.Velocity;
					// Log.Info( $"Note on: {note} {velocity} @ {midiEvent.Time}" );

					PlayMidiNote( note, velocity );
				}

				// _currentIndex = i;

				_currentTrackEventIndices[trackIndex] = i + 1;
			}
		}
	}

	/// <summary>
	///  Play a midi note to the instrument, converting the midi note to a note enum
	/// </summary>
	/// <param name="note"></param>
	private void PlayMidiNote( int note, int velocity )
	{
		if ( _transposePlayback != 0 )
		{
			note += _transposePlayback;
		}

		var octave = note / 12;
		var noteIndex = note % 12;

		Note noteEnum = (Note)noteIndex;
		PlayNote( octave, noteEnum, (float)velocity / 127.0f );
	}

	/*public void StartPlayback( string file )
	{
		_isPlayingBack = true;
		_nextPlaybackEntry = 0.0f;
		LoadFile( file );
	}*/

	private void LoadFile( string file )
	{
		var midiFile = new MidiFile( file );

		// 0 = single-track, 1 = multi-track, 2 = multi-pattern
		var midiFileformat = midiFile.Format;
		Log.Info( $"Midi file format: {midiFileformat}" );

		// also known as pulses per quarter note
		var ticksPerQuarterNote = midiFile.TicksPerQuarterNote;
		Log.Info( $"Ticks per quarter note: {ticksPerQuarterNote}" );

		Log.Info( $"Tracks: {midiFile.Tracks.Length}" );

		foreach ( var track in midiFile.Tracks )
		{
			foreach ( var midiEvent in track.MidiEvents )
			{
				if ( midiEvent.MidiEventType == MidiEventType.NoteOn )
				{
					var channel = midiEvent.Channel;
					var note = midiEvent.Note;
					var velocity = midiEvent.Velocity;
					// Log.Info( $"Note on: {note} {velocity} @ {midiEvent.Time}" );
				}

				if ( midiEvent.MidiEventType == MidiEventType.MetaEvent )
				{
					Log.Info(
						$"Meta event: {midiEvent.MetaEventType} - {midiEvent.Arg1} {midiEvent.Arg2} {midiEvent.Arg3}" );
				}
			}

			foreach ( var textEvent in track.TextEvents )
			{
				if ( textEvent.TextEventType == TextEventType.Lyric )
				{
					var time = textEvent.Time;
					var text = textEvent.Value;
					// Log.Info( $"Lyric: {time} {text}" );
				}
			}
		}

		_midiFile = midiFile;
		_playbackStarted = 0.0f;
		_isPlayingBack = true;
		// _currentIndex = 0;
		_transposePlayback = -12;

		_currentTrackEventIndices.Clear();
		foreach ( var track in _midiFile.Tracks )
		{
			_currentTrackEventIndices.Add( 0 );
		}

		/*_tracksEnabled.Clear();
		foreach ( var track in _midiFile.Tracks )
		{
			_tracksEnabled.Add( false );
		}

		_tracksEnabled[0] = true;*/

		_playbackTrackIndex = 0;
	}
}
