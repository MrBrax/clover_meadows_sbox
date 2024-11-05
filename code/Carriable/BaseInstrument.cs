using System;

namespace Clover.Carriable;

public partial class BaseInstrument : BaseCarriable
{
	public enum Note { C, CSharp, D, DSharp, E, F, FSharp, G, GSharp, A, ASharp, B }


	/*public class InstrumentPlaybackLoop : InstrumentPlaybackEntry
	{
		public int LoopCount { get; set; }
		public Queue<InstrumentPlaybackEntry> Entries { get; set; }
	}*/

	public record struct NoteEntry
	{
		[Property] public int Octave { get; set; }
		[Property] public Note Note { get; set; }
		[Property] public SoundEvent SoundEvent { get; set; }
		[Property] public SoundFile SoundFile { get; set; }
		[Property] public bool Pitchable { get; set; }
	}

	public static readonly Dictionary<Note, float> NoteFrequencies = new()
	{
		{ Note.C, 261.63f },
		{ Note.CSharp, 277.18f },
		{ Note.D, 293.66f },
		{ Note.DSharp, 311.13f },
		{ Note.E, 329.63f },
		{ Note.F, 349.23f },
		{ Note.FSharp, 369.99f },
		{ Note.G, 392.00f },
		{ Note.GSharp, 415.30f },
		{ Note.A, 440.00f },
		{ Note.ASharp, 466.16f },
		{ Note.B, 493.88f }
	};

	/*public static readonly Dictionary<Note, float> NotePitchAbsolute = new()
	{
		{ Note.C, 0.0f },
		{ Note.CSharp, 1.0f },
		{ Note.D, 2.0f },
		{ Note.DSharp, 3.0f },
		{ Note.E, 4.0f },
		{ Note.F, 5.0f },
		{ Note.FSharp, 6.0f },
		{ Note.G, 7.0f },
		{ Note.GSharp, 8.0f },
		{ Note.A, 9.0f },
		{ Note.ASharp, 10.0f },
		{ Note.B, 11.0f }
	};*/

	public static readonly Dictionary<Note, string> NoteNames = new()
	{
		{ Note.C, "C" },
		{ Note.CSharp, "C#" },
		{ Note.D, "D" },
		{ Note.DSharp, "D#" },
		{ Note.E, "E" },
		{ Note.F, "F" },
		{ Note.FSharp, "F#" },
		{ Note.G, "G" },
		{ Note.GSharp, "G#" },
		{ Note.A, "A" },
		{ Note.ASharp, "A#" },
		{ Note.B, "B" }
	};

	[Property, InlineEditor] public List<NoteEntry> Notes { get; set; }


	[Sync] public bool IsPlaying { get; set; }

	[Sync] private int CurrentOctave { get; set; } = 3;

	public override string GetUseName()
	{
		return IsPlaying ? "Stop" : "Play";
	}

	public override void OnUseDown()
	{
		base.OnUseDown();
		IsPlaying = !IsPlaying;
	}

	public override bool ShouldDisableMovement()
	{
		return IsPlaying;
	}

	[Broadcast]
	public void PlayNote( int octave, Note note )
	{
		var entry = Notes.FirstOrDefault( x => x.Octave == octave && x.Note == note );
		if ( entry.Equals( default ) )
		{
			// Log.Warning( $"No note found for {octave} {note}" );
			PlayPitched( octave, note );
			return;
		}

		if ( entry.SoundEvent != null )
		{
			var s = Sound.Play( entry.SoundEvent );
			s.Position = WorldPosition;
			s.Pitch = 1.0f;
			Log.Info( $"Played {entry.Note} {entry.Octave}" );
		}
		else if ( entry.SoundFile != null )
		{
			var s = Sound.PlayFile( entry.SoundFile );
			s.Position = WorldPosition;
			s.Pitch = 1.0f;
			Log.Info( $"Played {entry.Note} {entry.Octave}" );
		}
		else
		{
			Log.Warning( $"No sound event or sound file found for {octave} {note}" );
		}
	}

	/// <summary>
	///  Find the closest note downwards that can be pitched to the desired note and play it with the correct pitch.
	/// </summary>
	/// <param name="octave"></param>
	/// <param name="note"></param>
	private void PlayPitched( int octave, Note note )
	{
		// var entry = Notes.FirstOrDefault( x => x.Octave == octave && x.Pitchable );
		var entry = Notes.FirstOrDefault( x => x.Octave == octave && x.Note < note );
		if ( entry.Equals( default ) )
		{
			// Log.Warning( $"No pitchable note found for {octave} {note}" );
			// return;
			// this isn't working for some reason
			entry = Notes.FirstOrDefault( x => x.Octave == octave && x.Note > note );
			if ( entry.Equals( default ) )
			{
				Log.Warning( $"No pitchable note found for {octave} {note}" );
				return;
			}
		}

		var noteIndex = (int)note;
		var pitchIndex = (int)entry.Note;
		var pitch = noteIndex - pitchIndex;
		var frequency = NoteFrequencies[entry.Note] * MathF.Pow( 2.0f, pitch / 12.0f );

		var s = entry.SoundEvent != null ? Sound.Play( entry.SoundEvent ) : Sound.PlayFile( entry.SoundFile );
		s.Pitch = frequency / NoteFrequencies[entry.Note];
		s.Position = WorldPosition;

		// Log.Info( $"Playing pitched {entry.Note} at {frequency} Hz" );
		Log.Info( $"Played {note} {octave}" );
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( !IsPlaying )
			return;


		if ( _isPlayingBack )
		{
			Playback();
		}

		foreach ( var note in Enum.GetValues( typeof(Note) ) )
		{
			if ( Input.Pressed( $"PlayNote{(Note)note}" ) )
			{
				PlayNote( CurrentOctave, (Note)note );
				Input.Clear( $"PlayNote{(Note)note}" );
			}
		}

		/*if ( Input.Pressed( "OctaveUp" ) )
		{
			CurrentOctave++;
		}
		else if ( Input.Pressed( "OctaveDown" ) )
		{
			CurrentOctave--;
		}*/

		if ( Input.Pressed( "PlayOctave1" ) ) CurrentOctave = 1;
		if ( Input.Pressed( "PlayOctave2" ) ) CurrentOctave = 2;
		if ( Input.Pressed( "PlayOctave3" ) ) CurrentOctave = 3;
		if ( Input.Pressed( "PlayOctave4" ) ) CurrentOctave = 4;
	}

	public static bool IsBlackNote( Note note )
	{
		return note is Note.CSharp or Note.DSharp or Note.FSharp or Note.GSharp or Note.ASharp;
	}
}
