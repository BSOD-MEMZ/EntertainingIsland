using CommunityToolkit.Mvvm.ComponentModel;

namespace EntertainingIsland.Models;

/// <summary>
/// 摄像头监控设置 — 仅摄像头，不包含麦克风。
/// </summary>
public partial class CameraMonitorSettings : ObservableObject
{
    private bool _enableCameraMonitor = true;
    private int _pollingIntervalMs = 2000;

    /// <summary>是否启用摄像头监控</summary>
    public bool EnableCameraMonitor
    {
        get => _enableCameraMonitor;
        set => SetProperty(ref _enableCameraMonitor, value);
    }

    /// <summary>轮询间隔（毫秒），最小 500</summary>
    public int PollingIntervalMs
    {
        get => _pollingIntervalMs;
        set => SetProperty(ref _pollingIntervalMs, Math.Max(value, 500));
    }
}
