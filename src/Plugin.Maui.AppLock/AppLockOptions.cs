namespace Plugin.Maui.AppLock;

/// <summary>
/// Configuration for the application-security workflow.
/// </summary>
public sealed class AppLockOptions
{
    /// <summary>
    /// Gets or sets whether the lock workflow is active after registration.
    /// Persisted across process death when <see cref="PersistEnabled"/> is <c>true</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the grace period after the app backgrounds before a lock is required.
    /// <see cref="TimeSpan.Zero"/> locks immediately on background (recommended when
    /// the app switcher screenshot must not show sensitive UI).
    /// </summary>
    public TimeSpan LockAfter { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets whether Face ID, Touch ID, or fingerprint may unlock the app.
    /// </summary>
    public bool AllowBiometric { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the device PIN, pattern, or password may unlock the app.
    /// On iOS this is the LocalAuthentication passcode fallback.
    /// On Android this is <c>DEVICE_CREDENTIAL</c>.
    /// </summary>
    public bool AllowDevicePin { get; set; } = true;

    /// <summary>
    /// Gets or sets whether leaving the app starts the lock timer.
    /// </summary>
    public bool LockOnBackground { get; set; } = true;

    /// <summary>
    /// Gets or sets whether a cold start (process death) begins locked when the workflow is enabled.
    /// </summary>
    public bool LockOnStart { get; set; } = true;

    /// <summary>
    /// Gets or sets whether returning to the foreground while locked automatically
    /// calls <see cref="IAppLock.RequireAuthenticationAsync"/>.
    /// </summary>
    public bool AutoPromptOnResume { get; set; } = true;

    /// <summary>
    /// Gets or sets whether <see cref="Enabled"/> is written to preferences so a user
    /// who disables the lock keeps that choice after process death.
    /// </summary>
    public bool PersistEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the system-prompt title (Android). iOS uses <see cref="AuthenticationReason"/>.
    /// </summary>
    public string Title { get; set; } = "Unlock";

    /// <summary>
    /// Gets or sets the system-prompt subtitle (Android).
    /// </summary>
    public string Subtitle { get; set; } = "Confirm your identity";

    /// <summary>
    /// Gets or sets the reason shown on the Face ID / Touch ID / fingerprint sheet.
    /// </summary>
    public string AuthenticationReason { get; set; } = "Unlock to continue";

    /// <summary>
    /// Gets or sets the cancel button text when device PIN is not an allowed fallback.
    /// Android hides this button when <see cref="AllowDevicePin"/> is <c>true</c>.
    /// </summary>
    public string CancelText { get; set; } = "Cancel";
}
