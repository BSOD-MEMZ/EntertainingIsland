using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using EntertainingIsland.Models;

namespace EntertainingIsland.Services;

/// <summary>
/// 体育赛事数据管理——网络爬取、解析、存储、轮播。
/// </summary>
public partial class SportsService : ObservableObject
{
    private readonly SportsSettings _settings;
    private readonly List<SportsMatch> _matches = new();
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private System.Timers.Timer? _refreshTimer;

    [ObservableProperty]
    private int _currentIndex;

    [ObservableProperty]
    private bool _isLoading = true;

    public IReadOnlyList<SportsMatch> Matches => _matches;

    public int Count => _matches.Count;

    public SportsService(SportsSettings settings)
    {
        _settings = settings;

        // 先解析本地数据，再异步尝试网络获取
        ParseMatches();
        _ = SafeFetchAsync();
        StartAutoRefresh();

        _settings.PropertyChanged += (_, a) =>
        {
            var name = a.PropertyName;
            if (name == nameof(SportsSettings.MatchDataText))
                ParseMatches();
            else if (name == nameof(SportsSettings.DataSourceUrl)
                  || name == nameof(SportsSettings.SelectedLeagueId))
                _ = FetchFromUrlAsync();
            else if (name == nameof(SportsSettings.AutoRefreshMinutes))
                StartAutoRefresh();
        };
    }

    /// <summary>
    /// 安全异步拉取——fire-and-forget 不阻塞 UI。
    /// </summary>
    private async Task SafeFetchAsync()
    {
        try { await FetchFromUrlAsync(); }
        catch (Exception ex) { Logger.Warn($"[赛事] 初始拉取失败: {ex.Message}"); }
    }

    /// <summary>
    /// 从网络数据源获取比赛数据。支持两种格式：
    /// 1. TheSportsDB 格式：{"events":[{...}]}
    /// 2. 直接 SportsMatch 数组：[{...}]
    /// 失败时保持现有数据不变。
    /// </summary>
    public async Task FetchFromUrlAsync()
    {
        try
        {
            var url = _settings.GetEffectiveUrl();
            if (url != null)
            {
                // 单一 URL 模式
                await FetchSingleUrlAsync(url);
            }
            else if (string.IsNullOrEmpty(_settings.SelectedLeagueId))
            {
                // "全部" 模式：依次请求所有联赛
                await FetchAllLeaguesAsync();
            }
            // 自定义无 URL：不请求，只用本地数据
        }
        catch (Exception ex)
        {
            Logger.Warn($"[赛事] 网络获取失败: {ex.Message}，使用本地数据");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task FetchSingleUrlAsync(string url)
    {
        Logger.Info($"[赛事] 正在从 {url} 获取数据...");
        var json = await _http.GetStringAsync(url);
        var list = ParseResponse(json);
        if (list.Count > 0)
            ApplyMatches(list);
    }

    private async Task FetchAllLeaguesAsync()
    {
        Logger.Info("[赛事] 全部模式：依次获取所有联赛数据...");
        var all = new List<SportsMatch>();
        foreach (var id in SportsSettings.GetAllLeagueIds())
        {
            try
            {
                var url = $"https://www.thesportsdb.com/api/v1/json/3/eventspastleague.php?id={id}";
                var json = await _http.GetStringAsync(url);
                var list = ParseResponse(json);
                all.AddRange(list);
                Logger.Info($"[赛事] 联赛 {id} 获取了 {list.Count} 场");
            }
            catch (Exception ex)
            {
                Logger.Warn($"[赛事] 联赛 {id} 获取失败: {ex.Message}");
            }
        }

        if (all.Count > 0)
        {
            // 按比赛时间排序（最近的在前）
            all.Sort((a, b) => string.Compare(b.MatchTime, a.MatchTime, StringComparison.Ordinal));
            ApplyMatches(all);
        }
    }

    private void ApplyMatches(List<SportsMatch> list)
    {
        _matches.Clear();
        _matches.AddRange(list);
        CurrentIndex = 0;
        IsLoading = false;
        OnPropertyChanged(nameof(Matches));
        OnPropertyChanged(nameof(Count));
        Logger.Info($"[赛事] 共获取 {list.Count} 场比赛");
    }

    private static List<SportsMatch> ParseResponse(string json)
    {
        var list = new List<SportsMatch>();

        // 尝试 TheSportsDB 格式
        if (json.Contains("\"events\""))
        {
            list = ParseTheSportsDb(json);
        }

        // 回退到直接数组格式
        if (list.Count == 0)
        {
            list = JsonSerializer.Deserialize<List<SportsMatch>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }

        return list;
    }

    /// <summary>
    /// 解析 TheSportsDB API 返回的 JSON，并翻译为简体中文。
    /// 状态码：FT=已结束 NS=未开始 1H/2H=上下半场 HT=中场 AET=加时结束 POST=延期
    /// </summary>
    private static List<SportsMatch> ParseTheSportsDb(string json)
    {
        var result = new List<SportsMatch>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var evt in events.EnumerateArray())
            {
                var rawLeague = GetStr(evt, "strLeague");
                var m = new SportsMatch
                {
                    HomeTeam = GetStr(evt, "strHomeTeam"),
                    AwayTeam = GetStr(evt, "strAwayTeam"),
                    HomeScore = GetScoreStr(evt, "intHomeScore"),
                    AwayScore = GetScoreStr(evt, "intAwayScore"),
                    League = SportsSettings.TranslateLeague(rawLeague),
                    Status = MapStatus(GetStr(evt, "strStatus")),
                    MatchTime = GetStr(evt, "strTime") is string t && t.Length >= 5
                        ? t[..5] : ""
                };

                if (!string.IsNullOrEmpty(m.HomeTeam) && !string.IsNullOrEmpty(m.AwayTeam))
                    result.Add(m);
            }
        }
        catch { }

        return result;
    }

    private static string GetStr(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static string GetScoreStr(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v)) return "0";
        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString() ?? "0",
            JsonValueKind.Number => v.GetRawText(),
            _ => "0"
        };
    }

    /// <summary>
    /// TheSportsDB 状态码 → 中文
    /// </summary>
    private static string MapStatus(string s)
    {
        var t = (s ?? "").ToUpperInvariant();
        return t switch
        {
            "FT" or "AET" or "PEN" => "已结束",
            "NS" or "TBD" => "未开始",
            "1H" => "上半场",
            "2H" => "下半场",
            "HT" => "中场",
            "ET" => "加时",
            "POST" => "延期",
            "CANC" => "取消",
            "SUSP" or "INT" => "中断",
            "ABD" => "腰斩",
            "" => "",
            _ => s ?? ""
        };
    }

    private void StartAutoRefresh()
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();

        var minutes = _settings.AutoRefreshMinutes;
        if (minutes <= 0) return;

        _refreshTimer = new System.Timers.Timer(minutes * 60_000);
        _refreshTimer.Elapsed += async (_, _) => await FetchFromUrlAsync();
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();
    }

    /// <summary>
    /// 解析本地比赛数据。支持两种格式：
    /// 1. JSON 数组
    /// 2. 简洁文本：主队 比分:比分 客队 [赛事] [时间] [状态]
    /// </summary>
    public void ParseMatches()
    {
        var text = (_settings.MatchDataText ?? "").Trim();
        if (string.IsNullOrEmpty(text)) return;

        // 如果已经有网络数据且本地为空，不覆盖
        if (_matches.Count > 0 && string.IsNullOrEmpty(text))
            return;

        var parsed = new List<SportsMatch>();

        // 尝试 JSON
        if (text.StartsWith("["))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<SportsMatch>>(text,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (list != null) parsed.AddRange(list);
            }
            catch { }
        }

        // 纯文本解析
        if (parsed.Count == 0)
        {
            foreach (var line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                var match = ParseLine(trimmed);
                if (match != null) parsed.Add(match);
            }
        }

        if (parsed.Count > 0)
        {
            _matches.Clear();
            _matches.AddRange(parsed);
            CurrentIndex = 0;
            OnPropertyChanged(nameof(Matches));
            OnPropertyChanged(nameof(Count));
        }
    }

    private static SportsMatch? ParseLine(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return null;

        int scoreIdx = -1;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Contains(':'))
            {
                scoreIdx = i;
                break;
            }
        }
        if (scoreIdx < 1) return null;

        var match = new SportsMatch();
        match.HomeTeam = string.Join(" ", parts.Take(scoreIdx));

        var scores = parts[scoreIdx].Split(':');
        if (scores.Length == 2)
        {
            match.HomeScore = scores[0];
            match.AwayScore = scores[1];
        }

        var rest = parts.Skip(scoreIdx + 1).ToList();
        match.AwayTeam = rest.Count > 0 ? rest[0] : "";
        var remaining = rest.Skip(1).ToList();

        foreach (var token in remaining)
        {
            if (token.Contains(":"))
                match.MatchTime = token;
            else if (token is "进行中" or "已结束" or "未开始" or "中场" or "暂停")
                match.Status = token;
            else
                match.League = string.IsNullOrEmpty(match.League) ? token : match.League + " " + token;
        }

        return match;
    }

    /// <summary>翻页</summary>
    public SportsMatch? Flip(int direction)
    {
        if (_matches.Count == 0) return null;
        CurrentIndex = direction > 0
            ? (CurrentIndex + 1) % _matches.Count
            : (CurrentIndex - 1 + _matches.Count) % _matches.Count;
        return _matches[CurrentIndex];
    }

    /// <summary>获取当前比赛</summary>
    public SportsMatch? GetCurrent()
    {
        return _matches.Count == 0 ? null : _matches[CurrentIndex];
    }

    /// <summary>获取简洁显示文本</summary>
    public string GetSimpleText()
    {
        var m = GetCurrent();
        return m?.SimpleText ?? "暂无赛事数据";
    }

    /// <summary>获取详细显示文本</summary>
    public string GetDetailedText()
    {
        var m = GetCurrent();
        return m?.DetailedText ?? "暂无赛事数据";
    }
}
