using CommunityToolkit.Mvvm.ComponentModel;

namespace EntertainingIsland.Services;

/// <summary>
/// 全局娱乐状态 - 控制组件显隐与跨组件操作信号。
/// </summary>
public partial class EntertainmentState : ObservableObject
{
    /// <summary>全局安全键：隐藏/显示所有娱乐组件</summary>
    [ObservableProperty]
    private bool _isHidden;

    // ===== 小说阅读器操作信号 =====

    /// <summary>小说重新开始请求，翻转一次即触发</summary>
    [ObservableProperty]
    private bool _novelRestartRequest;

    /// <summary>小说暂停请求，翻转一次即触发</summary>
    [ObservableProperty]
    private bool _novelPauseRequest;

    /// <summary>小说继续请求，翻转一次即触发</summary>
    [ObservableProperty]
    private bool _novelResumeRequest;

    /// <summary>小说下一页请求，翻转一次即触发</summary>
    [ObservableProperty]
    private bool _novelNextPageRequest;

    /// <summary>小说上一页请求，翻转一次即触发</summary>
    [ObservableProperty]
    private bool _novelPrevPageRequest;

    // ===== RSS 新闻操作信号 =====

    /// <summary>RSS 下一条请求，翻转一次即触发</summary>
    [ObservableProperty]
    private bool _rssNextRequest;

    /// <summary>RSS 上一条请求，翻转一次即触发</summary>
    [ObservableProperty]
    private bool _rssPrevRequest;

    // ===== 口头禅操作信号 =====

    /// <summary>口头禅清空请求，翻转一次即触发</summary>
    [ObservableProperty]
    private bool _catchphraseClearRequest;
}
