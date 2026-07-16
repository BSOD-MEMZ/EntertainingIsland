using CommunityToolkit.Mvvm.ComponentModel;

namespace EntertainingIsland.Models;

/// <summary>
/// 功能开关。用户在 EntertainingIsland 设置中可以自由启用/禁用各组件、提醒提供方和自动化注册项。
/// 禁用后，对应的功能不会在 ClassIsland 的任何界面中出现。
/// </summary>
public partial class FeatureToggles : ObservableObject
{
    // ===== 完整功能模块（控制整个模块的注册，包括设置页、服务、浮窗等） =====
    [ObservableProperty]
    private bool _luckyPicker = true;

    // ===== 组件 =====
    [ObservableProperty]
    private bool _novelReader = true;

    [ObservableProperty]
    private bool _catchphrase = true;

    [ObservableProperty]
    private bool _avatarClassSchedule = true;

    [ObservableProperty]
    private bool _rssNews = true;

    [ObservableProperty]
    private bool _sports = true;

    [ObservableProperty]
    private bool _cameraStatus = true;

    [ObservableProperty]
    private bool _fortune = true;

    // ===== 提醒提供方 =====
    [ObservableProperty]
    private bool _alertHotkey = true;

    [ObservableProperty]
    private bool _classEndingReminder = true;

    [ObservableProperty]
    private bool _luckyPickerNotifier = true;

    // ===== 自动化行动 =====
    [ObservableProperty]
    private bool _toggleVisibility = true;

    [ObservableProperty]
    private bool _showAllComponents = true;

    [ObservableProperty]
    private bool _hideAllComponents = true;

    [ObservableProperty]
    private bool _novelActions = true;

    [ObservableProperty]
    private bool _rssActions = true;

    [ObservableProperty]
    private bool _catchphraseClearAction = true;

    [ObservableProperty]
    private bool _cameraActions = true;

    // ===== 自动化触发器 =====
    [ObservableProperty]
    private bool _cameraTriggers = true;
}
