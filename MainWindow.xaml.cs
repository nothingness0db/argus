using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using HotspotManager.Native;
using HotspotManager.Services;
using HotspotManager.ViewModels;

namespace HotspotManager
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private IntPtr _smallIconHandle = IntPtr.Zero;
        private IntPtr _bigIconHandle = IntPtr.Zero;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = (MainViewModel)DataContext;
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyTaskbarIcons();
        }

        private void ApplyTaskbarIcons()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            var iconPath = ResolveIconPath();
            if (iconPath == null) return;

            int smallW = Win32Native.GetSystemMetrics(Win32Native.SM_CXSMICON);
            int smallH = Win32Native.GetSystemMetrics(Win32Native.SM_CYSMICON);
            int bigW = Win32Native.GetSystemMetrics(Win32Native.SM_CXICON);
            int bigH = Win32Native.GetSystemMetrics(Win32Native.SM_CYICON);

            _smallIconHandle = Win32Native.LoadImage(IntPtr.Zero, iconPath, Win32Native.IMAGE_ICON, smallW, smallH, Win32Native.LR_LOADFROMFILE | Win32Native.LR_DEFAULTCOLOR);
            _bigIconHandle = Win32Native.LoadImage(IntPtr.Zero, iconPath, Win32Native.IMAGE_ICON, bigW, bigH, Win32Native.LR_LOADFROMFILE | Win32Native.LR_DEFAULTCOLOR);

            if (_smallIconHandle != IntPtr.Zero)
                Win32Native.SendMessage(hwnd, Win32Native.WM_SETICON, (IntPtr)Win32Native.ICON_SMALL, _smallIconHandle);
            if (_bigIconHandle != IntPtr.Zero)
                Win32Native.SendMessage(hwnd, Win32Native.WM_SETICON, (IntPtr)Win32Native.ICON_BIG, _bigIconHandle);
        }

        private static string ResolveIconPath()
        {
            var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(exeDir)) return null;
            var candidate = Path.Combine(exeDir, "Assets", "app.ico");
            return File.Exists(candidate) ? candidate : null;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Logger.Entries.CollectionChanged += OnLogChanged;
            await _viewModel.InitializeAsync();
        }

        private void OnLogChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (LogListBox?.Items?.Count > 0)
                {
                    LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
                }
            }
            catch { }
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            _viewModel.Dispose();
            if (_smallIconHandle != IntPtr.Zero) Win32Native.DestroyIcon(_smallIconHandle);
            if (_bigIconHandle != IntPtr.Zero) Win32Native.DestroyIcon(_bigIconHandle);
        }
    }
}
