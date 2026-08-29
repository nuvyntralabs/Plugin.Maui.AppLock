namespace Plugin.Maui.AppLock;

/// <summary>
/// Registers AppLock services without MAUI lifecycle hooks.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IAppLock"/> using the supplied options instance.
    /// </summary>
    public static IServiceCollection AddAppLock(this IServiceCollection services, AppLockOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.TryAddSingleton<IAuthenticator>(_ => PlatformAuthenticator.Create());
        services.TryAddSingleton<IClock>(_ => SystemClock.Instance);
        services.TryAddSingleton<IAppLockStore, PreferencesAppLockStore>();
        services.TryAddSingleton<IAppLock>(sp =>
        {
            var resolved = sp.GetService<AppLockOptions>() ?? options;
            var authenticator = sp.GetRequiredService<IAuthenticator>();
            var clock = sp.GetRequiredService<IClock>();
            var store = sp.GetRequiredService<IAppLockStore>();
            var instance = AppLock.Create(resolved, authenticator, clock, store);
            AppLock.SetCurrent(instance);
            return instance;
        });

        return services;
    }

    /// <summary>
    /// Adds <see cref="IAppLock"/> and applies <paramref name="configure"/> to a new options instance.
    /// </summary>
    public static IServiceCollection AddAppLock(this IServiceCollection services, Action<AppLockOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new AppLockOptions();
        configure?.Invoke(options);
        return services.AddAppLock(options);
    }
}
