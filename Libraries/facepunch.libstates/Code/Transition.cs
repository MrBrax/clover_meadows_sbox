using System;

namespace Sandbox.States;

public sealed class Transition : IComparable<Transition>, IValid
{
	private float? _minDelay;
	private float? _maxDelay;

	private Func<bool>? _condition;
	private string? _message;

	/// <summary>
	/// The state machine containing this transition.
	/// </summary>
	public StateMachineComponent StateMachine => Source.StateMachine;

	/// <summary>
	/// Unique ID of this transition in the <see cref="StateMachineComponent"/>.
	/// </summary>
	public int Id { get; }

	/// <summary>
	/// The state this transition originates from.
	/// </summary>
	public State Source { get; }

	/// <summary>
	/// The destination of this transition.
	/// </summary>
	public State Target { get; }

	/// <summary>
	/// Does this transition still belong to a state.
	/// </summary>
	public bool IsValid { get; internal set; }

	/// <summary>
	/// This transition has either a <see cref="Condition"/> or <see cref="Message"/>.
	/// </summary>
	public bool IsConditional => Condition is not null || Message is not null;

	/// <summary>
	/// This transition has either a min or max delay.
	/// </summary>
	public bool HasDelay => MinDelay is not null || MaxDelay is not null;

	public RealTimeSince LastTransitioned { get; internal set; }

	internal Transition( int id, State source, State target )
	{
		Source = source;
		Target = target;
		Id = id;
	}

	/// <summary>
	/// Optional delay before this transition can be taken. If <see cref="MaxDelay"/> is also provided,
	/// but without a <see cref="Condition"/>, then a uniformly random delay is selected between min and max.
	/// </summary>
	public float? MinDelay
	{
		get => _minDelay;
		set
		{
			_minDelay = value;
			Source.InvalidateTransitions();
		}
	}

	/// <summary>
	/// Optional delay until this transition can no longer be taken. If <see cref="MinDelay"/> is also provided,
	/// but without a <see cref="Condition"/>, then a uniformly random delay is selected between min and max.
	/// </summary>
	public float? MaxDelay
	{
		get => _maxDelay;
		set
		{
			_maxDelay = value;
			Source.InvalidateTransitions();
		}
	}

	public (float Min, float Max) DelayRange
	{
		get
		{
			var min = Math.Max( 0f, MinDelay ?? 0f );
			var max = Math.Max( MaxDelay ?? (IsConditional ? float.PositiveInfinity : min), min );

			return (min, max);
		}
	}

	/// <summary>
	/// Optional message string that will trigger this condition.
	/// Messages are sent with <see cref="StateMachineComponent.SendMessage"/>.
	/// </summary>
	public string? Message
	{
		get => _message;
		set
		{
			_message = value;
			Source.InvalidateTransitions();
		}
	}

	/// <summary>
	/// Optional condition to evaluate. If provided, the transition will be taken
	/// as soon as the condition evaluates to true, given we are between <see cref="MinDelay"/>
	/// and <see cref="MaxDelay"/>.
	/// </summary>
	public Func<bool>? Condition
	{
		get => _condition;
		set
		{
			_condition = value;
			Source.InvalidateTransitions();
		}
	}

	/// <summary>
	/// Action performed when this transition is taken.
	/// </summary>
	public Action? OnTransition { get; set; }

	public void Remove()
	{
		if ( !IsValid ) return;
		StateMachine.RemoveTransition( this );
	}

	public int CompareTo( Transition? other )
	{
		if ( other is null ) return 1;

		var minDelayCompare = (MinDelay ?? float.PositiveInfinity).CompareTo( other.MinDelay ?? float.PositiveInfinity );
		if ( minDelayCompare != 0 ) return minDelayCompare;

		var maxDelayCompare = (MaxDelay ?? float.PositiveInfinity).CompareTo( other.MaxDelay ?? float.PositiveInfinity );
		if ( maxDelayCompare != 0 ) return maxDelayCompare;

		var conditionCompare = (Condition is null).CompareTo( other.Condition is null );
		if ( conditionCompare != 0 ) return conditionCompare;

		var messageCompare = (Message is null).CompareTo( other.Message is null );
		if ( messageCompare != 0 ) return messageCompare;

		return Target.Id.CompareTo( other.Target.Id );
	}

	internal record Model( int Id, int SourceId, int TargetId, float? Delay, float? MinDelay, float? MaxDelay, string? Message, Func<bool>? Condition, Action? OnTransition );

	internal Model Serialize()
	{
		return new Model( Id, Source.Id, Target.Id, null, MinDelay, MaxDelay, Message, Condition, OnTransition );
	}

	internal void Deserialize( Model model )
	{
		if ( model.Delay is not null )
		{
			MinDelay = model.Delay;
			MaxDelay = null;
		}
		else
		{
			MinDelay = model.MinDelay;
			MaxDelay = model.MaxDelay;
		}

		Message = model.Message;
		Condition = model.Condition;
		OnTransition = model.OnTransition;
	}

	public void CopyFrom( Transition other )
	{
		Deserialize( other.Serialize() );
	}
	public override string ToString()
	{
		return $"{{ Id = {Id}, Source = {Source}, Target = {Target} }}";
	}
}
