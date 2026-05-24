using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HotspotManager.Services
{
    public class TrafficFilterService : IDisposable
    {
        private IntPtr _handle = IntPtr.Zero;
        private CancellationTokenSource _cts;
        private readonly Dictionary<string, TokenBucket> _buckets = new Dictionary<string, TokenBucket>();
        private readonly HashSet<string> _blacklist = new HashSet<string>();
        private readonly object _lock = new object();
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        public void SetBlacklist(IEnumerable<string> ips, bool blocked)
        {
            lock (_lock)
            {
                foreach (var ip in ips)
                {
                    if (blocked)
                    {
                        _blacklist.Add(ip);
                        Logger.Info("Filter", $"加入黑名单: {ip}");
                    }
                    else
                    {
                        _blacklist.Remove(ip);
                        Logger.Info("Filter", $"移出黑名单: {ip}");
                    }
                }
            }
        }

        public bool IsBlacklisted(string ip)
        {
            lock (_lock) return _blacklist.Contains(ip);
        }

        public void SetRateLimit(string ip, double kbps)
        {
            lock (_lock)
            {
                if (kbps <= 0)
                {
                    _buckets.Remove(ip);
                    Logger.Info("Filter", $"取消限速: {ip}");
                }
                else
                {
                    if (!_buckets.TryGetValue(ip, out var bucket))
                    {
                        bucket = new TokenBucket();
                        _buckets[ip] = bucket;
                    }
                    bucket.SetRate(kbps * 1024 / 8);
                    Logger.Info("Filter", $"设置限速: {ip} = {kbps} Kbps");
                }
            }
        }

        public bool Start()
        {
            if (_isRunning) return true;

            try
            {
                Logger.Info("Filter", "正在启动流量过滤器...");

                var filter = "outbound and ip";
                _handle = Native.Win32Native.WinDivertOpen(
                    filter,
                    Native.Win32Native.WINDIVERT_LAYER_NETWORK,
                    0,
                    Native.Win32Native.WINDIVERT_FLAG_SNIFF);

                if (_handle == IntPtr.Zero || _handle == new IntPtr(-1))
                {
                    Logger.Error("Filter", "WinDivert 打开失败: 需要管理员权限且 Assets 目录下有 WinDivert.dll 和 WinDivert64.sys");
                    return false;
                }

                Native.Win32Native.WinDivertSetParam(
                    _handle,
                    Native.Win32Native.WINDIVERT_PARAM_QUEUE_LENGTH,
                    8192);
                Native.Win32Native.WinDivertSetParam(
                    _handle,
                    Native.Win32Native.WINDIVERT_PARAM_QUEUE_TIME,
                    2048);

                _cts = new CancellationTokenSource();
                _isRunning = true;
                Logger.Info("Filter", "流量过滤器已启动");
                Task.Run(() => ProcessLoop(_cts.Token));
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Filter", "启动流量过滤器失败", ex);
                return false;
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            Logger.Info("Filter", "正在停止流量过滤器...");
            _isRunning = false;
            _cts?.Cancel();

            if (_handle != IntPtr.Zero && _handle != new IntPtr(-1))
            {
                Native.Win32Native.WinDivertClose(_handle);
                _handle = IntPtr.Zero;
            }
            Logger.Info("Filter", "流量过滤器已停止");
        }

        private void ProcessLoop(CancellationToken token)
        {
            var packet = new byte[65535];
            var addr = new Native.Win32Native.WinDivertAddress();
            uint recvLen = 0;

            while (!token.IsCancellationRequested)
            {
                if (!Native.Win32Native.WinDivertRecv(_handle, packet, (uint)packet.Length, ref addr, ref recvLen))
                    break;

                if (recvLen < 20)
                {
                    if (!Native.Win32Native.WinDivertSend(_handle, packet, recvLen, ref addr, ref recvLen))
                        break;
                    continue;
                }

                var ipHeader = BytesToStruct<Native.Win32Native.IPv4Header>(packet);
                var srcIp = Native.Win32Native.Uint32ToIp(ipHeader.SrcAddr);
                var dstIp = Native.Win32Native.Uint32ToIp(ipHeader.DstAddr);
                var totalLen = (uint)((ipHeader.TotalLength >> 8) | ((ipHeader.TotalLength & 0xFF) << 8));

                bool shouldDrop = false;

                lock (_lock)
                {
                    if (_blacklist.Contains(srcIp) || _blacklist.Contains(dstIp))
                    {
                        shouldDrop = true;
                    }

                    if (!shouldDrop && _buckets.TryGetValue(srcIp, out var bucket))
                    {
                        if (!bucket.Consume(totalLen))
                            shouldDrop = true;
                    }
                }

                if (shouldDrop) continue;

                if (!Native.Win32Native.WinDivertSend(_handle, packet, recvLen, ref addr, ref recvLen))
                    break;
            }
        }

        private static T BytesToStruct<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        public void Dispose()
        {
            Logger.Info("Filter", "TrafficFilter 已释放");
            Stop();
        }

        private class TokenBucket
        {
            private double _tokens;
            private double _maxTokens;
            private double _rate;
            private long _lastRefill;
            private readonly object _lock = new object();

            public void SetRate(double bytesPerSecond)
            {
                lock (_lock)
                {
                    _rate = bytesPerSecond;
                    _maxTokens = bytesPerSecond * 2;
                    _tokens = _maxTokens;
                    _lastRefill = DateTime.UtcNow.Ticks;
                }
            }

            public bool Consume(uint bytes)
            {
                lock (_lock)
                {
                    var now = DateTime.UtcNow.Ticks;
                    var elapsed = (now - _lastRefill) / (double)TimeSpan.TicksPerSecond;
                    _tokens = Math.Min(_maxTokens, _tokens + elapsed * _rate);
                    _lastRefill = now;

                    if (_tokens >= bytes)
                    {
                        _tokens -= bytes;
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}
