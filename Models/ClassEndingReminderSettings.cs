using CommunityToolkit.Mvvm.ComponentModel;

namespace EntertainingIsland.Models;

/// <summary>
/// 下课倒计时提醒设置（对齐 ClassIsland 内置"即将上课"提醒设置结构）
/// </summary>
public partial class ClassEndingReminderSettings : ObservableObject
{
    /// <summary>是否启用下课倒计时提醒</summary>
    [ObservableProperty]
    private bool _isEnabled = true;

    /// <summary>下课前多少秒触发提醒（默认 60 秒 = 1 分钟）</summary>
    [ObservableProperty]
    private int _deltaTime = 60;

    /// <summary>提醒遮罩文字</summary>
    [ObservableProperty]
    private string _maskText = "即将下课";

    /// <summary>提醒详细文字（叠加层第二页轮播显示）</summary>
    [ObservableProperty]
    private string _overlayText = "本堂课即将结束，请注意整理笔记。";

    /// <summary>是否显示教师名称</summary>
    [ObservableProperty]
    private bool _showTeacherName;

    /// <summary>是否启用强调效果（使用 ClassIsland 主题色涟漪动画）</summary>
    [ObservableProperty]
    private bool _enableEmphasisEffect = true;

    /// <summary>是否启用语音播报</summary>
    [ObservableProperty]
    private bool _enableSpeech;

    /// <summary>是否启用提示音效</summary>
    [ObservableProperty]
    private bool _enableSound;
}
