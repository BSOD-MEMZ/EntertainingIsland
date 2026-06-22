using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using EntertainingIsland.Models;
using EntertainingIsland.Services;

namespace EntertainingIsland.Views.Components;

[ComponentInfo(
    "2DABAE1D-4BEC-4C90-9258-B8A4D8B519F3",
    "体育赛事",
    "\uF02B",
    "显示体育比赛比分与排名，支持详细/简洁两种模式。"
)]
public partial class SportsComponent : ComponentBase<SportsSettings>
{
    private System.Timers.Timer? _flipTimer;
    private SportsService? _service;

    // 活动实例（供 Automation Actions 引用）
    private static SportsComponent? _activeInstance;

    public SportsComponent()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _activeInstance = this;

        _service = new SportsService(Settings);

        // 服务状态变化时刷新显示
        _service.PropertyChanged += (_, a) =>
        {
            if (a.PropertyName is nameof(SportsService.IsLoading) or nameof(SportsService.Count))
                Dispatcher.UIThread.Post(UpdateDisplay);
        };

        // 安全键隐藏
        var state = IAppHost.TryGetService<EntertainmentState>();
        if (state != null)
            state.PropertyChanged += (_, a) =>
            {
                if (a.PropertyName == nameof(EntertainmentState.IsHidden))
                    IsVisible = !state.IsHidden;
            };

        // 自动轮播
        _flipTimer = new System.Timers.Timer(Math.Max(2, Settings.FlipIntervalSeconds) * 1000);
        _flipTimer.Elapsed += (_, _) => Dispatcher.UIThread.InvokeAsync(() => Flip(+1));
        _flipTimer.Start();

        // 设置变更
        Settings.PropertyChanged += (s, a) =>
        {
            var name = a.PropertyName;
            if (name == nameof(SportsSettings.FlipIntervalSeconds) && _flipTimer != null)
                _flipTimer.Interval = Math.Max(2, Settings.FlipIntervalSeconds) * 1000;
            if (name == nameof(SportsSettings.DetailedMode))
                UpdateDisplay();
        };

        UpdateDisplay();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (_activeInstance == this) _activeInstance = null;
        _flipTimer?.Stop(); _flipTimer?.Dispose(); _flipTimer = null;
        base.OnDetachedFromVisualTree(e);
    }

    public void Flip(int direction)
    {
        if (_service == null || _service.Count <= 1) return;
        _service.Flip(direction);
        UpdateDisplay();
    }

    public void ToggleMode()
    {
        Settings.DetailedMode = !Settings.DetailedMode;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_service == null)
        {
            ScoreContent.Content = new TextBlock { Text = "", FontSize = 13 };
            PageLabel.Text = "";
            return;
        }

        // 加载中
        if (_service.IsLoading)
        {
            ScoreContent.Content = new TextBlock
            {
                Text = "加载中…",
                FontSize = 13, Opacity = 0.5
            };
            PageLabel.Text = "";
            return;
        }

        // 无数据
        if (_service.Count == 0)
        {
            ScoreContent.Content = new TextBlock
            {
                Text = "暂无赛事数据",
                FontSize = 13, Opacity = 0.6
            };
            PageLabel.Text = "";
            return;
        }

        var match = _service.GetCurrent();
        var league = string.IsNullOrEmpty(match?.League) ? "" : match!.League;
        var status = string.IsNullOrEmpty(match?.Status) ? "" : match!.Status;

        var text = Settings.DetailedMode
            ? _service.GetDetailedText()
            : _service.GetSimpleText();

        ScoreContent.Content = new TextBlock
        {
            Text = text,
            FontSize = Settings.DetailedMode ? 13 : 18,
            FontWeight = Settings.DetailedMode ? FontWeight.Normal : FontWeight.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        // 副标题：联赛名 + 状态（多页时才显示翻页位置）
        var subParts = new List<string>();
        if (!string.IsNullOrEmpty(league)) subParts.Add(league);
        if (!string.IsNullOrEmpty(status)) subParts.Add(status);
        if (_service.Count > 1)
            subParts.Add($"{_service.CurrentIndex + 1}/{_service.Count}");
        PageLabel.Text = string.Join("  ·  ", subParts);
    }
}
