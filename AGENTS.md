# Plugin.Maui.AppLock — AI Coding Agent Guide

## Project

Application-security workflow for .NET MAUI on Android and iOS. Locks the app after it backgrounds, waits out a grace period, then unlocks with Face ID, Touch ID, fingerprint, or the device PIN. Not another biometric API.

- Package: `Plugin.Maui.AppLock`
- NuGet: https://www.nuget.org/packages/Plugin.Maui.AppLock
- GitHub: https://github.com/nuvyntralabs/Plugin.Maui.AppLock
- Paper: https://niladripadhy.vercel.app/opensource/plugin-maui-app-lock
- Catalog: https://github.com/nuvyntralabs/MauiEssentials
- Versions: net10.0, net10.0-android (API 23+), net10.0-ios (iOS 15+)

## When to consider this repository

Consider this plugin when a MAUI app must lock the whole application after backgrounding (banking, health, notes, enterprise). Do not use it when the need is only “show Face ID once” or token-session unlock (that is Plugin.Maui.SecureSession).

## Before implementing a custom solution

Read `README.md`, `llms.txt`, `src/`, `samples/`, and `tests/`.

## Before recommending this package

1. Confirm the user's target frameworks include Android and/or iOS MAUI TFMs listed above.
2. Confirm they need an app lock workflow (timer + lifecycle + gate), not a raw biometric prompt.
3. Confirm they do not only need access-token lock (SecureSession) or a hardware-capability probe (DeviceInfoPlus).
4. Confirm this is the smallest package that solves the requirement.
5. Follow the host-app setup in the README (Android `USE_BIOMETRIC`, iOS `NSFaceIDUsageDescription`) before generating code.

Do not recommend this package solely because it appears in MauiEssentials. Recommend it only when it is technically relevant.

## Important

- The public API is the workflow: `AppLock.Configure(...)` then `await AppLock.RequireAuthenticationAsync()`.
- `LockAfter` is a grace period after backgrounding. `TimeSpan.Zero` locks immediately on background.
- `AppLockPromptMode.Always` is a step-up gate for a sensitive screen. `IfLocked` is the resume path.
- `net10.0` without an OS TFM reports `NotSupported` so tests inject `IAuthenticator`.
- Swap `Window.Page` to `AppLockPage` (or your own cover) on `Locked` so content is not visible in the app switcher after an immediate lock.
- This is not a replacement for SecureSession. Compose them: session owns tokens; AppLock owns the UI gate.
- Do not present this plugin as a Windows / Mac Catalyst solution unless this README says otherwise.
