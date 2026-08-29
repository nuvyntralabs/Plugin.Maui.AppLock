namespace Plugin.Maui.AppLock;

sealed class AppLockImplementation : IAppLock
{
    readonly AppLockOptions options;
    readonly IAuthenticator authenticator;
    readonly IClock clock;
    readonly IAppLockStore store;
    readonly object gate = new();

    AppLockState state;
    bool enabled;
    DateTimeOffset? backgroundedAt;
    Task<AppLockAuthResult>? inFlight;

    public AppLockImplementation(
        AppLockOptions options,
        IAuthenticator authenticator,
        IClock clock,
        IAppLockStore store)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.store = store ?? throw new ArgumentNullException(nameof(store));

        enabled = options.PersistEnabled
            ? store.GetEnabled() ?? options.Enabled
            : options.Enabled;

        state = !enabled
            ? AppLockState.Disabled
            : options.LockOnStart
                ? AppLockState.Locked
                : AppLockState.Unlocked;
    }

    public AppLockState State
    {
        get
        {
            lock (gate)
                return state;
        }
    }

    public bool IsEnabled
    {
        get
        {
            lock (gate)
                return enabled;
        }
    }

    public bool IsLocked
    {
        get
        {
            lock (gate)
                return enabled && state is AppLockState.Locked or AppLockState.Authenticating;
        }
    }

    public AppLockOptions Options => options;

    public event EventHandler<AppLockChangedEventArgs>? StateChanged;

    public event EventHandler? Locked;

    public event EventHandler? Unlocked;

    public event EventHandler<AppLockAuthResult>? AuthenticationCompleted;

    public void Configure(Action<AppLockOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        lock (gate)
            configure(options);
    }

    public Task EnableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AppLockState previous;
        lock (gate)
        {
            previous = state;
            enabled = true;
            options.Enabled = true;
            if (options.PersistEnabled)
                store.SetEnabled(true);

            if (state == AppLockState.Disabled)
                state = options.LockOnStart ? AppLockState.Locked : AppLockState.Unlocked;
        }

        RaiseIfChanged(previous, GetSnapshot());
        return Task.CompletedTask;
    }

    public Task DisableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AppLockState previous;
        lock (gate)
        {
            previous = state;
            enabled = false;
            options.Enabled = false;
            backgroundedAt = null;
            if (options.PersistEnabled)
                store.SetEnabled(false);
            state = AppLockState.Disabled;
        }

        RaiseIfChanged(previous, GetSnapshot());
        return Task.CompletedTask;
    }

    public Task LockAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsEnabled)
            return Task.CompletedTask;

        Transition(AppLockState.Locked);
        return Task.CompletedTask;
    }

    public Task<AppLockAuthResult> UnlockAsync(CancellationToken cancellationToken = default) =>
        RequireAuthenticationAsync(AppLockPromptMode.IfLocked, cancellationToken);

    public Task<AppLockAuthResult> RequireAuthenticationAsync(
        AppLockPromptMode mode = AppLockPromptMode.IfLocked,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (gate)
        {
            if (!enabled)
                return Task.FromResult(AppLockAuthResult.Success(AppLockMethod.None, "AppLock is disabled."));

            if (mode == AppLockPromptMode.IfLocked && state == AppLockState.Unlocked)
                return Task.FromResult(AppLockAuthResult.Success(AppLockMethod.None));

            if (!options.AllowBiometric && !options.AllowDevicePin)
            {
                throw new AppLockException(
                    "AppLock has no allowed authenticators. Set AllowBiometric and/or AllowDevicePin.");
            }

            if (inFlight is not null)
                return inFlight;

            inFlight = AuthenticateCoreAsync(cancellationToken);
            return inFlight;
        }
    }

    public Task<AppLockAvailability> GetAvailabilityAsync() =>
        authenticator.GetAvailabilityAsync(options);

    public AppLockSnapshot GetSnapshot()
    {
        lock (gate)
            return CaptureSnapshot();
    }

    public void NotifyBackground()
    {
        if (!IsEnabled || !options.LockOnBackground)
            return;

        AppLockState previous;
        bool lockedNow;
        lock (gate)
        {
            previous = state;
            backgroundedAt = clock.UtcNow;
            lockedNow = options.LockAfter <= TimeSpan.Zero;
            if (lockedNow && state != AppLockState.Authenticating)
                state = AppLockState.Locked;
        }

        if (lockedNow)
            RaiseIfChanged(previous, GetSnapshot());
    }

    public void NotifyForeground()
    {
        if (!IsEnabled)
            return;

        AppLockState previous;
        bool shouldPrompt;
        lock (gate)
        {
            previous = state;
            var elapsed = backgroundedAt is { } at && options.LockOnBackground
                && clock.UtcNow - at >= options.LockAfter;

            if (elapsed && state != AppLockState.Authenticating)
                state = AppLockState.Locked;

            backgroundedAt = null;
            shouldPrompt = options.AutoPromptOnResume
                && state is AppLockState.Locked;
        }

        RaiseIfChanged(previous, GetSnapshot());

        if (shouldPrompt)
            _ = PromptSafeAsync();
    }

    async Task PromptSafeAsync()
    {
        try
        {
            await RequireAuthenticationAsync(AppLockPromptMode.IfLocked).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Stay locked. The host can call RequireAuthenticationAsync again.
        }
    }

    async Task<AppLockAuthResult> AuthenticateCoreAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        Transition(AppLockState.Authenticating);

        AppLockAuthResult result;
        try
        {
            var availability = await authenticator.GetAvailabilityAsync(options).ConfigureAwait(false);
            result = availability switch
            {
                AppLockAvailability.NotEnrolled =>
                    AppLockAuthResult.Fail(AppLockFailureKind.NotEnrolled, "No biometric or device PIN is enrolled."),
                AppLockAvailability.NotSupported =>
                    AppLockAuthResult.Fail(AppLockFailureKind.NotAvailable, "This platform does not support AppLock prompts."),
                AppLockAvailability.Unavailable =>
                    AppLockAuthResult.Fail(AppLockFailureKind.NotAvailable, "Authentication is temporarily unavailable."),
                _ => await authenticator.AuthenticateAsync(options, cancellationToken).ConfigureAwait(false)
            };
        }
        catch (OperationCanceledException)
        {
            result = AppLockAuthResult.Fail(AppLockFailureKind.Canceled, "Authentication was cancelled.");
        }
        catch (AppLockException ex)
        {
            result = AppLockAuthResult.Fail(AppLockFailureKind.NotAvailable, ex.Message);
        }
        catch (Exception ex)
        {
            result = AppLockAuthResult.Fail(AppLockFailureKind.Failed, ex.Message);
        }
        finally
        {
            lock (gate)
                inFlight = null;
        }

        if (result.Succeeded && ShouldRemainUnlocked())
            Transition(AppLockState.Unlocked);
        else
            Transition(AppLockState.Locked);

        AuthenticationCompleted?.Invoke(this, result);
        return result;
    }

    bool ShouldRemainUnlocked()
    {
        lock (gate)
        {
            if (backgroundedAt is not { } at || !options.LockOnBackground)
                return true;

            return options.LockAfter > TimeSpan.Zero && clock.UtcNow - at < options.LockAfter;
        }
    }

    void Transition(AppLockState next)
    {
        AppLockState previous;
        AppLockSnapshot snapshot;
        lock (gate)
        {
            if (!enabled && next != AppLockState.Disabled)
                return;

            previous = state;
            state = next;
            snapshot = CaptureSnapshot();
        }

        RaiseIfChanged(previous, snapshot);
    }

    AppLockSnapshot CaptureSnapshot()
    {
        TimeSpan? remaining = null;
        if (enabled && backgroundedAt is { } at && options.LockOnBackground)
        {
            var elapsed = clock.UtcNow - at;
            remaining = elapsed >= options.LockAfter ? TimeSpan.Zero : options.LockAfter - elapsed;
        }

        return new AppLockSnapshot(
            state,
            enabled,
            enabled && state is AppLockState.Locked or AppLockState.Authenticating,
            options.LockAfter,
            options.AllowBiometric,
            options.AllowDevicePin,
            backgroundedAt,
            remaining);
    }

    void RaiseIfChanged(AppLockState previous, AppLockSnapshot snapshot)
    {
        if (previous == snapshot.State)
            return;

        StateChanged?.Invoke(this, new AppLockChangedEventArgs(previous, snapshot.State, snapshot));

        if (snapshot.State == AppLockState.Locked && previous is AppLockState.Unlocked or AppLockState.Disabled)
            Locked?.Invoke(this, EventArgs.Empty);

        if (snapshot.State == AppLockState.Unlocked && previous != AppLockState.Unlocked)
            Unlocked?.Invoke(this, EventArgs.Empty);
    }
}
