namespace Plugin.Maui.AppLock;

/// <summary>
/// Point-in-time view of the application lock.
/// </summary>
/// <param name="State">Current lifecycle state.</param>
/// <param name="IsEnabled">Whether the workflow is active.</param>
/// <param name="IsLocked">Whether authentication is required before the app is usable.</param>
/// <param name="LockAfter">Grace period after backgrounding.</param>
/// <param name="AllowBiometric">Whether Face ID / Touch ID / fingerprint is allowed.</param>
/// <param name="AllowDevicePin">Whether the device PIN / pattern / password is allowed.</param>
/// <param name="BackgroundedAt">When the app last entered the background, if it is still backgrounded.</param>
/// <param name="TimeUntilLock">Remaining grace period, or <c>null</c> when not timing.</param>
public sealed record AppLockSnapshot(
    AppLockState State,
    bool IsEnabled,
    bool IsLocked,
    TimeSpan LockAfter,
    bool AllowBiometric,
    bool AllowDevicePin,
    DateTimeOffset? BackgroundedAt,
    TimeSpan? TimeUntilLock);
