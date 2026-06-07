using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Linq;

namespace FPSOverlay
{
    public class HardwareMonitorManager : IDisposable
    {
        public event Action? OnHardwareDataUpdated;
        
        private FpsMonitor _fpsMonitor;
        private bool _isAmdGpu = false;

        private List<string> _availableGpus = new List<string>();
        public IReadOnlyList<string> AvailableGpus => _availableGpus;

        // --- NVAPI Dinamik Yükleme (AMD/Intel sistemlerde çökmemesi için) ---
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr NvAPI_QueryInterfaceDelegate(uint id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_InitializeDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_EnumPhysicalGPUsDelegate(IntPtr[] gpuHandles, out int gpuCount);

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct NV_SENSOR
        {
            public int controller;
            public int defaultMinTemp;
            public int defaultMaxTemp;
            public int currentTemp;
            public int target;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct NV_GPU_THERMAL_SETTINGS
        {
            public uint version;
            public uint count;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public NV_SENSOR[] sensor;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_GetThermalSettingsDelegate(IntPtr gpuHandle, int sensorIndex, ref NV_GPU_THERMAL_SETTINGS thermalSettings);

        private NvAPI_EnumPhysicalGPUsDelegate? NvAPI_EnumPhysicalGPUs;
        private NvAPI_GPU_GetThermalSettingsDelegate? NvAPI_GPU_GetThermalSettings;

        private bool _isNvApiInitialized = false;
        private IntPtr[] _gpuHandles = new IntPtr[64];
        private int _nvGpuCount = 0;
        private uint _nvGpuThermalSettingsVer;

        public HardwareMonitorManager()
        {
            _fpsMonitor = new FpsMonitor();
            InitNvApi();
            GetAvailableGpus();
        }

        private void InitNvApi()
        {
            try
            {
                // Dinamik yükleme: nvapi64.dll yoksa (AMD/Intel sistem) hata fırlatmaz, sadece false döner
                string dllName = Environment.Is64BitProcess ? "nvapi64.dll" : "nvapi.dll";
                IntPtr nvapiModule = LoadLibrary(dllName);
                if (nvapiModule == IntPtr.Zero) return; // NVIDIA sürücüsü yüklü değil, sorunsuz geç

                IntPtr queryInterfacePtr = GetProcAddress(nvapiModule, "nvapi_QueryInterface");
                if (queryInterfacePtr == IntPtr.Zero) return;

                var queryInterface = Marshal.GetDelegateForFunctionPointer<NvAPI_QueryInterfaceDelegate>(queryInterfacePtr);

                IntPtr initPtr = queryInterface(0x0150E828);
                IntPtr enumPtr = queryInterface(0xE5AC921F);
                IntPtr thermalPtr = queryInterface(0xE3640A56);

                if (initPtr == IntPtr.Zero || enumPtr == IntPtr.Zero || thermalPtr == IntPtr.Zero) return;

                var nvInit = Marshal.GetDelegateForFunctionPointer<NvAPI_InitializeDelegate>(initPtr);
                NvAPI_EnumPhysicalGPUs = Marshal.GetDelegateForFunctionPointer<NvAPI_EnumPhysicalGPUsDelegate>(enumPtr);
                NvAPI_GPU_GetThermalSettings = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_GetThermalSettingsDelegate>(thermalPtr);

                if (nvInit() == 0)
                {
                    if (NvAPI_EnumPhysicalGPUs(_gpuHandles, out _nvGpuCount) == 0 && _nvGpuCount > 0)
                    {
                        _nvGpuThermalSettingsVer = (uint)Marshal.SizeOf(typeof(NV_GPU_THERMAL_SETTINGS)) | (2 << 16); 
                        _isNvApiInitialized = true;
                    }
                }
            }
            catch { }
        }

        private void GetAvailableGpus()
        {
            _availableGpus.Clear();
            try
            {
                using (var searcher = new ManagementObjectSearcher("select Name from Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString() ?? "Bilinmeyen GPU";
                        _availableGpus.Add(name);

                        if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                        {
                            _isAmdGpu = true;
                        }
                    }
                }
            }
            catch { }
            
            if (_availableGpus.Count == 0)
                _availableGpus.Add("Bilinmeyen GPU");
        }

        public int GetCpuTemperature()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        uint temp = Convert.ToUInt32(obj["CurrentTemperature"]);
                        return (int)((temp - 2732) / 10.0);
                    }
                }
            }
            catch { }
            return 0; // Eğer yetki yoksa N/A yerine 0 döner, Overlay'de düzgün gözükür.
        }

        public int GetGpuTemperature(string selectedGpuName)
        {
            // 1. NVIDIA NVAPI
            if (string.IsNullOrEmpty(selectedGpuName) || selectedGpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
            {
                if (_isNvApiInitialized && _nvGpuCount > 0 && NvAPI_GPU_GetThermalSettings != null)
                {
                    try
                    {
                        var settings = new NV_GPU_THERMAL_SETTINGS();
                        settings.version = _nvGpuThermalSettingsVer;
                        settings.sensor = new NV_SENSOR[3];

                        if (NvAPI_GPU_GetThermalSettings(_gpuHandles[0], 15, ref settings) == 0)
                        {
                            return settings.sensor[0].currentTemp;
                        }
                        else
                        {
                            settings.version = (uint)Marshal.SizeOf(typeof(NV_GPU_THERMAL_SETTINGS)) | (1 << 16);
                            if (NvAPI_GPU_GetThermalSettings(_gpuHandles[0], 15, ref settings) == 0)
                                return settings.sensor[0].currentTemp;
                        }
                    }
                    catch { }
                }
            }

            bool isAmd = _isAmdGpu || selectedGpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) || selectedGpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase);

            // 2. AMD Graphics Hardware Fallback
            if (isAmd)
            {
                try
                {
                    var amdCategories = new string[] { "AMD Link", "Graphics Hardware" };
                    foreach (var catName in amdCategories)
                    {
                        if (PerformanceCounterCategory.Exists(catName))
                        {
                            var cat = new PerformanceCounterCategory(catName);
                            var instances = cat.GetInstanceNames();
                            foreach (var inst in instances)
                            {
                                if (inst.Contains("Temperature", StringComparison.OrdinalIgnoreCase))
                                {
                                    using (var pc = new PerformanceCounter(catName, "Temperature", inst))
                                    {
                                        return (int)pc.NextValue();
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            // 3. Intel / Generic Thermal Zone Fallback
            try
            {
                if (PerformanceCounterCategory.Exists("Thermal Zone Information"))
                {
                    var thermalCat = new PerformanceCounterCategory("Thermal Zone Information");
                    var instances = thermalCat.GetInstanceNames();
                    foreach (var inst in instances)
                    {
                        if (inst.Contains("GPU", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var pc = new PerformanceCounter("Thermal Zone Information", "Temperature", inst))
                            {
                                float val = pc.NextValue();
                                return (int)(val - 273.15);
                            }
                        }
                    }
                }
            }
            catch { }

            return 0; 
        }

        public int GetCurrentFps()
        {
            return _fpsMonitor.CurrentFps;
        }

        public string FormatOverlayText(OverlayConfig config)
        {
            string formattedText = "[{gpu_name}]  |  FPS: {fps}  |  CPU: {cpu_temp}°C  |  GPU: {gpu_temp}°C";

            string gpuName = string.IsNullOrEmpty(config.SelectedGpuName) ? (config.Language == "EN" ? "Unknown GPU" : "Bilinmeyen GPU") : config.SelectedGpuName;

            if (!config.ShowGpuName)
            {
                formattedText = formattedText.Replace("[{gpu_name}]  |  ", "");
                formattedText = formattedText.Replace("[{gpu_name}]", "");
            }

            int fps = GetCurrentFps();
            string fpsText = fps == -1 
                ? (config.Language == "EN" ? "ADMIN REQUIRED!" : "YÖNETİCİ İZNİ GEREKLİ!") 
                : fps.ToString();
            formattedText = formattedText.Replace("{fps}", fpsText);
            formattedText = formattedText.Replace("{gpu_name}", gpuName);

            if (config.ShowCpuTemp)
            {
                int cpuTemp = GetCpuTemperature();
                string tempStr = cpuTemp > 0 ? cpuTemp.ToString() : "N/A";
                formattedText = formattedText.Replace("{cpu_temp}", tempStr);
            }
            else
            {
                formattedText = formattedText.Replace("  |  CPU: {cpu_temp}°C", "");
                formattedText = formattedText.Replace("CPU: {cpu_temp}°C  |  ", "");
                formattedText = formattedText.Replace("CPU: {cpu_temp}°C", "");
            }

            if (config.ShowGpuTemp)
            {
                int gpuTemp = GetGpuTemperature(gpuName);
                string tempStr = gpuTemp > 0 ? gpuTemp.ToString() : "N/A";
                formattedText = formattedText.Replace("{gpu_temp}", tempStr);
            }
            else
            {
                formattedText = formattedText.Replace("  |  GPU: {gpu_temp}°C", "");
                formattedText = formattedText.Replace("GPU: {gpu_temp}°C  |  ", "");
                formattedText = formattedText.Replace("GPU: {gpu_temp}°C", "");
            }

            return formattedText.Trim();
        }

        public void TriggerUpdate()
        {
            OnHardwareDataUpdated?.Invoke();
        }

        public void Dispose()
        {
            _fpsMonitor?.Dispose();
        }
    }
}
