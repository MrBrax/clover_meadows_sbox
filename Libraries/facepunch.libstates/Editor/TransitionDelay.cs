using Editor.NodeEditor;
using System;

namespace Sandbox.States.Editor;

public record TransitionDelay( TransitionItem Item ) : ILabelSource
{
	protected StateMachineView View => Item.Source.View;
	public Transition Transition => Item.Transition!;

	public string Title => "Timing";

	public string? Description => Transition.HasDelay ? FormatDelayLong() : null;

	public string? Icon => Transition.HasDelay ? "alarm" : null;
	public string? Text => Transition.HasDelay ? FormatDelayShort() : null;

	public void BuildAddContextMenu( global::Editor.Menu menu )
	{
		if ( Transition.MinDelay is null )
		{
			BuildAddMinDelayMenu( menu );
		}

		if ( Transition.MaxDelay is null )
		{
			BuildAddMaxDelayMenu( menu );
		}
	}

	public void BuildModifyContextMenu( global::Editor.Menu menu )
	{
		if ( Transition.MinDelay is not null )
		{
			BuildModifyMinDelayMenu( menu );
		}

		if ( Transition.MaxDelay is not null )
		{
			BuildModifyMaxDelayMenu( menu );
		}
	}

	public bool IsValid => Transition.HasDelay;

	private string FormatDelayShort()
	{
		var (min, max) = Transition.DelayRange;

		if ( min <= 0f && float.IsPositiveInfinity( max ) )
		{
			return "N/A";
		}

		return min >= max ? $"={FormatDuration( min )}"
			: float.IsPositiveInfinity( max ) ? $">{FormatDuration( min )}"
			: min <= 0f ? $"<{FormatDuration( max )}"
			: $"{FormatDuration( min )} - {FormatDuration( max )}";
	}

	private string FormatDelayLong()
	{
		var (min, max) = Transition.DelayRange;
		var hasCondition = Transition.IsConditional;

		if ( min <= 0f && float.IsPositiveInfinity( max ) )
		{
			return "Can be taken at any time.";
		}

		return min >= max
			? $"Only taken after exactly <b>{FormatDuration( min )}</b>."
			: hasCondition
				? float.IsPositiveInfinity( max )
					? $"Taken as soon as a condition is met after at least <b>{FormatDuration( min )}</b>."
					: min <= 0f
						? $"Taken as soon as a condition is met before at most <b>{FormatDuration( max )}</b>."
						: $"Taken as soon as a condition is met after between <b>{FormatDuration( min )}</b> and <b>{FormatDuration( max )}</b>."
				: $"Taken at a random time between <b>{FormatDuration( min )}</b> and <b>{FormatDuration( max )}</b>.";
	}

	private static string FormatDuration( float seconds )
	{
		if ( float.IsPositiveInfinity( seconds ) )
		{
			return "\u221e";
		}

		if ( seconds < 0.001f )
		{
			return "0s";
		}

		var timeSpan = TimeSpan.FromSeconds( seconds );
		var result = "";

		if ( timeSpan.Hours > 0 )
		{
			result += $"{timeSpan.Hours}h";
		}

		if ( timeSpan.Minutes > 0 )
		{
			result += $"{timeSpan.Minutes}m";
		}

		if ( timeSpan.Seconds > 0 )
		{
			result += $"{timeSpan.Seconds}s";
		}

		if ( timeSpan.Milliseconds > 0 )
		{
			result += $"{timeSpan.Milliseconds}ms";
		}

		return result;
	}

	private void BuildAddMinDelayMenu( global::Editor.Menu menu )
	{
		menu.AddMenu( "Add Minimum Time", "hourglass_top" ).AddLineEdit( "Seconds", value: "1", autoFocus: true, onSubmit:
			delayStr =>
			{
				if ( !float.TryParse( delayStr, out var seconds ) || seconds <= 0f )
				{
					return;
				}

				View.LogEdit( "Transition Delay Added" );

				Transition.MinDelay = seconds;
				Item.ForceUpdate();
			} );
	}

	private void BuildModifyMinDelayMenu( global::Editor.Menu menu )
	{
		var minDelay = Transition.MinDelay!.Value;

		menu.AddHeading( "Minimum Time" );
		menu.AddLineEdit( "Seconds", value: minDelay.ToString( "R" ), autoFocus: false, onSubmit:
			delayStr =>
			{
				if ( !float.TryParse( delayStr, out var seconds ) ) return;

				View.LogEdit( "Transition Delay Changed" );

				Transition.MinDelay = seconds > 0f ? seconds : null;
				Item.ForceUpdate();
			} );

		menu.AddOption( "Clear", "clear", action: () =>
		{
			View.LogEdit( "Transition Delay Removed" );

			Transition.MinDelay = null;
			Item.ForceUpdate();
		} );
	}

	private void BuildAddMaxDelayMenu( global::Editor.Menu menu )
	{
		menu.AddMenu( "Add Maximum Time", "hourglass_bottom" ).AddLineEdit( "Seconds", value: (Transition.MinDelay ?? 1f).ToString( "R" ), autoFocus: true, onSubmit:
			delayStr =>
			{
				if ( !float.TryParse( delayStr, out var seconds ) || seconds < (Transition.MinDelay ?? 0f) ) return;

				View.LogEdit( "Transition Delay Added" );

				Transition.MaxDelay = seconds;
				Item.ForceUpdate();
			} );
	}

	private void BuildModifyMaxDelayMenu( global::Editor.Menu menu )
	{
		var maxDelay = Transition.MaxDelay!.Value;

		menu.AddHeading( "Maximum Time" );
		menu.AddLineEdit( "Seconds", value: maxDelay.ToString( "R" ), autoFocus: false, onSubmit:
			delayStr =>
			{
				if ( !float.TryParse( delayStr, out var seconds ) ) return;

				View.LogEdit( "Transition Delay Changed" );

				Transition.MaxDelay = seconds < Transition.MinDelay ? null : seconds;
				Item.ForceUpdate();
			} );

		menu.AddOption( "Clear", "clear", action: () =>
		{
			View.LogEdit( "Transition Delay Removed" );

			Transition.MaxDelay = null;
			Item.ForceUpdate();
		} );
	}

	public void Delete()
	{
		Transition.MinDelay = null;
		Transition.MaxDelay = null;
	}

	public void DoubleClick()
	{
		var menu = new global::Editor.Menu { DeleteOnClose = true };

		BuildModifyContextMenu( menu );

		menu.OpenAtCursor( true );
	}
}
