using System;
using System.Threading.Tasks;
using Sandbox.Network;

namespace Braxnet;

public sealed class TweenManager : GameObjectSystem
{
	
	private List<Tween> _tweens = new();
	
	public TweenManager(Scene scene) : base(scene)
	{
		Listen( Stage.StartUpdate, 0, Update, "Tween" );
	}
	
	private void Update()
	{
		foreach ( var tween in _tweens.ToList() )
		{
			// Log.Info( $"Tween > {tween.PropertyCount}" );
			tween.Update();
			if ( tween.IsFinished )
			{
				// Log.Info( "Tween finished" );
				_tweens.Remove( tween );
			}
		}
	}
	
	/*public Tween Add( float duration, Action<float> onUpdate, Action onFinish = null )
	{
		var tween = new Tween
		{
			Duration = duration,
			OnUpdate = onUpdate,
			OnFinish = onFinish,
		};
		tween.StartTime = 0f;
		_tweens.Add( tween );
		return tween;
	}*/

	public Tween Create()
	{
		var tween = new Tween();
		_tweens.Add( tween );
		return tween;
	}
	
	public static Tween CreateTween()
	{
		var instance = Game.ActiveScene.GetSystem<TweenManager>();
		return instance.Create();
	}
	
}

public class BaseTween
{
	public GameObject GameObject;
	public Action OnFinish;
	
	public Action<GameObject, float> OnUpdate;
	
	public float Duration;
	public TimeSince StartTime;
	
	public bool IsFinished => StartTime > Duration;
	
	public Func<float, float> EaseFunction;
	
	/// <summary>
	///  Execute before the tween starts
	/// </summary>
	public virtual void Setup()
	{
		GameObject.Tags.Add( "tweening" );
		StartTime = 0f;
	}
	
	/// <summary>
	///  Execute after the tween finishes
	/// </summary>
	public virtual void Cleanup()
	{
		GameObject.Tags.Remove( "tweening" );
	}
	
	public BaseTween SetEasing( Func<float, float> easing )
	{
		EaseFunction = easing;
		return this;
	}
	
	public BaseTween SetDelay( float delay )
	{
		StartTime = -delay;
		return this;
	}
	
	public async Task Wait()
	{
		await GameTask.DelaySeconds( Duration );
	}
	
	public virtual void Update()
	{
	}
	
}

public sealed class FloatTween : BaseTween
{
	
	public override void Update()
	{
		// OnUpdate( GameObject, Math.Clamp( StartTime / Duration, 0, 1 ) );
		
		if ( !GameObject.IsValid() )
		{
			StartTime = Duration;
			return;
		}
		
		if ( EaseFunction != null )
		{
			OnUpdate( GameObject, EaseFunction( Math.Clamp( StartTime / Duration, 0, 1 ) ) );
		}
		else
		{
			OnUpdate( GameObject, Math.Clamp( StartTime / Duration, 0, 1 ) );
		}
		
		if ( IsFinished )
		{
			OnFinish?.Invoke();
		}
	}
	
	
	
}

public sealed class PositionTween : BaseTween
{
	
	public Vector3 StartPosition;
	public Vector3 EndPosition;
	
	public override void Setup()
	{
		base.Setup();
		StartPosition = GameObject.Transform.Position;
	}
	
	public override void Update()
	{
		if ( !GameObject.IsValid() )
		{
			StartTime = Duration;
			return;
		}

		if ( StartTime < 0 ) return;
		
		if ( EaseFunction != null )
		{
			GameObject.WorldPosition = Vector3.Lerp( StartPosition, EndPosition, EaseFunction( Math.Clamp( StartTime / Duration, 0, 1 ) ) );
		}
		else
		{
			GameObject.WorldPosition = Vector3.Lerp( StartPosition, EndPosition, Math.Clamp( StartTime / Duration, 0, 1 ) );
		}
		
		if ( IsFinished )
		{
			OnFinish?.Invoke();
		}
	}

	
}


public sealed class ScaleTween : BaseTween
{
	
	public Vector3 StartScale;
	public Vector3 EndScale;
	
	public override void Setup()
	{
		base.Setup();
		StartScale = GameObject.Transform.Scale;
	}
	
	public override void Update()
	{
		if ( !GameObject.IsValid() )
		{
			StartTime = Duration;
			return;
		}
		
		if ( EaseFunction != null )
		{
			GameObject.Transform.Scale = Vector3.Lerp( StartScale, EndScale, EaseFunction( Math.Clamp( StartTime / Duration, 0, 1 ) ) );
		}
		else
		{
			GameObject.Transform.Scale = Vector3.Lerp( StartScale, EndScale, Math.Clamp( StartTime / Duration, 0, 1 ) );
		}
		
		if ( IsFinished )
		{
			OnFinish?.Invoke();
		}
	}
	
}

public sealed class RotationTween : BaseTween
{
	
	public Rotation StartRotation;
	public Rotation EndRotation;
	
	public override void Setup()
	{
		base.Setup();
		StartRotation = GameObject.WorldRotation;
	}
	
	public override void Update()
	{
		if ( !GameObject.IsValid() )
		{
			StartTime = Duration;
			return;
		}
		
		if ( EaseFunction != null )
		{
			GameObject.WorldRotation = Rotation.Lerp( StartRotation, EndRotation, EaseFunction( Math.Clamp( StartTime / Duration, 0, 1 ) ) );
		}
		else
		{
			GameObject.WorldRotation = Rotation.Lerp( StartRotation, EndRotation, Math.Clamp( StartTime / Duration, 0, 1 ) );
		}
		
		if ( IsFinished )
		{
			OnFinish?.Invoke();
		}
	}
	
}


public sealed class Tween
{
	
	public int PropertyCount => _propertyTweens.Count;
	
	// private int _currentTweenIndex;
	private List<BaseTween> _propertyTweens = new();
	
	public bool IsFinished => _propertyTweens.Count == 0;
	
	public Action OnFinish;
	
	private TaskCompletionSource<bool> _taskCompletionSource;
	
	public FloatTween AddFloat( GameObject gameObject, float duration, Action<GameObject, float> onUpdate, Action onFinish = null )
	{
		var tween = new FloatTween
		{
			GameObject = gameObject,
			Duration = duration,
			OnUpdate = onUpdate,
			OnFinish = onFinish,
		};
		_propertyTweens.Add( tween );
		tween.Setup();
		return tween;
	}
	
	public void Update()
	{
		foreach ( var tween in _propertyTweens.ToList() )
		{
			tween.Update();
			if ( tween.IsFinished )
			{
				// Log.Info( "PropertyTween finished" );
				tween.Cleanup();
				_propertyTweens.Remove( tween );
			}
		}
		
		if ( IsFinished )
		{
			// Log.Info( "Tween finished" );
			OnFinish?.Invoke();
			_taskCompletionSource?.SetResult( true );
		}
	}
	
	public async Task Wait()
	{
		_taskCompletionSource = new TaskCompletionSource<bool>();
		await _taskCompletionSource.Task;
	}
	
	/*public TimeSince StartTime;
	public float Duration;
	public Action<float> OnUpdate;
	
	public Action OnFinish;
	
	public Func<float, float> EaseFunction;
	
	public bool IsFinished => Duration < StartTime;
	
	public void Update()
	{
		// OnUpdate( Math.Clamp( StartTime / Duration, 0, 1 ) );
		if ( EaseFunction != null )
		{
			OnUpdate( EaseFunction( Math.Clamp( StartTime / Duration, 0, 1 ) ) );
		}
		else
		{
			OnUpdate( Math.Clamp( StartTime / Duration, 0, 1 ) );
		}
		
		if ( IsFinished )
		{
			OnFinish?.Invoke();
		}
	}
	
	public Tween SetEasing( Func<float, float> easing )
	{
		EaseFunction = easing;
		return this;
	}
	
	public void Cancel()
	{
		OnFinish?.Invoke();
	}
	
	public async Task Wait()
	{
		await GameTask.DelaySeconds( Duration );
	}*/

	public PositionTween AddPosition( GameObject resourceModel, Vector3 transformPosition, float duration )
	{
		var tween = new PositionTween
		{
			GameObject = resourceModel,
			Duration = duration,
			EndPosition = transformPosition,
		};
		_propertyTweens.Add( tween );
		tween.Setup();
		return tween;
	}
	
	public ScaleTween AddScale( GameObject resourceModel, Vector3 transformScale, float duration )
	{
		var tween = new ScaleTween
		{
			GameObject = resourceModel,
			Duration = duration,
			EndScale = transformScale,
		};
		_propertyTweens.Add( tween );
		tween.Setup();
		return tween;
	}
	
	public RotationTween AddRotation( GameObject resourceModel, Rotation rotation, float duration )
	{
		var tween = new RotationTween
		{
			GameObject = resourceModel,
			Duration = duration,
			EndRotation = rotation,
		};
		_propertyTweens.Add( tween );
		tween.Setup();
		return tween;
	}
}
