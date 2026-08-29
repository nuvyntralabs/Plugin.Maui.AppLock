using Microsoft.Extensions.Logging;
using Plugin.Maui.AppLock;

namespace Plugin.Maui.AppLock.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<MainPage>();

        builder
            .UseMauiApp<App>()
            .UseAppLock(options =>
            {
                options.LockAfter = TimeSpan.FromSeconds(10);
                options.AllowBiometric = true;
                options.AllowDevicePin = true;
                options.LockOnStart = true;
                options.AuthenticationReason = "Unlock AppLock Sample";
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
