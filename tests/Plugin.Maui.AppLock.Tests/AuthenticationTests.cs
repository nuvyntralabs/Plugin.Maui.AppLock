namespace Plugin.Maui.AppLock.Tests;

public sealed class AuthenticationTests
{
    [Fact]
    public async Task RequireAuthentication_when_unlocked_does_not_prompt()
    {
        var (appLock, auth, _, _) = Harness.Create();

        var result = await appLock.RequireAuthenticationAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(AppLockMethod.None, result.Method);
        Assert.Equal(0, auth.AuthenticateCalls);
    }

    [Fact]
    public async Task RequireAuthentication_unlocks_after_successful_prompt()
    {
        var (appLock, auth, _, _) = Harness.Create();
        await appLock.LockAsync();

        var result = await appLock.RequireAuthenticationAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(AppLockMethod.Biometric, result.Method);
        Assert.Equal(AppLockState.Unlocked, appLock.State);
        Assert.Equal(1, auth.AuthenticateCalls);
    }

    [Fact]
    public async Task Failed_prompt_stays_locked()
    {
        var (appLock, auth, _, _) = Harness.Create();
        auth.Result = AppLockAuthResult.Fail(AppLockFailureKind.Canceled, "User cancelled");
        await appLock.LockAsync();

        var result = await appLock.RequireAuthenticationAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(AppLockFailureKind.Canceled, result.Failure);
        Assert.True(appLock.IsLocked);
    }

    [Fact]
    public async Task Always_mode_prompts_even_when_unlocked()
    {
        var (appLock, auth, _, _) = Harness.Create();

        var result = await appLock.RequireAuthenticationAsync(AppLockPromptMode.Always);

        Assert.True(result.Succeeded);
        Assert.Equal(1, auth.AuthenticateCalls);
        Assert.Equal(AppLockState.Unlocked, appLock.State);
    }

    [Fact]
    public async Task Concurrent_calls_share_one_prompt()
    {
        var hold = new TaskCompletionSource<AppLockAuthResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var auth = new FakeAuthenticator(hold);
        var (appLock, _, _, _) = Harness.Create(authenticator: auth);
        await appLock.LockAsync();

        var first = appLock.RequireAuthenticationAsync();
        var second = appLock.RequireAuthenticationAsync();
        await WaitForAsync(() => auth.AuthenticateCalls == 1);

        hold.TrySetResult(AppLockAuthResult.Success(AppLockMethod.DeviceCredential));
        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, auth.AuthenticateCalls);
        Assert.All(results, result => Assert.True(result.Succeeded));
        Assert.Equal(AppLockState.Unlocked, appLock.State);
    }

    [Fact]
    public async Task NotEnrolled_does_not_open_prompt()
    {
        var (appLock, auth, _, _) = Harness.Create();
        auth.Availability = AppLockAvailability.NotEnrolled;
        await appLock.LockAsync();

        var result = await appLock.RequireAuthenticationAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(AppLockFailureKind.NotEnrolled, result.Failure);
        Assert.Equal(0, auth.AuthenticateCalls);
        Assert.True(appLock.IsLocked);
    }

    [Fact]
    public async Task Disabled_gate_succeeds_without_prompt()
    {
        var (appLock, auth, _, _) = Harness.Create();
        await appLock.DisableAsync();

        var result = await appLock.RequireAuthenticationAsync(AppLockPromptMode.Always);

        Assert.True(result.Succeeded);
        Assert.Equal(0, auth.AuthenticateCalls);
    }

    static async Task WaitForAsync(Func<bool> condition)
    {
        for (var i = 0; i < 50; i++)
        {
            if (condition())
                return;
            await Task.Delay(10);
        }

        throw new TimeoutException("Condition was not met.");
    }
}
