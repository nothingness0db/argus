# Argus

Windows' built-in Mobile Hotspot is unreliable. The classic failure mode: you switch the band to 5 GHz, the Wi-Fi adapter doesn't actually support 5 GHz AP mode, and now the entire `NetworkOperatorTetheringManager` is wedged. `StartTetheringAsync` keeps returning `WiFiDeviceOff` no matter what you do. Nothing in the Settings app gets you out of it. Rebooting is the only fix that works.

Argus is the tool I wrote so I'd stop having to reboot. It wraps the Windows hotspot API in a less-broken UI, adds per-device blocking and rate limiting on top via WinDivert, and exposes a "Reset System Hotspot" button that runs the service-restart dance manually.

.NET 10, WPF, single exe, EN / 中文 UI.

[![Release](https://img.shields.io/github/v/release/nothingness0db/argus)](https://github.com/nothingness0db/argus/releases/latest)

[中文](README_zh.md)

## What it does

**Hotspot controls.** Start/stop, change SSID, password, band (Auto / 2.4 GHz / 5 GHz), hide the SSID. Passwords shorter than 8 characters are rejected before they hit the API. Applying new settings while the hotspot is running stops it, configures, and restarts it.

**Reset System Hotspot.** The actual reason this app exists. When Windows wedges its tethering state, this button runs the following in order:

```
netsh wlan stop hostednetwork
net stop icssvc
net stop SharedAccess
sleep 1.5s
net start SharedAccess
net start icssvc
sleep 0.5s
```

Then re-initialises `NetworkOperatorTetheringManager` from a fresh internet connection profile. That forces Windows to throw out its cached adapter-capability detection so the next start attempt actually re-probes the hardware. Side effects: any other ICS share you have running is interrupted for a second or two, currently connected clients get kicked, admin rights required.

**Automatic band fallback.** If a start attempt fails on a specific band, the app reconfigures to Auto and retries once. The actual `TetheringOperationStatus` (`WiFiDeviceOff`, `OperationInProgress`, ...) ends up in the log so you can tell whether the failure was hardware or something else.

**Connected device list**, polled every 3 seconds. Each row shows hostname, MAC, IP, current rate limit, status badge (ACTIVE / BLOCKED), action button. MAC and hostname come from `GetTetheringClients()`. IP is resolved from the ARP table via `iphlpapi.dll GetIpNetTable` since the WinRT API doesn't expose it.

**Per-MAC blacklist** and **per-device rate limit in Kbps**. Both run on top of WinDivert: the service opens a NETWORK-layer sniff handle, checks each outbound packet's source/destination against the blacklist (drop on match), then runs it through a per-IP token bucket (drop if no tokens). The bucket allows a 2× burst over the configured rate. Rate limit is editable inline in the device table; 0 means unlimited.

**Sleep guard.** Keeps the host awake while the hotspot is on, even if the screen locks. Calls `SetThreadExecutionState` with `ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED` and re-asserts every 5 seconds in case anything else stomps on it.

**Logging.** Collapsible in-app panel with three levels (info / warn / error). Also written to `logs/hotspot_yyyyMMdd.log`, one file per day, UTF-8 with BOM.

**Bilingual UI.** EN / 中文 toggle at runtime. The choice is persisted to `%AppData%/Argus/locale.txt`.

## Requirements

- Windows 10 build 19041 or newer (Windows 11 fine). That's the floor for the `NetworkOperatorTetheringManager` surface this app relies on.
- .NET 10 SDK to build, .NET 10 desktop runtime to run.
- A Wi-Fi adapter that supports the Mobile Hotspot feature.
- **Administrator privileges.** The manifest requires elevation. Both the hotspot API and WinDivert refuse to function without it.

For the blacklist and rate-limit features:

- Drop `WinDivert.dll` and `WinDivert64.sys` into `Assets/`.
- Get them from <https://reqrypt.org/windivert.html>. They aren't bundled here because of WinDivert's licensing.

Without those two files the app still launches and the hotspot controls all work; only the filter-related buttons no-op.

## Build

```powershell
./build.ps1
```

Sets `DOTNET_ROOT`, runs `dotnet build -c Release`. The exe lands at `bin/x64/Release/net10.0-windows10.0.19041.0/HotspotManager.exe`.

Pass `-Run` to also kill any existing instance and launch the fresh build.

To regenerate the icon, put a PNG at `Assets/logo-src.png` and run `./build-icon.ps1`. You get a multi-size `Assets/app.ico` (16 / 24 / 32 / 48 / 64 / 128 / 256).

## Project layout

```
Models/        ConnectedDevice, HotspotConfig
Services/      HotspotService          start/stop/configure/reset
               DeviceMonitorService    3s polling + ARP lookup
               TrafficFilterService    WinDivert blacklist + token bucket
               SleepGuardService       SetThreadExecutionState
               LocaleService           EN/ZH dicts + persistence
               Logger                  in-app + file
ViewModels/    MainViewModel
Themes/        WPF resource dictionaries (MinimalTheme)
Native/        HotspotNative (TetheringManager wrapper),
               Win32Native (kernel32, iphlpapi, WinDivert P/Invoke)
Assets/        app.ico + WinDivert binaries if you add them
```

## Translations

Strings live in `Services/LocaleService.cs` as two dictionaries (`EN`, `ZH`). Add a key to both, bind in XAML via `{Binding L[Your.Key]}`. Log messages can be localised too — `Logger` has `TrInfo` / `TrWarn` / `TrError` that route through `LocaleService.Format(key, args...)`.

## License

MIT — see [LICENSE](LICENSE).

WinDivert is licensed separately (LGPL / GPL / commercial). Check their project page for terms.
