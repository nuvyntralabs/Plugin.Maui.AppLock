namespace Plugin.Maui.AppLock.Tests;

public sealed class LifecycleTests
{
    [Fact]
    public void Resume_within_grace_period_stays_unlocked()
    {
        var (appLock, _, clock, _) = Harness.Create();

        appLock.NotifyBackground();
        clock.Advance(TimeSpan.FromMinutes(1));
        appLock.NotifyForeground();

        Assert.Equal(AppLockState.Unlocked, appLock.State);
        Assert.False(appLock.IsLocked);
    }

    [Fact]
    public void Resume_after_grace_period_locks()
    {
        var (appLock, _, clock, _) = Harness.Create();
        var locked = 0;
        appLock.Locked += (_, _) => locked++;

        appLock.NotifyBackground();
        clock.Advance(TimeSpan.FromMinutes(2));
        appLock.NotifyForeground();

        Assert.Equal(AppLockState.Locked, appLock.State);
        Assert.True(appLock.IsLocked);
        Assert.Equal(1, locked);
    }

    [Fact]
    public void Zero_grace_period_locks_on_background()
    {
        var (appLock, _, _, _) = Harness.Create(options => options.LockAfter = TimeSpan.Zero);

        appLock.NotifyBackground();

        Assert.True(appLock.IsLocked);
        Assert.Equal(TimeSpan.Zero, appLock.GetSnapshot().TimeUntilLock);
    }

    [Fact]
    public async Task Disabled_workflow_ignores_background()
    {
        var (appLock, _, clock, _) = Harness.Create();
        await appLock.DisableAsync();

        appLock.NotifyBackground();
        clock.Advance(TimeSpan.FromHours(1));
        appLock.NotifyForeground();

        Assert.Equal(AppLockState.Disabled, appLock.State);
        Assert.False(appLock.IsLocked);
    }

    [Fact]
    public void LockOnStart_restores_locked()
    {
        var store = new MemoryAppLockStore();
        store.SetEnabled(true);
        var options = new AppLockOptions { LockOnStart = true, AutoPromptOnResume = false };
        var restored = AppLock.Create(options, new FakeAuthenticator(), new FakeClock(), store);

        Assert.True(restored.IsLocked);
        Assert.Equal(AppLockState.Locked, restored.State);
    }

    [Fact]
    public void Snapshot_reports_remaining_grace()
    {
        var (appLock, _, clock, _) = Harness.Create();

        appLock.NotifyBackground();
        clock.Advance(TimeSpan.FromSeconds(30));

        var remaining = appLock.GetSnapshot().TimeUntilLock;
        Assert.NotNull(remaining);
        Assert.Equal(TimeSpan.FromSeconds(90), remaining);
    }

    [Fact]
    public void LockOnBackground_false_never_starts_timer()
    {
        var (appLock, _, clock, _) = Harness.Create(options => options.LockOnBackground = false);

        appLock.NotifyBackground();
        clock.Advance(TimeSpan.FromHours(1));
        appLock.NotifyForeground();

        Assert.Equal(AppLockState.Unlocked, appLock.State);
    }

    [Fact]
    public async Task AutoPrompt_exception_raises_AuthenticationCompleted()
    {
        var (appLock, auth, clock, _) = Harness.Create(options => options.AutoPromptOnResume = true);
        auth.ThrowOnAuthenticate = new InvalidOperationException("secure hardware unavailable");
        AppLockAuthResult? completed = null;
        appLock.AuthenticationCompleted += (_, e) => completed = e;

        appLock.NotifyBackground();
        clock.Advance(TimeSpan.FromMinutes(2));
        appLock.NotifyForeground();

        for (var i = 0; i < 50 && completed is null; i++)
            await Task.Delay(20);

        Assert.NotNull(completed);
        Assert.False(completed!.Succeeded);
        Assert.Equal(AppLockFailureKind.Failed, completed.Failure);
        Assert.True(appLock.IsLocked);
    }
}
