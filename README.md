# Argus

Windows' built-in Mobile Hotspot is flaky. It corrupts its own state and refuses to come back up — especially if you touch the 5 GHz toggle on an adapter that doesn't actually support 5 GHz AP mode. Once that happens, the API just locks up and returns `WiFiDeviceOff` forever no matter what you click. I wrote this app mostly because I was tired of rebooting to fix it.

[![Release](https://img.shields.io/github/v/release/nothingness0db/argus)](https://github.com/nothingness0db/argus/releases/latest)

[中文](README_zh.md) / English. Built on .NET 10 + WPF. Wraps the Windows hotspot API and adds per-device blocking and bandwidth limiting via [WinDivert](https://www.reqrypt.org/windivert.html).

## What it does

- The usual hotspot controls: start/stop, change SSID, password, band (Auto / 2.4 GHz / 5 GHz), hide the network.
- **Reset System Hotspot** — the main reason this app exists. When Windows wedges its own hotspot state (the 5 GHz case above is the most common one — the API stays stuck on `WiFiDeviceOff` until you reboot), this button runs `netsh wlan stop hostednetwork`, restarts the `icssvc` and `SharedAccess` services, and re-initialises the TetheringManager. That clears the dirty state cache so the next start attempt actually re-detects adapter capabilities. Usually fixes it. Caveats: any other ICS share you have running gets interrupted for a second or two, currently-connected clients are kicked, admin rights required.
- Automatic band fallback — if Start fails on a specific band, the app reconfigures to Auto and retries once. The actual `TetheringOperationStatus` (`WiFiDeviceOff`, `OperationInProgress`, ...) shows up in the log so you can see what happened.
- Live list of connected clients (hostname, MAC, IP).
- Per-device blacklist by MAC — traffic is dropped at the network layer via WinDivert.
- Per-device speed limit, Kbps, token bucket.
- Sleep guard — keep the host awake while the hotspot is on.
- EN / 中文 toggle at runtime.
- Collapsible log panel; also written to `logs/`.

## Requirements

- Windows 10 19041 or newer (Windows 11 fine)
- .NET 10 SDK to build, .NET 10 desktop runtime to run
- A Wi-Fi adapter that supports the Mobile Hotspot feature
- **Administrator privileges** — the app manifest requests elevation; hotspot API and WinDivert both need it

For the filter / speed-limit / blacklist features:

- Drop `WinDivert.dll` and `WinDivert64.sys` into `Assets/`
- Get them from <https://reqrypt.org/windivert.html> — I can't redistribute them here, licensing

The app runs fine without them, you just lose the filter features.

## Build & Run

```powershell
# from the repo root
./build.ps1
```

The script:

1. Kills any running `HotspotManager.exe`
2. `dotnet build -c Release`
3. Launches the freshly built exe from `bin/x64/Release/...`

Run elevated (the manifest will trigger UAC).

To regenerate the app icon from a PNG, drop your source at `Assets/logo-src.png` and run:

```powershell
./build-icon.ps1
```

You get a multi-size `Assets/app.ico` (16 / 24 / 32 / 48 / 64 / 128 / 256).

## Project layout

```
Models/         HotspotConfig, ConnectedDevice
Services/       HotspotService, DeviceMonitorService, TrafficFilterService,
                SleepGuardService, LocaleService, Logger
ViewModels/     MainViewModel (commands, observable state)
Themes/         WPF resource dictionaries (MinimalTheme)
Native/         Win32 + WinRT hotspot interop, WinDivert P/Invoke
Assets/         app.ico (+ logo source, WinDivert binaries when present)
```

## Localisation

Strings live in `Services/LocaleService.cs` as two dictionaries (`EN`, `ZH`). Add a key to both, bind in XAML via `{Binding L[Your.Key]}`.

## License

MIT — see [LICENSE](LICENSE).

WinDivert is licensed separately (LGPL / GPL / commercial); see their project page for terms.
