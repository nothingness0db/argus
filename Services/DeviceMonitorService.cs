using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using HotspotManager.Models;

namespace HotspotManager.Services
{
    public class DeviceMonitorService : IDisposable
    {
        private readonly HotspotService _hotspotService;
        private Timer _pollTimer;
        private readonly Dictionary<string, ConnectedDevice> _devices = new Dictionary<string, ConnectedDevice>();
        private int _pollingRound;
        private string _hotspotSubnet = "";

        public ObservableCollection<ConnectedDevice> Devices { get; } = new ObservableCollection<ConnectedDevice>();

        public event EventHandler<ConnectedDevice> DeviceAdded;
        public event EventHandler<ConnectedDevice> DeviceRemoved;

        public DeviceMonitorService(HotspotService hotspotService)
        {
            _hotspotService = hotspotService;
        }

        public void StartMonitoring(int intervalMs = 3000)
        {
            Logger.Info("Monitor", $"开始设备监控, 间隔={intervalMs}ms");
            DetectHotspotSubnet();
            _pollTimer = new Timer(PollDevices, null, 500, intervalMs);
        }

        public void StopMonitoring()
        {
            Logger.Info("Monitor", "停止设备监控");
            _pollTimer?.Dispose();
            _pollTimer = null;
        }

        private void DetectHotspotSubnet()
        {
            try
            {
                var adapters = NetworkInterface.GetAllNetworkInterfaces();
                Logger.Info("Monitor", $"检测到 {adapters.Length} 个网络适配器");

                foreach (var adapter in adapters)
                {
                    var name = adapter.Name;
                    var desc = adapter.Description;

                    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                        adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        var ipProps = adapter.GetIPProperties();
                        var ipv4 = ipProps.UnicastAddresses
                            .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

                        if (ipv4 != null && ipv4.Address != null)
                        {
                            var ip = ipv4.Address.ToString();
                            var mask = ipv4.IPv4Mask?.ToString() ?? "255.255.255.0";
                            Logger.Info("Monitor", $"  适配器: {name}, IP={ip}/{mask}, 类型={adapter.NetworkInterfaceType}");
                        }
                    }
                }

                var hotspotIps = NetworkInterface.GetAllNetworkInterfaces()
                    .SelectMany(a => a.GetIPProperties().UnicastAddresses)
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.Address.ToString())
                    .ToList();

                foreach (var ip in hotspotIps)
                {
                    if (ip.StartsWith("192.168."))
                    {
                        var parts = ip.Split('.');
                        _hotspotSubnet = $"{parts[0]}.{parts[1]}.{parts[2]}";
                        Logger.Info("Monitor", $"热点子网推测: {_hotspotSubnet}.0/24 (来自 {ip})");
                        break;
                    }
                }

                if (string.IsNullOrEmpty(_hotspotSubnet))
                {
                    _hotspotSubnet = "192.168.137";
                    Logger.Info("Monitor", $"使用默认热点子网: {_hotspotSubnet}.0/24");
                }
            }
            catch (Exception ex)
            {
                _hotspotSubnet = "192.168.137";
                Logger.Warn("Monitor", "子网检测失败，使用默认值", ex);
            }
        }

        private void PollDevices(object state)
        {
            if (!_hotspotService.IsRunning) return;
            _pollingRound++;

            try
            {
                var arpTable = ReadArpTable();
                var clients = _hotspotService.GetConnectedClients();
                var currentMacs = new HashSet<string>();

                foreach (var client in clients)
                {
                    currentMacs.Add(client.MacAddress);

                    if (_devices.TryGetValue(client.MacAddress, out var existing))
                    {
                        existing.IsActive = true;
                        var ip = ResolveIpFromArpTable(existing.MacAddress, arpTable);
                        if (!string.IsNullOrEmpty(ip))
                            existing.IpAddress = ip;
                    }
                    else
                    {
                        var ip = ResolveIpFromArpTable(client.MacAddress, arpTable);
                        client.IpAddress = ip;
                        _devices[client.MacAddress] = client;

                        var app = System.Windows.Application.Current;
                        if (app != null)
                        {
                            app.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                Devices.Add(client);
                            }));
                        }
                        Logger.Info("Monitor", $"新设备: {client.DisplayName} IP={client.IpAddress}");
                        DeviceAdded?.Invoke(this, client);
                    }
                }

                var removedMacs = _devices.Keys.Except(currentMacs).ToList();
                foreach (var mac in removedMacs)
                {
                    if (_devices.TryGetValue(mac, out var device))
                    {
                        device.IsActive = false;
                        Logger.Info("Monitor", $"设备断开: {device.DisplayName}");
                        var app = System.Windows.Application.Current;
                        if (app != null)
                        {
                            app.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                Devices.Remove(device);
                            }));
                        }
                        _devices.Remove(mac);
                        DeviceRemoved?.Invoke(this, device);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_pollingRound <= 3)
                    Logger.Warn("Monitor", $"设备轮询异常 (第{_pollingRound}次)", ex);
            }
        }

        private static string ResolveIpFromArpTable(string mac, Dictionary<string, string> arpTable)
        {
            if (string.IsNullOrEmpty(mac)) return "";

            var normalizedMac = mac.Replace(":", "").Replace("-", "").Replace(" ", "").ToUpper();

            if (arpTable.TryGetValue(normalizedMac, out var ip))
                return ip;

            var partial = normalizedMac.Length >= 8 ? normalizedMac.Substring(0, 8) : normalizedMac;
            foreach (var kv in arpTable)
            {
                if (kv.Key.StartsWith(partial))
                    return kv.Value;
            }

            return "";
        }

        private Dictionary<string, string> ReadArpTable()
        {
            var result = new Dictionary<string, string>();

            try
            {
                var size = 0;
                ArpHelper.GetIpNetTable(IntPtr.Zero, ref size, false);
                if (size <= 0) return result;

                var ptr = Marshal.AllocHGlobal(size);
                try
                {
                    if (ArpHelper.GetIpNetTable(ptr, ref size, false) != 0)
                        return result;

                    var entryCount = Marshal.ReadInt32(ptr);
                    var entryPtr = IntPtr.Add(ptr, 4);
                    var entrySize = Marshal.SizeOf<ArpHelper.MIB_IPNETROW>();

                    for (int i = 0; i < entryCount; i++)
                    {
                        var entry = Marshal.PtrToStructure<ArpHelper.MIB_IPNETROW>(
                            IntPtr.Add(entryPtr, i * entrySize));

                        if (entry.dwType != 3 && entry.dwType != 4) continue;

                        var ip = new IPAddress(BitConverter.GetBytes(entry.dwAddr)).ToString();
                        var mac = BitConverter.ToString(entry.bPhysAddr, 0, (int)entry.dwPhysAddrLen)
                            .Replace("-", "").ToUpper();

                        if (mac.Length >= 12 && !mac.All(c => c == '0'))
                        {
                            result[mac] = ip;
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            catch { }

            return result;
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    }

    internal static class ArpHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_IPNETROW
        {
            public uint dwIndex;
            public uint dwPhysAddrLen;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] bPhysAddr;
            public uint dwAddr;
            public uint dwType;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int GetIpNetTable(IntPtr pIpNetTable, ref int pdwSize, bool bOrder);
    }
}
