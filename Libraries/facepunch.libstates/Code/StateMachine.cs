using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Diagnostics;

namespace Sandbox.States;

[Title( "State Machine" ), Icon( "smart_toy" ), Category( "State Machines" )]
public sealed class StateMachineComponent : Component
{
	/// <summary>
	/// How many instant state transitions in a row until we throw an error?
	/// </summary>
	public const int MaxInstantTransitions = 16;

	private readonly Dictionary<int, State> _states = new();
	private readonly Dictionary<int, Transition> _transitions = new();

	private int _nextId = 0;

	/// <summary>
	/// All states in this machine.
	/// </summary>
	public IEnumerable<State> States => _states.Values;

	/// <summary>
	/// All transitions between states in this machine.
	/// </summary>
	public IEnumerable<Transition> Transitions => _transitions.Values;

	/// <summary>
	/// Which state becomes active when the machine starts?
	/// </summary>
	public State? InitialState { get; set; }

	/// <summary>
	/// Which state is currently active?
	/// </summary>
	public State? CurrentState
	{
		get => CurrentStateId is {} id ? _states!.GetValueOrDefault( id ) : null;
		private set => CurrentStateId = value?.Id;
	}

	/// <summary>
	/// How long have we been in the current state?
	/// </summary>
	public float StateTime { get; private set; }

	[Property] private int? CurrentStateId { get; set; }

	private bool _firstUpdate = true;

	protected override void OnStart()
	{
		if ( !Network.IsProxy && InitialState is { } initial )
		{
			CurrentState = initial;
		}
	}

	/// <summary>
	/// Send a message to trigger a transition with a matching <see cref="Transition.Message"/>.
	/// </summary>
	/// <param name="message">Message name.</param>
	public void SendMessage( string message )
	{
		if ( IsProxy )
		{
			Log.Warning( $"Can't call {nameof(SendMessage)} on a StateMachine owned by another connection." );
			return;
		}

		if ( CurrentState?.GetNextTransition( message ) is { } transition )
		{
			DoTransition( transition );
		}
	}

	private static void InvokeSafe( Action? action )
	{
		try
		{
			action?.Invoke();
		}
		catch ( Exception ex )
		{
			Log.Error( ex );
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( _firstUpdate )
		{
			_firstUpdate = false;

			CurrentState?.Entered();
			InvokeSafe( CurrentState?.OnEnterState );
		}

		if ( !Network.IsProxy )
		{
			var transitions = 0;
			var prevTime = StateTime;

			StateTime += Time.Delta;

			while ( transitions++ < MaxInstantTransitions && CurrentState?.GetNextTransition( prevTime, StateTime ) is { } transition )
			{
				DoTransition( transition );
				prevTime = 0f;
			}
		}

		InvokeSafe( CurrentState?.OnUpdateState );
	}

	[Rpc.Broadcast( NetFlags.Reliable | NetFlags.OwnerOnly )]
	private void BroadcastTransition( int transitionId )
	{
		var transition = _transitions!.GetValueOrDefault( transitionId )
			?? throw new Exception( $"Unknown transition id: {transitionId}" );

		DoTransitionInternal( transition );
	}

	private void DoTransition( Transition transition )
	{
		if ( !IsProxy && Network.Active )
		{
			BroadcastTransition( transition.Id );
		}
		else
		{
			DoTransitionInternal( transition );
		}
	}

	private void DoTransitionInternal( Transition transition )
	{
		var current = CurrentState;

		if ( current != null && current != transition.Source )
		{
			Log.Warning( $"Expected to transition from {transition.Source}, but we're in state {current}!" );
		}

		InvokeSafe( current?.OnLeaveState );
		InvokeSafe( transition.OnTransition );

		transition.LastTransitioned = 0f;

		CurrentState = current = transition.Target;
		StateTime = 0f;

		current.Entered();
		InvokeSafe( current.OnEnterState );
	}

	public State AddState()
	{
		var state = new State( this, _nextId++ );

		_states.Add( state.Id, state );

		state.IsValid = true;

		InitialState ??= state;

		return state;
	}

	internal void RemoveState( State state )
	{
		Assert.AreEqual( this, state.StateMachine );
		Assert.AreEqual( state, _states[state.Id] );

		if ( InitialState == state )
		{
			InitialState = null;
		}

		if ( CurrentState == state )
		{
			CurrentState = null;
		}

		var transitions = Transitions
			.Where( x => x.Source == state || x.Target == state )
			.ToArray();

		foreach ( var transition in transitions )
		{
			transition.Remove();
		}

		_states.Remove( state.Id );

		state.IsValid = false;
	}

	internal Transition AddTransition( State source, State target )
	{
		ArgumentNullException.ThrowIfNull( source, nameof( source ) );
		ArgumentNullException.ThrowIfNull( target, nameof( target ) );

		Assert.AreEqual( this, source.StateMachine );
		Assert.AreEqual( this, target.StateMachine );

		var transition = new Transition( _nextId++, source, target );

		_transitions.Add( transition.Id, transition );

		transition.IsValid = true;

		source.InvalidateTransitions();

		return transition;
	}

	internal void RemoveTransition( Transition transition )
	{
		Assert.AreEqual( this, transition.StateMachine );
		Assert.AreEqual( transition, _transitions[transition.Id] );

		_transitions.Remove( transition.Id );

		transition.IsValid = false;
		transition.Source.InvalidateTransitions();
	}

	internal void Clear()
	{
		_states.Clear();
		_transitions.Clear();

		InitialState = null;
		StateTime = 0f;

		_nextId = 0;
	}

	[Property]
	private Model Serialized
	{
		get => SerializeInternal();
		set => DeserializeInternal( value, true );
	}

	internal record Model(
		IReadOnlyList<State.Model> States,
		IReadOnlyList<Transition.Model> Transitions,
		int? InitialStateId );

	internal Model SerializeInternal()
	{
		return new Model(
			States.Select( x => x.Serialize() ).OrderBy( x => x.Id ).ToArray(),
			Transitions.Select( x => x.Serialize() ).OrderBy( x => x.Id ).ToArray(),
			InitialState?.Id );
	}

	internal void DeserializeInternal( Model model, bool replace )
	{
		if ( replace )
		{
			Clear();
		}

		var idOffset = _nextId;

		foreach ( var stateModel in model.States )
		{
			var state = new State( this, stateModel.Id + idOffset );

			_states.Add( state.Id, state );
			_nextId = Math.Max( _nextId, state.Id + 1 );

			state.IsValid = true;

			state.Deserialize( stateModel );
		}

		foreach ( var transitionModel in model.Transitions )
		{
			var transition = new Transition( transitionModel.Id + idOffset,
				_states[transitionModel.SourceId + idOffset],
				_states[transitionModel.TargetId + idOffset] );

			_transitions.Add( transition.Id, transition );
			_nextId = Math.Max( _nextId, transition.Id + 1 );

			transition.IsValid = true;

			transition.Deserialize( transitionModel );
		}

		if ( replace )
		{
			InitialState = model.InitialStateId is { } id ? _states[id] : null;
		}
	}

	public string Serialize( IEnumerable<State> states, IEnumerable<Transition> transitions )
	{
		var stateSet = states
			.Where( x => x.IsValid && x.StateMachine == this )
			.ToHashSet();

		var transitionSet = transitions
			.Where( x => x.IsValid && stateSet.Contains( x.Source ) && stateSet.Contains( x.Target ) )
			.ToHashSet();

		var model = new Model(
			stateSet.Select( x => x.Serialize() ).OrderBy( x => x.Id ).ToArray(),
			transitionSet.Select( x => x.Serialize() ).OrderBy( x => x.Id ).ToArray(),
			null );

		return Json.Serialize( model );
	}

	public string SerializeAll()
	{
		return Json.Serialize( Serialized );
	}

	public void DeserializeAll( string json )
	{
		Serialized = Json.Deserialize<Model>( json );
	}

	public (IReadOnlyList<State> States, IReadOnlyList<Transition> Transitions) DeserializeInsert( string json )
	{
		var model = Json.Deserialize<Model>( json );
		var baseId = _nextId;

		DeserializeInternal( model, false );

		return (
			States.Where( x => x.Id >= baseId ).OrderBy( x => x.Id ).ToArray(),
			Transitions.Where( x => x.Id >= baseId ).OrderBy( x => x.Id ).ToArray() );
	}
}
