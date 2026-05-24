using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using HotspotManager.Models;

namespace HotspotManager.Services
{
    public class HotspotService : IDisposable
    {
        private readonly Native.HotspotNative _native = new Native.HotspotNative();
        private HotspotConfig _config = new HotspotConfig();
        private bool _isRunning;

        public HotspotConfig Config => _config;

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsInitialized { get; private set; }

        public event EventHandler StatusChanged;

        public HotspotConfig ReadSystemConfig()
        {
            return _native.GetCurrentConfig();
        }

        public async Task<bool> InitializeAsync()
        {
            Logger.Info("Hotspot", "HotspotService 正在初始化...");
            IsInitialized = await _native.InitializeAsync();
            if (IsInitialized)
            {
                IsRunning = _native.CurrentState ==
                    Windows.Networking.NetworkOperators.TetheringOperationalState.On;
                Logger.Info("Hotspot", $"HotspotService 初始化成功, 当前状态={(IsRunning ? "运行中" : "已关闭")}");
            }
            else
            {
                Logger.Error("Hotspot", "HotspotService 初始化失败");
            }
            return IsInitialized;
        }

        public async Task<bool> StartAsync()
        {
            if (!IsInitialized)
            {
                Logger.Warn("Hotspot", "无法开启: 服务未初始化");
                return false;
            }
            Logger.Info("Hotspot", "请求开启热点");
            var result = await _native.StartTetheringAsync();
            if (result) { IsRunning = true; return true; }

            var currentCfg = _native.GetCurrentConfig();
            if (currentCfg.Band != 0)
            {
                Logger.Warn("Hotspot", $"开启失败 (Status={_native.LastStartStatus}), 当前频段={currentCfg.Band}, 自动回退到 Auto(0) 重试");
                var reconfigured = await _native.ConfigureAsync(currentCfg.Ssid, currentCfg.Passphrase, 0, currentCfg.IsHidden);
                if (reconfigured)
                {
                    var retry = await _native.StartTetheringAsync();
                    if (retry)
                    {
                        Logger.Info("Hotspot", "回退到 Auto 频段后开启成功");
                        _config.Band = 0;
                        IsRunning = true;
                        return true;
                    }
                    Logger.Error("Hotspot", $"回退后仍失败 (Status={_native.LastStartStatus}) — 适配器可能不支持 AP 模式或被占用");
                }
                else Logger.Error("Hotspot", "回退配置写入失败");
            }
            return false;
        }

        public async Task<bool> StopAsync()
        {
            if (!IsInitialized)
            {
                Logger.Warn("Hotspot", "无法关闭: 服务未初始化");
                return false;
            }
            Logger.Info("Hotspot", "请求关闭热点");
            var result = await _native.StopTetheringAsync();
            if (result) IsRunning = false;
            return result;
        }

        public async Task<bool> ToggleAsync()
        {
            return IsRunning ? await StopAsync() : await StartAsync();
        }

        public async Task<bool> ApplyConfigAsync(HotspotConfig config)
        {
            if (!IsInitialized)
            {
                Logger.Warn("Hotspot", "无法应用配置: 服务未初始化");
                return false;
            }
            var wasRunning = IsRunning;

            Logger.Info("Hotspot", $"应用配置: SSID={config.Ssid}, Band={config.Band}, Hidden={config.IsHidden}");
            if (wasRunning) await StopAsync();

            var result = await _native.ConfigureAsync(
                config.Ssid,
                config.Passphrase,
                config.Band,
                config.IsHidden);

            if (wasRunning) await StartAsync();

            if (result) _config = config;
            return result;
        }

        public async Task<bool> ResetSystemHotspotAsync()
        {
            Logger.Info("Hotspot", "=== 开始重置系统热点 ===");
            try
            {
                if (IsRunning) await StopAsync();

                await Task.Run(() =>
                {
                    RunCmd("netsh", "wlan stop hostednetwork");
                    RunCmd("net", "stop icssvc");
                    RunCmd("net", "stop SharedAccess");
                    System.Threading.Thread.Sleep(1500);
                    RunCmd("net", "start SharedAccess");
                    RunCmd("net", "start icssvc");
                    System.Threading.Thread.Sleep(500);
                });

                _native.Dispose();
                IsInitialized = await _native.InitializeAsync();
                if (IsInitialized)
                {
                    IsRunning = _native.CurrentState ==
                        Windows.Networking.NetworkOperators.TetheringOperationalState.On;
                }
                Logger.Info("Hotspot", $"=== 重置完成, 初始化={(IsInitialized ? "成功" : "失败")} ===");
                return IsInitialized;
            }
            catch (Exception ex)
            {
                Logger.Error("Hotspot", "重置系统热点失败", ex);
                return false;
            }
        }

        private static void RunCmd(string file, string args)
        {
            var psi = new ProcessStartInfo(file, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            try
            {
                using var p = Process.Start(psi);
                if (p == null) { Logger.Warn("Hotspot", $"$ {file} {args} → 进程启动失败"); return; }
                p.WaitForExit(8000);
                var output = (p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd()).Trim();
                Logger.Info("Hotspot", $"$ {file} {args} → exit={p.ExitCode} {(output.Length > 0 ? "| " + output : "")}");
            }
            catch (Exception ex) { Logger.Warn("Hotspot", $"$ {file} {args} 失败", ex); }
        }

        public IReadOnlyList<ConnectedDevice> GetConnectedClients()
        {
            return _native.GetConnectedClients();
        }

        public void Dispose()
        {
            Logger.Info("Hotspot", "HotspotService 已释放");
            _native.Dispose();
        }
    }
}
