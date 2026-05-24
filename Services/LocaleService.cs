using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace HotspotManager.Services
{
    public static class LocaleService
    {
        private static string _current = LoadSaved();

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Argus", "locale.txt");

        private static string LoadSaved()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var v = File.ReadAllText(SettingsPath).Trim();
                    if (v == "EN" || v == "ZH") return v;
                }
            }
            catch { }
            return "EN";
        }

        private static void SaveCurrent()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, _current);
            }
            catch { }
        }

        private static readonly Dictionary<string, string> EN = new Dictionary<string, string>
        {
            ["App.Title"] = "Argus",
            ["App.Init"] = "Initializing...",
            ["App.InitFail"] = "Initialization failed. Run as Administrator.",
            ["App.InitStart"] = "Starting initialization...",
            ["App.InitDone"] = "Initialization complete",
            ["App.Started"] = "Argus started",
            ["App.Shutdown"] = "Argus shutting down",

            ["Config.Section"] = "Configuration",
            ["Config.Ssid"] = "SSID",
            ["Config.Password"] = "Password",
            ["Config.Apply"] = "Apply",
            ["Config.Band"] = "Band",
            ["Config.BandAuto"] = "Auto",
            ["Config.Band2G"] = "2.4 GHz",
            ["Config.Band5G"] = "5 GHz only",
            ["Config.Hidden"] = "Hidden network",
            ["Config.PwTooShort"] = "Password must be at least 8 characters",
            ["Config.Applying"] = "Applying configuration...",
            ["Config.Applied"] = "Configuration applied",
            ["Config.ApplyFail"] = "Failed to apply config",

            ["Devices.Section"] = "Connected Devices",
            ["Devices.Hostname"] = "Hostname",
            ["Devices.Mac"] = "MAC Address",
            ["Devices.Ip"] = "IP Address",
            ["Devices.Limit"] = "Limit Kbps",
            ["Devices.Status"] = "Status",
            ["Devices.Action"] = "Action",
            ["Devices.Active"] = "ACTIVE",
            ["Devices.Blocked"] = "BLOCKED",
            ["Devices.Block"] = "Block",
            ["Devices.Unblock"] = "Unblock",
            ["Devices.Hint"] = "Select a device, enter Kbps limit (0 = unlimited), click Apply Filter",

            ["Btn.Start"] = "Start Hotspot",
            ["Btn.Stop"] = "Stop Hotspot",
            ["Btn.GuardOn"] = "Enable Guard",
            ["Btn.GuardOff"] = "Disable Guard",
            ["Btn.Filter"] = "Apply Filter",
            ["Btn.ShowLog"] = "Show Log",
            ["Btn.HideLog"] = "Hide Log",
            ["Btn.ClearLog"] = "Clear",
            ["Btn.Lang"] = "ZH",

            ["Status.Starting"] = "Starting hotspot...",
            ["Status.Started"] = "Hotspot active",
            ["Status.StartFail"] = "Failed to start hotspot",
            ["Status.Stopping"] = "Stopping hotspot...",
            ["Status.Stopped"] = "Hotspot stopped",
            ["Status.StopFail"] = "Failed to stop hotspot",
            ["Status.Active"] = "Hotspot active",
            ["Status.Offline"] = "Hotspot offline",
            ["Status.Waiting"] = "Hotspot active -- waiting for connections",
            ["Status.Connected"] = "Hotspot active -- {0} device(s) connected",
            ["Status.FilterOn"] = "Traffic filter active",
            ["Status.FilterOff"] = "Traffic filter stopped",
            ["Status.FilterFail"] = "Filter start failed (Admin + WinDivert required)",
            ["Status.Resetting"] = "Resetting system hotspot...",
            ["Status.ResetOk"] = "System hotspot reset, you can retry Start",
            ["Status.ResetFail"] = "Reset failed, see log",

            ["Btn.ResetSys"] = "Reset System Hotspot",
            ["Btn.ResetSysTip"] = "Clears Windows hotspot stuck state: stops hostednetwork, restarts ICS, re-initialises",

            ["Bar.SleepGuard"] = "Sleep guard",
            ["Bar.Filter"] = "Filter",
            ["Bar.ON"] = "ON",
            ["Bar.OFF"] = "OFF",
            ["Bar.RUNNING"] = "RUNNING",
            ["Bar.STOPPED"] = "STOPPED",

            ["Log.Title"] = "LOG",
            ["Log.File"] = "Logs saved to logs/hotspot_*.log",

            ["LogMsg.App.Started"] = "Argus started",
            ["LogMsg.App.InitStart"] = "Starting initialization...",
            ["LogMsg.App.InitDone"] = "Initialization complete",
            ["LogMsg.App.InitFail"] = "Initialization failed -- see Hotspot log for details",
            ["LogMsg.App.Shutdown"] = "Argus shutting down",
            ["LogMsg.UI.StartReq"] = "Start hotspot requested",
            ["LogMsg.UI.StopReq"] = "Stop hotspot requested",
            ["LogMsg.UI.ResetReq"] = "Reset system hotspot requested",
            ["LogMsg.Hot.SvcInit"] = "HotspotService initializing...",
            ["LogMsg.Hot.SvcInitOk"] = "HotspotService initialized, current state={0}",
            ["LogMsg.Hot.SvcInitFail"] = "HotspotService initialization failed",
            ["LogMsg.Hot.GetProfile"] = "Fetching network connection profile...",
            ["LogMsg.Hot.NoInternet"] = "No internet profile, scanning for one with network access...",
            ["LogMsg.Hot.ProfilesFound"] = "Found {0} network profiles",
            ["LogMsg.Hot.ProfileItem"] = "  - {0}: connectivity={1}",
            ["LogMsg.Hot.NoUsableProfile"] = "No profile with internet access -- cannot create hotspot",
            ["LogMsg.Hot.FoundNet"] = "Found network: {0}, level={1}",
            ["LogMsg.Hot.MgrCreated"] = "TetheringManager created, current state={0}",
            ["LogMsg.Hot.InitFail"] = "Initialization failed",
            ["LogMsg.Hot.NotInit"] = "Cannot {0}: service not initialized",
            ["LogMsg.Hot.StartReq"] = "Start hotspot requested",
            ["LogMsg.Hot.Starting"] = "Starting hotspot...",
            ["LogMsg.Hot.AlreadyOn"] = "Hotspot already running",
            ["LogMsg.Hot.NoMgrStart"] = "Start failed: TetheringManager not initialized",
            ["LogMsg.Hot.StartResult"] = "StartTetheringAsync returned: Status={0}, AdditionalErrorMessage={1}",
            ["LogMsg.Hot.StartOk"] = "Hotspot started successfully",
            ["LogMsg.Hot.StartBadState"] = "Hotspot state abnormal after start: current state={0}",
            ["LogMsg.Hot.StartEx"] = "Failed to start hotspot",
            ["LogMsg.Hot.Fallback"] = "Start failed (Status={0}), current band={1}, falling back to Auto(0) and retrying",
            ["LogMsg.Hot.FallbackOk"] = "Hotspot started after falling back to Auto band",
            ["LogMsg.Hot.FallbackFail"] = "Still failed after fallback (Status={0}) -- adapter may not support AP mode or is in use",
            ["LogMsg.Hot.FallbackCfgFail"] = "Failed to write fallback config",
            ["LogMsg.Hot.StopReq"] = "Stop hotspot requested",
            ["LogMsg.Hot.Stopping"] = "Stopping hotspot...",
            ["LogMsg.Hot.AlreadyOff"] = "Hotspot already off",
            ["LogMsg.Hot.NoMgrStop"] = "Stop failed: TetheringManager not initialized",
            ["LogMsg.Hot.StopResult"] = "StopTetheringAsync returned: Status={0}, AdditionalErrorMessage={1}",
            ["LogMsg.Hot.StopOk"] = "Hotspot stopped successfully",
            ["LogMsg.Hot.StopBadState"] = "Hotspot state abnormal after stop: current state={0}",
            ["LogMsg.Hot.StopEx"] = "Failed to stop hotspot",
            ["LogMsg.Hot.CfgApply"] = "Applying config: SSID={0}, Band={1}, Hidden={2}",
            ["LogMsg.Hot.CfgFailNotInit"] = "Cannot apply config: service not initialized",
            ["LogMsg.Hot.CfgNoMgr"] = "Config failed: TetheringManager not initialized",
            ["LogMsg.Hot.CfgConfiguring"] = "Configuring hotspot: SSID={0}, Band={1}, Hidden={2}",
            ["LogMsg.Hot.CfgBandSet"] = "Band set to: {0}",
            ["LogMsg.Hot.CfgBandUnavailable"] = "Band property unavailable, this SDK may not support band selection",
            ["LogMsg.Hot.CfgBandSetFail"] = "Band set failed (feature may be unsupported)",
            ["LogMsg.Hot.CfgApplied"] = "Hotspot config applied",
            ["LogMsg.Hot.CfgReadBack"] = "Config read back: SSID={0}, Band={1} (requested={2})",
            ["LogMsg.Hot.CfgBandMismatch"] = "Band adjusted by system: requested={0}, actual={1} (adapter likely doesn't support that band)",
            ["LogMsg.Hot.CfgReadBackFail"] = "Failed to read back config",
            ["LogMsg.Hot.CfgEx"] = "Failed to configure hotspot",
            ["LogMsg.Hot.ReadCfg"] = "Read system config: SSID=\"{0}\", Band={1}",
            ["LogMsg.Hot.ReadCfgFail"] = "Failed to read system hotspot config",
            ["LogMsg.Hot.GetClientsFail"] = "Failed to get connected clients",
            ["LogMsg.Hot.ResetStart"] = "=== Begin system hotspot reset ===",
            ["LogMsg.Hot.ResetDone"] = "=== Reset complete, init={0} ===",
            ["LogMsg.Hot.ResetFail"] = "Failed to reset system hotspot",
            ["LogMsg.Hot.CmdResult"] = "$ {0} {1} -> exit={2} {3}",
            ["LogMsg.Hot.CmdLaunchFail"] = "$ {0} {1} -> failed to launch",
            ["LogMsg.Hot.CmdEx"] = "$ {0} {1} failed",
            ["LogMsg.Hot.Disposed"] = "HotspotService disposed",
            ["LogMsg.Hot.NativeDisposed"] = "Native resources released",
        };

        private static readonly Dictionary<string, string> ZH = new Dictionary<string, string>
        {
            ["App.Title"] = "Argus",
            ["App.Init"] = "初始化中...",
            ["App.InitFail"] = "初始化失败，请以管理员身份运行",
            ["App.InitStart"] = "开始初始化...",
            ["App.InitDone"] = "初始化完成",
            ["App.Started"] = "Argus 已启动",
            ["App.Shutdown"] = "Argus 正在关闭",

            ["Config.Section"] = "配置",
            ["Config.Ssid"] = "网络名称",
            ["Config.Password"] = "密码",
            ["Config.Apply"] = "应用",
            ["Config.Band"] = "频段",
            ["Config.BandAuto"] = "自动",
            ["Config.Band2G"] = "2.4 GHz",
            ["Config.Band5G"] = "仅 5 GHz",
            ["Config.Hidden"] = "隐藏网络",
            ["Config.PwTooShort"] = "密码至少需要 8 个字符",
            ["Config.Applying"] = "正在应用配置...",
            ["Config.Applied"] = "配置已应用",
            ["Config.ApplyFail"] = "配置应用失败",

            ["Devices.Section"] = "已连接设备",
            ["Devices.Hostname"] = "主机名",
            ["Devices.Mac"] = "MAC 地址",
            ["Devices.Ip"] = "IP 地址",
            ["Devices.Limit"] = "限速(Kbps)",
            ["Devices.Status"] = "状态",
            ["Devices.Action"] = "操作",
            ["Devices.Active"] = "正常",
            ["Devices.Blocked"] = "已拉黑",
            ["Devices.Block"] = "拉黑",
            ["Devices.Unblock"] = "解除",
            ["Devices.Hint"] = "选择设备，输入限速值(Kbps, 0=不限速)，点击「应用过滤」",

            ["Btn.Start"] = "开启热点",
            ["Btn.Stop"] = "关闭热点",
            ["Btn.GuardOn"] = "开启守护",
            ["Btn.GuardOff"] = "关闭守护",
            ["Btn.Filter"] = "应用过滤",
            ["Btn.ShowLog"] = "显示日志",
            ["Btn.HideLog"] = "隐藏日志",
            ["Btn.ClearLog"] = "清空",
            ["Btn.Lang"] = "EN",

            ["Status.Starting"] = "正在开启热点...",
            ["Status.Started"] = "热点已开启",
            ["Status.StartFail"] = "热点开启失败",
            ["Status.Stopping"] = "正在关闭热点...",
            ["Status.Stopped"] = "热点已关闭",
            ["Status.StopFail"] = "热点关闭失败",
            ["Status.Active"] = "热点已开启",
            ["Status.Offline"] = "热点已关闭",
            ["Status.Waiting"] = "热点已开启 -- 等待设备连接",
            ["Status.Connected"] = "热点已开启 -- {0} 台设备已连接",
            ["Status.FilterOn"] = "流量过滤已启用",
            ["Status.FilterOff"] = "流量过滤已停止",
            ["Status.FilterFail"] = "过滤启动失败 (需管理员权限 + WinDivert)",
            ["Status.Resetting"] = "正在重置系统热点...",
            ["Status.ResetOk"] = "系统热点已重置, 可以重试开启",
            ["Status.ResetFail"] = "重置失败, 请查看日志",

            ["Btn.ResetSys"] = "重置系统热点",
            ["Btn.ResetSysTip"] = "清除 Windows 当前热点状态: 停止 hostednetwork, 重启 ICS 服务, 重新初始化",

            ["Bar.SleepGuard"] = "不休眠",
            ["Bar.Filter"] = "过滤",
            ["Bar.ON"] = "已开启",
            ["Bar.OFF"] = "未启动",
            ["Bar.RUNNING"] = "运行中",
            ["Bar.STOPPED"] = "已停止",

            ["Log.Title"] = "日志",
            ["Log.File"] = "日志保存到 logs/hotspot_*.log",

            ["LogMsg.App.Started"] = "Argus 已启动",
            ["LogMsg.App.InitStart"] = "开始初始化...",
            ["LogMsg.App.InitDone"] = "初始化完成",
            ["LogMsg.App.InitFail"] = "初始化失败 -- 请查看 Hotspot 日志",
            ["LogMsg.App.Shutdown"] = "Argus 正在关闭",
            ["LogMsg.UI.StartReq"] = "请求开启热点",
            ["LogMsg.UI.StopReq"] = "请求关闭热点",
            ["LogMsg.UI.ResetReq"] = "请求重置系统热点",
            ["LogMsg.Hot.SvcInit"] = "HotspotService 正在初始化...",
            ["LogMsg.Hot.SvcInitOk"] = "HotspotService 初始化成功, 当前状态={0}",
            ["LogMsg.Hot.SvcInitFail"] = "HotspotService 初始化失败",
            ["LogMsg.Hot.GetProfile"] = "正在获取网络连接配置...",
            ["LogMsg.Hot.NoInternet"] = "未找到互联网连接, 尝试查找有网络访问的配置...",
            ["LogMsg.Hot.ProfilesFound"] = "找到 {0} 个网络配置",
            ["LogMsg.Hot.ProfileItem"] = "  - {0}: 连接级别={1}",
            ["LogMsg.Hot.NoUsableProfile"] = "未找到任何有互联网访问的网络连接, 无法创建热点",
            ["LogMsg.Hot.FoundNet"] = "找到网络: {0}, 级别={1}",
            ["LogMsg.Hot.MgrCreated"] = "TetheringManager 创建成功, 当前状态={0}",
            ["LogMsg.Hot.InitFail"] = "初始化失败",
            ["LogMsg.Hot.NotInit"] = "无法{0}: 服务未初始化",
            ["LogMsg.Hot.StartReq"] = "请求开启热点",
            ["LogMsg.Hot.Starting"] = "正在开启热点...",
            ["LogMsg.Hot.AlreadyOn"] = "热点已在运行中",
            ["LogMsg.Hot.NoMgrStart"] = "启动失败: TetheringManager 未初始化",
            ["LogMsg.Hot.StartResult"] = "StartTetheringAsync 返回: Status={0}, AdditionalErrorMessage={1}",
            ["LogMsg.Hot.StartOk"] = "热点已成功开启",
            ["LogMsg.Hot.StartBadState"] = "热点开启后状态异常: 当前状态={0}",
            ["LogMsg.Hot.StartEx"] = "开启热点失败",
            ["LogMsg.Hot.Fallback"] = "开启失败 (Status={0}), 当前频段={1}, 自动回退到 Auto(0) 重试",
            ["LogMsg.Hot.FallbackOk"] = "回退到 Auto 频段后开启成功",
            ["LogMsg.Hot.FallbackFail"] = "回退后仍失败 (Status={0}) -- 适配器可能不支持 AP 模式或被占用",
            ["LogMsg.Hot.FallbackCfgFail"] = "回退配置写入失败",
            ["LogMsg.Hot.StopReq"] = "请求关闭热点",
            ["LogMsg.Hot.Stopping"] = "正在关闭热点...",
            ["LogMsg.Hot.AlreadyOff"] = "热点已处于关闭状态",
            ["LogMsg.Hot.NoMgrStop"] = "停止失败: TetheringManager 未初始化",
            ["LogMsg.Hot.StopResult"] = "StopTetheringAsync 返回: Status={0}, AdditionalErrorMessage={1}",
            ["LogMsg.Hot.StopOk"] = "热点已成功关闭",
            ["LogMsg.Hot.StopBadState"] = "热点关闭后状态异常: 当前状态={0}",
            ["LogMsg.Hot.StopEx"] = "关闭热点失败",
            ["LogMsg.Hot.CfgApply"] = "应用配置: SSID={0}, Band={1}, Hidden={2}",
            ["LogMsg.Hot.CfgFailNotInit"] = "无法应用配置: 服务未初始化",
            ["LogMsg.Hot.CfgNoMgr"] = "配置失败: TetheringManager 未初始化",
            ["LogMsg.Hot.CfgConfiguring"] = "正在配置热点: SSID={0}, Band={1}, Hidden={2}",
            ["LogMsg.Hot.CfgBandSet"] = "频段已设置为: {0}",
            ["LogMsg.Hot.CfgBandUnavailable"] = "Band 属性不可用, 该版本 SDK 可能不支持频段选择",
            ["LogMsg.Hot.CfgBandSetFail"] = "Band 设置失败 (该功能可能不被当前系统支持)",
            ["LogMsg.Hot.CfgApplied"] = "热点配置已应用",
            ["LogMsg.Hot.CfgReadBack"] = "读回配置: SSID={0}, Band={1} (请求={2})",
            ["LogMsg.Hot.CfgBandMismatch"] = "频段被系统调整: 请求={0}, 实际={1} (适配器可能不支持该频段)",
            ["LogMsg.Hot.CfgReadBackFail"] = "读回配置失败",
            ["LogMsg.Hot.CfgEx"] = "配置热点失败",
            ["LogMsg.Hot.ReadCfg"] = "读取系统配置: SSID=\"{0}\", Band={1}",
            ["LogMsg.Hot.ReadCfgFail"] = "读取系统热点配置失败",
            ["LogMsg.Hot.GetClientsFail"] = "获取连接客户端列表失败",
            ["LogMsg.Hot.ResetStart"] = "=== 开始重置系统热点 ===",
            ["LogMsg.Hot.ResetDone"] = "=== 重置完成, 初始化={0} ===",
            ["LogMsg.Hot.ResetFail"] = "重置系统热点失败",
            ["LogMsg.Hot.CmdResult"] = "$ {0} {1} -> exit={2} {3}",
            ["LogMsg.Hot.CmdLaunchFail"] = "$ {0} {1} -> 进程启动失败",
            ["LogMsg.Hot.CmdEx"] = "$ {0} {1} 失败",
            ["LogMsg.Hot.Disposed"] = "HotspotService 已释放",
            ["LogMsg.Hot.NativeDisposed"] = "Native 资源已释放",
        };

        public static string Current => _current;

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        public static string Get(string key)
        {
            var dict = _current == "ZH" ? ZH : EN;
            return dict.TryGetValue(key, out var v) ? v : key;
        }

        public static string Format(string key, params object[] args)
        {
            var fmt = Get(key);
            if (args == null || args.Length == 0) return fmt;
            try { return string.Format(fmt, args); } catch { return fmt; }
        }

        public static void Toggle()
        {
            _current = _current == "EN" ? "ZH" : "EN";
            SaveCurrent();
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs("Item[]"));
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs("LangButton"));
        }

        public static string LangButtonText => _current == "EN" ? "ZH" : "EN";
    }
}
