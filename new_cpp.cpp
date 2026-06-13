#pragma comment(lib, "tdh.lib")
#pragma comment(lib, "advapi32.lib")

#include <windows.h>
#include <iostream>
#include <thread>
#include <atomic>
#include <string>
#include <evntrace.h>
#include <evntcons.h>
#include <tdh.h>

std::atomic<bool> g_etwRunning(true);
std::atomic<DWORD> g_targetPid(0);
std::atomic<int> g_gameFps(0);
double g_qpcFreq = 0.0;
TRACEHANDLE g_etwSession = 0;
TRACEHANDLE g_etwTrace = 0;
HANDLE hPipe = INVALID_HANDLE_VALUE;

static const char* ETW_SESSION_NAME = "emx17_Native_Session";

static const GUID DXGI_PROVIDER = { 0xCA11C036, 0x0102, 0x4A2D, { 0xA6, 0xAD, 0xF0, 0x3C, 0xFE, 0xD5, 0xD3, 0xC9 } };
static const GUID D3D9_PROVIDER = { 0x783ACA0A, 0x790E, 0x4D7F, { 0x84, 0x51, 0xAA, 0x85, 0x05, 0x11, 0xC6, 0xB9 } };
static const GUID DXGKRNL_PROVIDER = { 0x802EC45A, 0x1E99, 0x4B83, { 0x99, 0x20, 0x87, 0xC9, 0x82, 0x77, 0xBA, 0x9D } };

static const USHORT DXGKRNL_EVENT_PRESENT_INFO = 0x00B8;
static const USHORT DXGKRNL_EVENT_FLIP_INFO    = 0x00A8;
static const USHORT DXGKRNL_EVENT_BLIT_INFO    = 0x00A6;

static const ULONGLONG DXGKRNL_KEYWORD_PRESENT = 0x8000000;
static const ULONGLONG DXGKRNL_KEYWORD_BASE    = 0x1;

static void WINAPI EtwCallback(PEVENT_RECORD pEvent)
{
    if (!g_etwRunning.load(std::memory_order_relaxed)) return;

    DWORD pid = pEvent->EventHeader.ProcessId;
    DWORD target = g_targetPid.load(std::memory_order_relaxed);
    if (target == 0 || pid != target) return;

    bool isValidPresentEvent = false;
    bool isDxgiEvent = false;
    bool isD3D9Event = false;
    bool isDxgKrnlOnlyEvent = false;
    
    if (memcmp(&pEvent->EventHeader.ProviderId, &DXGI_PROVIDER, sizeof(GUID)) == 0) {
        if (pEvent->EventHeader.EventDescriptor.Id == 42) {
            isValidPresentEvent = true;
            isDxgiEvent = true;
        }
    }
    else if (memcmp(&pEvent->EventHeader.ProviderId, &D3D9_PROVIDER, sizeof(GUID)) == 0) {
        if (pEvent->EventHeader.EventDescriptor.Id == 1) {
            isValidPresentEvent = true;
            isD3D9Event = true;
        }
    }
    else if (memcmp(&pEvent->EventHeader.ProviderId, &DXGKRNL_PROVIDER, sizeof(GUID)) == 0) {
        USHORT eventId = pEvent->EventHeader.EventDescriptor.Id;
        if (eventId == DXGKRNL_EVENT_PRESENT_INFO ||
            eventId == DXGKRNL_EVENT_FLIP_INFO ||
            eventId == DXGKRNL_EVENT_BLIT_INFO) {
            isValidPresentEvent = true;
            isDxgKrnlOnlyEvent = true;
        }
    }

    if (!isValidPresentEvent) return;

    double ts = (double)pEvent->EventHeader.TimeStamp.QuadPart / g_qpcFreq;

    static DWORD s_lastPid   = 0;
    static double s_startTs  = 0;
    static int   s_dxgiCount = 0;
    static int   s_d3d9Count = 0;
    static int   s_dxgKrnlCount = 0;

    if (pid != s_lastPid) { 
        s_lastPid = pid;
        s_dxgiCount = 0;
        s_d3d9Count = 0;
        s_dxgKrnlCount = 0;
        s_startTs = ts;
    }

    if (isDxgiEvent) s_dxgiCount++;
    if (isD3D9Event) s_d3d9Count++;
    if (isDxgKrnlOnlyEvent) s_dxgKrnlCount++;
    
    double elapsed = ts - s_startTs;
    if (elapsed >= 1.0) {
        int frameCount = 0;
        if (s_d3d9Count > 0) {
            frameCount = s_d3d9Count;
        } else if (s_dxgiCount > 0) {
            frameCount = s_dxgiCount;
        } else if (s_dxgKrnlCount > 0) {
            float potentialFps = (float)s_dxgKrnlCount / (float)elapsed;
            if (potentialFps >= 20.0f) {
                frameCount = s_dxgKrnlCount;
            }
        }
        
        if (frameCount > 0) {
            g_gameFps.store((int)((float)frameCount / (float)elapsed), std::memory_order_relaxed);
        } else {
            g_gameFps.store(0, std::memory_order_relaxed);
        }
        
        s_dxgiCount = 0;
        s_d3d9Count = 0;
        s_dxgKrnlCount = 0;
        s_startTs = ts;
    }
}

static void StopEtwSession()
{
    if (!g_etwRunning.load()) return;
    g_etwRunning.store(false);

    if (g_etwTrace != 0 && g_etwTrace != (TRACEHANDLE)INVALID_HANDLE_VALUE) {
        CloseTrace(g_etwTrace);
        g_etwTrace = 0;
    }

    struct { EVENT_TRACE_PROPERTIES p; char name[256]; } buf;
    ZeroMemory(&buf, sizeof(buf));
    buf.p.Wnode.BufferSize = sizeof(buf);
    buf.p.LoggerNameOffset = offsetof(decltype(buf), name);
    ControlTraceA(g_etwSession, ETW_SESSION_NAME, &buf.p, EVENT_TRACE_CONTROL_STOP);
    g_etwSession = 0;

    g_gameFps.store(0);
}

static bool StartEtwSession()
{
    LARGE_INTEGER freq;
    QueryPerformanceFrequency(&freq);
    g_qpcFreq = (double)freq.QuadPart;

    struct { EVENT_TRACE_PROPERTIES p; char name[256]; } buf;

    ZeroMemory(&buf, sizeof(buf));
    buf.p.Wnode.BufferSize   = sizeof(buf);
    buf.p.LoggerNameOffset   = offsetof(decltype(buf), name);
    ControlTraceA(0, ETW_SESSION_NAME, &buf.p, EVENT_TRACE_CONTROL_STOP);

    ZeroMemory(&buf, sizeof(buf));
    buf.p.Wnode.BufferSize    = sizeof(buf);
    buf.p.Wnode.Flags         = WNODE_FLAG_TRACED_GUID;
    buf.p.Wnode.ClientContext = 1;
    buf.p.LogFileMode         = EVENT_TRACE_REAL_TIME_MODE;
    buf.p.LoggerNameOffset    = offsetof(decltype(buf), name);

    ULONG rc = StartTraceA(&g_etwSession, ETW_SESSION_NAME, &buf.p);
    if (rc != ERROR_SUCCESS) return false;

    rc = EnableTraceEx2(g_etwSession, &DXGI_PROVIDER, EVENT_CONTROL_CODE_ENABLE_PROVIDER, TRACE_LEVEL_INFORMATION, 0, 0, 0, nullptr);
    if (rc != ERROR_SUCCESS) {
        StopEtwSession();
        return false;
    }

    rc = EnableTraceEx2(g_etwSession, &D3D9_PROVIDER, EVENT_CONTROL_CODE_ENABLE_PROVIDER, TRACE_LEVEL_INFORMATION, 0, 0, 0, nullptr);
    rc = EnableTraceEx2(g_etwSession, &DXGKRNL_PROVIDER, EVENT_CONTROL_CODE_ENABLE_PROVIDER, TRACE_LEVEL_INFORMATION, DXGKRNL_KEYWORD_PRESENT | DXGKRNL_KEYWORD_BASE, 0, 0, nullptr);

    EVENT_TRACE_LOGFILEA logFile = {};
    logFile.LoggerName          = const_cast<LPSTR>(ETW_SESSION_NAME);
    logFile.ProcessTraceMode    = PROCESS_TRACE_MODE_REAL_TIME | PROCESS_TRACE_MODE_EVENT_RECORD;
    logFile.EventRecordCallback = EtwCallback;

    g_etwTrace = OpenTraceA(&logFile);
    if (g_etwTrace == (TRACEHANDLE)INVALID_HANDLE_VALUE) {
        StopEtwSession();
        return false;
    }

    g_etwRunning.store(true);
    return true;
}

void CalculationLoop() {
    hPipe = CreateNamedPipeA(
        "\\\\.\\pipe\\emx17_FPS_Pipe",
        PIPE_ACCESS_OUTBOUND,
        PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT,
        1, 1024, 1024, 0, NULL
    );

    if (hPipe == INVALID_HANDLE_VALUE) return;
    
    ConnectNamedPipe(hPipe, NULL);

    while (g_etwRunning.load()) {
        std::this_thread::sleep_for(std::chrono::milliseconds(250));

        int current_fps = g_gameFps.load();

        if (hPipe != INVALID_HANDLE_VALUE) {
            DWORD bytesWritten;
            std::string msg = std::to_string(current_fps) + "\n";
            if (!WriteFile(hPipe, msg.c_str(), msg.length(), &bytesWritten, NULL)) {
                StopEtwSession();
                break;
            }
        }
    }
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    if (__argc > 1) {
        g_targetPid.store(std::stoi(__argv[1]));
    }

    if (!StartEtwSession()) return 1;

    std::thread calc_thread(CalculationLoop);

    TRACEHANDLE h = g_etwTrace;
    ProcessTrace(&h, 1, nullptr, nullptr);

    StopEtwSession();
    if (calc_thread.joinable()) calc_thread.join();

    if (hPipe != INVALID_HANDLE_VALUE) CloseHandle(hPipe);
    return 0;
}

