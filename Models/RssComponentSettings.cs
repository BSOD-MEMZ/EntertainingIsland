using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EntertainingIsland.Models;

/// <summary>
/// RSS 新闻组件设置
/// </summary>
public partial class RssComponentSettings : ObservableObject
{
    [ObservableProperty]
    private string _rssFeedUrl = "https://www.zhihu.com/rss";

    [ObservableProperty]
    private int _rssDisplayCount = 1;

    [ObservableProperty]
    private int _rssFlipIntervalSeconds = 8;

    [ObservableProperty]
    private int _rssMaxTitleLength = 50;
}

/// <summary>
/// RSS 预设源
/// </summary>
public static class RssPresets
{
    public static ReadOnlyCollection<(string Name, string Url)> Feeds { get; } = new(
        new List<(string, string)>
        {
            ("少数派", "https://sspai.com/feed"),
            ("V2EX 最新", "https://www.v2ex.com/feed/tab/tech.xml"),
            ("Hacker News", "https://news.ycombinator.com/rss"),
            ("36氪", "https://36kr.com/feed"),
            ("TechCrunch", "http://feeds.feedburner.com/TechCrunch/"),
            ("The Verge", "https://www.theverge.com/rss/index.xml"),
            ("Ars Technica", "http://feeds.arstechnica.com/arstechnica/index/"),
        }
    );
}
