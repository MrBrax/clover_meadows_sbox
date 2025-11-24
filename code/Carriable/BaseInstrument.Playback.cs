using System;
using Clover.Utilities;

namespace Clover.Carriable;

public partial class BaseInstrument
{
	[Sync] public bool IsPlayingBack { get; set; }

	[Sync] public string PlaybackTitle { get; set; } = "";

	// [Sync] public NetList<bool> PlaybackTracksEnabled { get; set; } = new();
	[Sync] public NetList<BaseInstrument> PlaybackTracksInstruments { get; set; } = new();
	[Sync] public int TransposePlayback { get; set; } = 0;

	// private TimeUntil _nextPlaybackEntry;
	[Sync] public TimeSince PlaybackStarted { get; set; }

	public float PlaybackProgress;

	// private int _playbackTimeSignature = 4;
	private int _playbackTempo = 0;


	private MidiFile _midiFile;

	// private int _currentIndex;
	private List<int> _currentTrackEventIndices = new();
	private List<float> _currentTrackVolumes = new();


	// private int _playbackTrackIndex;


	private void Playback()
	{
		var finished = 0;

		for ( var trackIndex = 0; trackIndex < _midiFile.Tracks.Length; trackIndex++ )
		{
			var track = _midiFile.Tracks[trackIndex];

			// TODO: play multiple tracks at once or just one?
			/*if ( trackIndex != _playbackTrackIndex )
			{
				continue;
			}*/

			// Log.Info( $"{_currentTrackEventIndices[trackIndex]} / {track.MidiEvents.Count}" );
			if ( _currentTrackEventIndices[trackIndex] >= track.MidiEvents.Count )
			{
				finished++;
				continue;
			}

			for ( var i = _currentTrackEventIndices[trackIndex]; i < track.MidiEvents.Count; i++ )
			{
				// skip if track is disabled but keep track of the current index

				var midiEvent = track.MidiEvents[i];

				/*if ( midiEvent.MidiEventType != MidiEventType.NoteOn )
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
				}*/

				var bps = 60.0f / _playbackTempo;

				var time = ((float)midiEvent.Time / (float)_midiFile.TicksPerQuarterNote) * bps;

				if ( time > PlaybackStarted )
				{
					break;
				}

				if ( trackIndex > PlaybackTracksInstruments.Count || !PlaybackTracksInstruments[trackIndex].IsValid() )
				{
					_currentTrackEventIndices[trackIndex] = i + 1;
					continue;
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

					PlayMidiNote( trackIndex, note, velocity );
				}
				else if ( midiEvent.MidiEventType == MidiEventType.NoteOff )
				{
					var channel = midiEvent.Channel;
					var note = midiEvent.Note;
					var velocity = midiEvent.Velocity;
					// Log.Info( $"Note off: {note} {velocity} @ {midiEvent.Time}" );
				}
				else if ( midiEvent.MidiEventType == MidiEventType.MetaEvent )
				{
					if ( midiEvent.MetaEventType == MetaEventType.Tempo )
					{
						var tempo = midiEvent.Arg1;
						var beatsPerMinute = midiEvent.Arg2;
						_playbackTempo = beatsPerMinute;
						Log.Info( $"Tempo change: {midiEvent.Arg1}, {midiEvent.Arg2}, {midiEvent.Arg3}" );
					}
				}
				else if ( midiEvent.MidiEventType == MidiEventType.PitchBendChange )
				{
					var channel = midiEvent.Arg1;
					var value1 = midiEvent.Arg2;
					var value2 = midiEvent.Arg3;
					Log.Info( $"Pitch bend: {channel} {value1} {value2}" );
				}
				else if ( midiEvent.MidiEventType == MidiEventType.ProgramChange )
				{
					var channel = midiEvent.Arg1;
					var program = midiEvent.Arg2;
					Log.Info( $"Program change: {channel} {program}" );
				}
				else if ( midiEvent.MidiEventType == MidiEventType.ChannelAfterTouch )
				{
					var channel = midiEvent.Arg1;
					var value = midiEvent.Arg2;
					Log.Info( $"Channel after touch: {channel} {value}" );
				}
				else if ( midiEvent.MidiEventType == MidiEventType.KeyAfterTouch )
				{
					var channel = midiEvent.Arg1;
					var note = midiEvent.Arg2;
					var value = midiEvent.Arg3;
					Log.Info( $"Key after touch: {channel} {note} {value}" );
				}
				else if ( midiEvent.MidiEventType == MidiEventType.ControlChange )
				{
					var channel = midiEvent.Arg1;
					var controlChangeType = midiEvent.ControlChangeType;
					var value = midiEvent.Value;
					Log.Info( $"Control change: {channel} {controlChangeType} {value}" );

					if ( controlChangeType == ControlChangeType.Volume )
					{
						_currentTrackVolumes[trackIndex] = value / 127.0f;
					}
				}
				else
				{
					Log.Info( $"Event: {midiEvent.MidiEventType} {midiEvent.Time}" );
				}

				// _currentIndex = i;

				_currentTrackEventIndices[trackIndex] = i + 1;
			}
		}

		if ( finished >= _midiFile.Tracks.Length )
		{
			IsPlayingBack = false;
			Log.Info( "Finished playback" );
			return;
		}

		CalculateProgress();
	}

	private void CalculateProgress()
	{
		// add all the ticks from all the tracks
		var totalTicks = _midiFile.Tracks.Sum( x => x.MidiEvents.Count );

		// add all the ticks from the current track
		var currentTicks = _currentTrackEventIndices.Sum( x => x );

		// calculate the progress
		PlaybackProgress = (float)currentTicks / (float)totalTicks;


		// Log.Info( $"Progress: {PlaybackProgress}, {currentTicks} / {totalTicks}" );
	}

	/// <summary>
	///  Play a midi note to the instrument, converting the midi note to a note enum
	/// </summary>
	/// <param name="note"></param>
	private void PlayMidiNote( int trackIndex, int note, int velocity )
	{
		if ( TransposePlayback != 0 )
		{
			note += TransposePlayback;
		}

		var octave = note / 12;
		var noteIndex = note % 12;

		Note noteEnum = (Note)noteIndex;

		var volume = velocity / 127.0f;

		volume *= _currentTrackVolumes[trackIndex];

		var instrument = PlaybackTracksInstruments[trackIndex];
		if ( !instrument.IsValid() )
		{
			PlaybackTracksInstruments[trackIndex] = this;
			instrument = this;
		}

		instrument.PlayNote( octave, noteEnum, volume );
	}

	/*public void StartPlayback( string file )
	{
		_isPlayingBack = true;
		_nextPlaybackEntry = 0.0f;
		LoadFile( file );
	}*/

	public void LoadFile( string file )
	{
		IsPlayingBack = false;

		Log.Info( $"Loading midi file: {file}" );

		var midiFile = new MidiFile( $"midi/{file}" );

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
		PlaybackStarted = 0.0f;
		// IsPlayingBack = true;
		// _currentIndex = 0;
		// _transposePlayback = -12;

		PlaybackTracksInstruments.Clear();

		_currentTrackEventIndices.Clear();

		foreach ( var track in _midiFile.Tracks )
		{
			_currentTrackEventIndices.Add( 0 );
			PlaybackTracksInstruments.Add( this );
		}

		PlaybackTitle = file;

		/*_tracksEnabled.Clear();
		foreach ( var track in _midiFile.Tracks )
		{
			_tracksEnabled.Add( false );
		}

		_tracksEnabled[0] = true;*/
	}

	public void StartPlayback()
	{
		_currentTrackEventIndices.Clear();
		_currentTrackEventIndices.AddRange( Enumerable.Repeat( 0, _midiFile.Tracks.Length ) );

		_currentTrackVolumes.Clear();
		_currentTrackVolumes.AddRange( Enumerable.Repeat( 1.0f, _midiFile.Tracks.Length ) );

		PlaybackStarted = 0.0f;
		IsPlayingBack = true;
	}

	public void StopPlayback()
	{
		IsPlayingBack = false;
		PlaybackStarted = 0.0f;
		PlaybackProgress = 0.0f;
	}

	public void ToggleTrackPlayback( int index )
	{
		if ( index >= PlaybackTracksInstruments.Count )
		{
			return;
		}

		// PlaybackTracksEnabled[index] = !PlaybackTracksEnabled[index];
		if ( PlaybackTracksInstruments[index] == this )
		{
			PlaybackTracksInstruments[index] = null;
		}
		else
		{
			PlaybackTracksInstruments[index] = this;
		}

		// _currentTrackEventIndices[index] = 0;
	}

	public bool IsPlaybackTrackEnabled( int index )
	{
		if ( index >= PlaybackTracksInstruments.Count )
		{
			return false;
		}

		return PlaybackTracksInstruments[index].IsValid();
	}


	[Rpc.Owner]
	public void RequestTrackPlayback( BaseInstrument instrument, int trackIndex )
	{
		if ( instrument == this )
		{
			return;
		}

		if ( trackIndex >= PlaybackTracksInstruments.Count )
		{
			return;
		}

		Log.Info( $"Player {Rpc.Caller.DisplayName} added themselves to track {trackIndex}" );
		PlaybackTracksInstruments[trackIndex] = instrument;
	}
}
