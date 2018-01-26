namespace IdleBuster
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using IdleBuster.Properties;

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            Application.Run(new Context());
        }

        private sealed class Context : ApplicationContext
        {
            private bool _enabled;
            private readonly Timer _idleBusterTimer;
            private readonly NotifyIcon _trayIcon;

            public Context()
            {
                _idleBusterTimer = new Timer { Interval = 50 * 1000 };
                _idleBusterTimer.Tick += OnTimerTick;

                _trayIcon = new NotifyIcon
                {
                    Icon = Resources.Idle_Disarm,
                    ContextMenu = new ContextMenu(new[]
                    {
                        new MenuItem("Prevent Idle", ToggleIdle)
                        {
                            Checked = false,
                            ShowShortcut = true,
                            DefaultItem = true
                        },
                        new MenuItem("Exit", Exit)
                        {
                            ShowShortcut = true
                        }
                    }),
                    Visible = true
                };
            }

            private void ToggleIdle(object sender, EventArgs eArgs)
            {
                var menu = (MenuItem)sender;

                if (_enabled)
                {
                    _trayIcon.Icon = Resources.Idle_Disarm;
                    _idleBusterTimer.Stop();
                } else
                {
                    _trayIcon.Icon = Resources.Idle_Arm;
                    _idleBusterTimer.Start();
                }

                _enabled = !_enabled;
                menu.Checked = _enabled;
            }

            private void Exit(object sender, EventArgs eArgs)
            {
                _trayIcon.Visible = false;
                _trayIcon.Icon = Resources.Idle_Disarm;
                Application.Exit();
            }

            private static void OnTimerTick(object sender, EventArgs eArgs)
                => SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED);

            private enum EXECUTION_STATE : uint
            {
                ES_AWAYMODE_REQUIRED = 0x00000040,
                ES_CONTINUOUS = 0x80000000,
                ES_DISPLAY_REQUIRED = 0x00000002,
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
        }
    }
}
