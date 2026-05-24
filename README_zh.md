# Argus

Windows 自带的移动热点不太靠谱，最经典的坏法是：你把频段切到 5 GHz，但你的网卡其实不支持 5 GHz AP 模式，于是整个 `NetworkOperatorTetheringManager` 就陷进一个脏状态里出不来了 —— `StartTetheringAsync` 怎么调都返回 `WiFiDeviceOff`，系统设置里点啥都救不回来，只能重启电脑。

Argus 就是我为了不再重启电脑搞出来的小工具。它把 Windows 那套热点 API 包了一层不那么烂的 UI，顺带通过 WinDivert 加了"按设备拉黑"和"按设备限速"两个功能，再露出一个"重置系统热点"的按钮，把那一串救场的服务重启序列手动跑一遍。

.NET 10，WPF，单 exe，中英双语界面。

[![Release](https://img.shields.io/github/v/release/nothingness0db/argus)](https://github.com/nothingness0db/argus/releases/latest)

[English](README.md)

## 能干啥

**热点控制**。开关、改 SSID、改密码、选频段（自动 / 2.4 GHz / 5 GHz）、隐藏 SSID。密码不到 8 位会在送进 API 之前就被拒掉。已经开着热点的时候改配置，会先停掉、应用完了再起回来。

**重置系统热点**。这是这个 App 的核心功能。Windows 一旦把自己的热点状态搞坏，按这个按钮会顺序执行：

```
netsh wlan stop hostednetwork
net stop icssvc
net stop SharedAccess
sleep 1.5s
net start SharedAccess
net start icssvc
sleep 0.5s
```

然后从一个新的 internet connection profile 重新初始化 `NetworkOperatorTetheringManager`。这样能强制 Windows 把内部缓存的"网卡能力检测结果"扔掉，下次启动会重新探测硬件。代价：当前其他正在用的 ICS 共享会被打断一两秒，已经连进来的客户端会被踢掉，要管理员权限。

**频段失败自动回退**。如果指定频段起不来，App 会自动把配置改成 Auto 再试一次，把过程中的 `TetheringOperationStatus`（`WiFiDeviceOff` / `OperationInProgress` 等等）写到日志里，免得你分不清到底是硬件问题还是别的。

**已连接设备列表**，每 3 秒刷新一次。每行有主机名、MAC、IP、当前限速、状态（ACTIVE / BLOCKED）、操作按钮。MAC 和主机名来自 `GetTetheringClients()`；IP 是从 ARP 表里反查的（`iphlpapi.dll GetIpNetTable`），因为 WinRT 的那个 API 本身不告诉你 IP。

**按 MAC 拉黑** 和 **按设备限速（Kbps）**。两个都跑在 WinDivert 上：开一个 NETWORK 层的 sniff 句柄，每个出站包先看 src/dst IP 在不在黑名单里（在就丢），不在的再过一遍单 IP 的令牌桶（没令牌就丢）。令牌桶允许 2× 速率的突发。限速值在设备表里直接编辑，0 表示不限速。

**防睡眠**。热点开着但屏幕想锁屏不挂掉？开这个守护。底层是 `SetThreadExecutionState`，标志位 `ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED`，并且每 5 秒重新打一遍，防止被别的东西覆盖掉。

**日志**。底部有可折叠的日志面板，三档颜色（info 灰、warn 橙、error 红）。同时落盘到 `logs/hotspot_yyyyMMdd.log`，按天分文件，UTF-8 BOM。

**中英文运行时切换**。语言选择写到 `%AppData%/Argus/locale.txt`，重开还在。

## 环境要求

- Windows 10 build 19041 或更新（Win11 也行）。这个版本号是因为 App 用到的 `NetworkOperatorTetheringManager` 那一套 API 要求这个最低版本
- 编译要 .NET 10 SDK，运行要 .NET 10 桌面运行时
- 一张支持移动热点功能的 Wi-Fi 网卡
- **管理员权限**。manifest 里写死了 `requireAdministrator`，热点 API 和 WinDivert 都需要管理员才能用

想用拉黑/限速：

- 把 `WinDivert.dll` 和 `WinDivert64.sys` 放到 `Assets/`
- 自己从 <https://reqrypt.org/windivert.html> 下，许可证原因没法塞仓库里

没这俩文件 App 还是能跑，热点控制都正常工作，只是过滤相关的按钮点了不生效。

## 编译

```powershell
./build.ps1
```

设置 `DOTNET_ROOT`，跑 `dotnet build -c Release`，产物在 `bin/x64/Release/net10.0-windows10.0.19041.0/HotspotManager.exe`。

加 `-Run` 参数会顺手把正在跑的实例杀掉、再启动新构建好的。

想重新生成图标：放一张 PNG 到 `Assets/logo-src.png`，跑 `./build-icon.ps1`，会生成多尺寸的 `Assets/app.ico`（16 / 24 / 32 / 48 / 64 / 128 / 256）。

## 项目结构

```
Models/        ConnectedDevice, HotspotConfig
Services/      HotspotService          启停 / 配置 / 重置
               DeviceMonitorService    3 秒轮询 + ARP 表查 IP
               TrafficFilterService    WinDivert 黑名单 + 令牌桶
               SleepGuardService       SetThreadExecutionState
               LocaleService           EN/ZH 字典 + 持久化
               Logger                  屏内 + 文件
ViewModels/    MainViewModel
Themes/        WPF 资源字典 (MinimalTheme)
Native/        HotspotNative (TetheringManager 包装),
               Win32Native (kernel32 / iphlpapi / WinDivert P/Invoke)
Assets/        app.ico，以及你自己放的 WinDivert 二进制
```

## 加翻译

字符串都在 `Services/LocaleService.cs`，两个字典 `EN` 和 `ZH`。两边都加 key，XAML 里用 `{Binding L[Your.Key]}` 绑。日志消息也能本地化 —— `Logger` 上有 `TrInfo` / `TrWarn` / `TrError`，走的是 `LocaleService.Format(key, args...)`。

## 许可证

MIT，详见 [LICENSE](LICENSE)。

WinDivert 是单独授权的（LGPL / GPL / 商业），具体看他们项目页。
