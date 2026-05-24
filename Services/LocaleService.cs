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

            ["Bar.SleepGuard"] = "Sleep guard",
            ["Bar.Filter"] = "Filter",
            ["Bar.ON"] = "ON",
            ["Bar.OFF"] = "OFF",
            ["Bar.RUNNING"] = "RUNNING",
            ["Bar.STOPPED"] = "STOPPED",

            ["Log.Title"] = "LOG",
            ["Log.File"] = "Logs saved to logs/hotspot_*.log",
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

            ["Bar.SleepGuard"] = "不休眠",
            ["Bar.Filter"] = "过滤",
            ["Bar.ON"] = "已开启",
            ["Bar.OFF"] = "未启动",
            ["Bar.RUNNING"] = "运行中",
            ["Bar.STOPPED"] = "已停止",

            ["Log.Title"] = "日志",
            ["Log.File"] = "日志保存到 logs/hotspot_*.log",
        };

        public static string Current => _current;

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        public static string Get(string key)
        {
            var dict = _current == "ZH" ? ZH : EN;
            return dict.TryGetValue(key, out var v) ? v : key;
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
