using System;
using System.Collections.Generic;
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
            if (result) IsRunning = true;
            return result;
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
