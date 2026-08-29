using Plugin.Maui.AppLock;

namespace Plugin.Maui.AppLock.Sample;

public partial class MainPage : ContentPage
{
    readonly IAppLock appLock;
    bool suppressToggle;

    public MainPage(IAppLock appLock)
    {
        InitializeComponent();
        this.appLock = appLock;
        this.appLock.StateChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Refresh);
        LockAfterPicker.SelectedIndex = 1;
        Refresh();
        _ = RefreshAvailabilityAsync();
    }

    async void OnEnabledToggled(object? sender, ToggledEventArgs e)
    {
        if (suppressToggle)
            return;

        if (e.Value)
            await appLock.EnableAsync();
        else
            await appLock.DisableAsync();

        Refresh();
    }

    void OnOptionsChanged(object? sender, ToggledEventArgs e)
    {
        if (suppressToggle)
            return;

        appLock.Configure(options =>
        {
            options.AllowBiometric = BiometricSwitch.IsToggled;
            options.AllowDevicePin = PinSwitch.IsToggled;
        });
        Refresh();
    }

    void OnLockAfterChanged(object? sender, EventArgs e)
    {
        var lockAfter = LockAfterPicker.SelectedIndex switch
        {
            0 => TimeSpan.Zero,
            2 => TimeSpan.FromMinutes(2),
            _ => TimeSpan.FromSeconds(10)
        };

        appLock.Configure(options => options.LockAfter = lockAfter);
        Refresh();
    }

    async void OnLockClicked(object? sender, EventArgs e) =>
        await appLock.LockAsync();

    async void OnUnlockClicked(object? sender, EventArgs e) =>
        await RunAsync(() => appLock.UnlockAsync());

    async void OnBalanceClicked(object? sender, EventArgs e)
    {
        var result = await appLock.RequireAuthenticationAsync(AppLockPromptMode.Always);
        BalanceLabel.Text = result.Succeeded
            ? $"Balance  $12,480.00  ({result.Method})"
            : result.Message ?? "Balance hidden";
        Refresh();
    }

    void OnBackgroundClicked(object? sender, EventArgs e)
    {
        appLock.NotifyBackground();
        Refresh();
    }

    void OnForegroundClicked(object? sender, EventArgs e)
    {
        appLock.NotifyForeground();
        Refresh();
    }

    async Task RunAsync(Func<Task<AppLockAuthResult>> action)
    {
        try
        {
            var result = await action();
            if (!result.Succeeded)
                await DisplayAlert("AppLock", result.Message ?? result.Failure?.ToString(), "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("AppLock", ex.Message, "OK");
        }

        Refresh();
    }

    void Refresh()
    {
        var snapshot = appLock.GetSnapshot();
        StatusLabel.Text =
            $"State={snapshot.State}  enabled={snapshot.IsEnabled}  locked={snapshot.IsLocked}  grace={snapshot.LockAfter}  remaining={snapshot.TimeUntilLock?.ToString() ?? "—"}";

        suppressToggle = true;
        EnabledSwitch.IsToggled = snapshot.IsEnabled;
        BiometricSwitch.IsToggled = snapshot.AllowBiometric;
        PinSwitch.IsToggled = snapshot.AllowDevicePin;
        suppressToggle = false;
    }

    async Task RefreshAvailabilityAsync()
    {
        var availability = await appLock.GetAvailabilityAsync();
        AvailabilityLabel.Text = $"Availability={availability}  biometric={appLock.Options.AllowBiometric}  pin={appLock.Options.AllowDevicePin}";
    }
}
