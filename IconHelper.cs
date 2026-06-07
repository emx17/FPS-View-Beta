using System;
using System.IO;

namespace FPSOverlay
{
    public static class IconHelper
    {
        public static string? EnsureIcon()
        {
            string pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "normallogo.png");
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");

            if (!File.Exists(pngPath)) return null;

            try
            {
                byte[] pngData = File.ReadAllBytes(pngPath);
                using (var ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write((short)0); // Reserved
                    bw.Write((short)1); // Type ICO
                    bw.Write((short)1); // Image Count
                    
                    bw.Write((byte)0); // Width 256
                    bw.Write((byte)0); // Height 256
                    bw.Write((byte)0); // Color Count
                    bw.Write((byte)0); // Reserved
                    bw.Write((short)1); // Planes
                    bw.Write((short)32); // BPP
                    bw.Write(pngData.Length); // Size of data
                    bw.Write(22); // Offset (6 bytes header + 16 bytes entry)
                    
                    bw.Write(pngData);
                    
                    File.WriteAllBytes(icoPath, ms.ToArray());
                }
                return icoPath;
            }
            catch
            {
                return null;
            }
        }
    }
}