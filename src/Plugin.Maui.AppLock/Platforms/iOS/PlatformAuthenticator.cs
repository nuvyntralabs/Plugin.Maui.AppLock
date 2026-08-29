using LocalAuthentication;

namespace Plugin.Maui.AppLock;

sealed class PlatformAuthenticator : IAuthenticator
{
    public static IAuthenticator Create() => new PlatformAuthenticator();

    public Task<AppLockAvailability> GetAvailabilityAsync(AppLockOptions options)
    {
        using var context = new LAContext();
        var policy = ResolvePolicy(options);
        if (context.CanEvaluatePolicy(policy, out var error))
            return Task.FromResult(AppLockAvailability.Available);

        var availability = error?.Code switch
        {
            (int)LAStatus.BiometryNotEnrolled or (int)LAStatus.PasscodeNotSet => AppLockAvailability.NotEnrolled,
            (int)LAStatus.BiometryNotAvailable => options.AllowDevicePin
                ? AppLockAvailability.NotEnrolled
                : AppLockAvailability.NotSupported,
            (int)LAStatus.BiometryLockout => AppLockAvailability.Unavailable,
            _ => AppLockAvailability.Unavailable
        };

        return Task.FromResult(availability);
    }

    public async Task<AppLockAuthResult> AuthenticateAsync(AppLockOptions options, CancellationToken cancellationToken)
    {
        using var context = new LAContext();
        var policy = ResolvePolicy(options);
        if (!context.CanEvaluatePolicy(policy, out var error))
        {
            return error?.Code switch
            {
                (int)LAStatus.BiometryNotEnrolled or (int)LAStatus.PasscodeNotSet =>
                    AppLockAuthResult.Fail(AppLockFailureKind.NotEnrolled, error?.LocalizedDescription),
                (int)LAStatus.BiometryLockout =>
                    AppLockAuthResult.Fail(AppLockFailureKind.LockedOut, error?.LocalizedDescription),
                _ => AppLockAuthResult.Fail(AppLockFailureKind.NotAvailable, error?.LocalizedDescription)
            };
        }

        using var registration = cancellationToken.Register(context.Invalidate);
        try
        {
            var result = await context.EvaluatePolicyAsync(policy, options.AuthenticationReason)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!result.Item1)
            {
                return AppLockAuthResult.Fail(
                    AppLockFailureKind.Failed,
                    result.Item2?.LocalizedDescription ?? "Authentication failed.");
            }

            var method = policy == LAPolicy.DeviceOwnerAuthenticationWithBiometrics
                ? AppLockMethod.Biometric
                : InferIosMethod(context);

            return AppLockAuthResult.Success(method);
        }
        catch (OperationCanceledException)
        {
            return AppLockAuthResult.Fail(AppLockFailureKind.Canceled, "Authentication was cancelled.");
        }
        catch (Exception ex)
        {
            return IsUserCancel(ex)
                ? AppLockAuthResult.Fail(AppLockFailureKind.Canceled, ex.Message)
                : AppLockAuthResult.Fail(AppLockFailureKind.Failed, ex.Message);
        }
    }

    static LAPolicy ResolvePolicy(AppLockOptions options) =>
        options.AllowDevicePin
            ? LAPolicy.DeviceOwnerAuthentication
            : LAPolicy.DeviceOwnerAuthenticationWithBiometrics;

    static AppLockMethod InferIosMethod(LAContext context) =>
        context.BiometryType is LABiometryType.None
            ? AppLockMethod.DeviceCredential
            : AppLockMethod.Biometric;

    static bool IsUserCancel(Exception ex) =>
        ex.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("Code=-2", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("Code=-4", StringComparison.OrdinalIgnoreCase);
}
