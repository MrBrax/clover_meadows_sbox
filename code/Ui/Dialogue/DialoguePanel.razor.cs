using System;
using Clover.Dialogue;
using Clover.Player;
using Sandbox.Audio;
using Sandbox.Speech;
using Sandbox.UI;

namespace Clover.Ui;

public partial class DialoguePanel
{

	[ConVar( "clover.dialogue.speed", ConVarFlags.Saved )]
	public static float DialogueSpeed { get; set; } = 0.5f;

	private BaseDialogueNode CurrentNode => DialogueComponent.Instance?.CurrentNode;
	private string Text;
	private TimeUntil _nextLetterTime;
	private int _lettersShown;
	private const float LetterDelay = 0.05f; // Delay between each letter in seconds

	private string SpeakerName => !string.IsNullOrEmpty( CurrentNode.Speaker )
		? CurrentNode.Speaker
		: DialogueComponent.Instance?.NpcName ?? "NPC";

	private bool _isTyping => CurrentNode != null && _lettersShown < GetCurrentNodeText().Length;

	private bool IsFinalDialogueNode => DialogueComponent.Instance.IsFinalDialogueNode( CurrentNode );

	// private float _mouthMove;

	protected override void OnFixedUpdate()
	{
		if ( !DialogueComponent.Instance.IsValid() ) return;

		// _mouthMove = _mouthMove.LerpTo( 0, Time.Delta * 10.0f );

		/*if ( DialogueComponent.Instance.CurrentNpc.IsValid() )
		{
			DialogueComponent.Instance.CurrentNpc.SetMouthOpenAmount( _mouthMove );
		}
		else
		{
			XLog.Info( this, "No current NPC to set mouth open amount." );
		}*/

		// pressing interact will skip to the end of the current text if it's still typing, or advance the dialogue if it's fully shown
		if ( Input.Pressed( "use" ) && CurrentNode != null )
		{
			Input.Clear( "use" );

			if ( _isTyping )
			{
				FinishTyping();
			}
			else
			{
				if ( IsFinalDialogueNode )
				{
					ExitDialogue();
				}
				else if ( CurrentNode.Choices.Count == 0 )
				{
					AdvanceDialogue();
				}
			}

		}

		TypeEffect();
		NpcLookAtPlayer();
	}

	private void NpcLookAtPlayer()
	{
		if ( !DialogueComponent.Instance.CurrentNpc.IsValid() ) return;
		if ( !PlayerCharacter.Local.IsValid() ) return;

		DialogueComponent.Instance.CurrentNpc.LookAt( PlayerCharacter.Local.WorldPosition );
	}

	protected override void OnMouseDown( MousePanelEvent e )
	{
		// If the text is still typing out, finish it immediately
		if ( CurrentNode != null && _lettersShown < GetCurrentNodeText().Length )
		{
			FinishTyping();
		}
	}

	private void FinishTyping()
	{
		if ( CurrentNode == null )
			return;

		_lettersShown = GetCurrentNodeText().Length;
		Text = GetCurrentNodeText();
		StateHasChanged();
	}

	private string GetCurrentNodeText()
	{
		if ( CurrentNode == null )
			return "";

		var text = CurrentNode.Text;

		// replace hardcoded variables in the text
		text = text.Replace( "%PLAYERNAME%", PlayerCharacter.Local?.PlayerName ?? "Player" );

		// replace variable values in the text
		foreach ( var variable in DialogueComponent.Instance.GetAllDialogueVariables() )
		{
			text = text.Replace( $"%{variable.Key}%", variable.Value.ToString() );
		}

		return text;
	}

	private SoundHandle _letterSound;

	private void TypeEffect()
	{
		if ( CurrentNode == null || string.IsNullOrEmpty( CurrentNode.Text ) )
			return;

		var currentText = GetCurrentNodeText();

		if ( _lettersShown < currentText.Length )
		{
			if ( _nextLetterTime )
			{
				_lettersShown++;
				Text = currentText.Substring( 0, _lettersShown );
				_nextLetterTime = LetterDelay * DialogueSpeed;

				// if full stop or exclamation mark or anything like that, add a slightly longer delay
				if ( _lettersShown < currentText.Length )
				{
					var lastChar = currentText[_lettersShown - 1];
					if ( lastChar == '.' || lastChar == '!' || lastChar == '?' )
					{
						_nextLetterTime += 0.3f * DialogueSpeed;
					}
					else if ( lastChar == ',' || lastChar == ';' || lastChar == ':' )
					{
						_nextLetterTime += 0.1f * DialogueSpeed;
					}
				}

				var currentLetter = Text[^1];

				if ( DialogueComponent.Instance.CurrentNpc.IsValid() )
				{
					// DialogueComponent.Instance.CurrentNpc.SpeakLetter( currentLetter );
				}

				// Sound.Play( "sound/newcity/dialogue/dialogue.type.sound" );

				if ( _letterSound.IsValid() )
				{
					_letterSound.Stop( 0.1f );
					_letterSound = null;
				}

				_letterSound = Sound.PlayFile(
					SoundFile.Load( $"sound/speech/alphabet/{currentLetter.ToString().ToUpper()}.wav" ) );

				if ( _letterSound.IsValid() )
				{
					_letterSound.TargetMixer = Mixer.FindMixerByName( "Speech" );
					_letterSound.Volume = 0.1f;
					_letterSound.ListenLocal = true;
					_letterSound.Pitch = 0.9f + (Game.Random.Float() * 0.1f) + (float)(Math.Sin( Time.Now * 10 ) * 0.05f);
				}

				// if last letter, reset mouth
				if ( _lettersShown == currentText.Length )
				{
					if ( DialogueComponent.Instance.CurrentNpc.IsValid() )
					{
						// DialogueComponent.Instance.CurrentNpc.ResetMouth();
					}
				}

				// StateHasChanged();
				// Sound.Play( "sound/newcity/dialogue/dialogue.type.sound" );
			}
		}
		else
		{
			Text = currentText; // Ensure full text is shown
		}
	}

	private void SelectChoice( DialogueChoice choice )
	{
		DialogueComponent.Instance?.SelectChoice( choice );
		StateHasChanged();
	}

	private void AdvanceDialogue()
	{
		DialogueComponent.Instance?.AdvanceDialogue();
		StateHasChanged();
	}

	private void ExitDialogue()
	{
		DialogueComponent.Instance?.ExitDialogue();
		StateHasChanged();

		_letterSound?.Stop();
		_letterSound = null;

		/*if ( _voice.IsValid() )
		{
			_voice.Stop();
			_voice = null;
		}*/
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( CurrentNode, Text );
	}

	// private SoundHandle _voice;
	// private Synthesizer _voiceSynth = new();

	public void OnNodeChanged()
	{
		Text = "";
		_lettersShown = 0;
		_nextLetterTime = 0;

		/*if ( _voice.IsValid() )
		{
			XLog.Info( this, "Stopping previous voice playback." );
			_voice.Stop();
			_voice = null;
		}*/

		/*var currentText = GetCurrentNodeText();
		if ( !string.IsNullOrEmpty( currentText ) )
		{
			XLog.Info( this, $"Starting voice synthesis for text: {currentText}" );
			var voiceSynth = new Synthesizer();
			voiceSynth.WithText( currentText );
			voiceSynth.WithRate( 6 );
			voiceSynth.TrySetVoice( "Male", "80" );

			foreach ( var voice in voiceSynth.InstalledVoices )
			{
				Log.Info( $"Installed voice: {voice.Name} - age {voice.Age} - gender {voice.Gender}" );
			}

			_voice = voiceSynth.Play();
			if ( DialogueComponent.Instance.CurrentNpc.IsValid() )
			{
				_voice.Position = DialogueComponent.Instance.CurrentNpc.WorldPosition + Vector3.Up * 64;
			}
			else
			{
				_voice.Position = BraxnetPlayer.LocalPlayer.EyePosition;
			}
		}*/

		StateHasChanged();
	}

	public bool IsVisible => CurrentNode != null;

	public void ShowPanel()
	{
	}

	public void HidePanel()
	{
		ExitDialogue();
	}
}
