using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotspotManager.Models
{
    public class ConnectedDevice : INotifyPropertyChanged
    {
        private string _macAddress;
        private string _hostName;
        private string _ipAddress;
        private DateTime _connectedTime;
        private bool _isBlacklisted;
        private long _bytesDownloaded;
        private long _bytesUploaded;
        private double _speedLimitKbps; // 0 = unlimited
        private bool _isActive;

        public string MacAddress
        {
            get => _macAddress;
            set { _macAddress = value; OnPropertyChanged(); }
        }

        public string HostName
        {
            get => _hostName;
            set { _hostName = value; OnPropertyChanged(); }
        }

        public string IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(); }
        }

        public DateTime ConnectedTime
        {
            get => _connectedTime;
            set { _connectedTime = value; OnPropertyChanged(); }
        }

        public bool IsBlacklisted
        {
            get => _isBlacklisted;
            set { _isBlacklisted = value; OnPropertyChanged(); }
        }

        public long BytesDownloaded
        {
            get => _bytesDownloaded;
            set { _bytesDownloaded = value; OnPropertyChanged(); OnPropertyChanged(nameof(DownloadSpeedKbps)); }
        }

        public long BytesUploaded
        {
            get => _bytesUploaded;
            set { _bytesUploaded = value; OnPropertyChanged(); OnPropertyChanged(nameof(UploadSpeedKbps)); }
        }

        public long LastBytesDownloaded { get; set; }
        public long LastBytesUploaded { get; set; }

        public double DownloadSpeedKbps => (BytesDownloaded - LastBytesDownloaded) * 8.0 / 1024.0;
        public double UploadSpeedKbps => (BytesUploaded - LastBytesUploaded) * 8.0 / 1024.0;

        public double SpeedLimitKbps
        {
            get => _speedLimitKbps;
            set { _speedLimitKbps = value; OnPropertyChanged(); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public string DisplayName => string.IsNullOrEmpty(HostName) ? MacAddress : $"{HostName} ({MacAddress})";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
