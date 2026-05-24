using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotspotManager.Models
{
    public class HotspotConfig : INotifyPropertyChanged
    {
        private string _ssid = "MyHotspot";
        private string _passphrase = "Hotspot123!";
        private int _band; // 0=Auto, 1=2.4GHz, 2=5GHz
        private bool _isHidden;

        public string Ssid
        {
            get => _ssid;
            set { _ssid = value; OnPropertyChanged(); }
        }

        public string Passphrase
        {
            get => _passphrase;
            set { _passphrase = value; OnPropertyChanged(); }
        }

        public int Band
        {
            get => _band;
            set { _band = value; OnPropertyChanged(); }
        }

        public bool IsHidden
        {
            get => _isHidden;
            set { _isHidden = value; OnPropertyChanged(); }
        }

        public bool IsPasswordValid => Passphrase.Length >= 8;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
