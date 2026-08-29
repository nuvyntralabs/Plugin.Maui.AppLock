namespace Plugin.Maui.AppLock;

/// <summary>
/// Lifecycle state of the application lock.
/// </summary>
public enum AppLockState
{
    /// <summary>The lock is turned off. Sensitive screens are not gated.</summary>
    Disabled,

    /// <summary>The app is accessible without a prompt.</summary>
    Unlocked,

    /// <summary>The app is locked and requires authentication.</summary>
    Locked,

    /// <summary>A biometric or device-credential prompt is on screen.</summary>
    Authenticating
}
