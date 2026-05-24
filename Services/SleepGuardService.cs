using System;
using System.Runtime.InteropServices;

namespace HotspotManager.Services
{
    public class SleepGuardService : IDisposable
    {
        private System.Threading.Timer _keepAliveTimer;
        private bool _isGuarding;

        public bool IsGuarding
        {
            get => _isGuarding;
            private set
            {
                _isGuarding = value;
                GuardStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler GuardStateChanged;

        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint esFlags);

        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_AWAYMODE_REQUIRED = 0x00000040;

        public void Start()
        {
            if (IsGuarding) return;

            Logger.Info("SleepGuard", "开启不休眠守护");

            SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED);
            IsGuarding = true;

            _keepAliveTimer = new System.Threading.Timer(_ =>
            {
                if (IsGuarding)
                {
                    SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED);
                }
            }, null, 5000, 5000);
        }

        public void Stop()
        {
            if (!IsGuarding) return;

            Logger.Info("SleepGuard", "Closing sleep guard");
            IsGuarding = false;

            _keepAliveTimer?.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            _keepAliveTimer?.Dispose();
            _keepAliveTimer = null;

            SetThreadExecutionState(ES_CONTINUOUS);
        }

        public void Dispose()
        {
            Logger.Info("SleepGuard", "SleepGuard disposed");
            Stop();
        }
    }
}
