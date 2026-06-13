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
            
            for (int i = 0; i < CmbLanguage.Items.Count; i++)
            {
                if (CmbLanguage.Items[i] is ComboBoxItem item && item.Tag?.ToString() == _config.Language)
                {
                    CmbLanguage.SelectedIndex = i;
                    break;
                }
            }
            
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
            string lang = _config.Language ?? "EN";

            string tTitle = "emx17_FPSViewer - Control Panel";
            string tLang = "LANGUAGE";
            string tGpuSelect = "ACTIVE GPU";
            string tAppearance = "APPEARANCE";
            string tOverlayColor = "Overlay Color";
            string tFontSize = "Font Size";
            string tPosition = "POSITION & ALIGNMENT";
            string tPadding = "Padding";
            string tSensors = "SENSORS";
            string tOverlayCtrl = "OVERLAY CONTROL";
            string tOverlayToggle = "Show / hide on screen";
            string tShowGpuName = "Show GPU Name";
            string tShowCpu = "CPU Temp";
            string tShowGpu = "GPU Temp";
            string tPosUnlock = "Unlock Position (Drag)";

            switch (lang)
            {
                case "TR":
                    tTitle = "emx17_FPSViewer - Kontrol Paneli"; tLang = "DİL / LANGUAGE"; tGpuSelect = "AKTİF GPU SEÇİMİ";
                    tAppearance = "GÖRÜNÜM"; tOverlayColor = "Overlay Rengi"; tFontSize = "Yazı Boyutu";
                    tPosition = "POZİSYON & HİZALAMA"; tPadding = "Kenar Boşluğu"; tSensors = "SENSÖRLER";
                    tOverlayCtrl = "OVERLAY KONTROL"; tOverlayToggle = "Ekranda göster / gizle";
                    tShowGpuName = "GPU Adını Göster"; tShowCpu = "CPU Sıcaklığı"; tShowGpu = "GPU Sıcaklığı";
                    tPosUnlock = "Pozisyon Kilidini Aç (Sürükle)";
                    break;
                case "DE":
                    tTitle = "emx17_FPSViewer - Systemsteuerung"; tLang = "SPRACHE"; tGpuSelect = "AKTIVE GPU";
                    tAppearance = "ERSCHEINUNGSBILD"; tOverlayColor = "Overlay-Farbe"; tFontSize = "Schriftgröße";
                    tPosition = "POSITION & AUSRICHTUNG"; tPadding = "Abstand (Padding)"; tSensors = "SENSOREN";
                    tOverlayCtrl = "OVERLAY-STEUERUNG"; tOverlayToggle = "Auf dem Bildschirm ein-/ausblenden";
                    tShowGpuName = "GPU-Namen anzeigen"; tShowCpu = "CPU-Temp"; tShowGpu = "GPU-Temp";
                    tPosUnlock = "Position entsperren (Ziehen)";
                    break;
                case "ES":
                    tTitle = "emx17_FPSViewer - Panel de Control"; tLang = "IDIOMA"; tGpuSelect = "GPU ACTIVA";
                    tAppearance = "APARIENCIA"; tOverlayColor = "Color del Overlay"; tFontSize = "Tamaño de Fuente";
                    tPosition = "POSICIÓN Y ALINEACIÓN"; tPadding = "Margen"; tSensors = "SENSORES";
                    tOverlayCtrl = "CONTROL DEL OVERLAY"; tOverlayToggle = "Mostrar / ocultar en pantalla";
                    tShowGpuName = "Mostrar Nombre de GPU"; tShowCpu = "Temp. CPU"; tShowGpu = "Temp. GPU";
                    tPosUnlock = "Desbloquear Posición (Arrastrar)";
                    break;
                case "FR":
                    tTitle = "emx17_FPSViewer - Panneau de Configuration"; tLang = "LANGUE"; tGpuSelect = "GPU ACTIF";
                    tAppearance = "APPARENCE"; tOverlayColor = "Couleur de l'Overlay"; tFontSize = "Taille de Police";
                    tPosition = "POSITION ET ALIGNEMENT"; tPadding = "Marge"; tSensors = "CAPTEURS";
                    tOverlayCtrl = "CONTRÃ”LE DE L'OVERLAY"; tOverlayToggle = "Afficher / masquer à l'écran";
                    tShowGpuName = "Afficher le nom du GPU"; tShowCpu = "Temp. CPU"; tShowGpu = "Temp. GPU";
                    tPosUnlock = "Déverrouiller la Position (Glisser)";
                    break;
                case "PT":
                    tTitle = "emx17_FPSViewer - Painel de Controlo"; tLang = "IDIOMA"; tGpuSelect = "GPU ATIVA";
                    tAppearance = "APARÊNCIA"; tOverlayColor = "Cor do Overlay"; tFontSize = "Tamanho da Fonte";
                    tPosition = "POSIÇÃO E ALINHAMENTO"; tPadding = "Margem"; tSensors = "SENSORES";
                    tOverlayCtrl = "CONTROLO DO OVERLAY"; tOverlayToggle = "Mostrar / ocultar no ecrã";
                    tShowGpuName = "Mostrar Nome da GPU"; tShowCpu = "Temp. CPU"; tShowGpu = "Temp. GPU";
                    tPosUnlock = "Desbloquear Posição (Arrastar)";
                    break;
                case "BR":
                    tTitle = "emx17_FPSViewer - Painel de Controle"; tLang = "IDIOMA"; tGpuSelect = "GPU ATIVA";
                    tAppearance = "APARÊNCIA"; tOverlayColor = "Cor do Overlay"; tFontSize = "Tamanho da Fonte";
                    tPosition = "POSIÇÃO E ALINHAMENTO"; tPadding = "Margem"; tSensors = "SENSORES";
                    tOverlayCtrl = "CONTROLE DO OVERLAY"; tOverlayToggle = "Mostrar / ocultar na tela";
                    tShowGpuName = "Mostrar Nome da GPU"; tShowCpu = "Temp. CPU"; tShowGpu = "Temp. GPU";
                    tPosUnlock = "Desbloquear Posição (Arrastar)";
                    break;
                case "RU":
                    tTitle = "emx17_FPSViewer - Панель управления"; tLang = "ЯЗЫК"; tGpuSelect = "АКТИВНАЯ GPU";
                    tAppearance = "ВНЕШНИЙ ВИД"; tOverlayColor = "Цвет Оверлея"; tFontSize = "Размер Шрифта";
                    tPosition = "ПОЗИЦИЯ И ВЫРАВНИВАНИЕ"; tPadding = "Отступ"; tSensors = "ДАТЧИКИ";
                    tOverlayCtrl = "УПРАВЛЕНИЕ ОВЕРЛЕЕМ"; tOverlayToggle = "Показать / скрыть на экране";
                    tShowGpuName = "Показать Имя GPU"; tShowCpu = "Темп. CPU"; tShowGpu = "Темп. GPU";
                    tPosUnlock = "Разблокировать Позицию (Перетащить)";
                    break;
            }

            Title = tTitle;
            LblLanguage.Text = tLang;
            LblGpuSelect.Text = tGpuSelect;
            LblAppearance.Text = tAppearance;
            LblOverlayColor.Text = tOverlayColor;
            LblFontSize.Text = tFontSize;
            LblPosition.Text = tPosition;
            LblPadding.Text = tPadding;
            LblSensors.Text = tSensors;
            LblOverlayCtrl.Text = tOverlayCtrl;
            LblOverlayToggle.Text = tOverlayToggle;
            ChkShowGpuName.Content = tShowGpuName;
            ChkShowCpu.Content = tShowCpu;
            ChkShowGpu.Content = tShowGpu;
            ChkPositionUnlock.Content = tPosUnlock;
        }

        private void SaveAndApply()
        {
            if (_config == null) return; 

            if (CmbLanguage.SelectedItem is ComboBoxItem item && item.Tag != null)
                _config.Language = item.Tag.ToString();
            else
                _config.Language = "EN";
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

            // Overlay on/off toggle
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

