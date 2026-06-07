using System;
using System.Drawing;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;

namespace FPSOverlay
{
    public partial class App : System.Windows.Application
    {
        private Forms.NotifyIcon _notifyIcon = null!;
        private OverlayConfig _config = null!;
        private HardwareMonitorManager _hardwareManager = null!;
        
        private OverlayWindow _overlayWindow = null!;
        private ControlPanelWindow _controlPanelWindow = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 2. Ayarları yükle
            _config = OverlayConfig.Load();

            // 3. Donanım ve FPS Yöneticisini başlat
            _hardwareManager = new HardwareMonitorManager();

            InitializeNotifyIcon();

            _overlayWindow = new OverlayWindow(_config, _hardwareManager);
            _overlayWindow.Show();

            _controlPanelWindow = new ControlPanelWindow(_config, _hardwareManager, OnConfigChanged, ToggleOverlay);
            
            _overlayWindow.OnPositionChanged += (x, y) =>
            {
                _controlPanelWindow.NotifyCustomDrag();
            };

            _controlPanelWindow.Show();
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();

            var streamInfo = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/app.ico"));
            if (streamInfo != null)
                _notifyIcon.Icon = new Icon(streamInfo.Stream);
            else
                _notifyIcon.Icon = SystemIcons.Application;

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "FPS Overlay";
            
            _notifyIcon.DoubleClick += (s, args) =>
            {
                _controlPanelWindow.Show();
                _controlPanelWindow.WindowState = WindowState.Normal;
                _controlPanelWindow.Activate();
            };

            var contextMenu = new Forms.ContextMenuStrip();
            contextMenu.Items.Add("Ayarlar", null, (s, args) => _controlPanelWindow.Show());
            contextMenu.Items.Add("Çıkış", null, (s, args) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void OnConfigChanged()
        {
            _overlayWindow.ApplyConfig();
        }

        private void ToggleOverlay(bool isActive)
        {
            if (isActive)
                _overlayWindow.Show();
            else
                _overlayWindow.Hide();
        }

        private void ExitApplication()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            
            _overlayWindow?.Close();
            _controlPanelWindow?.Close();
            _hardwareManager?.Dispose();
            
            Current.Shutdown();
        }
    }
}
