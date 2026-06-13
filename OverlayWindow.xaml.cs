using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace FPSOverlay
{
    public partial class OverlayWindow : Window
    {
        private DispatcherTimer _updateTimer;
        private OverlayConfig _config;
        private HardwareMonitorManager _hardwareManager;
        private IntPtr _hwnd;
        private Thread _topMostThread;
        private volatile bool _isRunning = true;

        public Action<double, double>? OnPositionChanged;

        public OverlayWindow(OverlayConfig config, HardwareMonitorManager hardwareManager)
        {
            InitializeComponent();
            _config = config;
            _hardwareManager = hardwareManager;
            
            try { this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/app.ico")); } catch { }

            ApplyConfig();

            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(250);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
            
            // Mouse Drag & Drop handler
            this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
        }

        public void ApplyConfig()
        {
            OverlayText.FontFamily = new System.Windows.Media.FontFamily(_config.FontFamily);
            OverlayText.FontSize = _config.FontSize;
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_config.TextColorHex);
                OverlayText.Foreground = new System.Windows.Media.SolidColorBrush(color);
            }
            catch
            {
                OverlayText.Foreground = System.Windows.Media.Brushes.Lime;
            }

            // Update Position
            if (_hwnd != IntPtr.Zero)
            {
                UpdatePositionAndLockState();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_config.PositionLocked && e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();

                // Triggers when the user releases the mouse (DragMove completes)
                ClampPosition();
                _config.OverlayX = this.Left;
                _config.OverlayY = this.Top;
                _config.Save();

                // Fire an event to update the X and Y numerators in the control panel
                OnPositionChanged?.Invoke(this.Left, this.Top);
            }
        }

        private void ClampPosition()
        {
            double maxRight = SystemParameters.PrimaryScreenWidth - 50;
            double maxBottom = SystemParameters.PrimaryScreenHeight - 50;

            if (this.Left < 0) this.Left = 0;
            if (this.Top < 0) this.Top = 0;
            if (this.Left > maxRight) this.Left = maxRight;
            if (this.Top > maxBottom) this.Top = maxBottom;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hwnd = new WindowInteropHelper(this).Handle;
            
            // Boundary security and positioning
            ClampPosition();
            
            if (_config.OverlayX == -1) // First launch or default
            {
                this.Left = SystemParameters.PrimaryScreenWidth - this.Width;
                this.Top = _config.OverlayY;
            }
            else
            {
                this.Left = _config.OverlayX;
                this.Top = _config.OverlayY;
            }

            UpdatePositionAndLockState();

            _topMostThread = new Thread(TopMostLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            _topMostThread.Start();
        }

        public void UpdatePositionAndLockState()
        {
            if (_hwnd == IntPtr.Zero) return;

            int exStyle = Win32Api.GetWindowLong(_hwnd, Win32Api.GWL_EXSTYLE);
            exStyle |= Win32Api.WS_EX_LAYERED | Win32Api.WS_EX_TOOLWINDOW;

            if (_config.PositionLocked)
            {
                // Unclickable (Click-Through)
                exStyle |= Win32Api.WS_EX_TRANSPARENT;
                OverlayText.Cursor = System.Windows.Input.Cursors.Arrow;
            }
            else
            {
                // Clickable and Movable
                exStyle &= ~Win32Api.WS_EX_TRANSPARENT;
                OverlayText.Cursor = System.Windows.Input.Cursors.SizeAll;
            }

            Win32Api.SetWindowLong(_hwnd, Win32Api.GWL_EXSTYLE, exStyle);

            if (_config.OverlayX != -1)
            {
                this.Left = _config.OverlayX;
                this.Top = _config.OverlayY;
                ClampPosition();
            }
        }

        private void TopMostLoop()
        {
            while (_isRunning)
            {
                if (_hwnd != IntPtr.Zero)
                {
                    Win32Api.SetWindowPos(_hwnd, Win32Api.HWND_TOPMOST, 0, 0, 0, 0,
                        Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE | Win32Api.SWP_SHOWWINDOW);
                }
                Thread.Sleep(100);
            }
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            string formattedText = _hardwareManager.FormatOverlayText(_config);
            if (OverlayText.Text != formattedText)
            {
                OverlayText.Text = formattedText;
                OverlayText.UpdateLayout();
            }

            ApplyPresetPosition();
        }

        private void ApplyPresetPosition()
        {
            if (_config.PositionPreset == OverlayPositionPreset.Custom)
                return;

            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;
            double pad = _config.PositionPadding;

            double w = this.ActualWidth;
            double h = this.ActualHeight;

            switch (_config.PositionPreset)
            {
                case OverlayPositionPreset.TopLeft:
                    this.Left = pad;
                    this.Top = pad;
                    break;
                case OverlayPositionPreset.TopCenter:
                    this.Left = (screenW - w) / 2;
                    this.Top = pad;
                    break;
                case OverlayPositionPreset.TopRight:
                    this.Left = screenW - w - pad;
                    this.Top = pad;
                    break;

                case OverlayPositionPreset.MiddleLeft:
                    this.Left = pad;
                    this.Top = (screenH - h) / 2;
                    break;
                case OverlayPositionPreset.Center:
                    this.Left = (screenW - w) / 2;
                    this.Top = (screenH - h) / 2;
                    break;
                case OverlayPositionPreset.MiddleRight:
                    this.Left = screenW - w - pad;
                    this.Top = (screenH - h) / 2;
                    break;

                case OverlayPositionPreset.BottomLeft:
                    this.Left = pad;
                    this.Top = screenH - h - pad;
                    break;
                case OverlayPositionPreset.BottomCenter:
                    this.Left = (screenW - w) / 2;
                    this.Top = screenH - h - pad;
                    break;
                case OverlayPositionPreset.BottomRight:
                    this.Left = screenW - w - pad;
                    this.Top = screenH - h - pad;
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _isRunning = false;
            _updateTimer.Stop();
            base.OnClosed(e);
        }
    }
}

