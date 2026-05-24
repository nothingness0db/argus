using System;
using System.Runtime.InteropServices;

namespace HotspotManager.Native
{
    internal static class Win32Native
    {
        #region Power Management

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint SetThreadExecutionState(uint esFlags);

        public const uint ES_CONTINUOUS = 0x80000000;
        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        public const uint ES_DISPLAY_REQUIRED = 0x00000002;
        public const uint ES_AWAYMODE_REQUIRED = 0x00000040;

        #endregion

        #region Window Icon

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr hIcon);

        public const uint WM_SETICON = 0x0080;
        public const int ICON_SMALL = 0;
        public const int ICON_BIG = 1;
        public const uint IMAGE_ICON = 1;
        public const uint LR_LOADFROMFILE = 0x00000010;
        public const uint LR_DEFAULTCOLOR = 0x00000000;
        public const uint LR_SHARED = 0x00008000;
        public const int SM_CXICON = 11;
        public const int SM_CYICON = 12;
        public const int SM_CXSMICON = 49;
        public const int SM_CYSMICON = 50;

        #endregion

        #region WinDivert

        public const uint WINDIVERT_LAYER_NETWORK = 1;
        public const uint WINDIVERT_FLAG_SNIFF = 1;
        public const uint WINDIVERT_FLAG_DROP = 2;
        public const uint WINDIVERT_FLAG_RECV_ONLY = 4;
        public const uint WINDIVERT_FLAG_SEND_ONLY = 8;
        public const uint WINDIVERT_FLAG_NO_INSTALL = 16;

        public const uint WINDIVERT_PARAM_QUEUE_LENGTH = 0;
        public const uint WINDIVERT_PARAM_QUEUE_TIME = 1;
        public const uint WINDIVERT_PARAM_QUEUE_SIZE = 2;
        public const uint WINDIVERT_PARAM_VERSION_MAJOR = 3;
        public const uint WINDIVERT_PARAM_VERSION_MINOR = 4;

        [StructLayout(LayoutKind.Sequential)]
        public struct WinDivertAddress
        {
            public uint IfIdx;
            public uint SubIfIdx;
            public byte Direction;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IPv4Header
        {
            public byte VersionAndHeaderLength;
            public byte DSCPAndECN;
            public ushort TotalLength;
            public ushort Identification;
            public ushort FlagsAndFragmentOffset;
            public byte TTL;
            public byte Protocol;
            public ushort HeaderChecksum;
            public uint SrcAddr;
            public uint DstAddr;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TCPHeader
        {
            public ushort SrcPort;
            public ushort DstPort;
            public uint SequenceNumber;
            public uint AckNumber;
            public ushort DataOffsetAndFlags;
            public ushort WindowSize;
            public ushort Checksum;
            public ushort UrgentPointer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UDPHeader
        {
            public ushort SrcPort;
            public ushort DstPort;
            public ushort Length;
            public ushort Checksum;
        }

        [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WinDivertOpen(
            string filter,
            uint layer,
            short priority,
            ulong flags);

        [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool WinDivertClose(IntPtr handle);

        [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool WinDivertRecv(
            IntPtr handle,
            byte[] pPacket,
            uint packetLen,
            ref WinDivertAddress pAddr,
            ref uint pReadLen);

        [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool WinDivertSend(
            IntPtr handle,
            byte[] pPacket,
            uint packetLen,
            ref WinDivertAddress pAddr,
            ref uint pWriteLen);

        [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool WinDivertSetParam(
            IntPtr handle,
            uint param,
            ulong value);

        [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool WinDivertGetParam(
            IntPtr handle,
            uint param,
            out ulong pValue);

        #endregion

        #region Helper Methods

        public static uint IpToUint32(string ip)
        {
            var parts = ip.Split('.');
            if (parts.Length != 4) return 0;
            return ((uint.Parse(parts[0]) << 24) |
                    ((uint)uint.Parse(parts[1]) << 16) |
                    ((uint)uint.Parse(parts[2]) << 8) |
                    uint.Parse(parts[3]));
        }

        public static string Uint32ToIp(uint ip)
        {
            return $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";
        }

        #endregion
    }
}
