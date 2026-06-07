using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FPSOverlay
{
    public partial class ControlPanelWindow : Window
    {
        private OverlayConfig _config;
        private HardwareMonitorManager _hwManager;
        private Action _onConfigChanged;
        private Action<bool> _onOverlayToggle;
        private string _selectedColorHex;

        public ControlPanelWindow(OverlayConfig config, HardwareMonitorManager hwManager, Action onConfigChanged, Action<bool> onOverlayToggle)
        {
            InitializeComponent();
            _config = config;
            _hwManager = hwManager;
            _onConfigChanged = onConfigChanged;
            _onOverlayToggle = onOverlayToggle;
            _selectedColorHex = _config.TextColorHex;

            try { this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/app.ico")); } catch { }

            PopulateGpuSelector();
            LoadSettingsToUI();
            ApplyLanguage();
        }

        private void PopulateGpuSelector()
        {
            CmbGpuSelector.Items.Clear();
            List<string> gpus = new List<string>(_hwManager.AvailableGpus);
            if (gpus.Count == 0) gpus.Add("Bilinmeyen GPU / Unknown GPU");
            
            foreach(var gpu in gpus)
            {
                CmbGpuSelector.Items.Add(gpu);
            }

            if (!string.IsNullOrEmpty(_config.SelectedGpuName) && gpus.Contains(_config.SelectedGpuName))
                CmbGpuSelector.SelectedItem = _config.SelectedGpuName;
            else if (gpus.Count > 0)
                CmbGpuSelector.SelectedIndex = 0;
        }

        private void LoadSettingsToUI()
        {
            ChkShowGpuName.IsChecked = _config.ShowGpuName;
            ChkShowCpu.IsChecked = _config.ShowCpuTemp;
            ChkShowGpu.IsChecked = _config.ShowGpuTemp;
            ChkPositionUnlock.IsChecked = !_config.PositionLocked;
            
            SliderFontSize.Value = _config.FontSize;
            SliderPadding.Value = _config.PositionPadding;
            
            CmbLanguage.SelectedIndex = _config.Language == "TR" ? 0 : 1;
            
            switch (_config.PositionPreset)
            {
                case OverlayPositionPreset.TopLeft: PosTL.IsChecked = true; break;
                case OverlayPositionPreset.TopCenter: PosTC.IsChecked = true; break;
                case OverlayPositionPreset.TopRight: PosTR.IsChecked = true; break;
                case OverlayPositionPreset.MiddleLeft: PosML.IsChecked = true; break;
                case OverlayPositionPreset.Center: PosMC.IsChecked = true; break;
                case OverlayPositionPreset.MiddleRight: PosMR.IsChecked = true; break;
                case OverlayPositionPreset.BottomLeft: PosBL.IsChecked = true; break;
                case OverlayPositionPreset.BottomCenter: PosBC.IsChecked = true; break;
                case OverlayPositionPreset.BottomRight: PosBR.IsChecked = true; break;
            }
            
            UpdateColorPreview();
        }

        private void ApplyLanguage()
        {
            bool isTr = _config.Language == "TR";
            Title = isTr ? "FPS Overlay - Kontrol Paneli" : "FPS Overlay - Control Panel";
            
            LblLanguage.Text = isTr ? "DİL / LANGUAGE" : "LANGUAGE";
            LblGpuSelect.Text = isTr ? "AKTİF GPU SEÇİMİ" : "ACTIVE GPU";
            
            ChkShowGpuName.Content = isTr ? "GPU Adını Göster" : "Show GPU Name";
            ChkShowCpu.Content = isTr ? "CPU Sıcaklığı" : "CPU Temp";
            ChkShowGpu.Content = isTr ? "GPU Sıcaklığı" : "GPU Temp";
            ChkPositionUnlock.Content = isTr ? "Pozisyon Kilidini Aç (Sürükle)" : "Unlock Position (Drag)";
        }

        private void SaveAndApply()
        {
            if (_config == null) return; 

            _config.Language = CmbLanguage.SelectedIndex == 0 ? "TR" : "EN";
            _config.SelectedGpuName = CmbGpuSelector.SelectedItem?.ToString() ?? "";
            
            _config.ShowGpuName = ChkShowGpuName.IsChecked == true;
            _config.ShowCpuTemp = ChkShowCpu.IsChecked == true;
            _config.ShowGpuTemp = ChkShowGpu.IsChecked == true;
            _config.PositionLocked = ChkPositionUnlock.IsChecked != true;
            
            _config.FontSize = (int)SliderFontSize.Value;
            _config.PositionPadding = SliderPadding.Value;
            _config.TextColorHex = _selectedColorHex;

            if (PosTL.IsChecked == true) _config.PositionPreset = OverlayPositionPreset.TopLeft;
            else if (PosTC.IsChecked == true) _config.PositionPreset = OverlayPositionPreset.TopCenter;
            else if (PosTR.IsChecked == true) _config.PositionPreset = OverlayPositionPreset.TopRight;
            else if (PosML.IsChecked == true) _config.PositionPreset = OverlayPositionPreset.MiddleLeft;
            else if (PosMC.IsChecked == true) _config.PositionPreset = OverlayPositionPreset.Center;
            else if (PosMR.IsChecked == true) _config.PositionPreset = OverlayPositionPreset.MiddleRight;
            else if (PosBL.IsChecked == true) _config.PositionPreset = OverlayPositionPreset.BottomLeft;
            else if (PosBC.IsChecked == true) _config.PositionPreset = OverlayPositionPreset.BottomCenter;
            else if (PosBR.IsChecked == true) _config.PositionPreset = OverlayPositionPreset.BottomRight;
            else _config.PositionPreset = OverlayPositionPreset.Custom;

            _config.Save();
            ApplyLanguage();
            _onConfigChanged?.Invoke();
            _hwManager.TriggerUpdate();

            // Overlay aç/kapa toggle
            _onOverlayToggle?.Invoke(ChkOverlayActive.IsChecked == true);
        }

        private void InteractiveElement_Changed(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded) SaveAndApply();
        }
        
        private void InteractiveElement_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded) SaveAndApply();
        }

        private void SliderFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded) return;
            if (TxtFontSizeVal != null) TxtFontSizeVal.Text = $"{(int)e.NewValue} px";
            SaveAndApply();
        }

        private void SliderPadding_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded) return;
            if (TxtPaddingVal != null) TxtPaddingVal.Text = $"{(int)e.NewValue} px";
            SaveAndApply();
        }

        private void ColorPalette_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Background is SolidColorBrush brush)
            {
                _selectedColorHex = brush.Color.ToString();
                UpdateColorPreview();
                SaveAndApply();
            }
        }

        private void UpdateColorPreview()
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_selectedColorHex);
                if (ColorPreview != null) ColorPreview.Background = new SolidColorBrush(color);
            }
            catch
            {
                if (ColorPreview != null) ColorPreview.Background = new SolidColorBrush(Colors.Red);
            }
        }

        public void NotifyCustomDrag()
        {
            PosTL.IsChecked = false;
            PosTC.IsChecked = false;
            PosTR.IsChecked = false;
            PosML.IsChecked = false;
            PosMC.IsChecked = false;
            PosMR.IsChecked = false;
            PosBL.IsChecked = false;
            PosBC.IsChecked = false;
            PosBR.IsChecked = false;
            _config.PositionPreset = OverlayPositionPreset.Custom;
            _config.Save();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide(); 
        }

        private void GitHub_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/emx17",
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
