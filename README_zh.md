# Argus

> Windows 热点的百眼巨人守护者。

[![Release](https://img.shields.io/github/v/release/nothingness0db/argus)](https://github.com/nothingness0db/argus/releases/latest)

一个轻量级 WPF 工具，用于管理 Windows 移动热点，并通过 [WinDivert](https://www.reqrypt.org/windivert.html) 实现逐设备屏蔽和带宽限制。

中文 / English 双语界面。基于 .NET 10 + WPF 构建。

## 功能

- **热点控制** — 启动/停止 Windows 移动热点，修改 SSID、密码、频段（自动/2.4 GHz/5 GHz）、隐藏网络
- **重置系统热点** — 一键恢复 Windows 移动热点卡死状态（如选择 5 GHz 后适配器不支持 AP 模式，API 永久返回 `WiFiDeviceOff`）
- **自动频段回退** — 指定频段启动失败时，自动切换到重试一次
- **已连接设备** — 实时显示客户端列表（主机名、MAC、IP）
- **逐设备屏蔽** — 按 MAC 拉黑客户端，通过 WinDivert 在网络层丢弃流量
- **逐设备限速** — 每客户端令牌桶限速（Kbps）
- **防睡眠** — 热点开启时阻止主机休眠
- **双语界面** — 运行时切换 English / 中文
- **活动日志** — 可折叠日志面板，同时写入 `logs/`

## 系统要求

- Windows 10 19041 或更高版本（支持 Windows 11）
- 构建需要 .NET 10 SDK，运行需要 .NET 10 桌面运行时
- 支持移动热点功能的 Wi-Fi 适配器
- **管理员权限** — 应用清单请求提权，热点 API 和 WinDivert 需要管理员权限
- 流量过滤/限速/黑名单功能：
  - `WinDivert.dll` 和 `WinDivert64.sys` 放在 `Assets/` 目录
  - 从 <https://reqrypt.org/windivert.html> 下载

WinDivert 二进制文件**不**包含在本仓库中（许可证/分发问题）。应用仍可运行，仅过滤功能不可用。

## 构建与运行

```powershell
# 在仓库根目录
./build.ps1
```

脚本会：

1. 终止正在运行的 `HotspotManager.exe`
2. `dotnet build -c Release`
3. 从 `bin/x64/Release/...` 启动构建好的 exe

以管理员身份运行（清单会触发 UAC）。

## 项目结构

```
Models/         HotspotConfig, ConnectedDevice
Services/       HotspotService, DeviceMonitorService, TrafficFilterService,
                SleepGuardService, LocaleService, Logger
ViewModels/     MainViewModel (命令、可观察状态)
Themes/         WPF 资源字典 (MinimalTheme)
Native/         Win32 + WinRT 热点互操作、WinDivert P/Invoke
Assets/         app.ico (+ logo 源文件、WinDivert 二进制文件)
```

## 本地化

字符串在 `Services/LocaleService.cs` 中以两个字典（`EN`、`ZH`）存储。添加键后，在 XAML 中使用 `{Binding L[Your.Key]}` 绑定。

## 许可证

MIT — 详见 [LICENSE](LICENSE)。

WinDivert 许可证独立（LGPL / GPL / 商业），详见其项目页面。
