namespace Plugin.Maui.AppLock;

/// <summary>
/// Whether the device can satisfy the configured lock methods.
/// </summary>
public enum AppLockAvailability
{
    /// <summary>At least one allowed method (biometric or device PIN) can run.</summary>
    Available,

    /// <summary>Hardware exists but nothing is enrolled (no Face ID / fingerprint / device PIN).</summary>
    NotEnrolled,

    /// <summary>The platform does not support the requested authenticators.</summary>
    NotSupported,

    /// <summary>Hardware or OS policy currently prevents a prompt (lockout, no activity, etc.).</summary>
    Unavailable
}
