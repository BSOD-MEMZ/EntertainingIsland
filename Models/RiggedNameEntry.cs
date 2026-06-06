using CommunityToolkit.Mvvm.ComponentModel;

namespace EntertainingIsland.Models;

/// <summary>
/// 隐藏的爆率条目 — 仅在 Ctrl+Shift+Alt+C 后可见
/// </summary>
public partial class RiggedNameEntry : ObservableObject
{
    /// <summary>姓名</summary>
    [ObservableProperty]
    private string _name = "";

    /// <summary>权重（1.0=正常，>1.0 更容易被抽中，<1.0 更难被抽中）</summary>
    [ObservableProperty]
    private double _weight = 1.0;

    /// <summary>保底次数（0=不保底，N=连续N次未被抽中后，第N+1次必中）</summary>
    [ObservableProperty]
    private int _pityThreshold;

    /// <summary>当前连续未抽中次数（运行时追踪）</summary>
    [ObservableProperty]
    private int _currentMissStreak;
}
