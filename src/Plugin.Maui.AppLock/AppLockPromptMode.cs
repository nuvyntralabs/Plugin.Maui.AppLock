namespace Plugin.Maui.AppLock;

/// <summary>
/// When <see cref="IAppLock.RequireAuthenticationAsync"/> shows a system prompt.
/// </summary>
public enum AppLockPromptMode
{
    /// <summary>
    /// Prompt only when the app is locked. The usual resume / gate path.
    /// </summary>
    IfLocked,

    /// <summary>
    /// Always prompt. Use this as a step-up gate on a sensitive screen
    /// (view balance, export data) even if the lock timer has not elapsed.
    /// </summary>
    Always
}
