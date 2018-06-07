namespace IdleBuster
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;
    using IdleBuster.Properties;
    using Timer = System.Windows.Forms.Timer;

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var _mutex = new Mutex(true, "935CAD5A-0EAC-4ABB-B4A7-70821EE058CC"))
            {
                if (!_mutex.WaitOne(TimeSpan.Zero, true))
                {
                    MessageBox.Show(@"Only a single instance of this program should be running.");
                }
                else
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    Application.Run(new Context());
                }
            }
        }

        private sealed class Context : ApplicationContext
        {
            private readonly NotifyIcon _trayIcon;
            private readonly MenuItem _preventIdleMenuItem, _periodMenuItem;

            private bool _isIdle;
            private bool _inPeriodMode;

            private TimeSpan _durationStart, _durationEnd;

            public Context()
            {
                _preventIdleMenuItem = new MenuItem("Prevent Idle", ToggleIdle)
                {
                    Checked = false,
                    ShowShortcut = true,
                    DefaultItem = true,
                    Name = "menu-item-prevent-idle"
                };

                _periodMenuItem = new MenuItem("Prevent During Period", ToggleIdleDuringPeriod)
                {
                    Checked = false,
                    ShowShortcut = true,
                    DefaultItem = false,
                    Name = "menu-item-time-period"
                };

                _trayIcon = new NotifyIcon
                {
                    Icon = Resources.Idle_Disarm,
                    ContextMenu = new ContextMenu(new[]
                    {
                        _preventIdleMenuItem,
                        _periodMenuItem,
                        new MenuItem("Exit", Exit)
                        {
                            ShowShortcut = true,
                            Name = "menu-item-exit"
                        }
                    }),
                    Visible = true
                };

                var timer = new Timer { Interval = 1000 };
                timer.Tick += OnTimerTick;

                Application.ApplicationExit += (_, __) =>
                {
                    timer.Dispose();

                    _trayIcon.Visible = false;
                    _trayIcon.Icon = Resources.Idle_Disarm;
                    AllowMonitorPowerdown();
                };

                ToggleIdle(null, EventArgs.Empty);

                timer.Start();
            }

            private void ToggleIdle(object sender, EventArgs eArgs)
            {
                if (_inPeriodMode)
                {
                    _inPeriodMode = false;
                    _isIdle = false;
                    _periodMenuItem.Checked = false;
                }

                ToggleIdleImpl();
                
                _preventIdleMenuItem.Checked = _isIdle;
            }

            private void ToggleIdleDuringPeriod(object sender, EventArgs eArgs)
            {
                _inPeriodMode = TryPromptForDuration(out var duration);
                
                var (start, end) = duration;
                
                _durationStart = start;
                _durationEnd = end;

                _periodMenuItem.Checked = _inPeriodMode;

                if (_inPeriodMode)
                {
                    _preventIdleMenuItem.Checked = false;
                } else
                {
                    _isIdle = true;
                    ToggleIdleImpl();
                }
            }

            private void ToggleIdleImpl()
            {
                if (_isIdle)
                {
                    _trayIcon.Icon = Resources.Idle_Disarm;
                    AllowMonitorPowerdown();
                } else
                {
                    _trayIcon.Icon = Resources.Idle_Arm;
                    PreventMonitorPowerdown();
                }

                _isIdle = !_isIdle;
            }

            private void OnTimerTick(object sender, EventArgs e)
            {
                if (!_inPeriodMode) { return; }

                if (IsInPeriod(_durationStart, _durationEnd))
                {
                    if (_isIdle) { return; }
                } else
                {
                    if (!_isIdle) { return; }
                }

                ToggleIdleImpl();
            }

            private static bool IsInPeriod(TimeSpan start, TimeSpan end)
            {
                var now = DateTime.Now.TimeOfDay;
                
                if (start <= end)
                {
                    // start and stop times are in the same day
                    if (now >= start && now <= end)
                    {
                        return true;
                    }

                    return false;
                }

                // start and stop times are in different days
                if (now >= start || now <= end)
                {
                    return true;
                }

                return false;
            }

            private static bool TryPromptForDuration(out (TimeSpan, TimeSpan) fromToEnd)
            {
                using (var prompt = new Form
                {
                    Width = 200,
                    Height = 165,
                    Text = string.Empty,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterScreen
                })
                {
                    var startLbl = new Label
                    {
                        Text = @"Start",
                        Width = 70,
                        Dock = DockStyle.Bottom,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Font = new Font("Arial", 18, FontStyle.Regular)
                    };
                    var endLbl = new Label
                    {
                        Width = 70,
                        Text = @"End",
                        Dock = DockStyle.Fill,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Font = new Font("Arial", 18, FontStyle.Regular)
                    };

                    var startPicker = new DateTimePicker
                    {
                        CustomFormat = @"HH:mm",
                        Format = DateTimePickerFormat.Custom,
                        ShowUpDown = true,
                        Width = 20,
                        Dock = DockStyle.Fill,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right,
                        Font = new Font("Arial", 18, FontStyle.Bold),
                        Value = DateTime.Today.AddHours(12)
                    };

                    var endPicker = new DateTimePicker
                    {
                        CustomFormat = @"HH:mm",
                        Format = DateTimePickerFormat.Custom,
                        ShowUpDown = true,
                        Width = 20,
                        Dock = DockStyle.Fill,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right,
                        Font = new Font("Arial", 18, FontStyle.Bold),
                        Value = DateTime.Today.AddHours(13)
                    };

                    var confirmation = new Button
                    {
                        Text = @"Submit",
                        DialogResult = DialogResult.OK,
                        Dock = DockStyle.Fill,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right
                    };

                    var layout = new TableLayoutPanel
                    {
                        Dock = DockStyle.Fill,
                        ColumnCount = 2,
                        RowCount = 3,
                        Padding = new Padding(10, 10, 10, 10),
                        GrowStyle = TableLayoutPanelGrowStyle.FixedSize
                    };
                    layout.Controls.Add(startLbl, 0, 0);
                    layout.Controls.Add(startPicker, 1, 0);

                    layout.Controls.Add(endLbl, 0, 1);
                    layout.Controls.Add(endPicker, 1, 1);
                    layout.Controls.Add(confirmation, 0, 2);
                    layout.SetColumnSpan(confirmation, 2);

                    prompt.Controls.Add(layout);
                    prompt.AcceptButton = confirmation;


                    if (prompt.ShowDialog() == DialogResult.OK)
                    {
                        fromToEnd = (startPicker.Value.TimeOfDay, endPicker.Value.TimeOfDay);
                        return true;
                    }

                    fromToEnd = (TimeSpan.MinValue, TimeSpan.MinValue);
                    return false;
                }
            }

            private static void Exit(object sender, EventArgs eArgs) => Application.Exit();

            private static void PreventMonitorPowerdown()
                => SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS);

            private static void AllowMonitorPowerdown() => SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);

            // Prevent Idle-to-Sleep (monitor not affected)
            private static void PreventSleep()
                => SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_AWAYMODE_REQUIRED);

            private static void KeepSystemAwake()
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
