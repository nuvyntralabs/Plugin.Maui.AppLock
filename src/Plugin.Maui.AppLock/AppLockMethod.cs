namespace Plugin.Maui.AppLock;

/// <summary>
/// How the user satisfied <see cref="IAppLock.RequireAuthenticationAsync"/>.
/// </summary>
public enum AppLockMethod
{
    /// <summary>No prompt ran (already unlocked, or the lock is disabled).</summary>
    None,

    /// <summary>Face ID, Touch ID, or fingerprint.</summary>
    Biometric,

    /// <summary>Device PIN, pattern, or password.</summary>
    DeviceCredential
}
