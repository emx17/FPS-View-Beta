using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
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
        private Forms.ToolStripItem _menuItemSettings = null!;
        private Forms.ToolStripItem _menuItemExit = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 2. Load settings
            _config = OverlayConfig.Load();

            // 3. Initialize Hardware and FPS Manager
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
            _notifyIcon.Text = "emx17_FPSViewer";
            
            _notifyIcon.DoubleClick += (s, args) =>
            {
                _controlPanelWindow.Show();
                _controlPanelWindow.WindowState = WindowState.Normal;
                _controlPanelWindow.Activate();
            };

            var contextMenu = new Forms.ContextMenuStrip();
            _menuItemSettings = contextMenu.Items.Add("Ayarlar", null, (s, args) => _controlPanelWindow.Show());
            _menuItemExit = contextMenu.Items.Add("Çıkış", null, (s, args) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;
            
            UpdateTrayLanguage();
        }

        private void UpdateTrayLanguage()
        {
            if (_menuItemSettings == null || _menuItemExit == null || _config == null) return;
            string lang = _config.Language ?? "EN";
            switch (lang)
            {
                case "TR": _menuItemSettings.Text = "Ayarlar"; _menuItemExit.Text = "Çıkış"; break;
                case "DE": _menuItemSettings.Text = "Einstellungen"; _menuItemExit.Text = "Beenden"; break;
                case "ES": _menuItemSettings.Text = "Ajustes"; _menuItemExit.Text = "Salir"; break;
                case "FR": _menuItemSettings.Text = "Paramètres"; _menuItemExit.Text = "Quitter"; break;
                case "PT": _menuItemSettings.Text = "Definições"; _menuItemExit.Text = "Sair"; break;
                case "BR": _menuItemSettings.Text = "Configurações"; _menuItemExit.Text = "Sair"; break;
                case "RU": _menuItemSettings.Text = "Настройки"; _menuItemExit.Text = "Выход"; break;
                default: _menuItemSettings.Text = "Settings"; _menuItemExit.Text = "Exit"; break;
            }
        }

        private void OnConfigChanged()
        {
            _overlayWindow.ApplyConfig();
            UpdateTrayLanguage();
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

