using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;
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
    "BBCDEEFF-2222-3333-4444-555566667777",
    "RSS 新闻",
    "\uEEAD",
    "显示 RSS 新闻标题，支持自定义源、快捷键翻页和打开链接"
)]
public partial class RssComponent : ComponentBase<RssComponentSettings>
{
    private System.Timers.Timer? _flipTimer;
    private List<RssItem> _items = new();
    private string _sourceName = "";
    private int _currentIndex;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    // 全局热键
    private static RssComponent? _activeInstance;
    private const int WM_HOTKEY = 0x0312;
    private const int HK_RSS_PAGEUP = 8010;
    private const int HK_RSS_PAGEDOWN = 8011;
    private const int HK_RSS_OPEN = 8012;
    private bool _hotkeysRegistered;
    private bool _hooked;
    private System.Timers.Timer? _hotkeyRetry;

    private record RssItem(string Title, string Url);

    public RssComponent()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _activeInstance = this;

        // 全局安全键隐藏
        var state = IAppHost.TryGetService<EntertainmentState>();
        if (state != null)
        {
            state.PropertyChanged += (s, a) =>
            {
                var name = a.PropertyName;
                if (name == nameof(EntertainmentState.IsHidden))
                    IsVisible = !state.IsHidden;
                else if (name == nameof(EntertainmentState.RssNextRequest))
                    Flip(+1);
                else if (name == nameof(EntertainmentState.RssPrevRequest))
                    Flip(-1);
            };
        }

        LoadFeed();

        _flipTimer = new System.Timers.Timer(Math.Max(2, Settings.RssFlipIntervalSeconds) * 1000);
        _flipTimer.Elapsed += (_, _) => Dispatcher.UIThread.InvokeAsync(() => Flip(+1));
        _flipTimer.Start();

        Settings.PropertyChanged += (s, a) =>
        {
            var name = a.PropertyName;
            if (name == nameof(RssComponentSettings.RssFeedUrl))
                LoadFeed();
            if (name == nameof(RssComponentSettings.RssFlipIntervalSeconds) && _flipTimer != null)
                _flipTimer.Interval = Math.Max(2, Settings.RssFlipIntervalSeconds) * 1000;
            if (name is nameof(RssComponentSettings.PageUpHotkey)
                or nameof(RssComponentSettings.PageDownHotkey)
                or nameof(RssComponentSettings.OpenHotkey))
            {
                Dispatcher.UIThread.Post(() => { UnregisterHotkeys(); TryRegisterHotkeys(); });
            }
        };

        Settings.PageUpHotkey.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => { UnregisterHotkeys(); TryRegisterHotkeys(); });
        Settings.PageDownHotkey.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => { UnregisterHotkeys(); TryRegisterHotkeys(); });
        Settings.OpenHotkey.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => { UnregisterHotkeys(); TryRegisterHotkeys(); });

        // 启动热键重试定时器（窗口句柄未就绪时自动重试）
        _hotkeyRetry = new System.Timers.Timer(2000);
        _hotkeyRetry.Elapsed += (_, _) => Dispatcher.UIThread.Post(TryRegisterHotkeys);
        _hotkeyRetry.AutoReset = true;
        _hotkeyRetry.Start();

        Dispatcher.UIThread.Post(TryRegisterHotkeys);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        UnregisterHotkeys();
        _hotkeyRetry?.Stop();
        _hotkeyRetry?.Dispose();
        _hotkeyRetry = null;
        if (_activeInstance == this) _activeInstance = null;
        _flipTimer?.Stop(); _flipTimer?.Dispose(); _flipTimer = null;
        base.OnDetachedFromVisualTree(e);
    }

    private async void LoadFeed()
    {
        var url = Settings.RssFeedUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            ShowPlaceholder("请先在组件设置中配置 RSS 源");
            return;
        }

        try
        {
            var xml = await _http.GetStringAsync(url);
            var doc = XDocument.Parse(xml);

            // RSS 2.0
            var channel = doc.Root?.Element("channel");
            if (channel != null)
            {
                _sourceName = channel.Element("title")?.Value ?? "RSS";
                _items = channel.Elements("item")
                    .Select(e => new RssItem(
                        e.Element("title")?.Value ?? "",
                        e.Element("link")?.Value ?? ""))
                    .Where(i => !string.IsNullOrWhiteSpace(i.Title))
                    .ToList();
            }
            // Atom
            else if (doc.Root?.Name.LocalName == "feed")
            {
                var ns = doc.Root.Name.NamespaceName;
                _sourceName = doc.Root.Element(XName.Get("title", ns))?.Value ?? "Atom";
                _items = doc.Root.Elements(XName.Get("entry", ns))
                    .Select(e => new RssItem(
                        e.Element(XName.Get("title", ns))?.Value ?? "",
                        e.Elements(XName.Get("link", ns))
                            .FirstOrDefault(l => l.Attribute("href") != null)?
                            .Attribute("href")?.Value ?? ""))
                    .Where(i => !string.IsNullOrWhiteSpace(i.Title))
                    .ToList();
            }

            if (_items.Count == 0)
            {
                ShowPlaceholder("未获取到新闻条目");
                return;
            }

            _currentIndex = 0;
            UpdateDisplay();
        }
        catch (Exception ex)
        {
            ShowPlaceholder($"RSS 加载失败: {ex.Message}");
        }
    }

    public void Flip(int direction)
    {
        if (_items.Count == 0) return;
        _currentIndex = direction > 0
            ? (_currentIndex + 1) % _items.Count
            : (_currentIndex - 1 + _items.Count) % _items.Count;
        UpdateDisplay();
    }

    private void OpenCurrentUrl()
    {
        if (_items.Count == 0 || _currentIndex >= _items.Count) return;
        var url = _items[_currentIndex].Url;
        if (!string.IsNullOrWhiteSpace(url))
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        }
    }

    private void UpdateDisplay()
    {
        if (_items.Count == 0) return;
        var item = _items[_currentIndex];
        var title = item.Title;
        var maxLen = Math.Max(10, Settings.RssMaxTitleLength);
        if (title.Length > maxLen) title = title[..maxLen] + "...";

        TitleContent.Content = new TextBlock
        {
            Text = title,
            FontSize = 14,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        SourceLabel.Text = $"{_sourceName}  ·  {_currentIndex + 1}/{_items.Count}";
    }

    private void ShowPlaceholder(string msg)
    {
        _items.Clear();
        TitleContent.Content = new TextBlock { Text = msg, FontSize = 13, Opacity = 0.6 };
        SourceLabel.Text = "";
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

        var hkUp = Settings.PageUpHotkey ?? new() { Ctrl = true, Key = "Left" };
        var hkDown = Settings.PageDownHotkey ?? new() { Ctrl = true, Key = "Right" };
        var hkOpen = Settings.OpenHotkey ?? new() { Ctrl = true, Shift = true, Key = "O" };

        bool anyOk = false;
        if (NativeMethods.RegisterHotKey(hwnd, HK_RSS_PAGEUP, hkUp.GetModifiers(), HotkeyHelper.VkFromKey(hkUp.Key)))
            anyOk = true;
        if (NativeMethods.RegisterHotKey(hwnd, HK_RSS_PAGEDOWN, hkDown.GetModifiers(), HotkeyHelper.VkFromKey(hkDown.Key)))
            anyOk = true;
        if (NativeMethods.RegisterHotKey(hwnd, HK_RSS_OPEN, hkOpen.GetModifiers(), HotkeyHelper.VkFromKey(hkOpen.Key)))
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
            NativeMethods.UnregisterHotKey(ph.Handle, HK_RSS_PAGEUP);
            NativeMethods.UnregisterHotKey(ph.Handle, HK_RSS_PAGEDOWN);
            NativeMethods.UnregisterHotKey(ph.Handle, HK_RSS_OPEN);
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
            if (id == HK_RSS_PAGEUP) _activeInstance.Flip(-1);
            else if (id == HK_RSS_PAGEDOWN) _activeInstance.Flip(+1);
            else if (id == HK_RSS_OPEN) _activeInstance.OpenCurrentUrl();
        });
        handled = true;
        return IntPtr.Zero;
    }

}
