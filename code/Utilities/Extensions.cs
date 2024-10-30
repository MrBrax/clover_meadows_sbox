using System;

namespace Clover.Utilities;

public static class Extensions
{
	/// <summary>
	/// Returns the result of a framerate-independent "lerp smoothing" to <paramref name="target"/>
	/// using a decay constant. For best results, choose a value for <paramref name="decay"/> that is
	/// between 1 and 25.
	/// An explanation of this method can be found here: https://youtu.be/LSNQuFEDOyQ
	/// </summary>
	public static float ExpDecayTo( this float from, float target, float decay )
	{
		return target + (from - target) * MathF.Exp( -decay * Time.Delta );
	}

	public static Vector3 ExpDecayTo( this Vector3 from, Vector3 target, float decay )
	{
		return target + (from - target) * MathF.Exp( -decay * Time.Delta );
	}
}
