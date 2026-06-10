using Sandbox;

namespace Beastborne.UI;

/// <summary>
/// Plays a looping video and exposes its texture for use as a CSS background-image
/// on a Razor UI panel. Ported from the Flow project (new_game/Code/VideoBackground.cs).
///
/// Usage pattern:
///   1. Spawn a GameObject and attach this Component (see MainMenu.OnStart).
///   2. In the Razor code-behind, grab an element reference via `@ref` and call
///      `element.Style.SetBackgroundImage(VideoBackground.Instance.VideoTexture)`
///      every Tick once the texture is non-null. This is the CANONICAL s&box
///      pattern for binding a live Texture to a Panel — string interpolation
///      of `url({texture})` fails because Texture.ToString() does NOT produce
///      a CSS-parseable URL.
///   3. Force a re-render each frame (e.g. include SpriteAnimator.GlobalFrame in
///      BuildHash) so the renderer resamples the texture handle.
///
/// Note: s&box's raw &lt;video&gt; HTML tag is NOT supported in the Razor UI parser —
/// attempting to use one logs "Error when building render tree on CustomBuildPanel -
/// Couldn't find previous tag". This component is the supported pattern.
/// </summary>
public sealed class VideoBackground : Component
{
	public static VideoBackground Instance { get; private set; }

	// h264 mp4 ONLY — s&box's VideoPlayer rejects VP8/VP9 webm with
	// "[engine/VideoPlayer] No supported streams found" (verified 2026-06-09).
	public const string DefaultVideoPath = "ui/main menu/loopbackground_wave.mp4";

	[Property] public string VideoPath { get; set; } = DefaultVideoPath;
	[Property] public bool Loop { get; set; } = true;
	[Property] public bool Muted { get; set; } = true;

	public VideoPlayer Player { get; private set; }
	private bool _seekedThisWrap = false;
	public Texture VideoTexture => Player?.Texture;
	public bool VideoLoaded { get; private set; } = false;

	protected override void OnStart()
	{
		Instance = this;

		Log.Info( "[VideoBackground] OnStart — component spawned, initialising VideoPlayer" );

		Player = new VideoPlayer();
		Player.Repeat = Loop;
		Player.Muted = Muted;

		Player.OnLoaded += () =>
		{
			VideoLoaded = true;
			Log.Info( $"[VideoBackground] OnLoaded fired — resolution: {Player.Width}x{Player.Height}, duration: {Player.Duration}s, texture: {(Player.Texture != null ? "non-null" : "NULL")}" );
		};

		// Play from mounted filesystem (local assets).
		var exists = FileSystem.Mounted.FileExists( VideoPath );
		Log.Info( $"[VideoBackground] FileSystem.Mounted.FileExists(\"{VideoPath}\") = {exists}" );
		Log.Info( $"[VideoBackground] Calling Player.Play(FileSystem.Mounted, \"{VideoPath}\")" );
		Player.Play( FileSystem.Mounted, VideoPath );
	}

	protected override void OnUpdate()
	{
		// Must be called every frame to advance the video decoder and refresh Texture.
		Player?.Present();

		// C#-MANAGED LOOP: the engine's Repeat wraps by letting the decoder hit EOF
		// and restart, which has shown freezes ("H264: dropping pending sample (MFT
		// deadlock)") right at the seam. Instead we Seek(0) ourselves ~2 frames
		// before the end so the decoder never reaches EOF. The wave bg is encoded
		// all-intra (every frame a keyframe), so the seek is stateless + instant.
		//
		// DEBOUNCED (2026-06-09): PlaybackTime does not reset the same frame the
		// seek is issued — without the flag this fired a BURST of seeks at every
		// wrap (one per frame until PlaybackTime caught up), which itself read as
		// a loop stutter. One seek per wrap; the flag re-arms once playback is
		// observably back near the start. Repeat stays on as a backstop.
		if ( VideoLoaded && Player != null && Player.Duration > 0.1f )
		{
			if ( !_seekedThisWrap && Player.PlaybackTime >= Player.Duration - 0.034f )
			{
				Log.Info( $"[VideoBackground] wrap seek at t={Player.PlaybackTime:F3}/{Player.Duration:F3}" );
				Player.Seek( 0f );
				_seekedThisWrap = true;
			}
			else if ( _seekedThisWrap && Player.PlaybackTime < 1f )
			{
				_seekedThisWrap = false; // back near the start — re-arm for the next wrap
			}
		}
	}

	protected override void OnDestroy()
	{
		Log.Info( "[VideoBackground] OnDestroy — stopping and disposing player" );
		Player?.Stop();
		Player?.Dispose();
		Player = null;

		if ( Instance == this )
			Instance = null;
	}
}
