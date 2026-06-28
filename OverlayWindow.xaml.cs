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
            
            System.Windows.Media.SolidColorBrush selectedColorBrush;
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_config.TextColorHex);
                selectedColorBrush = new System.Windows.Media.SolidColorBrush(color);
            }
            catch
            {
                selectedColorBrush = System.Windows.Media.Brushes.Lime;
            }
            OverlayText.Foreground = selectedColorBrush;

            // Apply scaling to Advanced HUD based on FontSize (default 20 = scale 1.0)
            double scale = _config.FontSize / 20.0;
            AdvancedHudBorder.LayoutTransform = new System.Windows.Media.ScaleTransform(scale, scale);

            // Apply selected color to Advanced HUD text elements
            AdvFpsText.Foreground = selectedColorBrush;
            AdvFrametimeText.Foreground = selectedColorBrush;
            AdvFrametimeGraph.Stroke = selectedColorBrush;
            AdvCpuProgress.Foreground = selectedColorBrush;
            AdvRamProgress.Foreground = selectedColorBrush;

            ApplyProfileStyle();

            // Update Position
            if (_hwnd != IntPtr.Zero)
            {
                UpdatePositionAndLockState();
            }
        }

        private void ApplyProfileStyle()
        {
            if (OverlayBorder == null || AdvancedHudBorder == null) return;

            if (_config.OverlayProfileIndex == 3)
            {
                OverlayBorder.Visibility = Visibility.Collapsed;
                AdvancedHudBorder.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                OverlayBorder.Visibility = Visibility.Visible;
                AdvancedHudBorder.Visibility = Visibility.Collapsed;
            }

            switch (_config.OverlayProfileIndex)
            {
                case 1: // Gamer Panel
                    OverlayBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 0, 0, 0)); // #80000000
                    OverlayBorder.CornerRadius = new CornerRadius(4);
                    OverlayBorder.Padding = new Thickness(10, 6, 10, 6);
                    break;
                case 2: // Steam Deck Style
                    OverlayBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 26, 26, 26)); // #E61A1A1A
                    OverlayBorder.CornerRadius = new CornerRadius(6);
                    OverlayBorder.Padding = new Thickness(12, 8, 12, 8);
                    break;
                case 0: // Classic Minimalist
                default:
                    OverlayBorder.Background = System.Windows.Media.Brushes.Transparent;
                    OverlayBorder.CornerRadius = new CornerRadius(0);
                    OverlayBorder.Padding = new Thickness(0);
                    break;
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
            if (_config.OverlayProfileIndex == 3)
            {
                UpdateAdvancedHud();
            }
            else
            {
                string formattedText = _hardwareManager.FormatOverlayText(_config);
                if (OverlayText.Text != formattedText)
                {
                    OverlayText.Text = formattedText;
                    OverlayText.UpdateLayout();
                }
            }

            ApplyPresetPosition();
        }

        private void UpdateAdvancedHud()
        {
            var data = _hardwareManager.GetAdvancedData(_config.SelectedGpuName);
            var fps = _hardwareManager.FpsMonitor;
            
            // Force active PID refresh for ETW
            fps.RefreshFps();

            // Block 1: FPS
            AdvFpsText.Text = fps.CurrentFps.ToString();
            AdvOnePercentLowText.Text = $"{fps.OnePercentLowFps:F1} FPS";
            AdvFrametimeText.Text = $"{fps.CurrentFrametimeMs:F1} ms";

            // Draw Frametime Graph
            var times = fps.GetFrametimesSnapshot();
            if (times.Length > 0)
            {
                var points = new System.Windows.Media.PointCollection();
                double width = 100.0;
                double height = 25.0;
                double step = width / Math.Max(1, times.Length - 1);
                
                // Max expected frametime for scaling (e.g. 50ms = 20fps)
                double maxMs = 50.0; 

                for (int i = 0; i < times.Length; i++)
                {
                    double x = i * step;
                    double y = height - (Math.Min(times[i], maxMs) / maxMs * height);
                    points.Add(new System.Windows.Point(x, y));
                }
                AdvFrametimeGraph.Points = points;
            }

            // Block 2: CPU
            if (_config.ShowCpuTemp)
            {
                AdvCpuBlock.Visibility = Visibility.Visible;
                AdvCpuName.Text = data.CpuName;
                AdvCpuLoad.Text = $"{data.CpuLoad:F0}%";
                AdvCpuFreq.Text = $"{data.CpuFreq:F0} MHz";
                AdvCpuTemp.Text = $"{data.CpuTemp:F0}°C";
                AdvCpuProgress.Value = data.CpuLoad;
            }
            else
            {
                AdvCpuBlock.Visibility = Visibility.Collapsed;
            }

            // Block 3: RAM
            if (_config.ShowRamUsage)
            {
                AdvRamBlock.Visibility = Visibility.Visible;
                AdvRamName.Text = data.RamName;
                AdvRamLoad.Text = $"{data.RamLoad:F0}%";
                AdvRamUsage.Text = $"{data.RamUsedGB:F1} / {data.RamTotalGB:F1} GB";
                AdvRamProgress.Value = data.RamLoad;
            }
            else
            {
                AdvRamBlock.Visibility = Visibility.Collapsed;
            }

            // Block 4: GPU
            if (_config.ShowGpuTemp) // Reusing ShowGpuTemp as general GPU toggle
            {
                AdvGpuBlock.Visibility = Visibility.Visible;
                AdvGpuName.Text = data.GpuName;
                AdvGpuLoad.Text = $"{data.GpuLoad:F0}%";
                AdvGpuFreq.Text = $"{data.GpuFreq:F0} MHz";
                AdvGpuTemp.Text = $"{data.GpuTemp:F0}°C";
                AdvGpuProgress.Value = data.GpuLoad;
            }
            else
            {
                AdvGpuBlock.Visibility = Visibility.Collapsed;
            }

            // Block 5: VRAM
            if (_config.ShowVramUsage)
            {
                AdvVramBlock.Visibility = Visibility.Visible;
                AdvVramName.Text = data.VramName;
                AdvVramLoad.Text = $"{data.VramLoad:F0}%";
                AdvVramUsage.Text = $"{data.VramUsedGB:F1} / {data.VramTotalGB:F1} GB";
                AdvVramProgress.Value = data.VramLoad;
            }
            else
            {
                AdvVramBlock.Visibility = Visibility.Collapsed;
            }
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

