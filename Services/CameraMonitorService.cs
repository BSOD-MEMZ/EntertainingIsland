using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Timers;
using Avalonia.Threading;
using ClassIsland.Shared;
using EntertainingIsland.Models;
using Microsoft.Win32;
using Timer = System.Timers.Timer;

namespace EntertainingIsland.Services;

/// <summary>
/// 跨平台摄像头安全检测服务。
/// Windows: 轮询注册表和进程判断摄像头是否被占用。
/// Linux: 检查 /dev/video* 设备是否被占用（通过 open() + EBUSY 检测）。
/// </summary>
public class CameraMonitorService : INotifyPropertyChanged, IDisposable
{
    private const string WebcamRegPath =
        @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam";

    private CameraMonitorSettings? _settings;
    private Timer? _pollTimer;
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

        if (_enabled)
            StartPolling();

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
                if (_enabled) StartPolling();
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
    }

    private void StopPolling()
    {
        _pollTimer?.Stop();
        _pollTimer?.Dispose();
        _pollTimer = null;
    }

    private void OnPollTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_settings == null || !_enabled) return;

        try
        {
            bool cameraInUse;

            if (OperatingSystem.IsWindows())
                cameraInUse = IsCameraInUseWindows();
            else if (OperatingSystem.IsLinux())
                cameraInUse = IsCameraInUseLinux();
            else
                return;

            IsCameraInUse = cameraInUse;
        }
        catch (Exception)
        {
        }
    }

    // ==================== Windows 检测 ====================

    [SupportedOSPlatform("windows")]
    private static bool IsCameraInUseWindows()
    {
        return IsCapabilityInUse(WebcamRegPath) || IsCameraProcessRunning();
    }

    [SupportedOSPlatform("windows")]
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

    [SupportedOSPlatform("windows")]
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

    // ==================== Linux 检测 ====================

    // libc open/close
    private const string Libc = "libc.so.6";
    private const int O_RDWR = 2;
    private const int O_NONBLOCK = 0x800;  // 2048 on most Linux
    private const int EBUSY = 16;

    [SupportedOSPlatform("linux")]
    [DllImport(Libc, SetLastError = true)]
    private static extern int open(string pathname, int flags);

    [SupportedOSPlatform("linux")]
    [DllImport(Libc)]
    private static extern int close(int fd);

    [SupportedOSPlatform("linux")]
    private static bool IsCameraInUseLinux()
    {
        try
        {
            for (int i = 0; i < 10; i++)
            {
                var path = $"/dev/video{i}";
                if (!File.Exists(path)) continue;

                // 以非阻塞读写模式尝试打开设备
                int fd = open(path, O_RDWR | O_NONBLOCK);
                if (fd >= 0)
                {
                    // 成功打开 → 设备存在且未被占用
                    close(fd);
                    continue;
                }

                // 打开失败 → 检查是否因为设备正被占用 (EBUSY)
                int errno = Marshal.GetLastPInvokeError();
                if (errno == EBUSY)
                    return true;
            }
        }
        catch { }
        return false;
    }

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
