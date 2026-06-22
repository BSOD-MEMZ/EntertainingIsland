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

    /// <summary>阅读进度（内部使用，自动保存）</summary>
    [ObservableProperty]
    private int _savedPosition;
}
