namespace Plugin.Maui.AppLock;

interface IAuthenticator
{
    Task<AppLockAvailability> GetAvailabilityAsync(AppLockOptions options);

    Task<AppLockAuthResult> AuthenticateAsync(AppLockOptions options, CancellationToken cancellationToken);
}
