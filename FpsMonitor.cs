using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Diagnostics.Tracing.Session;

namespace FPSOverlay
{
    public class FpsMonitor : IDisposable
    {
        private Thread _etwThread;
        private Thread _calcThread;
        private volatile bool _isRunning = true;
        private TraceEventSession? _session;

        public int CurrentFps { get; private set; } = 0;

        // Aktif pencerenin PID'si
        private volatile int _activePid = 0;

        // Her thread için kare sayısı (saf sayaç)
        private ConcurrentDictionary<int, int> _threadFrameCounts = new();

        public FpsMonitor()
        {
            _calcThread = new Thread(CalculateFpsLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.Normal
            };
            _calcThread.Start();

            _etwThread = new Thread(EtwLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _etwThread.Start();
        }

        private void EtwLoop()
        {
            try
            {
                foreach (var sName in TraceEventSession.GetActiveSessionNames())
                {
                    if (sName.StartsWith("FPSOverlay_ETW_Session_"))
                    {
                        try
                        {
                            var oldSession = TraceEventSession.GetActiveSession(sName);
                            oldSession?.Stop();
                            oldSession?.Dispose();
                        }
                        catch { }
                    }
                }

                string sessionName = "FPSOverlay_ETW_Session_" + Guid.NewGuid().ToString()[..8];
                using (_session = new TraceEventSession(sessionName))
                {
                    _session.StopOnDispose = true;

                    var dxgKrnlProvider = Guid.Parse("802ec45a-1e99-4b83-9920-87c98277ba9d");
                    _session.EnableProvider(dxgKrnlProvider, Microsoft.Diagnostics.Tracing.TraceEventLevel.Informational, 0);

                    _session.Source.Dynamic.All += (data) =>
                    {
                        if (!_isRunning) return;

                        if (data.EventName != null &&
                            data.EventName.Equals("Present", StringComparison.OrdinalIgnoreCase))
                        {
                            int cachedPid = _activePid;
                            if (data.ProcessID == cachedPid && cachedPid != 0)
                            {
                                // Her zaman güncel sözlük referansını kullan
                                _threadFrameCounts.AddOrUpdate(data.ThreadID, 1, (_, count) => count + 1);
                            }
                        }
                    };

                    _session.Source.Process();
                }
            }
            catch (UnauthorizedAccessException)
            {
                CurrentFps = -1;
            }
            catch
            {
                CurrentFps = -1;
            }
        }

        private void CalculateFpsLoop()
        {
            var sw = new Stopwatch();

            while (_isRunning)
            {
                IntPtr hwnd = Win32Api.GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                {
                    Win32Api.GetWindowThreadProcessId(hwnd, out uint pid);
                    _activePid = (int)pid;
                }

                sw.Restart();
                // Tam olarak 1 saniye bekle
                Thread.Sleep(1000);
                sw.Stop();

                // Atomik olarak sözlüğü yenisiyle değiştir (Kayıp frame olmasını engeller)
                var currentCounts = Interlocked.Exchange(ref _threadFrameCounts, new ConcurrentDictionary<int, int>());

                int maxFrames = 0;
                foreach (var count in currentCounts.Values)
                {
                    if (count > maxFrames)
                        maxFrames = count;
                }

                // Saniyede bir net sıfırlanan, saf FPS (Filtresiz)
                int rawFps = (int)Math.Round(maxFrames / sw.Elapsed.TotalSeconds);

                // Stabilizasyon (Dalgalanmayı Önleme)
                if (CurrentFps <= 0)
                {
                    CurrentFps = rawFps;
                }
                else
                {
                    // %70 anlık, %30 önceki frame (hızlı tepki verir ama ani sıçramaları törpüler)
                    CurrentFps = (int)Math.Round(CurrentFps * 0.3 + rawFps * 0.7);
                }
            }
        }

        public void Dispose()
        {
            _isRunning = false;

            try
            {
                _session?.Source.StopProcessing();
                _session?.Dispose();
            }
            catch { }
        }
    }
}
