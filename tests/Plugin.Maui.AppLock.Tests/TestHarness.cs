namespace Plugin.Maui.AppLock.Tests;

sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 29, 10, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan duration) => UtcNow += duration;
}

sealed class FakeAuthenticator : IAuthenticator
{
    readonly TaskCompletionSource<AppLockAuthResult>? hold;

    public FakeAuthenticator(TaskCompletionSource<AppLockAuthResult>? hold = null)
    {
        this.hold = hold;
    }

    public AppLockAvailability Availability { get; set; } = AppLockAvailability.Available;

    public AppLockAuthResult Result { get; set; } = AppLockAuthResult.Success(AppLockMethod.Biometric);

    public Exception? ThrowOnAuthenticate { get; set; }

    public int AuthenticateCalls { get; private set; }

    public int AvailabilityCalls { get; private set; }

    public Task<AppLockAvailability> GetAvailabilityAsync(AppLockOptions options)
    {
        AvailabilityCalls++;
        return Task.FromResult(Availability);
    }

    public async Task<AppLockAuthResult> AuthenticateAsync(AppLockOptions options, CancellationToken cancellationToken)
    {
        AuthenticateCalls++;
        if (ThrowOnAuthenticate is not null)
            throw ThrowOnAuthenticate;
        if (hold is not null)
            return await hold.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return Result;
    }
}

static class Harness
{
    public static (AppLockImplementation AppLock, FakeAuthenticator Auth, FakeClock Clock, MemoryAppLockStore Store) Create(
        Action<AppLockOptions>? configure = null,
        FakeAuthenticator? authenticator = null)
    {
        var options = new AppLockOptions
        {
            Enabled = true,
            LockOnStart = false,
            AutoPromptOnResume = false,
            PersistEnabled = true,
            LockAfter = TimeSpan.FromMinutes(2),
            AllowBiometric = true,
            AllowDevicePin = true
        };
        configure?.Invoke(options);

        var auth = authenticator ?? new FakeAuthenticator();
        var clock = new FakeClock();
        var store = new MemoryAppLockStore();
        var appLock = AppLock.Create(options, auth, clock, store);
        return (appLock, auth, clock, store);
    }
}
