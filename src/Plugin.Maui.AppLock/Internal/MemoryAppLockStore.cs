namespace Plugin.Maui.AppLock;

sealed class MemoryAppLockStore : IAppLockStore
{
    bool? enabled;

    public bool? GetEnabled() => enabled;

    public void SetEnabled(bool value) => enabled = value;
}
