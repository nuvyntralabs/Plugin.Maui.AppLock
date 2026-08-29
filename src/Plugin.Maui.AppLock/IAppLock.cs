namespace Plugin.Maui.AppLock;

/// <summary>
/// Application-security workflow: lock after background, wait out a grace period,
/// then unlock with biometrics or the device PIN.
/// </summary>
public interface IAppLock
{
    /// <summary>
    /// Gets the current lock lifecycle state.
    /// </summary>
    AppLockState State { get; }

    /// <summary>
    /// Gets a value indicating whether the workflow is active.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the app requires authentication before use.
    /// </summary>
    bool IsLocked { get; }

    /// <summary>
    /// Gets the live options. Mutate through <see cref="Configure"/>.
    /// </summary>
    AppLockOptions Options { get; }

    /// <summary>
    /// Raised when <see cref="State"/> changes.
    /// </summary>
    event EventHandler<AppLockChangedEventArgs>? StateChanged;

    /// <summary>
    /// Raised after the app becomes locked.
    /// </summary>
    event EventHandler? Locked;

    /// <summary>
    /// Raised after a successful unlock.
    /// </summary>
    event EventHandler? Unlocked;

    /// <summary>
    /// Raised after every authentication attempt, success or failure.
    /// </summary>
    event EventHandler<AppLockAuthResult>? AuthenticationCompleted;

    /// <summary>
    /// Updates lock-timer and authenticator options.
    /// </summary>
    void Configure(Action<AppLockOptions> configure);

    /// <summary>
    /// Turns the workflow on and persists the preference when configured.
    /// </summary>
    Task EnableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Turns the workflow off, unlocks, and persists the preference when configured.
    /// </summary>
    Task DisableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks immediately without a prompt. Use this when a sensitive screen closes
    /// or when you want to hide content before backgrounding.
    /// </summary>
    Task LockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Prompts if locked and unlocks on success. Equivalent to
    /// <see cref="RequireAuthenticationAsync"/> with <see cref="AppLockPromptMode.IfLocked"/>.
    /// </summary>
    Task<AppLockAuthResult> UnlockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The primary gate. Call this on resume, at a lock page, or before a sensitive action.
    /// Cancelled or failed prompts return a result; the app stays locked.
    /// </summary>
    Task<AppLockAuthResult> RequireAuthenticationAsync(
        AppLockPromptMode mode = AppLockPromptMode.IfLocked,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns whether the device can satisfy the configured methods.
    /// </summary>
    Task<AppLockAvailability> GetAvailabilityAsync();

    /// <summary>
    /// Returns a point-in-time view of the lock.
    /// </summary>
    AppLockSnapshot GetSnapshot();

    /// <summary>
    /// Called when the app moves to the background. Starts the lock timer.
    /// </summary>
    void NotifyBackground();

    /// <summary>
    /// Called when the app returns to the foreground. Locks if the grace period elapsed.
    /// </summary>
    void NotifyForeground();
}
