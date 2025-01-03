﻿using System;
using Clover.Components;
using Clover.Data;
using Clover.Interactable;
using Clover.Player;
using Sandbox.Diagnostics;
using Sandbox.States;

namespace Clover.Npc;

[Title( "Base Npc" )]
[Icon( "face" )]
[Category( "Clover/Npc" )]
public class BaseNpc : Component, IInteract
{
	public enum NpcState
	{
		Idle,
		Walking,
		Interacting
	}

	[RequireComponent] public NavMeshAgent Agent { get; set; }

	[Property] public string Name { get; set; }

	public GameObject InteractionTarget;

	public NpcState State { get; set; } = NpcState.Idle;

	private TimeUntil _nextAction;
	private TimeSince _startWalking;

	private const float WalkRadius = 256f;

	[Property] public DialogueCollection DialogueCollection { get; set; }

	public void SetState( NpcState state )
	{
		Log.Info( $"BaseNpc SetState: {state}" );
		State = state;
	}

	protected override void OnStart()
	{
		_nextAction = Random.Shared.Float( 3, 15 );
		SetState( NpcState.Idle );
	}

	private void Idle()
	{
		Agent.Stop();
		TargetPosition = null;
		if ( _nextAction )
		{
			WalkToRandomTarget();
			SetState( NpcState.Walking );
		}
	}

	private void Walking()
	{
		LookAt( WorldPosition + Agent.Velocity.Normal );
		if ( IsCloseToTarget )
		{
			_nextAction = Random.Shared.Float( 3, 15 );
			SetState( NpcState.Idle );
		}
		else if ( _startWalking > 20f )
		{
			Log.Warning( "Villager Walking: Took too long to reach target" );
			_nextAction = Random.Shared.Float( 3, 15 );
			SetState( NpcState.Idle );
		}
	}

	private void Interacting()
	{
		if ( InteractionTarget.IsValid() )
		{
			LookAt( InteractionTarget );
			if ( InteractionTarget.WorldPosition.Distance( WorldPosition ) > 128f )
			{
				if ( InteractionTarget.Components.TryGet<PlayerCharacter>( out var player ) )
				{
					player.PlayerInteract.InteractionTarget = null;
				}

				_nextAction = Random.Shared.Float( 3, 8 );
				EndInteraction();
			}
		}
		else
		{
			_nextAction = Random.Shared.Float( 3, 8 );
			EndInteraction();
		}
	}

	public void LoadDialogue( Dialogue dialogue )
	{
		var window = DialogueManager.Instance.DialogueWindow;

		if ( window == null )
		{
			Log.Error( "BaseNpc: No dialogue window found" );
			return;
		}

		window.Enabled = true;

		window.LoadDialogue( dialogue );
	}

	public Vector3? TargetPosition;

	public bool IsCloseToTarget => TargetPosition.HasValue && WorldPosition.Distance( TargetPosition.Value ) < 32f;

	public void WalkToRandomTarget()
	{
		var pos = Scene.NavMesh.GetClosestPoint( WorldPosition +
		                                         new Vector3( Random.Shared.Float( -WalkRadius, WalkRadius ),
			                                         Random.Shared.Float( -WalkRadius, WalkRadius ), 0 ) );

		if ( !pos.HasValue )
		{
			Log.Error( "Villager WalkToRandomTarget: No valid position found" );
			return;
		}

		var path = Scene.NavMesh.GetSimplePath( WorldPosition, pos.Value );

		if ( path.Count == 0 )
		{
			Log.Error( "Villager WalkToRandomTarget: No path found" );
			return;
		}

		if ( path.Count < 2 )
		{
			Log.Error( "Villager WalkToRandomTarget: Path is too short" );
			return;
		}

		_startWalking = 0;
		TargetPosition = pos.Value;

		Log.Info( "Villager WalkToRandomTarget: " + TargetPosition );
		Agent.MoveTo( TargetPosition.Value );
	}

	public virtual void StartInteract( PlayerCharacter player )
	{
		Log.Info( "BaseNpc StartInteract" );

		if ( InteractionTarget.IsValid() )
		{
			Log.Error( "BaseNpc StartInteract: Busy" );
			return;
		}

		player.PlayerInteract.InteractionTarget = GameObject;
		player.ModelLookAt( WorldPosition );
		InteractionTarget = player.GameObject;
		SetState( NpcState.Interacting );
		DispatchDialogue();
	}

	public void EndInteraction()
	{
		if ( InteractionTarget.IsValid() )
		{
			if ( InteractionTarget.Components.TryGet<PlayerCharacter>( out var player ) )
			{
				player.PlayerInteract.InteractionTarget = null;
			}
		}

		InteractionTarget = null;
		SetState( NpcState.Idle );
	}

	protected void DispatchDialogue()
	{
		if ( DialogueCollection == null )
		{
			Log.Error( "BaseNpc DispatchDialogue: No dialogue collection found" );
			EndInteraction();
			return;
		}

		Assert.NotNull( DialogueManager.Instance.DialogueWindow, "BaseNpc DispatchDialogue: No dialogue window found" );
		Assert.NotNull( PlayerCharacter.Local, "BaseNpc DispatchDialogue: No player found" );
		Assert.NotNull( DialogueCollection.Entries, "BaseNpc DispatchDialogue: No dialogue entries found" );
		Assert.True( DialogueCollection.Entries.Count > 0, "BaseNpc DispatchDialogue: No dialogue entries found" );

		var entries = DialogueCollection.Entries.ToList().OrderBy( x => Guid.NewGuid() );
		foreach ( var dialogue in entries )
		{
			if ( dialogue.Condition == null || dialogue.Condition.Invoke( DialogueManager.Instance.DialogueWindow,
				    PlayerCharacter.Local,
				    new List<GameObject>() { GameObject } ) )
			{
				DialogueManager.Instance.DialogueWindow.SetData( "PlayerName", PlayerCharacter.Local.PlayerName );
				DialogueManager.Instance.DialogueWindow.SetTarget( 0, GameObject );
				LoadDialogue( dialogue.DialogueData );

				DialogueManager.Instance.DialogueWindow.OnDialogueEnd += EndInteraction;

				return;
			}
		}
	}

	public string GetInteractName()
	{
		return $"Talk to {Name}";
	}

	protected Rotation TargetLookAt;

	public void LookAt( Vector3 target )
	{
		var dir = (target - WorldPosition).Normal;
		var rot = Rotation.LookAt( dir );
		TargetLookAt = rot.Angles().WithPitch( 0 );
	}

	public void LookAt( GameObject target )
	{
		LookAt( target.WorldPosition );
	}

	protected override void OnFixedUpdate()
	{
		StateLogic();
		WorldRotation = Rotation.Slerp( WorldRotation, TargetLookAt, Time.Delta * 5f );
	}

	protected virtual void StateLogic()
	{
		switch ( State )
		{
			case NpcState.Idle:
				Idle();
				break;
			case NpcState.Walking:
				Walking();
				break;
			case NpcState.Interacting:
				Interacting();
				break;
		}
	}
}
