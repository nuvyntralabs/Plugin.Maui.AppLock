namespace Plugin.Maui.AppLock;

/// <summary>
/// Outcome of <see cref="IAppLock.RequireAuthenticationAsync"/>.
/// Failed attempts stay locked; they do not throw.
/// </summary>
public sealed class AppLockAuthResult
{
    /// <summary>
    /// Initializes a result.
    /// </summary>
    public AppLockAuthResult(bool succeeded, AppLockMethod method, AppLockFailureKind? failure = null, string? message = null)
    {
        Succeeded = succeeded;
        Method = method;
        Failure = failure;
        Message = message;
    }

    /// <summary>
    /// Gets a value indicating whether the app is now unlocked (or the lock is disabled).
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the method that unlocked the app, or <see cref="AppLockMethod.None"/>.
    /// </summary>
    public AppLockMethod Method { get; }

    /// <summary>
    /// Gets the failure classification when <see cref="Succeeded"/> is <c>false</c>.
    /// </summary>
    public AppLockFailureKind? Failure { get; }

    /// <summary>
    /// Gets an optional platform or plugin message.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static AppLockAuthResult Success(AppLockMethod method, string? message = null) =>
        new(true, method, null, message);

    /// <summary>
    /// Creates a failed result. The app stays locked.
    /// </summary>
    public static AppLockAuthResult Fail(AppLockFailureKind failure, string? message = null) =>
        new(false, AppLockMethod.None, failure, message);
}
