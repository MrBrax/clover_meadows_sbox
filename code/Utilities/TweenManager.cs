using System;
using System.Threading.Tasks;

namespace Braxnet;

public sealed class TweenManager : GameObjectSystem
{
	public static TweenManager Instance => Game.ActiveScene.GetSystem<TweenManager>();

	private List<Tween> _tweens = new();

	public TweenManager( Scene scene ) : base( scene )
	{
		Listen( Stage.StartUpdate, 0, Update, "Tween" );
	}

	public void RemoveTween( Tween tween )
	{
		_tweens.Remove( tween );
	}

	private void Update()
	{
		foreach ( var tween in _tweens.ToList() )
		{
			// Log.Info( $"Tween > {tween.PropertyCount}" );
			tween.Update();
			/*if ( tween.IsFinished )
			{
				// Log.Info( "Tween finished" );
				_tweens.Remove( tween );
			}*/
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
	public Action OnBounce;

	public Action<GameObject, float> OnUpdate;

	public float Duration;
	public TimeSince StartTime;

	public bool Parallel;

	public float Progress => Math.Clamp( StartTime / Duration, 0, 1 );

	public bool IsFinished => StartTime > Duration;

	public Func<float, float> EaseFunction;

	public bool IsSetup;

	/// <summary>
	///  Execute before the tween starts
	/// </summary>
	public virtual void Setup()
	{
		GameObject?.Tags.Add( "tweening" );
		StartTime = 0f;
	}

	/// <summary>
	///  Execute after the tween finishes
	/// </summary>
	public virtual void Cleanup()
	{
		GameObject?.Tags.Remove( "tweening" );
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

	protected float GetFraction()
	{
		return EaseFunction != null
			? EaseFunction( Math.Clamp( StartTime / Duration, 0, 1 ) )
			: Math.Clamp( StartTime / Duration, 0, 1 );
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

		/*if ( EaseFunction != null )
		{
			OnUpdate( GameObject, EaseFunction( Math.Clamp( StartTime / Duration, 0, 1 ) ) );
		}
		else
		{
			OnUpdate( GameObject, Math.Clamp( StartTime / Duration, 0, 1 ) );
		}
		*/

		OnUpdate?.Invoke( GameObject, GetFraction() );

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
		StartPosition = GameObject.WorldPosition;
	}

	private bool _bounceDebounce;

	public override void Update()
	{
		if ( !GameObject.IsValid() )
		{
			StartTime = Duration;
			return;
		}

		if ( StartTime < 0 ) return;

		/*if ( EaseFunction != null )
		{
			GameObject.WorldPosition = Vector3.Lerp( StartPosition, EndPosition, EaseFunction( Math.Clamp( StartTime / Duration, 0, 1 ) ) );
		}
		else
		{
			GameObject.WorldPosition = Vector3.Lerp( StartPosition, EndPosition, Math.Clamp( StartTime / Duration, 0, 1 ) );
		}*/

		// var frac = EaseFunction != null ? EaseFunction( Math.Clamp( StartTime / Duration, 0, 1 ) ) : Math.Clamp( StartTime / Duration, 0, 1 );
		var frac = GetFraction();

		GameObject.WorldPosition = Vector3.Lerp( StartPosition, EndPosition, frac );

		// Log.Info( GameObject.Name + ": " + GameObject.WorldPosition.Distance( EndPosition ) + " " + frac );
		if ( Progress < 0.9f )
		{
			var dist = GameObject.WorldPosition.Distance( EndPosition );
			if ( dist.Floor() == 0 )
			{
				if ( !_bounceDebounce )
				{
					OnBounce?.Invoke();
					_bounceDebounce = true;
				}
			}
			else
			{
				_bounceDebounce = false;
			}
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
		StartScale = GameObject.WorldScale;
	}

	public override void Update()
	{
		if ( !GameObject.IsValid() )
		{
			StartTime = Duration;
			return;
		}

		/*if ( EaseFunction != null )
		{
			GameObject.WorldScale = Vector3.Lerp( StartScale, EndScale, EaseFunction( Math.Clamp( StartTime / Duration, 0, 1 ) ) );
		}
		else
		{
			GameObject.WorldScale = Vector3.Lerp( StartScale, EndScale, Math.Clamp( StartTime / Duration, 0, 1 ) );
		}*/

		GameObject.WorldScale = Vector3.Lerp( StartScale, EndScale, GetFraction() );

		// ( $"{GameObject.Name} {GameObject.WorldScale.z} {GetFraction()}" );

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
	public bool Local;

	public override void Setup()
	{
		base.Setup();
		StartRotation = Local ? GameObject.LocalRotation : GameObject.WorldRotation;
	}

	public override void Update()
	{
		if ( !GameObject.IsValid() )
		{
			StartTime = Duration;
			return;
		}

		/*if ( EaseFunction != null )
		{
			GameObject.WorldRotation = Rotation.Lerp( StartRotation, EndRotation, EaseFunction( Math.Clamp( StartTime / Duration, 0, 1 ) ) );
		}
		else
		{
			GameObject.WorldRotation = Rotation.Lerp( StartRotation, EndRotation, Math.Clamp( StartTime / Duration, 0, 1 ) );
		}*/

		var frac = GetFraction();

		if ( Local )
		{
			GameObject.LocalRotation = Rotation.Lerp( StartRotation, EndRotation, frac );
		}
		else
		{
			GameObject.WorldRotation = Rotation.Lerp( StartRotation, EndRotation, frac );
		}

		if ( StartRotation.Distance( EndRotation ).AlmostEqual( 0 ) && frac < 0.9f )
		{
			OnBounce?.Invoke();
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

	// private int _currentTweenIndex;

	public FloatTween AddFloat( GameObject gameObject, float duration, Action<GameObject, float> onUpdate,
		Action onFinish = null )
	{
		var tween = new FloatTween
		{
			GameObject = gameObject, Duration = duration, OnUpdate = onUpdate, OnFinish = onFinish,
		};
		_propertyTweens.Add( tween );
		// tween.Setup();
		return tween;
	}

	public PositionTween AddPosition( GameObject resourceModel, Vector3 transformPosition, float duration )
	{
		var tween = new PositionTween
		{
			GameObject = resourceModel, Duration = duration, EndPosition = transformPosition,
		};
		_propertyTweens.Add( tween );
		// tween.Setup();
		return tween;
	}

	public ScaleTween AddScale( GameObject resourceModel, Vector3 transformScale, float duration )
	{
		var tween = new ScaleTween { GameObject = resourceModel, Duration = duration, EndScale = transformScale, };
		_propertyTweens.Add( tween );
		// tween.Setup();
		return tween;
	}

	public RotationTween AddRotation( GameObject resourceModel, Rotation rotation, float duration )
	{
		var tween = new RotationTween { GameObject = resourceModel, Duration = duration, EndRotation = rotation, };
		_propertyTweens.Add( tween );
		// tween.Setup();
		return tween;
	}

	public RotationTween AddLocalRotation( GameObject model, Rotation rotation, float duration )
	{
		var tween = new RotationTween { GameObject = model, Duration = duration, EndRotation = rotation, Local = true };
		_propertyTweens.Add( tween );
		// tween.Setup();
		return tween;
	}

	public void Update()
	{
		/*foreach ( var tween in _propertyTweens.ToList() )
		{
			tween.Update();
			if ( tween.IsFinished )
			{
				// Log.Info( "PropertyTween finished" );
				tween.Cleanup();
				_propertyTweens.Remove( tween );
			}
		}*/

		if ( _propertyTweens.Count == 0 )
		{
			Log.Info( "Tween finished" );
			OnFinish?.Invoke();
			_taskCompletionSource?.SetResult( true );
			_taskCompletionSource = null;
			TweenManager.Instance.RemoveTween( this );
			return;
		}

		// Log.Info( $"Tween has {_propertyTweens.Count} tweens" );

		// separately handle parallel tweens
		var parallelTweens = _propertyTweens.Where( x => x.Parallel ).ToList();
		foreach ( var tween in parallelTweens )
		{
			if ( !tween.IsSetup )
			{
				tween.Setup();
				tween.IsSetup = true;
			}

			tween.Update();

			if ( tween.IsFinished )
			{
				// Log.Info( $"Parallel tween finished, {_propertyTweens.Count} tweens left" );
				tween.Cleanup();
				_propertyTweens.Remove( tween );
			}
		}

		var currentTween = _propertyTweens.FirstOrDefault( x => !x.Parallel );

		if ( currentTween == null )
		{
			// Log.Warning( $"Tween is null, no tweens left" );
			return;
		}

		if ( !currentTween.IsSetup )
		{
			currentTween.Setup();
			currentTween.IsSetup = true;
		}

		currentTween.Update();

		if ( currentTween.IsFinished )
		{
			currentTween.Cleanup();
			_propertyTweens.Remove( currentTween );

			// Log.Info( $"Sync tween finished, {_propertyTweens.Count} tweens left" );

			/*if ( _propertyTweens.Count > 0 )
			{
				_currentTweenIndex = Math.Clamp( _currentTweenIndex + 1, 0, _propertyTweens.Count - 1 );
			}*/
		}
	}

	public async Task Wait()
	{
		_taskCompletionSource = new TaskCompletionSource<bool>();
		await _taskCompletionSource.Task;
	}
}
