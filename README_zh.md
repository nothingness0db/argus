# Argus

Windows 自带那个移动热点真的不太行。状态一抽风就卡死，尤其是手贱点了 5 GHz 但适配器其实压根不支持 5 GHz AP 模式 —— 然后整个 API 就锁死了，怎么点都开不起来。我就是被这玩意儿坑烦了才写的这个小工具。

[![Release](https://img.shields.io/github/v/release/nothingness0db/argus)](https://github.com/nothingness0db/argus/releases/latest)

中文 / [English](README.md)。.NET 10 + WPF 写的，包了一层 Windows 热点 API，顺手用 [WinDivert](https://www.reqrypt.org/windivert.html) 加了点单设备拉黑和限速的东西。

## 能干啥

- 该有的热点控制都有：开/关、改 SSID、密码、频段（自动 / 2.4 GHz / 5 GHz）、隐藏网络
- **一键重置系统热点** —— 这个是重头戏，我写这个 App 主要就是冲它来的。Windows 时不时会把热点状态搞坏，最常见的就是上面说的 5 GHz 那个 case：API 一直返回 `WiFiDeviceOff`，重启电脑之外没什么好办法。点这个按钮会跑一遍 `netsh wlan stop hostednetwork`，把 `icssvc` 和 `SharedAccess` 这俩服务重启了，再把 TetheringManager 重新初始化一遍，基本就能救回来。代价：当前其他正在用的 ICS 共享会断个 1–2 秒，已经连进来的客户端会被踢掉，需要管理员权限
- 启动失败自动回退 —— 选了具体频段开不起来的话，会自动改成 Auto 再试一次，过程里 `TetheringOperationStatus`（`WiFiDeviceOff` / `OperationInProgress` 等等）都会写到日志里
- 实时看连进来的设备：主机名、MAC、IP
- 按 MAC 拉黑某个设备，WinDivert 直接在网络层把它的包丢了
- 按设备限速，Kbps，令牌桶
- 热点开着的时候阻止电脑睡眠
- 中英文运行时切换
- 日志面板能折叠，也同步写到 `logs/`

## 跑之前需要

- Windows 10 19041 或更新（Win11 也行）
- 编译要 .NET 10 SDK，运行要 .NET 10 桌面运行时
- 一张能用移动热点的 Wi-Fi 卡
- **管理员权限** —— manifest 里申请了 UAC，热点 API 和 WinDivert 都得管理员

如果你想用限速/拉黑这些过滤功能：

- 把 `WinDivert.dll` 和 `WinDivert64.sys` 放到 `Assets/`
- 从 <https://reqrypt.org/windivert.html> 自己下，许可证原因没法直接塞仓库里

没这俩文件 App 也能跑，只是过滤相关的按钮点了不生效。

## 编译运行

```powershell
# 仓库根目录
./build.ps1
```

脚本干三件事：

1. 杀掉正在跑的 `HotspotManager.exe`
2. `dotnet build -c Release`
3. 从 `bin/x64/Release/...` 启动

记得用管理员身份跑。

想重新生成图标的话，把 PNG 放到 `Assets/logo-src.png`，然后：

```powershell
./build-icon.ps1
```

会生成一份多尺寸的 `Assets/app.ico`（16 / 24 / 32 / 48 / 64 / 128 / 256）。

## 项目结构

```
Models/         HotspotConfig, ConnectedDevice
Services/       HotspotService, DeviceMonitorService, TrafficFilterService,
                SleepGuardService, LocaleService, Logger
ViewModels/     MainViewModel（命令、可观察状态）
Themes/         WPF 资源字典 (MinimalTheme)
Native/         Win32 + WinRT 热点互操作、WinDivert P/Invoke
Assets/         app.ico（以及 logo 源文件和 WinDivert 二进制，如果你放了的话）
```

## 加翻译

字符串都在 `Services/LocaleService.cs`，两个字典 `EN` 和 `ZH`。两边都加上 key，XAML 里用 `{Binding L[Your.Key]}` 绑就行。

## 许可证

MIT，见 [LICENSE](LICENSE)。

WinDivert 是单独授权的（LGPL / GPL / 商业），具体看他们项目页。
