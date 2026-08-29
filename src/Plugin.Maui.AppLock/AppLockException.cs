namespace Plugin.Maui.AppLock;

/// <summary>
/// Thrown when the lock workflow cannot start (misconfiguration, missing host activity).
/// User cancel and failed matches are returned as <see cref="AppLockAuthResult"/>, not thrown.
/// </summary>
public sealed class AppLockException : Exception
{
    /// <summary>
    /// Initializes a new exception.
    /// </summary>
    public AppLockException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
