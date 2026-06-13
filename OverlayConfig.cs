using System;
using System.IO;
using System.Text.Json;

namespace FPSOverlay
{
    public enum OverlayPositionPreset
    {
        Custom = 0,
        TopLeft = 1, TopCenter = 2, TopRight = 3,
        MiddleLeft = 4, Center = 5, MiddleRight = 6,
        BottomLeft = 7, BottomCenter = 8, BottomRight = 9
    }

    public class OverlayConfig
    {
        public bool ShowGpuName { get; set; } = true;
        public bool ShowCpuTemp { get; set; } = true;
        public bool ShowGpuTemp { get; set; } = true;
        public int FontSize { get; set; } = 20;
        public string FontFamily { get; set; } = "Orbitron, Rajdhani, Segoe UI Semibold, Consolas";
        public string TextColorHex { get; set; } = "#E31E24";
        
        public OverlayPositionPreset PositionPreset { get; set; } = OverlayPositionPreset.TopRight;
        public double PositionPadding { get; set; } = 25;
        public double OverlayX { get; set; } = -1;
        public double OverlayY { get; set; } = -1;
        public bool PositionLocked { get; set; } = true;
        
        public string Language { get; set; } = "TR";
        public string SelectedGpuName { get; set; } = "";

        private static string GetConfigPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        }

        public static OverlayConfig Load()
        {
            string configPath = GetConfigPath();
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<OverlayConfig>(json) ?? new OverlayConfig();
                }
                catch
                {
                    return new OverlayConfig();
                }
            }
            return new OverlayConfig();
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(GetConfigPath(), json);
            }
            catch { }
        }
    }
}

