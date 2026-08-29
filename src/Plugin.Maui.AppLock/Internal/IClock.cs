namespace Plugin.Maui.AppLock;

interface IClock
{
    DateTimeOffset UtcNow { get; }
}
