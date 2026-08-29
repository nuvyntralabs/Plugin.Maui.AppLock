namespace Plugin.Maui.AppLock;

interface IAppLockStore
{
    bool? GetEnabled();

    void SetEnabled(bool enabled);
}
