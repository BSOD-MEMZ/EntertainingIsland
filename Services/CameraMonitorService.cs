using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Timers;
using Avalonia.Threading;
using ClassIsland.Shared;
using EntertainingIsland.Models;
using Microsoft.Win32;
using Timer = System.Timers.Timer;

namespace EntertainingIsland.Services;

/// <summary>
/// 摄像头安全检测服务 — 轮询注册表和进程判断摄像头是否被占用，
/// 通过 INotifyPropertyChanged 通知 UI 组件更新绿点指示器。
/// 不发送任何通知（与 AntiMonitor 不同，已移除通知逻辑）。
/// </summary>
public class CameraMonitorService : INotifyPropertyChanged, IDisposable
{
    private const string WebcamRegPath =
        @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam";

    private CameraMonitorSettings? _settings;
    private Timer? _pollTimer;
    private int _tickCount;
    private bool _isCameraInUse;
    private bool _enabled;
    private bool _disposed;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>摄像头是否正在被某应用使用</summary>
    public bool IsCameraInUse
    {
        get => _isCameraInUse;
        private set
        {
            if (_isCameraInUse == value) return;
            _isCameraInUse = value;
            OnPropertyChanged();
        }
    }

    /// <summary>监控是否已启用</summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            OnPropertyChanged();
            if (_enabled) StartPolling();
            else StopPolling();
        }
    }

    public CameraMonitorService()
    {
    }

    /// <summary>使用插件设置初始化服务</summary>
    public void Initialize(CameraMonitorSettings settings)
    {
        _settings = settings;
        _enabled = settings.EnableCameraMonitor;
        Log("=== 摄像头监控服务启动 ===");
        Log($"启用={settings.EnableCameraMonitor}, 轮询间隔={settings.PollingIntervalMs}ms");

        if (_enabled)
            StartPolling();

        // 监听设置变更
        settings.PropertyChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_settings == null) return;

        if (e.PropertyName == nameof(CameraMonitorSettings.EnableCameraMonitor))
        {
            Dispatcher.UIThread.Post(() => Enabled = _settings.EnableCameraMonitor);
        }
        else if (e.PropertyName == nameof(CameraMonitorSettings.PollingIntervalMs))
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_enabled) StartPolling(); // 重启定时器以应用新间隔
            });
        }
    }

    private void StartPolling()
    {
        StopPolling();
        var interval = Math.Max(_settings?.PollingIntervalMs ?? 2000, 500);
        _pollTimer = new Timer(interval) { AutoReset = true };
        _pollTimer.Elapsed += OnPollTimerElapsed;
        _pollTimer.Start();
        _tickCount = 0;
        Log($"轮询已启动 (间隔={interval}ms)");
    }

    private void StopPolling()
    {
        _pollTimer?.Stop();
        _pollTimer?.Dispose();
        _pollTimer = null;
        Log("轮询已停止");
    }

    private void OnPollTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_settings == null || !_enabled) return;
        _tickCount++;
        var verbose = _tickCount <= 5 || _tickCount % 30 == 0;

        try
        {
            bool camReg = IsCapabilityInUse(WebcamRegPath);
            bool camProc = IsCameraProcessRunning();
            bool cameraInUse = camReg || camProc;

            if (verbose || cameraInUse != _isCameraInUse)
                Log($"Tick#{_tickCount} | 摄像头: Reg={camReg} Proc={camProc} => {cameraInUse}");

            IsCameraInUse = cameraInUse;
        }
        catch (Exception ex)
        {
            LogError($"异常(Tick#{_tickCount}): {ex.Message}");
        }
    }

    // ================ 摄像头检测：注册表 ================

    private static bool IsCapabilityInUse(string registrySubPath)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(registrySubPath);
            if (key == null) return false;

            foreach (var subFolder in new[] { "NonPackaged", "Packages" })
            {
                using var subKey = key.OpenSubKey(subFolder);
                if (subKey == null) continue;

                foreach (var appName in subKey.GetSubKeyNames())
                {
                    using var appKey = subKey.OpenSubKey(appName);
                    if (appKey == null) continue;

                    long stop = ReadRegInt64(appKey, "LastUsedTimeStop");
                    long start = ReadRegInt64(appKey, "LastUsedTimeStart");
                    if (stop == 0 || (start > 0 && start > stop)) return true;
                }
            }
        }
        catch { }
        return false;
    }

    private static long ReadRegInt64(RegistryKey key, string name)
    {
        var val = key.GetValue(name);
        if (val is long l) return l;
        if (val is int i) return i;
        if (val is uint u) return u;
        return 0;
    }

    // ================ 摄像头检测：进程 ================

    private static bool IsCameraProcessRunning()
    {
        try
        {
            foreach (var proc in Process.GetProcessesByName("WindowsCamera"))
            {
                proc.Dispose();
                return true;
            }
        }
        catch { }
        return false;
    }

    // ================ 日志 ================

    private void Log(string msg) => Logger.Info($"[摄像头] {msg}");
    private void LogError(string msg) => Logger.Error($"[摄像头] {msg}");

    // ================ INotifyPropertyChanged ================

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Dispatcher.UIThread.Post(() =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }

    // ================ IDisposable ================

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopPolling();
        if (_settings != null)
            _settings.PropertyChanged -= OnSettingsChanged;
    }
}
