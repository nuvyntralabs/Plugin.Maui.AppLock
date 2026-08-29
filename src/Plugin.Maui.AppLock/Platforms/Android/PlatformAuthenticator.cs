using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using Java.Lang;

namespace Plugin.Maui.AppLock;

sealed class PlatformAuthenticator : IAuthenticator
{
    public static IAuthenticator Create() => new PlatformAuthenticator();

    public Task<AppLockAvailability> GetAvailabilityAsync(AppLockOptions options)
    {
        var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity
                      ?? Microsoft.Maui.ApplicationModel.Platform.AppContext;
        if (context is null)
            return Task.FromResult(AppLockAvailability.Unavailable);

        if (!options.AllowBiometric && !options.AllowDevicePin)
            return Task.FromResult(AppLockAvailability.NotSupported);

        var authenticators = ResolveAuthenticators(options);
        var result = BiometricManager.From(context).CanAuthenticate(authenticators);
        var availability = result switch
        {
            BiometricManager.BiometricSuccess => AppLockAvailability.Available,
            BiometricManager.BiometricErrorNoneEnrolled => options.AllowDevicePin && DeviceSecure(context)
                ? AppLockAvailability.Available
                : AppLockAvailability.NotEnrolled,
            BiometricManager.BiometricErrorNoHardware => options.AllowDevicePin && DeviceSecure(context)
                ? AppLockAvailability.Available
                : AppLockAvailability.NotSupported,
            BiometricManager.BiometricErrorHwUnavailable => options.AllowDevicePin && DeviceSecure(context)
                ? AppLockAvailability.Available
                : AppLockAvailability.Unavailable,
            BiometricManager.BiometricErrorUnsupported => options.AllowDevicePin && DeviceSecure(context)
                ? AppLockAvailability.Available
                : AppLockAvailability.NotSupported,
            _ => AppLockAvailability.Unavailable
        };

        return Task.FromResult(availability);
    }

    public Task<AppLockAuthResult> AuthenticateAsync(AppLockOptions options, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<AppLockAuthResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not FragmentActivity activity)
                {
                    tcs.TrySetResult(AppLockAuthResult.Fail(
                        AppLockFailureKind.NotAvailable,
                        "AppLock requires a foreground activity."));
                    return;
                }

                var callback = new AuthCallback(tcs);
                var executor = ContextCompat.GetMainExecutor(activity)
                    ?? throw new AppLockException("No main executor is available.");
                var prompt = new BiometricPrompt(activity, executor, callback);
                var info = BuildPrompt(options);

                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(() =>
                    {
                        prompt.CancelAuthentication();
                        tcs.TrySetCanceled(cancellationToken);
                    });
                }

                prompt.Authenticate(info);
            }
            catch (System.Exception ex)
            {
                tcs.TrySetResult(AppLockAuthResult.Fail(AppLockFailureKind.NotAvailable, ex.Message));
            }
        });

        return tcs.Task;
    }

    static BiometricPrompt.PromptInfo BuildPrompt(AppLockOptions options)
    {
        var builder = new BiometricPrompt.PromptInfo.Builder()
            .SetTitle(options.Title)
            .SetSubtitle(string.IsNullOrWhiteSpace(options.Subtitle) ? options.AuthenticationReason : options.Subtitle)
            .SetDescription(options.AuthenticationReason);

        var allowPin = options.AllowDevicePin;
        var sdk = (int)Build.VERSION.SdkInt;

        if (allowPin && sdk < (int)BuildVersionCodes.R)
        {
#pragma warning disable CS0618
            builder.SetDeviceCredentialAllowed(true);
#pragma warning restore CS0618
        }
        else
        {
            builder.SetAllowedAuthenticators(ResolveAuthenticators(options));
            if (!allowPin)
                builder.SetNegativeButtonText(options.CancelText);
        }

        return builder.Build();
    }

    static int ResolveAuthenticators(AppLockOptions options)
    {
        var flags = 0;
        if (options.AllowBiometric)
        {
            flags |= BiometricManager.Authenticators.BiometricStrong
                     | BiometricManager.Authenticators.BiometricWeak;
        }

        if (options.AllowDevicePin)
            flags |= BiometricManager.Authenticators.DeviceCredential;

        return flags;
    }

    static bool DeviceSecure(Context context)
    {
        var keyguard = context.GetSystemService(Context.KeyguardService) as KeyguardManager;
        return keyguard?.IsDeviceSecure == true;
    }

    sealed class AuthCallback : BiometricPrompt.AuthenticationCallback
    {
        readonly TaskCompletionSource<AppLockAuthResult> tcs;

        public AuthCallback(TaskCompletionSource<AppLockAuthResult> tcs)
        {
            this.tcs = tcs;
        }

        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
        {
            var method = result?.AuthenticationType switch
            {
                BiometricPrompt.AuthenticationResultTypeDeviceCredential => AppLockMethod.DeviceCredential,
                BiometricPrompt.AuthenticationResultTypeBiometric => AppLockMethod.Biometric,
                _ => AppLockMethod.Biometric
            };

            tcs.TrySetResult(AppLockAuthResult.Success(method));
        }

        public override void OnAuthenticationFailed()
        {
            // Keep listening. A later success, error, or cancel completes the prompt.
        }

        public override void OnAuthenticationError(int errorCode, ICharSequence errString)
        {
            var message = errString?.ToString();
            var result = errorCode switch
            {
                BiometricPrompt.ErrorUserCanceled or BiometricPrompt.ErrorNegativeButton or BiometricPrompt.ErrorCanceled
                    => AppLockAuthResult.Fail(AppLockFailureKind.Canceled, message),
                BiometricPrompt.ErrorLockout or BiometricPrompt.ErrorLockoutPermanent
                    => AppLockAuthResult.Fail(AppLockFailureKind.LockedOut, message),
                BiometricPrompt.ErrorHwNotPresent or BiometricPrompt.ErrorHwUnavailable
                    => AppLockAuthResult.Fail(AppLockFailureKind.NotAvailable, message),
                BiometricPrompt.ErrorNoBiometrics or BiometricPrompt.ErrorNoDeviceCredential
                    => AppLockAuthResult.Fail(AppLockFailureKind.NotEnrolled, message),
                _ => AppLockAuthResult.Fail(AppLockFailureKind.Failed, message)
            };

            tcs.TrySetResult(result);
        }
    }
}
