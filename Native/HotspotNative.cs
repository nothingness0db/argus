using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;
using HotspotManager.Services;

namespace HotspotManager.Native
{
    internal class HotspotNative : IDisposable
    {
        private NetworkOperatorTetheringManager _manager;
        private ConnectionProfile _profile;

        public bool IsAvailable => _manager != null;

        public TetheringOperationalState CurrentState =>
            _manager?.TetheringOperationalState ?? TetheringOperationalState.Off;

        public TetheringOperationStatus LastStartStatus { get; private set; }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                Logger.Info("Hotspot", "正在获取网络连接配置...");

                _profile = NetworkInformation.GetInternetConnectionProfile();
                if (_profile == null)
                {
                    Logger.Info("Hotspot", "未找到互联网连接，尝试查找有网络访问的配置...");
                    var allProfiles = NetworkInformation.GetConnectionProfiles();
                    Logger.Info("Hotspot", $"找到 {allProfiles.Count} 个网络配置");

                    foreach (var p in allProfiles)
                    {
                        var level = p.GetNetworkConnectivityLevel();
                        var name = p.ProfileName;
                        Logger.Info("Hotspot", $"  - {name}: 连接级别={level}");
                    }

                    _profile = allProfiles
                        .FirstOrDefault(p => p.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
                }

                if (_profile == null)
                {
                    Logger.Error("Hotspot", "未找到任何有互联网访问的网络连接，无法创建热点");
                    return false;
                }

                Logger.Info("Hotspot", $"找到网络: {_profile.ProfileName}, 级别={_profile.GetNetworkConnectivityLevel()}");

                _manager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(_profile);
                Logger.Info("Hotspot", $"TetheringManager 创建成功, 当前状态={_manager.TetheringOperationalState}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Hotspot", "初始化失败", ex);
                return false;
            }
        }

        public async Task<bool> StartTetheringAsync()
        {
            if (_manager == null)
            {
                Logger.Warn("Hotspot", "启动失败: TetheringManager 未初始化");
                return false;
            }
            try
            {
                if (_manager.TetheringOperationalState == TetheringOperationalState.On)
                {
                    Logger.Info("Hotspot", "热点已在运行中");
                    return true;
                }

                Logger.Info("Hotspot", "正在开启热点...");
                var opResult = await _manager.StartTetheringAsync();
                LastStartStatus = opResult.Status;
                Logger.Info("Hotspot", $"StartTetheringAsync 返回: Status={opResult.Status}, AdditionalErrorMessage={opResult.AdditionalErrorMessage ?? "(none)"}");
                var success = _manager.TetheringOperationalState == TetheringOperationalState.On;
                Logger.Info("Hotspot", success ? "热点已成功开启" : $"热点开启后状态异常: 当前状态={_manager.TetheringOperationalState}");
                return success;
            }
            catch (Exception ex)
            {
                Logger.Error("Hotspot", "开启热点失败", ex);
                return false;
            }
        }

        public async Task<bool> StopTetheringAsync()
        {
            if (_manager == null)
            {
                Logger.Warn("Hotspot", "停止失败: TetheringManager 未初始化");
                return false;
            }
            try
            {
                if (_manager.TetheringOperationalState == TetheringOperationalState.Off)
                {
                    Logger.Info("Hotspot", "热点已处于关闭状态");
                    return true;
                }

                Logger.Info("Hotspot", "正在关闭热点...");
                var opResult = await _manager.StopTetheringAsync();
                Logger.Info("Hotspot", $"StopTetheringAsync 返回: Status={opResult.Status}, AdditionalErrorMessage={opResult.AdditionalErrorMessage ?? "(none)"}");
                var success = _manager.TetheringOperationalState == TetheringOperationalState.Off;
                Logger.Info("Hotspot", success ? "热点已成功关闭" : $"热点关闭后状态异常: 当前状态={_manager.TetheringOperationalState}");
                return success;
            }
            catch (Exception ex)
            {
                Logger.Error("Hotspot", "关闭热点失败", ex);
                return false;
            }
        }

        public async Task<bool> ConfigureAsync(string ssid, string passphrase, int band, bool isHidden)
        {
            if (_manager == null)
            {
                Logger.Warn("Hotspot", "配置失败: TetheringManager 未初始化");
                return false;
            }
            try
            {
                Logger.Info("Hotspot", $"正在配置热点: SSID={ssid}, Band={band}, Hidden={isHidden}");
                var config = _manager.GetCurrentAccessPointConfiguration();
                config.Ssid = ssid;
                config.Passphrase = passphrase;

                try
                {
                    var bandProp = config.GetType().GetProperty("Band");
                    if (bandProp != null)
                    {
                        bandProp.SetValue(config, band);
                        Logger.Info("Hotspot", $"频段已设置为: {band}");
                    }
                    else
                    {
                        Logger.Warn("Hotspot", "Band 属性不可用, 该版本 SDK 可能不支持频段选择");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("Hotspot", $"Band 设置失败 (该功能可能不被当前系统支持)", ex);
                }

                await _manager.ConfigureAccessPointAsync(config);
                Logger.Info("Hotspot", "热点配置已应用");

                try
                {
                    var verify = _manager.GetCurrentAccessPointConfiguration();
                    var verifyBandProp = verify.GetType().GetProperty("Band");
                    var actualBand = verifyBandProp != null ? verifyBandProp.GetValue(verify) : null;
                    Logger.Info("Hotspot", $"读回配置: SSID={verify.Ssid}, Band={actualBand ?? "N/A"} (请求={band})");
                    if (actualBand != null && (int)actualBand != band)
                        Logger.Warn("Hotspot", $"频段被系统调整: 请求={band}, 实际={actualBand} (适配器可能不支持该频段)");
                }
                catch (Exception ex) { Logger.Warn("Hotspot", "读回配置失败", ex); }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Hotspot", "配置热点失败", ex);
                return false;
            }
        }

        public Models.HotspotConfig GetCurrentConfig()
        {
            var cfg = new Models.HotspotConfig();
            if (_manager == null) return cfg;

            try
            {
                var winCfg = _manager.GetCurrentAccessPointConfiguration();
                cfg.Ssid = winCfg.Ssid ?? "";
                cfg.Passphrase = winCfg.Passphrase ?? "";

                try
                {
                    var bandProp = winCfg.GetType().GetProperty("Band");
                    if (bandProp != null)
                        cfg.Band = (int)bandProp.GetValue(winCfg)!;
                }
                catch { }

                Logger.Info("Hotspot", $"Read system config: SSID=\"{cfg.Ssid}\", Band={cfg.Band}");
            }
            catch (Exception ex)
            {
                Logger.Warn("Hotspot", "Failed to read system hotspot config", ex);
            }

            return cfg;
        }

        public IReadOnlyList<Models.ConnectedDevice> GetConnectedClients()
        {
            var result = new List<Models.ConnectedDevice>();
            if (_manager == null) return result;

            try
            {
                var clients = _manager.GetTetheringClients();
                if (clients == null) return result;

                foreach (var client in clients)
                {
                    result.Add(new Models.ConnectedDevice
                    {
                        MacAddress = FormatMac(client.MacAddress),
                        HostName = client.HostNames?.FirstOrDefault()?.CanonicalName ?? "",
                        ConnectedTime = DateTime.Now,
                        IsActive = true
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Hotspot", $"获取连接客户端列表失败", ex);
            }

            return result;
        }

        private static string FormatMac(string mac)
        {
            if (string.IsNullOrEmpty(mac)) return "";
            mac = mac.Replace(":", "").Replace("-", "").ToUpper();
            if (mac.Length == 12)
            {
                return string.Join(":", Enumerable.Range(0, 6)
                    .Select(i => mac.Substring(i * 2, 2)));
            }
            return mac;
        }

        public void Dispose()
        {
            Logger.Info("Hotspot", "Native 资源已释放");
            _manager = null;
            _profile = null;
        }
    }
}
