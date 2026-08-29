namespace Plugin.Maui.AppLock.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void Configure_updates_lock_after()
    {
        var (appLock, _, _, _) = Harness.Create();

        appLock.Configure(options => options.LockAfter = TimeSpan.FromSeconds(15));

        Assert.Equal(TimeSpan.FromSeconds(15), appLock.Options.LockAfter);
        Assert.Equal(TimeSpan.FromSeconds(15), appLock.GetSnapshot().LockAfter);
    }

    [Fact]
    public async Task No_allowed_methods_throws()
    {
        var (appLock, _, _, _) = Harness.Create(options =>
        {
            options.AllowBiometric = false;
            options.AllowDevicePin = false;
            options.LockOnStart = true;
        });

        Assert.True(appLock.IsLocked);
        await Assert.ThrowsAsync<AppLockException>(() => appLock.RequireAuthenticationAsync());
    }

    [Fact]
    public async Task Enable_persists_and_can_lock_on_start()
    {
        var (first, auth, clock, store) = Harness.Create(options => options.LockOnStart = true);
        await first.EnableAsync();

        var restored = AppLock.Create(
            new AppLockOptions { LockOnStart = true, AutoPromptOnResume = false },
            auth,
            clock,
            store);

        Assert.True(store.GetEnabled());
        Assert.True(restored.IsLocked);
    }

    [Fact]
    public async Task Disable_persists_and_restores_disabled()
    {
        var (first, auth, clock, store) = Harness.Create();
        await first.DisableAsync();

        var restored = AppLock.Create(
            new AppLockOptions { Enabled = true, LockOnStart = true },
            auth,
            clock,
            store);

        Assert.False(store.GetEnabled());
        Assert.Equal(AppLockState.Disabled, restored.State);
    }

    [Fact]
    public async Task Static_configure_creates_shared_instance()
    {
        AppLock.SetCurrent(null!);
        var (instance, _, _, _) = Harness.Create();
        AppLock.SetDefault(instance);

        AppLock.Configure(options =>
        {
            options.LockAfter = TimeSpan.FromMinutes(5);
            options.AllowBiometric = true;
            options.AllowDevicePin = true;
        });

        Assert.Equal(TimeSpan.FromMinutes(5), AppLock.Current.Options.LockAfter);
        await AppLock.LockAsync();
        Assert.True(AppLock.IsLocked);
    }
}
