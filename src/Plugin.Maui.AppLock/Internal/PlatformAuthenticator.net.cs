#if !ANDROID && !IOS
namespace Plugin.Maui.AppLock;

sealed class PlatformAuthenticator : IAuthenticator
{
    public static IAuthenticator Create() => new PlatformAuthenticator();

    public Task<AppLockAvailability> GetAvailabilityAsync(AppLockOptions options)
    {
        _ = options;
        return Task.FromResult(AppLockAvailability.NotSupported);
    }

    public Task<AppLockAuthResult> AuthenticateAsync(AppLockOptions options, CancellationToken cancellationToken)
    {
        _ = options;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(AppLockAuthResult.Fail(
            AppLockFailureKind.NotAvailable,
            "AppLock prompts are not available on this target. Use net10.0-android or net10.0-ios."));
    }
}
#endif
