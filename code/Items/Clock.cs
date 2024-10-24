﻿using System;
using Clover.WorldBuilder;

namespace Clover.Items;

[Category( "Clover/Items" )]
public class Clock : Component, ITimeEvent
{
	[Property] public SkinnedModelRenderer Renderer { get; set; }

	[Property] public SoundEvent ArmMoveSound { get; set; }
	[Property] public SoundEvent TickSound { get; set; }

	private SoundHandle _tickSoundHandle;

	protected override void OnStart()
	{
		base.OnStart();
		UpdateArms();

		if ( TickSound != null )
		{
			Log.Info( "Playing tick sound" );
			_tickSoundHandle = GameObject.PlaySound( TickSound );
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if ( _tickSoundHandle.IsValid() )
		{
			_tickSoundHandle.Stop();
		}
	}

	private void UpdateArms()
	{
		var hourBone = Renderer.Model.Bones.GetBone( "hour" );
		var minuteBone = Renderer.Model.Bones.GetBone( "minute" );

		if ( hourBone == null )
		{
			Log.Error( "Hour bone not found" );
			return;
		}

		if ( minuteBone == null )
		{
			Log.Error( "Minute bone not found" );
			return;
		}

		var time = TimeManager.Time;

		var hourAngle = (time.Hour % 12) * 30 + time.Minute * 0.5f;
		var minuteAngle = time.Minute * 6;

		// invert hour and minute angle
		hourAngle = -hourAngle;
		minuteAngle = -minuteAngle;

		var basePosition = Vector3.Forward * 1.2f;

		var hourRotation = Rotation.Identity;
		hourRotation = hourRotation.RotateAroundAxis( Vector3.Right, -90 );
		hourRotation = hourRotation.RotateAroundAxis( Vector3.Up, 90 );
		hourRotation = hourRotation.RotateAroundAxis( Vector3.Up, hourAngle );

		var minuteRotation = Rotation.Identity;
		minuteRotation = minuteRotation.RotateAroundAxis( Vector3.Right, -90 );
		minuteRotation = minuteRotation.RotateAroundAxis( Vector3.Up, 90 );
		minuteRotation = minuteRotation.RotateAroundAxis( Vector3.Up, minuteAngle );

		Renderer?.SetBoneTransform( hourBone,
			new Transform( basePosition, hourRotation ) );

		Renderer?.SetBoneTransform( minuteBone,
			new Transform( basePosition, minuteRotation ) );

		if ( ArmMoveSound != null )
		{
			GameObject.PlaySound( ArmMoveSound );
		}
	}

	public void OnNewHour( int hour )
	{
		UpdateArms();
	}

	public void OnNewMinute( int minute )
	{
		UpdateArms();
	}
}
