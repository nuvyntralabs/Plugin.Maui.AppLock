namespace Plugin.Maui.AppLock;

/// <summary>
/// Default lock cover. Swap this in as <c>Window.Page</c> when <see cref="IAppLock.Locked"/> fires
/// so the rest of the app is not visible while waiting for authentication.
/// </summary>
public sealed partial class AppLockPage : ContentPage
{
    readonly IAppLock appLock;

    /// <summary>
    /// Creates a lock cover bound to <paramref name="appLock"/> or <see cref="AppLock.Current"/>.
    /// </summary>
    public AppLockPage(IAppLock? appLock = null)
    {
        InitializeComponent();
        this.appLock = appLock ?? AppLock.Current;
        Title = this.appLock.Options.Title;
        TitleLabel.Text = this.appLock.Options.Title;
        StatusLabel.Text = this.appLock.Options.AuthenticationReason;
        this.appLock.AuthenticationCompleted += OnAuthenticationCompleted;
    }

    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing();
        appLock.AuthenticationCompleted -= OnAuthenticationCompleted;
        appLock.AuthenticationCompleted += OnAuthenticationCompleted;
        _ = PromptAsync();
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        appLock.AuthenticationCompleted -= OnAuthenticationCompleted;
        base.OnDisappearing();
    }

    async void OnUnlockClicked(object? sender, EventArgs e) => await PromptAsync();

    void OnAuthenticationCompleted(object? sender, AppLockAuthResult result)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = result.Succeeded
                ? "Unlocked"
                : result.Message ?? "Authentication failed. Try again.";
        });
    }

    async Task PromptAsync()
    {
        UnlockButton.IsEnabled = false;
        try
        {
            var result = await appLock.RequireAuthenticationAsync().ConfigureAwait(true);
            StatusLabel.Text = result.Succeeded
                ? "Unlocked"
                : result.Message ?? "Authentication failed. Try again.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
        finally
        {
            UnlockButton.IsEnabled = true;
        }
    }
}
