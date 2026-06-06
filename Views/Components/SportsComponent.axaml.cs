using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Win32;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using EntertainingIsland.Helpers;
using EntertainingIsland.Models;
using EntertainingIsland.Services;

namespace EntertainingIsland.Views.Components;

[ComponentInfo(
    "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
    "体育赛事",
    "\uE7A5",
    "显示体育比赛比分与排名，支持详细/简洁两种模式。"
)]
public partial class SportsComponent : ComponentBase<SportsSettings>
{
    private System.Timers.Timer? _flipTimer;
    private SportsService? _service;

    // 全局热键
    private static SportsComponent? _activeInstance;
    private const int WM_HOTKEY = 0x0312;
    private const int HK_PAGEUP = 8020;
    private const int HK_PAGEDOWN = 8021;
    private const int HK_TOGGLE_MODE = 8022;
    private bool _hotkeysRegistered;
    private bool _hooked;
    private System.Timers.Timer? _hotkeyRetry;

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
                Dispatcher.UIThread.Post(() => { UnregisterHotkeys(); TryRegisterHotkeys(); });
            }
        };

        Settings.PageUpHotkey.PropertyChanged += (_, _) =>
            Dispatcher.UIThread.Post(() => { UnregisterHotkeys(); TryRegisterHotkeys(); });
        Settings.PageDownHotkey.PropertyChanged += (_, _) =>
            Dispatcher.UIThread.Post(() => { UnregisterHotkeys(); TryRegisterHotkeys(); });
        Settings.ToggleModeHotkey.PropertyChanged += (_, _) =>
            Dispatcher.UIThread.Post(() => { UnregisterHotkeys(); TryRegisterHotkeys(); });

        // 热键重试
        _hotkeyRetry = new System.Timers.Timer(2000);
        _hotkeyRetry.Elapsed += (_, _) => Dispatcher.UIThread.Post(TryRegisterHotkeys);
        _hotkeyRetry.AutoReset = true;
        _hotkeyRetry.Start();

        Dispatcher.UIThread.Post(TryRegisterHotkeys);
        UpdateDisplay();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        UnregisterHotkeys();
        _hotkeyRetry?.Stop(); _hotkeyRetry?.Dispose(); _hotkeyRetry = null;
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

    // ===== 热键 =====
    private void TryRegisterHotkeys()
    {
        if (_hotkeysRegistered) { _hotkeyRetry?.Stop(); return; }
        var w = AppBase.Current.MainWindow ?? AppBase.Current.GetRootWindow();
        if (w == null) return;
        var ph = w.TryGetPlatformHandle();
        if (ph == null) return;
        var hwnd = ph.Handle;

        bool anyOk = false;
        if (NativeMethods.RegisterHotKey(hwnd, HK_PAGEUP,
                Settings.PageUpHotkey.GetModifiers(), HotkeyHelper.VkFromKey(Settings.PageUpHotkey.Key)))
            anyOk = true;
        if (NativeMethods.RegisterHotKey(hwnd, HK_PAGEDOWN,
                Settings.PageDownHotkey.GetModifiers(), HotkeyHelper.VkFromKey(Settings.PageDownHotkey.Key)))
            anyOk = true;
        if (NativeMethods.RegisterHotKey(hwnd, HK_TOGGLE_MODE,
                Settings.ToggleModeHotkey.GetModifiers(), HotkeyHelper.VkFromKey(Settings.ToggleModeHotkey.Key)))
            anyOk = true;

        if (anyOk)
        {
            _hotkeysRegistered = true;
            _hotkeyRetry?.Stop();
            if (!_hooked)
            {
                _hooked = true;
                Win32Properties.AddWndProcHookCallback(w, WndProc);
            }
        }
    }

    private void UnregisterHotkeys()
    {
        var w = AppBase.Current.MainWindow ?? AppBase.Current.GetRootWindow();
        var ph = w?.TryGetPlatformHandle();
        if (ph != null)
        {
            NativeMethods.UnregisterHotKey(ph.Handle, HK_PAGEUP);
            NativeMethods.UnregisterHotKey(ph.Handle, HK_PAGEDOWN);
            NativeMethods.UnregisterHotKey(ph.Handle, HK_TOGGLE_MODE);
        }
        _hotkeysRegistered = false;
    }

    private IntPtr WndProc(IntPtr h, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_HOTKEY) return IntPtr.Zero;
        int id = wParam.ToInt32();
        Dispatcher.UIThread.Post(() =>
        {
            if (_activeInstance == null) return;
            if (id == HK_PAGEUP) _activeInstance.Flip(-1);
            else if (id == HK_PAGEDOWN) _activeInstance.Flip(+1);
            else if (id == HK_TOGGLE_MODE) _activeInstance.ToggleMode();
        });
        handled = true;
        return IntPtr.Zero;
    }
}
