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
                Logger.TrInfo("Hotspot", "LogMsg.Hot.GetProfile");

                _profile = NetworkInformation.GetInternetConnectionProfile();
                if (_profile == null)
                {
                    Logger.TrInfo("Hotspot", "LogMsg.Hot.NoInternet");
                    var allProfiles = NetworkInformation.GetConnectionProfiles();
                    Logger.TrInfo("Hotspot", "LogMsg.Hot.ProfilesFound", allProfiles.Count);

                    foreach (var p in allProfiles)
                    {
                        var level = p.GetNetworkConnectivityLevel();
                        var name = p.ProfileName;
                        Logger.TrInfo("Hotspot", "LogMsg.Hot.ProfileItem", name, level);
                    }

                    _profile = allProfiles
                        .FirstOrDefault(p => p.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
                }

                if (_profile == null)
                {
                    Logger.TrError("Hotspot", "LogMsg.Hot.NoUsableProfile");
                    return false;
                }

                Logger.TrInfo("Hotspot", "LogMsg.Hot.FoundNet", _profile.ProfileName, _profile.GetNetworkConnectivityLevel());

                _manager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(_profile);
                Logger.TrInfo("Hotspot", "LogMsg.Hot.MgrCreated", _manager.TetheringOperationalState);
                return true;
            }
            catch (Exception ex)
            {
                Logger.TrError("Hotspot", "LogMsg.Hot.InitFail", ex);
                return false;
            }
        }

        public async Task<bool> StartTetheringAsync()
        {
            if (_manager == null)
            {
                Logger.TrWarn("Hotspot", "LogMsg.Hot.NoMgrStart");
                return false;
            }
            try
            {
                if (_manager.TetheringOperationalState == TetheringOperationalState.On)
                {
                    Logger.TrInfo("Hotspot", "LogMsg.Hot.AlreadyOn");
                    return true;
                }

                Logger.TrInfo("Hotspot", "LogMsg.Hot.Starting");
                var opResult = await _manager.StartTetheringAsync();
                LastStartStatus = opResult.Status;
                Logger.TrInfo("Hotspot", "LogMsg.Hot.StartResult", opResult.Status, opResult.AdditionalErrorMessage ?? "(none)");
                var success = _manager.TetheringOperationalState == TetheringOperationalState.On;
                if (success) Logger.TrInfo("Hotspot", "LogMsg.Hot.StartOk");
                else Logger.TrInfo("Hotspot", "LogMsg.Hot.StartBadState", _manager.TetheringOperationalState);
                return success;
            }
            catch (Exception ex)
            {
                Logger.TrError("Hotspot", "LogMsg.Hot.StartEx", ex);
                return false;
            }
        }

        public async Task<bool> StopTetheringAsync()
        {
            if (_manager == null)
            {
                Logger.TrWarn("Hotspot", "LogMsg.Hot.NoMgrStop");
                return false;
            }
            try
            {
                if (_manager.TetheringOperationalState == TetheringOperationalState.Off)
                {
                    Logger.TrInfo("Hotspot", "LogMsg.Hot.AlreadyOff");
                    return true;
                }

                Logger.TrInfo("Hotspot", "LogMsg.Hot.Stopping");
                var opResult = await _manager.StopTetheringAsync();
                Logger.TrInfo("Hotspot", "LogMsg.Hot.StopResult", opResult.Status, opResult.AdditionalErrorMessage ?? "(none)");
                var success = _manager.TetheringOperationalState == TetheringOperationalState.Off;
                if (success) Logger.TrInfo("Hotspot", "LogMsg.Hot.StopOk");
                else Logger.TrInfo("Hotspot", "LogMsg.Hot.StopBadState", _manager.TetheringOperationalState);
                return success;
            }
            catch (Exception ex)
            {
                Logger.TrError("Hotspot", "LogMsg.Hot.StopEx", ex);
                return false;
            }
        }

        public async Task<bool> ConfigureAsync(string ssid, string passphrase, int band, bool isHidden)
        {
            if (_manager == null)
            {
                Logger.TrWarn("Hotspot", "LogMsg.Hot.CfgNoMgr");
                return false;
            }
            try
            {
                Logger.TrInfo("Hotspot", "LogMsg.Hot.CfgConfiguring", ssid, band, isHidden);
                var config = _manager.GetCurrentAccessPointConfiguration();
                config.Ssid = ssid;
                config.Passphrase = passphrase;

                try
                {
                    var bandProp = config.GetType().GetProperty("Band");
                    if (bandProp != null)
                    {
                        bandProp.SetValue(config, band);
                        Logger.TrInfo("Hotspot", "LogMsg.Hot.CfgBandSet", band);
                    }
                    else
                    {
                        Logger.TrWarn("Hotspot", "LogMsg.Hot.CfgBandUnavailable");
                    }
                }
                catch (Exception ex)
                {
                    Logger.TrWarn("Hotspot", "LogMsg.Hot.CfgBandSetFail", ex);
                }

                await _manager.ConfigureAccessPointAsync(config);
                Logger.TrInfo("Hotspot", "LogMsg.Hot.CfgApplied");

                try
                {
                    var verify = _manager.GetCurrentAccessPointConfiguration();
                    var verifyBandProp = verify.GetType().GetProperty("Band");
                    var actualBand = verifyBandProp != null ? verifyBandProp.GetValue(verify) : null;
                    Logger.TrInfo("Hotspot", "LogMsg.Hot.CfgReadBack", verify.Ssid, actualBand ?? "N/A", band);
                    if (actualBand != null && (int)actualBand != band)
                        Logger.TrWarn("Hotspot", "LogMsg.Hot.CfgBandMismatch", band, actualBand);
                }
                catch (Exception ex) { Logger.TrWarn("Hotspot", "LogMsg.Hot.CfgReadBackFail", ex); }

                return true;
            }
            catch (Exception ex)
            {
                Logger.TrError("Hotspot", "LogMsg.Hot.CfgEx", ex);
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

                Logger.TrInfo("Hotspot", "LogMsg.Hot.ReadCfg", cfg.Ssid, cfg.Band);
            }
            catch (Exception ex)
            {
                Logger.TrWarn("Hotspot", "LogMsg.Hot.ReadCfgFail", ex);
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
                Logger.TrWarn("Hotspot", "LogMsg.Hot.GetClientsFail", ex);
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
            Logger.TrInfo("Hotspot", "LogMsg.Hot.NativeDisposed");
            _manager = null;
            _profile = null;
        }
    }
}
