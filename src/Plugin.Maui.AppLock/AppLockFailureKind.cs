namespace Plugin.Maui.AppLock;

/// <summary>
/// Why <see cref="IAppLock.RequireAuthenticationAsync"/> did not unlock the app.
/// </summary>
public enum AppLockFailureKind
{
    /// <summary>The user cancelled the system prompt.</summary>
    Canceled,

    /// <summary>The attempt was rejected (wrong biometric, failed match).</summary>
    Failed,

    /// <summary>No allowed authenticator can run on this device right now.</summary>
    NotAvailable,

    /// <summary>Nothing is enrolled (no Face ID / fingerprint / device PIN).</summary>
    NotEnrolled,

    /// <summary>The OS has locked out biometrics after too many failures.</summary>
    LockedOut
}
