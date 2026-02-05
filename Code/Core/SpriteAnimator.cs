using Sandbox;

namespace Beastborne.Core;

/// <summary>
/// Global sprite animation timing - provides synchronized frame indices for UI animations
/// </summary>
public static class SpriteAnimator
{
	private static float _timer = 0f;
	private static float _frameRate = 8f; // 8 FPS default
	private static int _globalFrame = 0;
	private static int _lastUpdateFrame = -1;

	/// <summary>
	/// Current global animation frame (increments at frameRate FPS)
	/// </summary>
	public static int GlobalFrame => _globalFrame;

	/// <summary>
	/// Call this from a PanelComponent's OnUpdate to drive the animation.
	/// Safe to call multiple times per frame - only updates once.
	/// Returns true if the frame changed.
	/// </summary>
	public static bool Update()
	{
		// Prevent multiple updates in the same engine frame
		int engineFrame = (int)(Time.Now * 1000);
		if ( engineFrame == _lastUpdateFrame )
			return false;
		_lastUpdateFrame = engineFrame;

		_timer += Time.Delta;
		float frameInterval = 1f / _frameRate;

		if ( _timer >= frameInterval )
		{
			_timer -= frameInterval;
			_globalFrame++;

			// Wrap at a reasonable number to prevent overflow
			if ( _globalFrame > 10000 )
				_globalFrame = 0;

			return true; // Frame changed
		}

		return false; // Frame didn't change
	}

	/// <summary>
	/// Get the current frame index for a specific frame count
	/// </summary>
	public static int GetFrameIndex( int frameCount )
	{
		if ( frameCount <= 0 )
			return 0;

		return _globalFrame % frameCount;
	}
}
