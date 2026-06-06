using CommunityToolkit.Mvvm.ComponentModel;

namespace EntertainingIsland.Models;

/// <summary>
/// 窗外有老师警报提醒设置（用于提醒提供方的独立设置）
/// </summary>
public partial class AlertHotkeySettings : ObservableRecipient
{
    /// <summary>是否启用警报功能</summary>
    [ObservableProperty]
    private bool _enableTeacherAlert = true;

    /// <summary>触发警报热键</summary>
    [ObservableProperty]
    private HotkeyConfig _alertHotkey = new() { Ctrl = true, Shift = true, Key = "J" };

    /// <summary>警报消息内容</summary>
    [ObservableProperty]
    private string _alertMessage = "窗外有老师！";

    /// <summary>通知持续秒数</summary>
    [ObservableProperty]
    private int _alertDurationSeconds = 10;

    /// <summary>是否启用强调效果（使用 ClassIsland 主题色）</summary>
    [ObservableProperty]
    private bool _enableEmphasisEffect = true;

    /// <summary>是否启用语音播报</summary>
    [ObservableProperty]
    private bool _enableSpeech;

    /// <summary>是否启用提示音效</summary>
    [ObservableProperty]
    private bool _enableSound;

    /// <summary>通知时是否置顶主窗口</summary>
    [ObservableProperty]
    private bool _enableTopmost;
}
