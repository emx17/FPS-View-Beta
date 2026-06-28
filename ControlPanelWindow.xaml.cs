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
            ChkShowRam.IsChecked = _config.ShowRamUsage;
            ChkShowVram.IsChecked = _config.ShowVramUsage;
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

            for (int i = 0; i < CmbProfile.Items.Count; i++)
            {
                if (CmbProfile.Items[i] is ComboBoxItem item && item.Tag?.ToString() == _config.OverlayProfileIndex.ToString())
                {
                    CmbProfile.SelectedIndex = i;
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
            string tProfile = "OVERLAY PROFILE / THEME";
            string tGpuSelect = "ACTIVE GPU";
            string tAppearance = "APPEARANCE";
            string tOverlayColor = "Overlay Color";
            string tCustomColor = "Custom Color";
            string tFontSize = "Overlay Size";
            string tPosition = "POSITION & ALIGNMENT";
            string tPadding = "Padding";
            string tSensors = "SENSORS";
            string tOverlayCtrl = "OVERLAY CONTROL";
            string tOverlayToggle = "Show / hide on screen";
            string tShowGpuName = "Show GPU Name";
            string tShowCpu = "CPU Temp";
            string tShowGpu = "GPU Temp";
            string tShowRam = "RAM Usage";
            string tShowVram = "VRAM Usage";
            string tPosUnlock = "Unlock Position (Drag)";

            switch (lang)
            {
                case "CN":
                    tTitle = "emx17_FPSViewer - 控制面板"; tLang = "语言 / LANGUAGE"; tProfile = "叠加层配置文件 / 主题"; tGpuSelect = "活动GPU";
                    tAppearance = "外观"; tOverlayColor = "叠加层颜色"; tCustomColor = "自定义颜色"; tFontSize = "叠加层大小";
                    tPosition = "位置和对齐"; tPadding = "内边距"; tSensors = "传感器";
                    tOverlayCtrl = "叠加层控制"; tOverlayToggle = "在屏幕上显示/隐藏";
                    tShowGpuName = "显示GPU名称"; tShowCpu = "CPU温度"; tShowGpu = "GPU温度";
                    tShowRam = "内存占用"; tShowVram = "显存占用";
                    tPosUnlock = "解锁位置 (拖动)";
                    break;
                case "TR":
                    tTitle = "emx17_FPSViewer - Kontrol Paneli"; tLang = "DİL / LANGUAGE"; tProfile = "OVERLAY PROFİLİ / TEMA"; tGpuSelect = "AKTİF GPU SEÇİMİ";
                    tAppearance = "GÖRÜNÜM"; tOverlayColor = "Overlay Rengi"; tCustomColor = "Özel Renk Seç"; tFontSize = "Overlay Boyutu";
                    tPosition = "POZİSYON & HİZALAMA"; tPadding = "Kenar Boşluğu"; tSensors = "SENSÖRLER";
                    tOverlayCtrl = "OVERLAY KONTROL"; tOverlayToggle = "Ekranda göster / gizle";
                    tShowGpuName = "GPU Adını Göster"; tShowCpu = "CPU Sıcaklığı"; tShowGpu = "GPU Sıcaklığı";
                    tShowRam = "RAM Kullanımı"; tShowVram = "VRAM Kullanımı";
                    tPosUnlock = "Pozisyon Kilidini Aç (Sürükle)";
                    break;
                case "DE":
                    tTitle = "emx17_FPSViewer - Systemsteuerung"; tLang = "SPRACHE"; tProfile = "OVERLAY-PROFIL / DESIGN"; tGpuSelect = "AKTIVE GPU";
                    tAppearance = "ERSCHEINUNGSBILD"; tOverlayColor = "Overlay-Farbe"; tCustomColor = "Benutzerdefinierte Farbe"; tFontSize = "Overlay-Größe";
                    tPosition = "POSITION & AUSRICHTUNG"; tPadding = "Abstand (Padding)"; tSensors = "SENSOREN";
                    tOverlayCtrl = "OVERLAY-STEUERUNG"; tOverlayToggle = "Auf dem Bildschirm ein-/ausblenden";
                    tShowGpuName = "GPU-Namen anzeigen"; tShowCpu = "CPU-Temp"; tShowGpu = "GPU-Temp";
                    tShowRam = "RAM-Nutzung"; tShowVram = "VRAM-Nutzung";
                    tPosUnlock = "Position entsperren (Ziehen)";
                    break;
                case "ES":
                    tTitle = "emx17_FPSViewer - Panel de control"; tLang = "IDIOMA"; tProfile = "PERFIL DE OVERLAY / TEMA"; tGpuSelect = "GPU ACTIVA";
                    tAppearance = "APARIENCIA"; tOverlayColor = "Color del overlay"; tCustomColor = "Color personalizado"; tFontSize = "Tamaño del overlay";
                    tPosition = "POSICIÓN Y ALINEACIÓN"; tPadding = "Margen"; tSensors = "SENSORES";
                    tOverlayCtrl = "CONTROL DEL OVERLAY"; tOverlayToggle = "Mostrar / ocultar en pantalla";
                    tShowGpuName = "Mostrar Nombre de GPU"; tShowCpu = "Temp. CPU"; tShowGpu = "Temp. GPU";
                    tShowRam = "Uso de RAM"; tShowVram = "Uso de VRAM";
                    tPosUnlock = "Desbloquear Posición (Arrastrar)";
                    break;
                case "FR":
                    tTitle = "emx17_FPSViewer - Panneau de configuration"; tLang = "LANGUE"; tProfile = "PROFIL D'OVERLAY / THÈME"; tGpuSelect = "GPU ACTIF";
                    tAppearance = "APPARENCE"; tOverlayColor = "Couleur de l'overlay"; tCustomColor = "Couleur personnalisée"; tFontSize = "Taille de l'overlay";
                    tPosition = "POSITION ET ALIGNEMENT"; tPadding = "Marge"; tSensors = "CAPTEURS";
                    tOverlayCtrl = "CONTRÔLE DE L'OVERLAY"; tOverlayToggle = "Afficher / masquer à l'écran";
                    tShowGpuName = "Afficher le nom du GPU"; tShowCpu = "Temp. CPU"; tShowGpu = "Temp. GPU";
                    tShowRam = "Utilisation RAM"; tShowVram = "Utilisation VRAM";
                    tPosUnlock = "Déverrouiller la Position (Glisser)";
                    break;
                case "PT":
                    tTitle = "emx17_FPSViewer - Painel de Controlo"; tLang = "IDIOMA"; tProfile = "PERFIL DE OVERLAY / TEMA"; tGpuSelect = "GPU ATIVA";
                    tAppearance = "APARÊNCIA"; tOverlayColor = "Cor do Overlay"; tCustomColor = "Cor Personalizada"; tFontSize = "Tamanho da Fonte";
                    tPosition = "POSIÇÃO E ALINHAMENTO"; tPadding = "Margem"; tSensors = "SENSORES";
                    tOverlayCtrl = "CONTROLO DO OVERLAY"; tOverlayToggle = "Mostrar / ocultar no ecrã";
                    tShowGpuName = "Mostrar Nome da GPU"; tShowCpu = "Temp. CPU"; tShowGpu = "Temp. GPU";
                    tShowRam = "Uso de RAM"; tShowVram = "Uso de VRAM";
                    tPosUnlock = "Desbloquear Posição (Arrastar)";
                    break;
                case "BR":
                    tTitle = "emx17_FPSViewer - Painel de Controle"; tLang = "IDIOMA / LANGUAGE"; tProfile = "PERFIL DE OVERLAY / TEMA"; tGpuSelect = "GPU ATIVA";
                    tAppearance = "APARÊNCIA"; tOverlayColor = "Cor do Overlay"; tCustomColor = "Cor Personalizada"; tFontSize = "Tamanho do Overlay";
                    tPosition = "POSIÇÃO E ALINHAMENTO"; tPadding = "Margem"; tSensors = "SENSORES";
                    tOverlayCtrl = "CONTROLE DO OVERLAY"; tOverlayToggle = "Mostrar / ocultar na tela";
                    tShowGpuName = "Mostrar Nome da GPU"; tShowCpu = "Temp. CPU"; tShowGpu = "Temp. GPU";
                    tShowRam = "Uso de RAM"; tShowVram = "Uso de VRAM";
                    tPosUnlock = "Desbloquear Posição (Arrastar)";
                    break;
                case "RU":
                    tTitle = "emx17_FPSViewer - Панель управления"; tLang = "ЯЗЫК"; tProfile = "ПРОФИЛЬ ОВЕРЛЕЯ / ТЕМА"; tGpuSelect = "АКТИВНАЯ GPU";
                    tAppearance = "ВНЕШНИЙ ВИД"; tOverlayColor = "Цвет оверлея"; tCustomColor = "Пользовательский цвет"; tFontSize = "Размер оверлея";
                    tPosition = "ПОЗИЦИЯ И ВЫРАВНИВАНИЕ"; tPadding = "Отступы (Padding)"; tSensors = "ДАТЧИКИ";
                    tOverlayCtrl = "УПРАВЛЕНИЕ ОВЕРЛЕЕМ"; tOverlayToggle = "Показать / скрыть на экране";
                    tShowGpuName = "Показать Имя GPU"; tShowCpu = "Темп. CPU"; tShowGpu = "Темп. GPU";
                    tShowRam = "Использ. RAM"; tShowVram = "Использ. VRAM";
                    tPosUnlock = "Разблокировать Позицию (Перетащить)";
                    break;
            }

            Title = tTitle;
            LblLanguage.Text = tLang;
            LblProfile.Text = tProfile;
            LblGpuSelect.Text = tGpuSelect;
            LblAppearance.Text = tAppearance;
            LblOverlayColor.Text = tOverlayColor;
            BtnCustomColor.Content = tCustomColor;
            LblFontSize.Text = tFontSize;
            LblPosition.Text = tPosition;
            LblPadding.Text = tPadding;
            LblSensors.Text = tSensors;
            LblOverlayCtrl.Text = tOverlayCtrl;
            LblOverlayToggle.Text = tOverlayToggle;
            ChkShowGpuName.Content = tShowGpuName;
            ChkShowCpu.Content = tShowCpu;
            ChkShowGpu.Content = tShowGpu;
            ChkShowRam.Content = tShowRam;
            ChkShowVram.Content = tShowVram;
            ChkPositionUnlock.Content = tPosUnlock;
        }

        private void SaveAndApply()
        {
            if (_config == null) return; 

            if (CmbLanguage.SelectedItem is ComboBoxItem item && item.Tag != null)
                _config.Language = item.Tag.ToString() ?? "EN";
            else
                _config.Language = "EN";
                
            if (CmbProfile.SelectedItem is ComboBoxItem profileItem && profileItem.Tag != null)
                _config.OverlayProfileIndex = int.Parse(profileItem.Tag.ToString() ?? "0");

            _config.SelectedGpuName = CmbGpuSelector.SelectedItem?.ToString() ?? "";
            
            _config.ShowGpuName = ChkShowGpuName.IsChecked == true;
            _config.ShowCpuTemp = ChkShowCpu.IsChecked == true;
            _config.ShowGpuTemp = ChkShowGpu.IsChecked == true;
            _config.ShowRamUsage = ChkShowRam.IsChecked == true;
            _config.ShowVramUsage = ChkShowVram.IsChecked == true;
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

        private void BtnCustomColor_Click(object sender, RoutedEventArgs e)
        {
            var picker = new ColorPickerWindow(_config);
            picker.Owner = this;

            if (picker.ShowDialog() == true)
            {
                // Apply the selected overlay color
                string newColor = picker.SelectedColorHex;
                _config.TextColorHex = newColor;
                _selectedColorHex = newColor;
                try
                {
                    var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(newColor);
                    ColorPreview.Background = new SolidColorBrush(color);
                }
                catch { }

                _config.Save();
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

