using CommunityToolkit.Mvvm.ComponentModel;

namespace EntertainingIsland.Models;

/// <summary>
/// 小说阅读器组件设置
/// </summary>
public partial class NovelReaderSettings : ObservableObject
{
    [ObservableProperty]
    private string _novelFilePath = "";

    [ObservableProperty]
    private int _novelCharsPerPage = 20;

    [ObservableProperty]
    private int _novelFlipIntervalSeconds = 5;

    /// <summary>上翻快捷键</summary>
    [ObservableProperty]
    private HotkeyConfig _pageUpHotkey = new() { Ctrl = true, Shift = true, Key = "Up" };

    /// <summary>下翻快捷键</summary>
    [ObservableProperty]
    private HotkeyConfig _pageDownHotkey = new() { Ctrl = true, Shift = true, Key = "Down" };

    /// <summary>暂停/继续快捷键</summary>
    [ObservableProperty]
    private HotkeyConfig _pauseHotkey = new() { Ctrl = true, Shift = true, Key = "Space" };

    /// <summary>阅读进度（内部使用，自动保存）</summary>
    [ObservableProperty]
    private int _savedPosition;
}
