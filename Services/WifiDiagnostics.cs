using System.Linq;
using System.Net.NetworkInformation;

namespace HotspotManager.Services
{
    public static class WifiDiagnostics
    {
        public static long GetWlanLinkSpeedMbps()
        {
            var ni = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up
                                  && n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);
            return ni?.Speed > 0 ? ni.Speed / 1_000_000 : -1;
        }

        public static bool IsLikelyClientOn2_4G(out long mbps)
        {
            mbps = GetWlanLinkSpeedMbps();
            return mbps > 0 && mbps < 433;
        }
    }
}
