using Microsoft.Maui.Storage;

namespace Plugin.Maui.AppLock;

sealed class PreferencesAppLockStore : IAppLockStore
{
    const string EnabledKey = "Plugin.Maui.AppLock.Enabled";

    public bool? GetEnabled()
    {
        if (!Preferences.Default.ContainsKey(EnabledKey))
            return null;

        return Preferences.Default.Get(EnabledKey, true);
    }

    public void SetEnabled(bool enabled) => Preferences.Default.Set(EnabledKey, enabled);
}
