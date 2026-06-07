# emx17 FPS Viewer (Beta)

**emx17 FPS Viewer** is a sleek, modern, and fully gamer-themed system monitor (overlay) tool that allows you to track your in-game performance in real-time. Operating seamlessly on Windows, it displays crucial hardware metrics at any corner of your screen without distracting you from your gameplay.

## 🚀 Features & Monitored Metrics
The application dives deep into your system architecture to read and display the following metrics with minimal latency:

- **Raw FPS (Frames Per Second):** Measures the instant frame rate of your active game. Unlike other tools that capture display capture hooks, it directly counts the raw "Present" signals fired by the game engine.
- **CPU Temperature:** Monitors the real-time thermal status of your processor.
- **GPU Temperature:** Reads the exact temperature of your graphics hardware (supporting NVIDIA, AMD, or Intel).
- **Fully Customizable UI:** Font colors, text size, and screen positioning (via either real-time mouse drag-and-drop or preset grid layouts) are entirely under your control.

## 🛠 Technologies & Frameworks Used
To achieve maximum accuracy and zero performance overhead, the project utilizes low-level Windows APIs and the following core components:
- **`Microsoft.Diagnostics.Tracing.TraceEvent`**: Used to read FPS metrics directly from the Windows Kernel (ETW - Event Tracing for Windows / DxgKrnl). This ensures safe, real-time tracking without injecting any code into your game processes.
- **`System.Management` (WMI)**: Utilized to query CPU and motherboard sensors for thermal telemetry.
- **NVIDIA NVAPI & Performance Counters**: Leveraged to pull precise GPU diagnostics. It interfaces directly with native NVAPI for NVIDIA cards and generic Windows Performance Counters for AMD/Intel architectures.
- **.NET 10.0 (WPF)**: Powering the ultra-smooth, hardware-accelerated, responsive control panel dashboard and the overlay layer.

## ⚠️ Important Note About the Beta Version
This software is currently in its **BETA** stage. Due to variance in hardware configurations (especially motherboard sensor layouts) and evolving Windows core updates, you might encounter minor transient fluctuations, variations, or slight delays in thermal or frame rate readouts. Future updates will continually optimize these components based on your feedback.

## 🔗 Latest Release & Community
To download the latest compiled binaries, inspect the underlying C# architecture, or submit issue tracking/feature requests, please visit the official GitHub repository:

👉 **[github.com/emx17](https://github.com/emx17)**