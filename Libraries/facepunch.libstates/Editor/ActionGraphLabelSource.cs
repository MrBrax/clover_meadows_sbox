using Editor;
using Facepunch.ActionGraphs;
using System;
using System.Linq;
using System.Text;
using Sandbox.ActionGraphs;

namespace Sandbox.States.Editor;

public abstract record ActionGraphLabelSource<T> : ILabelSource
	where T : Delegate
{
	protected abstract StateMachineView View { get; }
	protected StateMachineComponent StateMachine => View.StateMachine;
	protected abstract string ContextName { get; }
	
	public abstract string Title { get; }

	public string? Icon => ActionGraph is { } graph ? graph.HasErrors() ? "error" : string.IsNullOrEmpty( graph.Icon ) ? DefaultIcon : graph.Icon : null;
	public string? Text => ActionGraph is { } graph ? graph.Title ?? "Unnamed" : null;

	public abstract string? Description { get; }

	public bool IsValid => Delegate is not null;

	string? ILabelSource.Description
	{
		get
		{
			var builder = new StringBuilder();

			builder.Append( $"<p>{Description}</p>" );

			if ( ActionGraph is { } graph )
			{
				if ( graph.Description is { } desc )
				{
					builder.Append( $"<p>{desc}</p>" );
				}

				if ( graph.HasErrors() )
				{
					builder.Append( "<p><font color=\"#ff0000\">" );

					foreach ( var message in graph.Messages.Where( x => x.IsError ) )
					{
						builder.AppendLine( message.Value );
					}

					builder.Append( "</font></p>" );
				}
			}

			return builder.ToString();
		}
	}

	public Color? Color => ActionGraph is { } graph && graph.HasErrors() ? global::Color.Red.Darken( 0.05f ) : (Color?)null;

	protected abstract string DefaultIcon { get; }
	protected abstract T? Delegate { get; set; }

	protected ActionGraph? ActionGraph
	{
		get => Delegate.TryGetActionGraphImplementation( out var graph, out _ ) ? graph : null;
		set => Delegate = (ActionGraph<T>?)value;
	}

	public void BuildAddContextMenu( global::Editor.Menu menu )
	{
		if ( ActionGraph is not null ) return;

		menu.AddOption( $"Add {Title}", DefaultIcon, action: CreateOrEdit );
	}

	public void BuildModifyContextMenu( global::Editor.Menu menu )
	{
		menu.AddHeading( Title );
		menu.AddOption( "Edit", "edit", action: CreateOrEdit );
		menu.AddOption( "Clear", "clear", action: () =>
		{
			View.LogEdit( $"{ContextName} {Title} Removed" );

			Delegate = null;
			ForceUpdate();
		} );
	}

	private void CreateOrEdit()
	{
		if ( Delegate is null )
		{
			View.LogEdit( $"{ContextName} {Title} Added" );

			Delegate = CreateGraph( Title );
			EditorEvent.Run( "actiongraph.inspect", ActionGraph );
			ForceUpdate();
		}
		else
		{
			EditorEvent.Run( "actiongraph.inspect", ActionGraph );
		}
	}

	public void Delete()
	{
		Delegate = null;
	}

	public void DoubleClick()
	{
		CreateOrEdit();
	}

	protected abstract void ForceUpdate();

	protected T CreateGraph( string title )
	{
		var graph = ActionGraph.Create<T>( EditorNodeLibrary );
		var inner = (ActionGraph)graph;

		inner.Title = title;
		inner.SourceLocation = new SourceLocation( StateMachine.GameObject.Scene.Source );
		inner.SetParameters(
			inner.Inputs.Values.Concat( InputDefinition.Target( typeof( GameObject ), StateMachine.GameObject ) ),
			inner.Outputs.Values.ToArray() );

		return graph;
	}
}

public abstract record StateAction( StateItem Item ) : ActionGraphLabelSource<Action>
{
	public State State => Item.State;
	protected override StateMachineView View => Item.View;
	protected override string ContextName => "State";
	protected override void ForceUpdate()
	{
		Item.ForceUpdate();
	}
}

public record StateEnterAction( StateItem Item ) : StateAction( Item )
{
	public override string Title => "Enter";
	public override string Description => "Action performed when entering this state.";
	protected override string DefaultIcon => "login";

	protected override Action? Delegate
	{
		get => State.OnEnterState;
		set => State.OnEnterState = value;
	}
}

public record StateUpdateAction( StateItem Item ) : StateAction( Item )
{
	public override string Title => "Update";
	public override string Description => "Action performed every fixed update while in this state.";
	protected override string DefaultIcon => "update";

	protected override Action? Delegate
	{
		get => State.OnUpdateState;
		set => State.OnUpdateState = value;
	}
}

public record StateLeaveAction( StateItem Item ) : StateAction( Item )
{
	public override string Title => "Leave";
	public override string Description => "Action performed when leaving this state.";
	protected override string DefaultIcon => "logout";

	protected override Action? Delegate
	{
		get => State.OnLeaveState;
		set => State.OnLeaveState = value;
	}
}

public abstract record TransitionActionGraph<T>( TransitionItem Item ) : ActionGraphLabelSource<T>
	where T : Delegate
{
	public Transition Transition => Item.Transition!;
	protected override StateMachineView View => Item.Source.View;
	protected override string ContextName => "Transition";
	protected override void ForceUpdate()
	{
		Item.ForceUpdate();
	}
}

public record TransitionCondition( TransitionItem Item ) : TransitionActionGraph<Func<bool>>( Item )
{
	public override string Title => "Condition";
	public override string Description => "This transition will only be taken if this expression is true.";

	protected override string DefaultIcon => "question_mark";

	protected override Func<bool>? Delegate
	{
		get => Transition.Condition;
		set => Transition.Condition = value;
	}
}

public record TransitionAction( TransitionItem Item ) : TransitionActionGraph<Action>( Item )
{
	public override string Title => "Action";
	public override string Description => "Action performed when this transition is taken.";

	protected override string DefaultIcon => "directions_run";

	protected override Action? Delegate
	{
		get => Transition.OnTransition;
		set => Transition.OnTransition = value;
	}
}
