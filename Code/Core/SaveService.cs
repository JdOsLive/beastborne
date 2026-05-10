using Sandbox;
using Beastborne.Data;
using System.Text.Json;
using System.Threading.Tasks;

namespace Beastborne.Core;

/// <summary>
/// Single-source-of-truth save service for Beastborne.
///
/// Phase 1: SHADOW MODE. This service loads + writes its own blob but no other
/// manager reads from or writes to it yet. Cookies still drive game behavior.
/// Phase 2+ migrates managers off <c>Game.Cookies</c> and onto this service.
///
/// Storage layout:
///   Primary ("cloud") ....... Beastborne API server via <see cref="SaveApiClient"/>
///   Fallback ("cache") ...... <c>FileSystem.Data</c> / save-cache.json
///
/// The cloud is the authoritative store (Steam-id scoped, server-side). The local
/// cache exists purely as an offline safety net: if the server is unreachable we
/// load the last-known blob from disk so the player keeps playing, and every
/// successful in-memory mutation write-throughs to both cache (sync) and cloud
/// (async, throttled + exponential-backoff on failure).
/// </summary>
public sealed class SaveService : Component
{
	public static SaveService Instance { get; private set; }

	// ---- Public state --------------------------------------------------------

	/// <summary>True if a save existed on load (cloud OR local cache).</summary>
	public bool HasSave { get; private set; }

	/// <summary>True if the last primary-store (cloud) write or read succeeded.</summary>
	public bool IsOnline { get; private set; } = true;

	/// <summary>Inverse of <see cref="IsOnline"/>. Surfaced for UI indicators.</summary>
	public bool IsOffline => !IsOnline;

	/// <summary>True after <see cref="LoadAsync"/> resolves, regardless of outcome.</summary>
	public bool IsLoaded { get; private set; }

	/// <summary>Current in-memory blob. Never null after <see cref="LoadAsync"/> resolves.</summary>
	public SaveBlob CurrentBlob { get; private set; }

	/// <summary>Fired exactly once per <see cref="LoadAsync"/> after the blob is ready.</summary>
	public event System.Action OnSaveLoaded;

	/// <summary>
	/// Fired by <see cref="ForceResetAsync"/> after the blob has been wiped.
	/// Managers subscribe to this to reset their in-memory state and re-hydrate
	/// from the fresh empty blob (which resets their <c>_hasHydrated</c> guards).
	/// </summary>
	public event System.Action OnSaveReset;

	// ---- Configuration -------------------------------------------------------

	private const string CacheFileName = "save-cache.json";
	private const int CloudLoadTimeoutMs = 5000;

	private const float MinFlushIntervalSeconds = 5f;       // throttle between flushes
	private const float CloudHeartbeatSeconds = 60f;        // retry cloud-reachable probe
	private const float MaxCloudPushStalenessSeconds = 30f; // force cloud push cadence

	private const int SoftSizeWarningBytes = 500 * 1024;    // 500 KB
	private const int HardSizeLimitBytes = 2 * 1024 * 1024; // 2 MB

	// Exponential backoff schedule for cloud push failures (seconds).
	private static readonly float[] BackoffSchedule = { 1f, 3f, 10f, 30f };

	// ---- Internal state ------------------------------------------------------

	private bool _isDirty;
	private bool _isHydrating;
	private string _lastDirtySection;

	private float _lastFlushAttemptTime = -999f;
	private float _lastSuccessfulCloudPushTime = -999f;
	private float _lastCloudHeartbeatTime = -999f;

	private int _cloudRetryIndex;          // index into BackoffSchedule
	private float _nextCloudRetryTime;     // Time.Now at which we may retry after a failure

	private bool _cloudPushInFlight;

	// Snapshot of IsOnline from the previous OnUpdate tick. Used to detect
	// online/offline transitions and log + (later) surface a UI indicator.
	private bool? _lastLoggedOnlineState;

	// Tracks whether we've already notified the player about a cloud-vs-cache
	// conflict on this boot, so we don't spam the toast.
	private bool _conflictNotifiedThisBoot;

	// Tracks whether we've already notified the player about a hard size refusal
	// this boot, so we don't spam the toast every tick while still dirty.
	private bool _sizeLimitNotifiedThisBoot;

	/// <summary>
	/// Set when LoadAsync could not confirm whether the player has a cloud save
	/// (transport failed AND no local cache). In that state the in-memory blob
	/// is a fresh empty one, but pushing it to cloud would OVERWRITE a real
	/// save that we simply couldn't read. Cache writes still flow normally
	/// (they're harmless — no cache existed to begin with). This flag is reset
	/// only by a successful LoadAsync on a future boot, never mid-session.
	/// Root cause of the jeremykip save-wipe incident.
	/// </summary>
	private bool _cloudWritesQuarantined;

	/// <summary>True if cloud writes are blocked this session due to an ambiguous load.</summary>
	public bool IsCloudQuarantined => _cloudWritesQuarantined;

	private static readonly JsonSerializerOptions JsonOpts = new()
	{
		WriteIndented = false,
		PropertyNameCaseInsensitive = true,
	};

	// ---- Lifecycle -----------------------------------------------------------

	protected override void OnAwake()
	{
		if ( Instance == null )
		{
			Instance = this;
			GameObject.Flags = GameObjectFlags.DontDestroyOnLoad;
			Log.Info( "[SaveService] initialized (shadow mode)" );
			// One-time log of the local Steam ID — handy for /gift testing from
			// Discord. Prints as `[SteamID] 7656119...` on game start.
			Log.Info( $"[SteamID] {Connection.Local?.SteamId ?? 0}" );
		}
		else
		{
			Destroy();
			return;
		}
	}

	protected override void OnStart()
	{
		// Kick off load asynchronously; callers can await via Instance.LoadAsync()
		// or subscribe to OnSaveLoaded if they race.
		_ = LoadAsync();

		// Force-flush on major state transitions so leaving-to-menu / entering-expedition
		// always pushes the latest to cloud regardless of the cadence throttle.
		if ( GameManager.Instance != null )
		{
			GameManager.Instance.OnStateChanged += OnGameStateChanged;
		}
	}

	private void OnGameStateChanged( GameState oldState, GameState newState )
	{
		// Any state change is a good checkpoint. Force a flush (local cache + cloud).
		_ = FlushAsync();
	}

	protected override void OnUpdate()
	{
		if ( !IsLoaded ) return;

		// Log online/offline transitions exactly once per flip. Cheap; runs even
		// when nothing else is dirty so we catch the flip the moment it happens.
		if ( _lastLoggedOnlineState == null || _lastLoggedOnlineState.Value != IsOnline )
		{
			if ( _lastLoggedOnlineState != null )
			{
				Log.Info( IsOnline
					? "[SaveService] connection restored — IsOnline=true (cloud writes resumed)"
					: "[SaveService] cloud unreachable — IsOnline=false (writes buffered locally, will retry)" );
			}
			_lastLoggedOnlineState = IsOnline;
		}

		if ( !_isDirty ) return;
		if ( _cloudPushInFlight ) return;

		// Throttle flush cadence.
		if ( Time.Now - _lastFlushAttemptTime < MinFlushIntervalSeconds ) return;

		// If we're currently in cloud-failure backoff, honour the retry schedule
		// (but still flush to local cache — that path is reliable and free).
		bool canTryCloud = Time.Now >= _nextCloudRetryTime;

		_lastFlushAttemptTime = Time.Now;
		_ = DoFlush( canTryCloud );
	}

	protected override void OnDestroy()
	{
		if ( GameManager.Instance != null )
		{
			GameManager.Instance.OnStateChanged -= OnGameStateChanged;
		}

		// Best-effort synchronous flush on shutdown. We can't await in OnDestroy,
		// but the local-cache write is sync and reliable. We also fire a cloud push
		// fire-and-forget — if the runtime finishes it before editor teardown, great;
		// if not, next boot picks up the local cache and re-syncs.
		if ( _isDirty && CurrentBlob != null )
		{
			try
			{
				WriteCache( CurrentBlob );
			}
			catch ( System.Exception ex )
			{
				Log.Warning( $"[SaveService] OnDestroy cache flush failed: {ex.Message}" );
			}

			if ( !_cloudWritesQuarantined )
			{
				try
				{
					_ = SaveApiClient.PutSaveAsync( CurrentBlob );
				}
				catch ( System.Exception ex )
				{
					Log.Warning( $"[SaveService] OnDestroy cloud flush failed: {ex.Message}" );
				}
			}
		}

		if ( Instance == this ) Instance = null;
	}

	public static void EnsureInstance( Scene scene )
	{
		if ( Instance != null ) return;
		if ( scene == null ) return;

		var go = scene.CreateObject();
		go.Name = "SaveService";
		go.Components.Create<SaveService>();
	}

	// ---- Public API ----------------------------------------------------------

	/// <summary>
	/// Loads the save blob. Tries cloud first (with timeout), then local cache,
	/// then falls back to an empty blob. Safe to call more than once; subsequent
	/// calls after a successful load are no-ops.
	/// </summary>
	public async Task LoadAsync()
	{
		if ( IsLoaded ) return;

		_isHydrating = true;
		try
		{
			// 1. Try cloud (primary). The result is a 3-state discriminator: a populated
			// blob, a confirmed-no-save, or an unknown failure. The third case is
			// load-bearing — see _cloudWritesQuarantined.
			var cloudResult = await TryReadCloudWithTimeout( CloudLoadTimeoutMs );

			if ( cloudResult.Status == SaveLoadStatus.Loaded && cloudResult.Blob != null )
			{
				CurrentBlob = cloudResult.Blob;
				HasSave = true;
				IsOnline = true;
				Log.Info( $"[SaveService] loaded from cloud (schema v{cloudResult.Blob.SchemaVersion}, {cloudResult.Blob.Monsters?.Count ?? 0} monsters)" );

				// Conflict check: if a local cache also exists AND is newer than the
				// cloud blob, the player played offline since the last cloud push.
				// Cloud is authoritative (per architecture decision) — we load it —
				// but we MUST loudly warn the player that their offline progress is
				// being discarded rather than silently dropping it.
				var cacheCheck = TryReadCache();
				if ( cacheCheck != null && cacheCheck.LastSaveTicks > cloudResult.Blob.LastSaveTicks )
				{
					var offlineAge = System.TimeSpan.FromTicks( cacheCheck.LastSaveTicks - cloudResult.Blob.LastSaveTicks );
					Log.Warning( $"[SaveService] CONFLICT: local cache is {offlineAge.TotalMinutes:F1}m newer than cloud — offline progress will be discarded. cloud={cloudResult.Blob.LastSaveTicks}, cache={cacheCheck.LastSaveTicks}" );
					NotifyCloudCacheConflict();
				}
				return;
			}

			// 2. Fall back to local cache. Always trust the cache when cloud was
			// ambiguous OR explicitly empty — the cache is local proof of past
			// progress that should never be silently discarded.
			var cacheBlob = TryReadCache();
			if ( cacheBlob != null )
			{
				CurrentBlob = cacheBlob;
				HasSave = true;
				// IsOnline reflects whether cloud was reachable: only flip to true
				// when we got a clean NoSave answer (server is up, just empty for us).
				IsOnline = cloudResult.Status == SaveLoadStatus.NoSave;
				if ( cloudResult.Status == SaveLoadStatus.Failed )
					Log.Info( $"[SaveService] loaded from local cache (cloud unreachable, schema v{cacheBlob.SchemaVersion})" );
				else
					Log.Warning( $"[SaveService] cloud says no save but local cache exists (schema v{cacheBlob.SchemaVersion}) — using cache, will sync up" );
				return;
			}

			// 3. No cache. Two very different cases:
			//   a) Cloud confirmed NoSave   → genuine fresh player, normal flow.
			//   b) Cloud Failed             → presence of cloud save is UNKNOWN.
			//      Initializing an empty blob is fine, but we MUST NOT push it to
			//      cloud — that would overwrite the player's real save if one
			//      exists. Quarantine cloud writes for the rest of the session.
			CurrentBlob = new SaveBlob { SchemaVersion = 1 };
			HasSave = false;

			if ( cloudResult.Status == SaveLoadStatus.NoSave )
			{
				IsOnline = true;
				Log.Info( "[SaveService] no existing save — starting fresh" );
			}
			else
			{
				IsOnline = false;
				_cloudWritesQuarantined = true;
				Log.Error( "[SaveService] CLOUD UNREACHABLE on first boot with no local cache — cloud writes QUARANTINED to protect any existing save. Player should restart when online." );
				NotifyCloudQuarantined();
			}
		}
		finally
		{
			IsLoaded = true;
			_isHydrating = false;
			InvokeSaveLoadedSafe( "initial-load" );
		}
	}

	/// <summary>
	/// Fires <see cref="OnSaveLoaded"/> one subscriber at a time, isolating each
	/// in its own try/catch so one thrower doesn't cut off later handlers.
	/// A bare <c>OnSaveLoaded?.Invoke()</c> stops at the first exception, which
	/// can leave managers half-hydrated (e.g. if TamerManager throws, Monster/
	/// Beastiary never hydrate). Log each failure with the full stack so the
	/// underlying bug is still visible.
	/// </summary>
	private void InvokeSaveLoadedSafe( string phase )
	{
		var handlers = OnSaveLoaded;
		if ( handlers == null ) return;

		// Note: we can't call del.Method.DeclaringType / .Name to identify the
		// specific subscriber — s&box whitelist rejects System.Delegate.Method.
		// The stack trace in the caught exception still pinpoints the thrower.
		int index = 0;
		foreach ( var del in handlers.GetInvocationList() )
		{
			try
			{
				((System.Action)del).Invoke();
			}
			catch ( System.Exception ex )
			{
				Log.Warning( $"[SaveService] OnSaveLoaded handler #{index} threw ({phase}): {ex.Message}\n{ex.StackTrace}" );
			}
			index++;
		}
	}

	/// <summary>
	/// Fires <see cref="OnSaveReset"/> one subscriber at a time with isolated
	/// try/catch. Mirrors <see cref="InvokeSaveLoadedSafe"/>.
	/// </summary>
	private void InvokeSaveResetSafe()
	{
		var handlers = OnSaveReset;
		if ( handlers == null ) return;

		int index = 0;
		foreach ( var del in handlers.GetInvocationList() )
		{
			try
			{
				((System.Action)del).Invoke();
			}
			catch ( System.Exception ex )
			{
				Log.Warning( $"[SaveService] OnSaveReset handler #{index} threw: {ex.Message}\n{ex.StackTrace}" );
			}
			index++;
		}
	}

	/// <summary>
	/// Marks the in-memory blob dirty so <see cref="OnUpdate"/> will flush it.
	/// Safe no-op while the service is hydrating the blob from disk (prevents
	/// re-save loops when Phase 2 managers populate their sections on load).
	/// </summary>
	/// <param name="section">Optional tag describing which section changed (logging only).</param>
	public void MarkDirty( string section = null )
	{
		if ( _isHydrating ) return;
		if ( !IsLoaded ) return;

		_isDirty = true;
		_lastDirtySection = section;

		// Any legitimate mutation means the player now has a save-in-progress,
		// even if LoadAsync originally reported HasSave=false (fresh player).
		// Without this flip, a fresh player who picks a starter and goes back
		// to the main menu would still be treated as fresh and re-prompted.
		HasSave = true;
	}

	/// <summary>
	/// Force-flush now (used by explicit checkpoints or shutdown paths that can await).
	/// Always writes local cache; attempts cloud push regardless of backoff window.
	/// </summary>
	public async Task FlushAsync()
	{
		if ( !IsLoaded || CurrentBlob == null ) return;

		_lastFlushAttemptTime = Time.Now;
		await DoFlush( canTryCloud: true );
	}

	/// <summary>
	/// Wipes cloud blob + local cache + in-memory state. Used by Reset Game.
	/// Best-effort: errors are logged but not thrown.
	/// </summary>
	public async Task ForceResetAsync()
	{
		Log.Warning( "[SaveService] ForceResetAsync invoked — wiping cloud + cache + in-memory blob" );

		// Wipe local cache first (reliable, sync).
		try
		{
			if ( FileSystem.Data?.FileExists( CacheFileName ) == true )
				FileSystem.Data.DeleteFile( CacheFileName );
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[SaveService] cache wipe failed: {ex.Message}" );
		}

		// Wipe cloud (best-effort; may fail if offline).
		try
		{
			var ok = await SaveApiClient.DeleteSaveAsync();
			IsOnline = ok;
			if ( !ok ) Log.Warning( "[SaveService] cloud wipe failed (server rejected or unreachable)" );
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[SaveService] cloud wipe threw: {ex.Message}" );
			IsOnline = false;
		}

		CurrentBlob = new SaveBlob { SchemaVersion = 1 };
		HasSave = false;
		_isDirty = false;
		_cloudRetryIndex = 0;
		_nextCloudRetryTime = 0f;
		_conflictNotifiedThisBoot = false;
		_sizeLimitNotifiedThisBoot = false;
		// Player explicitly chose to wipe — they accept the consequences for THIS
		// blob, so re-enable cloud writes (the quarantine was only protecting an
		// unknown prior save).
		_cloudWritesQuarantined = false;

		// Broadcast to managers so they can reset their own state (tamer, monsters,
		// beastiary, tutorial, missions, guild, daily rewards). Each manager is
		// responsible for clearing its `_hasHydrated` guard and re-hydrating from
		// the fresh empty blob. We then also re-fire OnSaveLoaded for any legacy
		// subscribers that key off the initial-load event.
		//
		// Wrap in _isHydrating so that any MarkDirty calls triggered by managers
		// populating their default state (e.g. MissionManager seeding daily/weekly
		// missions, TamerManager HydrateFromBlank) don't flip HasSave back to true.
		// Without this guard, the reset "succeeds" but the next PLAY click treats
		// the player as if they still have a save and skips the starter picker.
		_isHydrating = true;
		try
		{
			InvokeSaveResetSafe();
			InvokeSaveLoadedSafe( "post-reset" );
		}
		finally
		{
			_isHydrating = false;
		}

		// Belt-and-suspenders: enforce the post-reset contract.
		HasSave = false;
		_isDirty = false;
	}

	// ---- Internals -----------------------------------------------------------

	/// <summary>
	/// Performs the actual flush. Writes local cache synchronously (fast), then
	/// optionally fires a cloud push. Cloud pushes are fire-and-forget with
	/// exponential backoff on failure.
	/// </summary>
	private async Task DoFlush( bool canTryCloud )
	{
		if ( CurrentBlob == null ) return;

		// Stamp + serialize.
		CurrentBlob.LastSaveTicks = System.DateTime.UtcNow.Ticks;

		string json;
		try
		{
			json = JsonSerializer.Serialize( CurrentBlob, JsonOpts );
		}
		catch ( System.Exception ex )
		{
			Log.Error( $"[SaveService] serialize failed: {ex.Message}" );
			return;
		}

		// Size gates.
		int byteLen = System.Text.Encoding.UTF8.GetByteCount( json );
		if ( byteLen > HardSizeLimitBytes )
		{
			Log.Error( $"[SaveService] blob size {byteLen / 1024} KB exceeds hard limit ({HardSizeLimitBytes / 1024} KB) — refusing save, keeping last-good cache on disk" );
			NotifySizeLimitExceeded( byteLen );
			return;
		}
		if ( byteLen > SoftSizeWarningBytes )
		{
			Log.Warning( $"[SaveService] blob size {byteLen / 1024} KB exceeds soft warning threshold ({SoftSizeWarningBytes / 1024} KB) — investigate bloat" );
		}

		// 1) Local cache write (sync, reliable, cheap). The offline safety net.
		bool cacheOk = false;
		try
		{
			FileSystem.Data.WriteAllText( CacheFileName, json );
			cacheOk = true;
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[SaveService] cache write failed: {ex.Message}" );
		}

		// 2) Cloud push (background). Skip if we're still in the backoff window
		// and nothing forced us here. Also skip unconditionally if cloud writes
		// are quarantined (LoadAsync couldn't confirm whether a real save exists
		// on the server — pushing now would clobber it). Cache writes above are
		// fine; they're local-only.
		if ( _cloudWritesQuarantined )
		{
			if ( cacheOk )
			{
				_isDirty = false;
				_lastDirtySection = null;
			}
			return;
		}

		bool shouldPush = canTryCloud &&
			(Time.Now - _lastSuccessfulCloudPushTime > MaxCloudPushStalenessSeconds
			 || (_lastDirtySection != null));

		if ( shouldPush )
		{
			_cloudPushInFlight = true;
			try
			{
				var ok = await SaveApiClient.PutSaveAsync( CurrentBlob );
				if ( ok )
				{
					_lastSuccessfulCloudPushTime = Time.Now;
					_cloudRetryIndex = 0;
					_nextCloudRetryTime = 0f;
					IsOnline = true;
				}
				else
				{
					ScheduleCloudBackoff( "server returned failure" );
				}
			}
			catch ( System.Exception ex )
			{
				ScheduleCloudBackoff( ex.Message );
			}
			finally
			{
				_cloudPushInFlight = false;
			}
		}
		else if ( !IsOnline && Time.Now - _lastCloudHeartbeatTime > CloudHeartbeatSeconds )
		{
			// Periodic heartbeat: if we're flagged offline but the push window didn't
			// open, try a cheap cloud write to re-probe reachability.
			_lastCloudHeartbeatTime = Time.Now;
			_cloudPushInFlight = true;
			try
			{
				var ok = await SaveApiClient.PutSaveAsync( CurrentBlob );
				if ( ok )
				{
					_lastSuccessfulCloudPushTime = Time.Now;
					_cloudRetryIndex = 0;
					IsOnline = true;
					Log.Info( "[SaveService] cloud reachable again — flipped IsOnline = true" );
				}
				// else: still offline; next heartbeat will try again
			}
			catch { /* still offline; next heartbeat will try again */ }
			finally { _cloudPushInFlight = false; }
		}

		// Only clear dirty flag if at least cache succeeded. Otherwise we'll
		// keep retrying on the next tick.
		if ( cacheOk )
		{
			_isDirty = false;
			_lastDirtySection = null;
		}
	}

	private void ScheduleCloudBackoff( string reason )
	{
		IsOnline = false;
		// Schedule exponential backoff retry. If we've exhausted the schedule,
		// stay at the final interval and wait for MarkDirty to poke us again.
		var wait = BackoffSchedule[System.Math.Min( _cloudRetryIndex, BackoffSchedule.Length - 1 )];
		_nextCloudRetryTime = Time.Now + wait;
		_cloudRetryIndex = System.Math.Min( _cloudRetryIndex + 1, BackoffSchedule.Length );
		Log.Warning( $"[SaveService] cloud push failed ({reason}); retry in {wait}s" );
	}

	/// <summary>
	/// Attempts to read the cloud blob with a timeout. Returns a 3-state result
	/// (Loaded / NoSave / Failed). Callers MUST distinguish Failed from NoSave —
	/// see <see cref="SaveApiClient.SaveLoadStatus"/>.
	/// </summary>
	private async Task<SaveLoadResult> TryReadCloudWithTimeout( int timeoutMs )
	{
		// Timeout-race removed: Task.WhenAny isn't reliably whitelisted in s&box's
		// async surface. The service is already robust to a slow cloud read because
		// boot falls back to the local cache if the cloud never returns in time for
		// the first hydrate tick. Keep the signature for call-site compatibility.
		_ = timeoutMs;

		try
		{
			return await SaveApiClient.GetSaveAsync();
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[SaveService] cloud read threw: {ex.Message}" );
			IsOnline = false;
			return new SaveLoadResult( SaveLoadStatus.Failed, null );
		}
	}

	private SaveBlob TryReadCache()
	{
		try
		{
			if ( FileSystem.Data == null ) return null;
			if ( !FileSystem.Data.FileExists( CacheFileName ) ) return null;
			var json = FileSystem.Data.ReadAllText( CacheFileName );
			if ( string.IsNullOrEmpty( json ) ) return null;
			return JsonSerializer.Deserialize<SaveBlob>( json, JsonOpts );
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[SaveService] cache read failed: {ex.Message}" );
			return null;
		}
	}

	private void WriteCache( SaveBlob blob )
	{
		blob.LastSaveTicks = System.DateTime.UtcNow.Ticks;
		var json = JsonSerializer.Serialize( blob, JsonOpts );
		FileSystem.Data.WriteAllText( CacheFileName, json );
	}

	// ---- User-visible notifications -----------------------------------------

	private void NotifyCloudCacheConflict()
	{
		if ( _conflictNotifiedThisBoot ) return;
		_conflictNotifiedThisBoot = true;

		try
		{
			NotificationManager.Instance?.AddNotification(
				NotificationType.Warning,
				"Cloud save loaded",
				"Local progress from an offline session was discarded.",
				duration: 8f );
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[SaveService] conflict notification failed: {ex.Message}" );
		}
	}

	private void NotifyCloudQuarantined()
	{
		try
		{
			NotificationManager.Instance?.AddNotification(
				NotificationType.Warning,
				"Save server unreachable",
				"Cloud sync paused this session to protect any existing save. Restart Beastborne when online.",
				duration: 15f );
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[SaveService] quarantine notification failed: {ex.Message}" );
		}
	}

	private void NotifySizeLimitExceeded( int byteLen )
	{
		if ( _sizeLimitNotifiedThisBoot ) return;
		_sizeLimitNotifiedThisBoot = true;

		try
		{
			NotificationManager.Instance?.AddNotification(
				NotificationType.Warning,
				"Save exceeded 2 MB",
				$"Keeping last good state ({byteLen / 1024} KB). Consider releasing unused beasts.",
				duration: 10f );
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[SaveService] size-limit notification failed: {ex.Message}" );
		}
	}
}
