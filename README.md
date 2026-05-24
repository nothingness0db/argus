# Hotspot Manager

A minimal WPF utility for managing the built-in Windows Mobile Hotspot, plus
per-device blocking and bandwidth limiting via [WinDivert](https://www.reqrypt.org/windivert.html).

中文 / English bilingual UI. Built on .NET 10 + WPF.

## Features

- **Hotspot control** — start / stop the Windows Mobile Hotspot, change SSID,
  password, band (Auto / 2.4 GHz / 5 GHz), hide the network.
- **Connected devices** — live list of clients (hostname, MAC, IP) polled
  from the hotspot.
- **Per-device blocking** — blacklist a client by MAC; its traffic is dropped
  at the network layer via WinDivert.
- **Per-device speed limit** — token-bucket throttling in Kbps per client.
- **Sleep guard** — prevent the host from sleeping while the hotspot is on.
- **Bilingual UI** — toggle between English and 中文 at runtime.
- **Activity log** — collapsible in-app log panel; also written to `logs/`.

## Requirements

- Windows 10 19041 or newer (Windows 11 supported)
- .NET 10 SDK to build, .NET 10 desktop runtime to run
- A Wi-Fi adapter that supports the Mobile Hotspot feature
- **Administrator privileges** — the app manifest requests elevation; required
  by the hotspot API and WinDivert
- For traffic filtering / speed limit / blacklist:
  - `WinDivert.dll` and `WinDivert64.sys` placed in `Assets/`
  - Download from <https://reqrypt.org/windivert.html>

WinDivert binaries are **not** included in this repo (licensing / distribution).
The app still runs without them — only the filter features are disabled.

## Build & Run

```powershell
# from the repo root
./build.ps1
```

The script:

1. Kills any running `HotspotManager.exe`
2. `dotnet build -c Release`
3. Launches the freshly built exe from `bin/x64/Release/...`

Run as Administrator (the manifest will trigger UAC).

To regenerate the app icon from a PNG:

```powershell
# place your source PNG at Assets/logo-src.png, then:
./build-icon.ps1
```

This writes a multi-size `Assets/app.ico` (16 / 24 / 32 / 48 / 64 / 128 / 256).

## Project layout

```
Models/         HotspotConfig, ConnectedDevice
Services/      HotspotService, DeviceMonitorService, TrafficFilterService,
                SleepGuardService, LocaleService, Logger
ViewModels/     MainViewModel (commands, observable state)
Themes/         WPF resource dictionaries (MinimalTheme)
Native/         Win32 + WinRT hotspot interop, WinDivert P/Invoke
Assets/         app.ico (+ logo source, WinDivert binaries when present)
```

## Localisation

Strings live in `Services/LocaleService.cs` as two dictionaries (`EN`, `ZH`).
Add a key to both, bind in XAML with `{Binding L[Your.Key]}`.

## License

MIT — see [LICENSE](LICENSE).

WinDivert is licensed separately (LGPL / GPL / commercial); see its project
page for terms.
