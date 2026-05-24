using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using HotspotManager.Models;
using HotspotManager.Services;

namespace HotspotManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly HotspotService _hotspotService;
        private readonly DeviceMonitorService _deviceMonitor;
        private readonly TrafficFilterService _trafficFilter;
        private readonly SleepGuardService _sleepGuard;

        private string _statusText;
        private bool _isHotspotOn;
        private bool _isSleepGuardOn;
        private bool _isFilterActive;
        private string _ssid = "MyHotspot";
        private string _passphrase = "12345678";
        private int _selectedBandIndex;
        private bool _isHidden;
        private ConnectedDevice _selectedDevice;
        private bool _isLogVisible;

        public LocaleStrings L { get; } = new LocaleStrings();

        public MainViewModel()
        {
            Logger.Init();
            _statusText = LocaleService.Get("App.Init");
            Logger.Info("App", LocaleService.Get("App.Started"));

            _hotspotService = new HotspotService();
            _deviceMonitor = new DeviceMonitorService(_hotspotService);
            _trafficFilter = new TrafficFilterService();
            _sleepGuard = new SleepGuardService();

            StartCommand = new RelayCommand(async _ => await StartHotspot(), _ => !IsHotspotOn && _hotspotService.IsInitialized);
            StopCommand = new RelayCommand(async _ => await StopHotspot(), _ => IsHotspotOn);
            ToggleCommand = new RelayCommand(async _ => await ToggleHotspot(), _ => _hotspotService.IsInitialized);
            ApplyConfigCommand = new RelayCommand(async _ => await ApplyConfig(), _ => _hotspotService.IsInitialized);
            ToggleSleepGuardCommand = new RelayCommand(_ => ToggleSleepGuard());
            ToggleFilterCommand = new RelayCommand(_ => ToggleFilter());
            BlacklistDeviceCommand = new RelayCommand(async _ => await BlacklistDevice(), _ => SelectedDevice != null);
            SetSpeedLimitCommand = new RelayCommand(async _ => await SetSpeedLimit());
            ToggleLogCommand = new RelayCommand(_ => IsLogVisible = !IsLogVisible);
            ClearLogCommand = new RelayCommand(_ => Logger.Clear());
            ToggleLangCommand = new RelayCommand(_ => ToggleLanguage());

            _sleepGuard.GuardStateChanged += (s, e) =>
            {
                IsSleepGuardOn = _sleepGuard.IsGuarding;
                RefreshLocaleButtons();
            };

            _hotspotService.StatusChanged += (s, e) =>
            {
                IsHotspotOn = _hotspotService.IsRunning;
                UpdateStatus();
            };

            LocaleService.StaticPropertyChanged += (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    UpdateStatus();
                    RefreshLocaleButtons();
                    L.Refresh();
                });
            };
        }

        public async Task InitializeAsync()
        {
            Logger.Info("App", LocaleService.Get("App.InitStart"));
            var initialized = await _hotspotService.InitializeAsync();
            if (initialized)
            {
                var sysCfg = _hotspotService.ReadSystemConfig();
                _ssid = sysCfg.Ssid;
                _passphrase = sysCfg.Passphrase;
                _selectedBandIndex = sysCfg.Band;
                _isHidden = sysCfg.IsHidden;

                OnPropertyChanged(nameof(Ssid));
                OnPropertyChanged(nameof(Passphrase));
                OnPropertyChanged(nameof(SelectedBandIndex));
                OnPropertyChanged(nameof(IsHidden));

                _deviceMonitor.StartMonitoring();
                UpdateStatus();
                Logger.Info("App", LocaleService.Get("App.InitDone"));
            }
            else
            {
                StatusText = LocaleService.Get("App.InitFail");
                Logger.Error("App", "Init failed - check Hotspot log for details");
            }
        }

        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }
        public bool IsHotspotOn { get => _isHotspotOn; set { _isHotspotOn = value; OnPropertyChanged(nameof(ToggleButtonText)); CommandManager.InvalidateRequerySuggested(); } }
        public bool IsSleepGuardOn { get => _isSleepGuardOn; set { _isSleepGuardOn = value; OnPropertyChanged(); RefreshBadges(); } }
        public bool IsFilterActive { get => _isFilterActive; set { _isFilterActive = value; OnPropertyChanged(); RefreshBadges(); } }
        public string Ssid { get => _ssid; set { _ssid = value; OnPropertyChanged(); } }
        public string Passphrase { get => _passphrase; set { _passphrase = value; OnPropertyChanged(); } }
        public int SelectedBandIndex { get => _selectedBandIndex; set { _selectedBandIndex = value; OnPropertyChanged(); } }
        public bool IsHidden { get => _isHidden; set { _isHidden = value; OnPropertyChanged(); } }
        public ConnectedDevice SelectedDevice { get => _selectedDevice; set { _selectedDevice = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
        public bool IsLogVisible { get => _isLogVisible; set { _isLogVisible = value; OnPropertyChanged(nameof(LogButtonText)); } }

        public ObservableCollection<ConnectedDevice> Devices => _deviceMonitor.Devices;
        public ObservableCollection<LogEntry> LogEntries => Logger.Entries;

        public string ToggleButtonText => IsHotspotOn ? LocaleService.Get("Btn.Stop") : LocaleService.Get("Btn.Start");
        public string SleepGuardButtonText => IsSleepGuardOn ? LocaleService.Get("Btn.GuardOff") : LocaleService.Get("Btn.GuardOn");
        public string LogButtonText => IsLogVisible ? LocaleService.Get("Btn.HideLog") : LocaleService.Get("Btn.ShowLog");
        public string LangButtonText => LocaleService.Get("Btn.Lang");

        public string SleepGuardBadgeText => IsSleepGuardOn ? LocaleService.Get("Bar.ON") : LocaleService.Get("Bar.OFF");
        public string SleepGuardBadgeColor => IsSleepGuardOn ? "#346538" : "#787774";
        public string SleepGuardBadgeBg => IsSleepGuardOn ? "#EDF3EC" : "#F0F0F0";

        public string FilterBadgeText => IsFilterActive ? LocaleService.Get("Bar.RUNNING") : LocaleService.Get("Bar.STOPPED");
        public string FilterBadgeColor => IsFilterActive ? "#346538" : "#787774";
        public string FilterBadgeBg => IsFilterActive ? "#EDF3EC" : "#F0F0F0";

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ToggleCommand { get; }
        public ICommand ApplyConfigCommand { get; }
        public ICommand ToggleSleepGuardCommand { get; }
        public ICommand ToggleFilterCommand { get; }
        public ICommand BlacklistDeviceCommand { get; }
        public ICommand SetSpeedLimitCommand { get; }
        public ICommand ToggleLogCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand ToggleLangCommand { get; }

        private void ToggleLanguage()
        {
            LocaleService.Toggle();
            RefreshLocaleButtons();
            UpdateStatus();
        }

        private void RefreshLocaleButtons()
        {
            OnPropertyChanged(nameof(ToggleButtonText));
            OnPropertyChanged(nameof(SleepGuardButtonText));
            OnPropertyChanged(nameof(LogButtonText));
            OnPropertyChanged(nameof(LangButtonText));
            RefreshBadges();
        }

        private void RefreshBadges()
        {
            OnPropertyChanged(nameof(SleepGuardBadgeText));
            OnPropertyChanged(nameof(SleepGuardBadgeColor));
            OnPropertyChanged(nameof(SleepGuardBadgeBg));
            OnPropertyChanged(nameof(FilterBadgeText));
            OnPropertyChanged(nameof(FilterBadgeColor));
            OnPropertyChanged(nameof(FilterBadgeBg));
        }

        private async Task StartHotspot()
        {
            Logger.Info("UI", "Start hotspot requested");
            StatusText = LocaleService.Get("Status.Starting");
            var r = await _hotspotService.StartAsync();
            StatusText = r ? LocaleService.Get("Status.Started") : LocaleService.Get("Status.StartFail");
        }

        private async Task StopHotspot()
        {
            Logger.Info("UI", "Stop hotspot requested");
            StatusText = LocaleService.Get("Status.Stopping");
            var r = await _hotspotService.StopAsync();
            StatusText = r ? LocaleService.Get("Status.Stopped") : LocaleService.Get("Status.StopFail");
        }

        private async Task ToggleHotspot() { if (IsHotspotOn) await StopHotspot(); else await StartHotspot(); }

        private async Task ApplyConfig()
        {
            if (Passphrase.Length < 8) { StatusText = LocaleService.Get("Config.PwTooShort"); return; }
            StatusText = LocaleService.Get("Config.Applying");
            var cfg = new HotspotConfig { Ssid = Ssid, Passphrase = Passphrase, Band = SelectedBandIndex, IsHidden = IsHidden };
            var r = await _hotspotService.ApplyConfigAsync(cfg);
            StatusText = r ? LocaleService.Get("Config.Applied") : LocaleService.Get("Config.ApplyFail");
        }

        private void ToggleSleepGuard() { if (_sleepGuard.IsGuarding) _sleepGuard.Stop(); else _sleepGuard.Start(); }
        private void ToggleFilter()
        {
            if (_trafficFilter.IsRunning) { _trafficFilter.Stop(); IsFilterActive = false; StatusText = LocaleService.Get("Status.FilterOff"); }
            else { var r = _trafficFilter.Start(); IsFilterActive = r; StatusText = r ? LocaleService.Get("Status.FilterOn") : LocaleService.Get("Status.FilterFail"); }
        }

        private async Task BlacklistDevice()
        {
            if (SelectedDevice == null) return;
            var ns = !SelectedDevice.IsBlacklisted;
            SelectedDevice.IsBlacklisted = ns;
            _trafficFilter.SetBlacklist(new[] { SelectedDevice.IpAddress }, ns);
        }

        private async Task SetSpeedLimit()
        {
            if (SelectedDevice == null)
            {
                foreach (var d in Devices.Where(x => x.SpeedLimitKbps > 0))
                    _trafficFilter.SetRateLimit(d.IpAddress, d.SpeedLimitKbps);
            }
            else { _trafficFilter.SetRateLimit(SelectedDevice.IpAddress, SelectedDevice.SpeedLimitKbps); }
        }

        private void UpdateStatus()
        {
            if (IsHotspotOn)
            {
                var c = Devices.Count;
                StatusText = c > 0
                    ? string.Format(LocaleService.Get("Status.Connected"), c)
                    : LocaleService.Get("Status.Waiting");
            }
            else StatusText = LocaleService.Get("Status.Offline");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose()
        {
            Logger.Info("App", LocaleService.Get("App.Shutdown"));
            _trafficFilter.Dispose(); _deviceMonitor.Dispose(); _sleepGuard.Dispose(); _hotspotService.Dispose();
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _e;
        private readonly Func<object?, bool>? _c;
        public RelayCommand(Action<object?> e, Func<object?, bool>? c = null) { _e = e; _c = c; }
        public bool CanExecute(object? p) => _c?.Invoke(p) ?? true;
        public void Execute(object? p) => _e(p);
        public event EventHandler? CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
    }
}
