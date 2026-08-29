# Changelog

## 1.0.0

- Application-security workflow for .NET MAUI on Android and iOS
- `AppLock.Configure` / `await AppLock.RequireAuthenticationAsync()`
- Background grace period (`LockAfter`), then lock on resume
- Face ID, Touch ID, fingerprint, and device PIN / credential
- `AppLockPage` cover so the rest of the app is not visible while locked
- Sample app and unit tests for the lock timer, single-flight prompt, and step-up gate
