namespace Plugin.Maui.AppLock;

/// <summary>
/// Raised when the lock state changes.
/// </summary>
public sealed class AppLockChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes event arguments.
    /// </summary>
    public AppLockChangedEventArgs(AppLockState previous, AppLockState current, AppLockSnapshot snapshot)
    {
        Previous = previous;
        Current = current;
        Snapshot = snapshot;
    }

    /// <summary>
    /// Gets the state before the change.
    /// </summary>
    public AppLockState Previous { get; }

    /// <summary>
    /// Gets the state after the change.
    /// </summary>
    public AppLockState Current { get; }

    /// <summary>
    /// Gets a snapshot taken after the change.
    /// </summary>
    public AppLockSnapshot Snapshot { get; }
}
