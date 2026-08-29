using Microsoft.Maui.Hosting;

namespace Plugin.Maui.AppLock;

sealed class AppLockInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var appLock = services.GetService<IAppLock>();
        if (appLock is null)
            return;

        AppLock.SetCurrent(appLock);
    }
}
