namespace Plugin.Maui.AppLock;

/// <summary>
/// Default lock cover. Swap this in as <c>Window.Page</c> when <see cref="IAppLock.Locked"/> fires
/// so the rest of the app is not visible while waiting for authentication.
/// </summary>
public sealed class AppLockPage : ContentPage
{
    readonly IAppLock appLock;
    readonly Label statusLabel;
    readonly Button unlockButton;

    /// <summary>
    /// Creates a lock cover bound to <paramref name="appLock"/> or <see cref="AppLock.Current"/>.
    /// </summary>
    public AppLockPage(IAppLock? appLock = null)
    {
        this.appLock = appLock ?? AppLock.Current;
        Title = this.appLock.Options.Title;

        statusLabel = new Label
        {
            Text = this.appLock.Options.AuthenticationReason,
            HorizontalTextAlignment = TextAlignment.Center,
            FontSize = 16
        };

        unlockButton = new Button
        {
            Text = "Unlock",
            HorizontalOptions = LayoutOptions.Center
        };
        unlockButton.Clicked += OnUnlockClicked;

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(32),
            Spacing = 20,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = "🔒",
                    FontSize = 48,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                new Label
                {
                    Text = this.appLock.Options.Title,
                    FontSize = 24,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                statusLabel,
                unlockButton
            }
        };

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
            statusLabel.Text = result.Succeeded
                ? "Unlocked"
                : result.Message ?? "Authentication failed. Try again.";
        });
    }

    async Task PromptAsync()
    {
        unlockButton.IsEnabled = false;
        try
        {
            var result = await appLock.RequireAuthenticationAsync().ConfigureAwait(true);
            statusLabel.Text = result.Succeeded
                ? "Unlocked"
                : result.Message ?? "Authentication failed. Try again.";
        }
        catch (Exception ex)
        {
            statusLabel.Text = ex.Message;
        }
        finally
        {
            unlockButton.IsEnabled = true;
        }
    }
}
