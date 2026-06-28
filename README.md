# emx17 FPS Viewer (Beta v3.0)

**emx17 FPS Viewer** is a sleek, modern, and fully gamer-themed system monitor (overlay) tool that allows you to track your in-game performance in real-time. Operating seamlessly on Windows, it displays crucial hardware metrics at any corner of your screen without distracting you from your gameplay.

**⚠️ URGENT UPDATE:** This is by far the most stable and optimized beta release to date. If you are using previous Beta versions, please upgrade to Beta 3.0 immediately!

## 🚀 What's New in Beta 3.0?

- **Upgraded Telemetry Engine:** Fully migrated to the `LibreHardwareMonitor.Hardware` package for much deeper and robust system readings.
- **Vastly Expanded Metrics:** Added support for **RAM, VRAM, CPU/GPU MHz (Clock Speeds), 1% Low FPS, and Frametime (ms) latency** values.
- **Pinpoint Thermal Accuracy:** CPU and GPU temperature readings are now **96% more stable and accurate** compared to Beta 2.0 and 2.1.
- **3 Brand New Overlay Themes:** Previously, only the default "Classic Minimalist" FPS overlay was available. Now, 3 new overlays have been added: **"Gamer Panel"**, **"Steam Deck Style"**, and **"Advanced Performance HUD"**.
- **Infinite Color Customization:** You are no longer limited to predefined colors. With the brand new, math-driven **Color Picker Wheel**, you can select any exact color you want and instantly reflect it onto your overlay.
- **Flawless Localization:** Fixed several translation and localization bugs across 8 different languages.
- **Bulletproof Installer (Inno Setup):** Automatically handles all prerequisites (.NET 8 Runtime and VC++ Redistributable) in the background.

## 🚀 Features & Monitored Metrics

The application dives deep into your system architecture to read and display the following metrics with minimal latency:

- **Raw FPS & 1% Lows:** Measures the instant frame rate and 1% low drops of your active game directly from the Windows Kernel (ETW / DxgKrnl) without hooking or injecting code.
- **Frametime (ms):** Tracks frame latency for a buttery-smooth gaming experience.
- **CPU & GPU Telemetry:** Reads exact thermal status and real-time core clock frequencies (MHz).
- **RAM & VRAM Usage:** Comprehensive real-time memory allocation monitoring.
- **Fully Customizable UI:** Advanced RGB color wheel, text size, and screen positioning (via drag-and-drop or grid presets) are entirely under your control.

## 🛠 Technologies & Frameworks Used

- **`Microsoft.Diagnostics.Tracing.TraceEvent`**: For zero-overhead, direct Windows Kernel FPS monitoring.
- **`LibreHardwareMonitor.Hardware`**: Core hardware communication layer for precise CPU, GPU, RAM, and VRAM sensors.
- **.NET 8.0 (WPF)**: Powering the ultra-smooth, hardware-accelerated, responsive dashboard and overlay layer on the Long-Term Support (LTS) framework.
- **Pure Math UI Generation**: Custom WPF Color Picker built purely on `System.Windows.Media` and Trigonometry for maximum performance.

## 🔗 Latest Release & Community

To download the latest compiled binaries, inspect the underlying C# architecture, or submit issue tracking/feature requests, please visit the official GitHub repository:

👉 **[github.com/emx17](https://github.com/emx17)**