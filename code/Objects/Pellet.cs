using System;
using Clover.Carriable;

namespace Clover.Objects;

public class Pellet : Component, Component.ICollisionListener, Component.ITriggerListener
{
	[Property] public Collider Hitbox { get; set; }
	
	[Property] public SoundEvent HitSound { get; set; }

	public float Speed;

	public Vector3 StartPosition;

	private TimeSince _shot;

	public PelletGun PelletGun { get; set; }

	public Action<GameObject, PelletGun> OnHit;
	public Action OnTimeout;

	protected override void OnStart()
	{
		_shot = 0;
	}

	void ICollisionListener.OnCollisionStart( Collision collision )
	{
		Log.Info( "Pellet collided with " + collision.Other.GameObject.Name );
		OnHitObject( collision.Other.GameObject );
	}
	
	void ICollisionListener.OnCollisionStop( CollisionStop collision )
	{
		Log.Info( "Pellet stopped colliding with " + collision.Other.GameObject.Name );
	}

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		Log.Info( "Pellet entered trigger with " + other.GameObject.Name );
		OnHitObject( other.GameObject );
	}
	
	void ITriggerListener.OnTriggerExit( Collider other )
	{
		Log.Info( "Pellet exited trigger with " + other.GameObject.Name );
	}

	void ICollisionListener.OnCollisionUpdate( Collision collision )
	{
		Log.Info( "Pellet collided with " + collision.Other.GameObject.Name );
	}
	
	private void OnHitObject( GameObject otherGameObject )
	{
		
		OnHit?.Invoke( otherGameObject, PelletGun );
		
		Log.Info( $"Pellet hit {otherGameObject.Name}" );
		Log.Info( otherGameObject );

		PlayHitSound();
		DestroyGameObject();
	}

	private void PlayHitSound()
	{
		SoundEx.Play( HitSound, WorldPosition );
	}

	protected override void OnFixedUpdate()
	{
		WorldPosition += WorldRotation.Forward * Speed * Time.Delta;
		
		var trace = Scene.Trace.Ray( WorldPosition, WorldPosition + WorldRotation.Forward * 8f )
			.IgnoreGameObject( GameObject )
			.IgnoreGameObjectHierarchy( PelletGun.Player.GameObject )
			.Run();

		if ( trace.Hit && trace.GameObject.IsValid() )
		{
			OnHitObject( trace.GameObject );
			return;
		}

		if ( _shot > 2 )
		{
			Log.Info( "Pellet timed out" );
			OnTimeout?.Invoke();
			DestroyGameObject();
		}
		else if ( WorldPosition.Distance( StartPosition ) > 1024 )
		{
			Log.Info( "Pellet went too far" );
			OnTimeout?.Invoke();
			DestroyGameObject();
		}
	}
}
