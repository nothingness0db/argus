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
            Logger.TrInfo("Hotspot", "LogMsg.Hot.SvcInit");
            IsInitialized = await _native.InitializeAsync();
            if (IsInitialized)
            {
                IsRunning = _native.CurrentState ==
                    Windows.Networking.NetworkOperators.TetheringOperationalState.On;
                Logger.TrInfo("Hotspot", "LogMsg.Hot.SvcInitOk", IsRunning ? "On" : "Off");
            }
            else
            {
                Logger.TrError("Hotspot", "LogMsg.Hot.SvcInitFail");
            }
            return IsInitialized;
        }

        public async Task<bool> StartAsync()
        {
            if (!IsInitialized)
            {
                Logger.TrWarn("Hotspot", "LogMsg.Hot.NotInit", "start");
                return false;
            }
            Logger.TrInfo("Hotspot", "LogMsg.Hot.StartReq");
            var result = await _native.StartTetheringAsync();
            if (result) { IsRunning = true; return true; }

            var currentCfg = _native.GetCurrentConfig();
            if (currentCfg.Band != 0)
            {
                Logger.TrWarn("Hotspot", "LogMsg.Hot.Fallback", _native.LastStartStatus, currentCfg.Band);
                var reconfigured = await _native.ConfigureAsync(currentCfg.Ssid, currentCfg.Passphrase, 0, currentCfg.IsHidden);
                if (reconfigured)
                {
                    var retry = await _native.StartTetheringAsync();
                    if (retry)
                    {
                        Logger.TrInfo("Hotspot", "LogMsg.Hot.FallbackOk");
                        _config.Band = 0;
                        IsRunning = true;
                        return true;
                    }
                    Logger.TrError("Hotspot", "LogMsg.Hot.FallbackFail", _native.LastStartStatus);
                }
                else Logger.TrError("Hotspot", "LogMsg.Hot.FallbackCfgFail");
            }
            return false;
        }

        public async Task<bool> StopAsync()
        {
            if (!IsInitialized)
            {
                Logger.TrWarn("Hotspot", "LogMsg.Hot.NotInit", "stop");
                return false;
            }
            Logger.TrInfo("Hotspot", "LogMsg.Hot.StopReq");
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
                Logger.TrWarn("Hotspot", "LogMsg.Hot.CfgFailNotInit");
                return false;
            }
            var wasRunning = IsRunning;

            Logger.TrInfo("Hotspot", "LogMsg.Hot.CfgApply", config.Ssid, config.Band, config.IsHidden);
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
            Logger.TrInfo("Hotspot", "LogMsg.Hot.ResetStart");
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
                Logger.TrInfo("Hotspot", "LogMsg.Hot.ResetDone", IsInitialized ? "OK" : "FAIL");
                return IsInitialized;
            }
            catch (Exception ex)
            {
                Logger.TrError("Hotspot", "LogMsg.Hot.ResetFail", ex);
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
                if (p == null) { Logger.TrWarn("Hotspot", "LogMsg.Hot.CmdLaunchFail", file, args); return; }
                p.WaitForExit(8000);
                var output = (p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd()).Trim();
                Logger.TrInfo("Hotspot", "LogMsg.Hot.CmdResult", file, args, p.ExitCode, output.Length > 0 ? "| " + output : "");
            }
            catch (Exception ex) { Logger.TrWarn("Hotspot", "LogMsg.Hot.CmdEx", ex, file, args); }
        }

        public IReadOnlyList<ConnectedDevice> GetConnectedClients()
        {
            return _native.GetConnectedClients();
        }

        public void Dispose()
        {
            Logger.TrInfo("Hotspot", "LogMsg.Hot.Disposed");
            _native.Dispose();
        }
    }
}
