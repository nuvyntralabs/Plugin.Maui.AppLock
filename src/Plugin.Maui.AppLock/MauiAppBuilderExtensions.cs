using Microsoft.Maui.LifecycleEvents;

namespace Plugin.Maui.AppLock;

/// <summary>
/// MAUI host registration for AppLock.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="IAppLock"/> and hooks Android pause/resume and iOS background/activate
    /// so the lock timer runs without extra host code.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.UseAppLock(options =>
    /// {
    ///     options.LockAfter = TimeSpan.FromMinutes(2);
    ///     options.AllowBiometric = true;
    ///     options.AllowDevicePin = true;
    /// });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseAppLock(this MauiAppBuilder builder, Action<AppLockOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddAppLock(configure);
        builder.Services.AddTransient<IMauiInitializeService, AppLockInitializer>();

        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android =>
            {
                android.OnPause(_ => AppLock.Current.NotifyBackground());
                android.OnResume(_ => AppLock.Current.NotifyForeground());
            });
#elif IOS
            events.AddiOS(ios =>
            {
                ios.DidEnterBackground(_ => AppLock.Current.NotifyBackground());
                ios.OnActivated(_ => AppLock.Current.NotifyForeground());
            });
#endif
        });

        return builder;
    }
}
