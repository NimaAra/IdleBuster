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
            private readonly NotifyIcon _trayIcon;

            public Context()
            {
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

                Application.ApplicationExit += (_, __) =>
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Icon = Resources.Idle_Disarm;
                    AllowMonitorPowerdown();
                };
            }

            private void ToggleIdle(object sender, EventArgs eArgs)
            {
                var menu = (MenuItem)sender;

                if (_enabled)
                {
                    _trayIcon.Icon = Resources.Idle_Disarm;
                    AllowMonitorPowerdown();
                } else
                {
                    _trayIcon.Icon = Resources.Idle_Arm;
                    PreventMonitorPowerdown();
                }

                _enabled = !_enabled;
                menu.Checked = _enabled;
            }

            private static void Exit(object sender, EventArgs eArgs) => Application.Exit();

            private static void PreventMonitorPowerdown () 
                => SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS);

            private static void AllowMonitorPowerdown () => SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
            
            // Prevent Idle-to-Sleep (monitor not affected)
            private static void PreventSleep () 
                => SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_AWAYMODE_REQUIRED);

            private static void KeepSystemAwake () 
                => SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED);

            /// <summary>
            /// <see href="http://pinvoke.net/default.aspx/kernel32.setthreadexecutionstate"/>
            /// </summary>
            [Flags]
            private enum EXECUTION_STATE : uint
            {
                ES_AWAYMODE_REQUIRED = 0x00000040,
                ES_CONTINUOUS = 0x80000000,
                ES_DISPLAY_REQUIRED = 0x00000002,
                ES_SYSTEM_REQUIRED = 0x00000001
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
        }
    }
}
