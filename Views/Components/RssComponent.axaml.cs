using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;
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
    "753ADD86-8B3D-4A56-87DC-8200810C4E49",
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

    // 活动实例（供 Automation Actions 引用）
    private static RssComponent? _activeInstance;

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
        };
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (_activeInstance == this) _activeInstance = null;
        _flipTimer?.Stop(); _flipTimer?.Dispose(); _flipTimer = null;
        base.OnDetachedFromVisualTree(e);
    }

    private async void LoadFeed()
    {
        var url = Settings.RssFeedUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            ShowPlaceholder("加载失败");
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
                ShowPlaceholder("加载失败");
                return;
            }

            _currentIndex = 0;
            UpdateDisplay();
        }
        catch (Exception)
        {
            ShowPlaceholder("加载失败");
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
}
