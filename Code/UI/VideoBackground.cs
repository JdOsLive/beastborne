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

	[Property] public string VideoPath { get; set; } = "ui/main menu/loopbackground_wave.webm";
	[Property] public bool Loop { get; set; } = true;
	[Property] public bool Muted { get; set; } = true;

	public VideoPlayer Player { get; private set; }
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
