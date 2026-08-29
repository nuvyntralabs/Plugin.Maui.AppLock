namespace Plugin.Maui.AppLock;

/// <summary>
/// Static entry point for the application-security workflow.
/// </summary>
public static class AppLock
{
    static IAppLock? current;

    /// <summary>
    /// Gets the instance registered by <c>UseAppLock</c> or created by <see cref="Configure"/>.
    /// </summary>
    public static IAppLock Current =>
        current ?? throw new InvalidOperationException(
            "AppLock is not initialized. Call builder.UseAppLock() or AppLock.Configure().");

    /// <summary>
    /// Gets a value indicating whether the shared instance is locked.
    /// </summary>
    public static bool IsLocked => Current.IsLocked;

    /// <summary>
    /// Gets the current lock state.
    /// </summary>
    public static AppLockState State => Current.State;

    /// <summary>
    /// Updates options on the shared instance, creating one if needed.
    /// </summary>
    /// <example>
    /// <code>
    /// AppLock.Configure(options =>
    /// {
    ///     options.LockAfter = TimeSpan.FromMinutes(2);
    ///     options.AllowBiometric = true;
    ///     options.AllowDevicePin = true;
    /// });
    /// </code>
    /// </example>
    public static void Configure(Action<AppLockOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        if (current is not null)
        {
            current.Configure(configure);
            return;
        }

        var options = new AppLockOptions();
        configure(options);
        SetDefault(Create(options));
    }

    /// <summary>
    /// Prompts for Face ID, Touch ID, fingerprint, or the device PIN when the app is locked.
    /// </summary>
    public static Task<AppLockAuthResult> RequireAuthenticationAsync(
        AppLockPromptMode mode = AppLockPromptMode.IfLocked,
        CancellationToken cancellationToken = default) =>
        Current.RequireAuthenticationAsync(mode, cancellationToken);

    /// <summary>
    /// Locks immediately.
    /// </summary>
    public static Task LockAsync(CancellationToken cancellationToken = default) =>
        Current.LockAsync(cancellationToken);

    /// <summary>
    /// Unlocks with a system prompt when locked.
    /// </summary>
    public static Task<AppLockAuthResult> UnlockAsync(CancellationToken cancellationToken = default) =>
        Current.UnlockAsync(cancellationToken);

    /// <summary>
    /// Creates a lock that uses the platform authenticator and preferences store.
    /// </summary>
    public static IAppLock Create(AppLockOptions? options = null)
    {
        var instance = Create(
            options ?? new AppLockOptions(),
            PlatformAuthenticator.Create(),
            SystemClock.Instance,
            new PreferencesAppLockStore());
        SetDefault(instance);
        return instance;
    }

    /// <summary>
    /// Replaces the shared instance. Intended for tests and custom implementations.
    /// </summary>
    public static void SetDefault(IAppLock implementation) =>
        current = implementation ?? throw new ArgumentNullException(nameof(implementation));

    internal static AppLockImplementation Create(
        AppLockOptions options,
        IAuthenticator authenticator,
        IClock clock,
        IAppLockStore store) =>
        new(options, authenticator, clock, store);

    internal static void SetCurrent(IAppLock? instance) => current = instance;
}
