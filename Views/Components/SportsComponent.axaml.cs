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
    "CE50F0A5-A312-466F-8EE4-58EB21A7987A",
    "体育赛事",
    "\uF02B",
    "显示体育比赛比分与排名，支持详细/简洁两种模式。"
)]
public partial class SportsComponent : ComponentBase<SportsSettings>
{
    private System.Timers.Timer? _flipTimer;
    private SportsService? _service;

    // 全局热键
    private static SportsComponent? _activeInstance;
    private const int HK_PAGEUP = 8020;
    private const int HK_PAGEDOWN = 8021;
    private const int HK_TOGGLE_MODE = 8022;
    private HotkeyManager? _hotkeys;

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
            if (name is nameof(SportsSettings.PageUpHotkey)
                or nameof(SportsSettings.PageDownHotkey)
                or nameof(SportsSettings.ToggleModeHotkey))
            {
                _hotkeys?.Refresh();
            }
        };

        Settings.PageUpHotkey.PropertyChanged += (_, _) => _hotkeys?.Refresh();
        Settings.PageDownHotkey.PropertyChanged += (_, _) => _hotkeys?.Refresh();
        Settings.ToggleModeHotkey.PropertyChanged += (_, _) => _hotkeys?.Refresh();

        // 热键管理器
        _hotkeys = new HotkeyManager(OnHotkey);
        _hotkeys.Add(HK_PAGEUP, Settings.PageUpHotkey);
        _hotkeys.Add(HK_PAGEDOWN, Settings.PageDownHotkey);
        _hotkeys.Add(HK_TOGGLE_MODE, Settings.ToggleModeHotkey);
        _hotkeys.Start();

        UpdateDisplay();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _hotkeys?.Dispose();
        _hotkeys = null;
        if (_activeInstance == this) _activeInstance = null;
        _flipTimer?.Stop(); _flipTimer?.Dispose(); _flipTimer = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnHotkey(int id)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_activeInstance == null) return;
            if (id == HK_PAGEUP) _activeInstance.Flip(-1);
            else if (id == HK_PAGEDOWN) _activeInstance.Flip(+1);
            else if (id == HK_TOGGLE_MODE) _activeInstance.ToggleMode();
        });
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
