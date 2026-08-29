using Plugin.Maui.AppLock;

namespace Plugin.Maui.AppLock.Sample;

public partial class App : Application
{
    readonly MainPage mainPage;
    readonly IAppLock appLock;
    Window? window;

    public App(MainPage mainPage, IAppLock appLock)
    {
        InitializeComponent();
        this.mainPage = mainPage;
        this.appLock = appLock;
        this.appLock.StateChanged += OnStateChanged;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        window = new Window(CreatePage());
        return window;
    }

    Page CreatePage() =>
        appLock.IsLocked
            ? new AppLockPage(appLock)
            : new NavigationPage(mainPage);

    void OnStateChanged(object? sender, AppLockChangedEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (window is null)
                return;

            var showLock = e.Current is AppLockState.Locked or AppLockState.Authenticating;
            if (showLock && window.Page is AppLockPage)
                return;

            window.Page = showLock
                ? new AppLockPage(appLock)
                : new NavigationPage(mainPage);
        });
}
