using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace FPSOverlay
{
    public class AdvancedOverlayData
    {
        public string CpuName { get; set; } = "CPU";
        public float CpuLoad { get; set; }
        public float CpuFreq { get; set; }
        public float CpuTemp { get; set; }
        
        public string RamName { get; set; } = "RAM";
        public float RamUsedGB { get; set; }
        public float RamTotalGB { get; set; }
        public float RamLoad { get; set; }
        
        public string GpuName { get; set; } = "GPU";
        public float GpuLoad { get; set; }
        public float GpuFreq { get; set; }
        public float GpuTemp { get; set; }
        
        public string VramName { get; set; } = "VRAM";
        public float VramUsedGB { get; set; }
        public float VramTotalGB { get; set; }
        public float VramLoad { get; set; }
    }

    public class HardwareMonitorManager : IDisposable
    {
        public event Action? OnHardwareDataUpdated;
        
        private FpsMonitor _fpsMonitor;
        public FpsMonitor FpsMonitor => _fpsMonitor;
        
        private Computer _computer;

        private List<string> _availableGpus = new List<string>();
        public IReadOnlyList<string> AvailableGpus => _availableGpus;

        public HardwareMonitorManager()
        {
            _fpsMonitor = new FpsMonitor();
            
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true
            };
            
            try
            {
                _computer.Open();
                GetAvailableGpus();
            }
            catch
            {
                // In case Open() fails
            }
        }

        private void GetAvailableGpus()
        {
            _availableGpus.Clear();
            try
            {
                foreach (var hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.GpuNvidia || 
                        hardware.HardwareType == HardwareType.GpuAmd ||
                        hardware.HardwareType == HardwareType.GpuIntel)
                    {
                        _availableGpus.Add(hardware.Name);
                    }
                }
            }
            catch { }
            
            if (_availableGpus.Count == 0)
                _availableGpus.Add("Bilinmeyen GPU / Unknown GPU");
        }

        public int GetCpuTemperature()
        {
            try
            {
                foreach (var hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.Cpu)
                    {
                        hardware.Update();
                        
                        var packageSensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Package"));
                        if (packageSensor?.Value != null)
                        {
                            return (int)packageSensor.Value.Value;
                        }

                        var coreSensors = hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Core"));
                        if (coreSensors.Any() && coreSensors.Max(s => s.Value) != null)
                        {
                            var maxVal = coreSensors.Max(s => s.Value);
                            if (maxVal.HasValue)
                            {
                                return (int)maxVal.Value;
                            }
                        }

                        var anyTempSensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                        if (anyTempSensor?.Value != null)
                        {
                            return (int)anyTempSensor.Value.Value;
                        }
                    }
                }
            }
            catch { }
            return 0; 
        }

        public int GetGpuTemperature(string selectedGpuName)
        {
            try
            {
                foreach (var hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.GpuNvidia || 
                        hardware.HardwareType == HardwareType.GpuAmd ||
                        hardware.HardwareType == HardwareType.GpuIntel)
                    {
                        if (string.IsNullOrEmpty(selectedGpuName) || 
                            selectedGpuName == "Bilinmeyen GPU / Unknown GPU" ||
                            hardware.Name.Contains(selectedGpuName, StringComparison.OrdinalIgnoreCase) ||
                            selectedGpuName.Contains(hardware.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            hardware.Update();
                            
                            var coreSensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Core"));
                            if (coreSensor?.Value != null)
                            {
                                return (int)coreSensor.Value.Value;
                            }

                            var anyTempSensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                            if (anyTempSensor?.Value != null)
                            {
                                return (int)anyTempSensor.Value.Value;
                            }
                        }
                    }
                }
            }
            catch { }
            return 0; 
        }

        public string GetRamUsage()
        {
            try
            {
                foreach (var hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.Memory)
                    {
                        hardware.Update();
                        var usedMemSensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Memory Used"));
                        if (usedMemSensor?.Value != null)
                        {
                            return $"{usedMemSensor.Value.Value:F1} GB";
                        }
                    }
                }
            }
            catch { }
            return "N/A";
        }

        public string GetVramUsage(string selectedGpuName)
        {
            try
            {
                foreach (var hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.GpuNvidia || 
                        hardware.HardwareType == HardwareType.GpuAmd ||
                        hardware.HardwareType == HardwareType.GpuIntel)
                    {
                        if (string.IsNullOrEmpty(selectedGpuName) || 
                            selectedGpuName == "Bilinmeyen GPU / Unknown GPU" ||
                            hardware.Name.Contains(selectedGpuName, StringComparison.OrdinalIgnoreCase) ||
                            selectedGpuName.Contains(hardware.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            hardware.Update();
                            
                            var vramSensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && s.Name.Contains("Memory Used"));
                            if (vramSensor == null)
                                vramSensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Memory Used"));

                            if (vramSensor?.Value != null)
                            {
                                float val = vramSensor.Value.Value;
                                if (vramSensor.SensorType == SensorType.SmallData) 
                                {
                                    return $"{(val / 1024f):F1} GB";
                                }
                                else 
                                {
                                    return $"{val:F1} GB";
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return "N/A";
        }

        public int GetCurrentFps()
        {
            _fpsMonitor.RefreshFps();
            return _fpsMonitor.CurrentFps;
        }

        public string FormatOverlayText(OverlayConfig config)
        {
            string defaultGpuName = "Unknown GPU";
            string adminReq = "ADMIN REQUIRED!";
            string lang = config.Language ?? "EN";
            switch (lang)
            {
                case "TR": defaultGpuName = "Bilinmeyen GPU"; adminReq = "YÖNETİCİ İZNİ GEREKLİ!"; break;
                case "DE": defaultGpuName = "Unbekannte GPU"; adminReq = "ADMIN-RECHTE ERFORDERLICH!"; break;
                case "ES": defaultGpuName = "GPU Desconocida"; adminReq = "¡SE REQUIERE ADMINISTRADOR!"; break;
                case "FR": defaultGpuName = "GPU Inconnu"; adminReq = "ADMINISTRATEUR REQUIS !"; break;
                case "PT": defaultGpuName = "GPU Desconhecida"; adminReq = "ADMINISTRADOR NECESSÁRIO!"; break;
                case "BR": defaultGpuName = "GPU Desconhecida"; adminReq = "NECESSÁRIO ADMINISTRADOR!"; break;
                case "RU": defaultGpuName = "Неизвестная GPU"; adminReq = "ТРЕБУЮТСЯ ПРАВА АДМИНИСТРАТОРА!"; break;
            }

            string gpuName = string.IsNullOrEmpty(config.SelectedGpuName) ? defaultGpuName : config.SelectedGpuName;
            int fps = GetCurrentFps();
            string fpsText = fps == -1 ? adminReq : fps.ToString();

            List<string> topParts = new List<string>();
            List<string> bottomParts = new List<string>();

            if (config.ShowGpuName) topParts.Add($"[{gpuName}]");
            topParts.Add($"FPS: {fpsText}");

            if (config.ShowCpuTemp)
            {
                int cpuTemp = GetCpuTemperature();
                bottomParts.Add($"CPU: {(cpuTemp > 0 ? cpuTemp.ToString() : "N/A")}°C");
            }

            if (config.ShowGpuTemp)
            {
                int gpuTemp = GetGpuTemperature(gpuName);
                bottomParts.Add($"GPU: {(gpuTemp > 0 ? gpuTemp.ToString() : "N/A")}°C");
            }

            if (config.ShowVramUsage) bottomParts.Add($"VRAM: {GetVramUsage(gpuName)}");
            if (config.ShowRamUsage) bottomParts.Add($"RAM: {GetRamUsage()}");

            if (config.OverlayProfileIndex == 2)
            {
                string topStr = string.Join("  |  ", topParts);
                string bottomStr = string.Join("  |  ", bottomParts);
                
                if (string.IsNullOrEmpty(bottomStr)) return topStr;
                if (string.IsNullOrEmpty(topStr)) return bottomStr;
                
                return $"{topStr}\n{bottomStr}";
            }
            else
            {
                List<string> allParts = new List<string>(topParts);
                allParts.AddRange(bottomParts);
                return string.Join("  |  ", allParts);
            }
        }

        public AdvancedOverlayData GetAdvancedData(string selectedGpuName)
        {
            var data = new AdvancedOverlayData();
            
            try
            {
                foreach (var hardware in _computer.Hardware)
                {
                    hardware.Update();

                    if (hardware.HardwareType == HardwareType.Cpu)
                    {
                        data.CpuName = hardware.Name;
                        var load = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Total"));
                        if (load?.Value != null) data.CpuLoad = load.Value.Value;

                        var clock = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Clock);
                        if (clock?.Value != null) data.CpuFreq = clock.Value.Value;

                        var temp = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && (s.Name.Contains("Package") || s.Name.Contains("Core (Tctl/Tdie)")));
                        if (temp == null) temp = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                        if (temp?.Value != null) data.CpuTemp = temp.Value.Value;
                    }
                    else if (hardware.HardwareType == HardwareType.Memory)
                    {
                        var used = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Memory Used"));
                        var avail = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Memory Available"));
                        var load = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Memory"));
                        
                        if (used?.Value != null) data.RamUsedGB = used.Value.Value;
                        if (avail?.Value != null) data.RamTotalGB = data.RamUsedGB + avail.Value.Value;
                        if (load?.Value != null) data.RamLoad = load.Value.Value;
                        else if (data.RamTotalGB > 0) data.RamLoad = (data.RamUsedGB / data.RamTotalGB) * 100f;
                    }
                    else if (hardware.HardwareType == HardwareType.GpuNvidia || 
                             hardware.HardwareType == HardwareType.GpuAmd ||
                             hardware.HardwareType == HardwareType.GpuIntel)
                    {
                        if (string.IsNullOrEmpty(selectedGpuName) || 
                            selectedGpuName == "Bilinmeyen GPU / Unknown GPU" ||
                            hardware.Name.Contains(selectedGpuName, StringComparison.OrdinalIgnoreCase) ||
                            selectedGpuName.Contains(hardware.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            data.GpuName = hardware.Name;
                            
                            var load = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Core"));
                            if (load?.Value != null) data.GpuLoad = load.Value.Value;

                            var clock = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Clock && s.Name.Contains("Core"));
                            if (clock?.Value != null) data.GpuFreq = clock.Value.Value;

                            var temp = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Core"));
                            if (temp?.Value != null) data.GpuTemp = temp.Value.Value;

                            var vramUsed = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && s.Name.Contains("Memory Used"));
                            var vramTotal = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && s.Name.Contains("Memory Total"));
                            
                            if (vramUsed == null) vramUsed = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Memory Used"));
                            if (vramTotal == null) vramTotal = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Memory Total"));

                            if (vramUsed?.Value != null)
                            {
                                float val = vramUsed.Value.Value;
                                data.VramUsedGB = vramUsed.SensorType == SensorType.SmallData ? val / 1024f : val;
                            }
                            
                            if (vramTotal?.Value != null)
                            {
                                float val = vramTotal.Value.Value;
                                data.VramTotalGB = vramTotal.SensorType == SensorType.SmallData ? val / 1024f : val;
                            }

                            if (data.VramTotalGB > 0)
                            {
                                data.VramLoad = (data.VramUsedGB / data.VramTotalGB) * 100f;
                            }
                        }
                    }
                }
            }
            catch { }

            return data;
        }

        public void TriggerUpdate()
        {
            OnHardwareDataUpdated?.Invoke();
        }

        public void Dispose()
        {
            _fpsMonitor?.Dispose();
            
            try
            {
                _computer?.Close();
            }
            catch { }
        }
    }
}
