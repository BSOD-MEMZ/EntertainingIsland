using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace EntertainingIsland.Models;

/// <summary>
/// 体育赛事组件设置
/// </summary>
public partial class SportsSettings : ObservableObject
{
    /// <summary>选中的联赛 ID（TheSportsDB）。空字符串 = "全部"</summary>
    [ObservableProperty]
    private string _selectedLeagueId = "";

    /// <summary>自定义数据源 URL（留空则使用上方的联赛选择。填入 URL 则覆盖联赛选择。）</summary>
    [ObservableProperty]
    private string _dataSourceUrl = "";

    /// <summary>手动赛事数据（网络获取失败时的后备，每行格式：主队 比分:比分 客队）</summary>
    [ObservableProperty]
    private string _matchDataText = "";

    /// <summary>自动刷新间隔（分钟，0=不自动刷新）</summary>
    [ObservableProperty]
    private int _autoRefreshMinutes = 5;

    /// <summary>轮播间隔秒数</summary>
    [ObservableProperty]
    private int _flipIntervalSeconds = 8;

    /// <summary>显示模式：true=详细，false=简洁</summary>
    [ObservableProperty]
    private bool _detailedMode = true;

    /// <summary>联赛预设（TheSportsDB ID）。空字符串 = 全部联赛</summary>
    public static List<LeaguePreset> LeaguePresets { get; } = new()
    {
        new("全部", ""),
        new("NBA", "4387"),
        new("英超", "4328"),
        new("西甲", "4335"),
        new("德甲", "4331"),
        new("意甲", "4332"),
        new("法甲", "4334"),
        new("欧冠", "4480"),
        new("NFL", "4391"),
        new("NHL", "4380"),
        new("MLB", "4424"),
        new("F1", "4370"),
        new("自定义", "__custom__"),
    };

    /// <summary>构建实际请求 URL。返回 null 代表需要多联赛获取。</summary>
    public string? GetEffectiveUrl()
    {
        if (!string.IsNullOrWhiteSpace(DataSourceUrl))
            return DataSourceUrl;
        if (!string.IsNullOrWhiteSpace(SelectedLeagueId) && SelectedLeagueId != "__custom__")
            return $"https://www.thesportsdb.com/api/v1/json/3/eventspastleague.php?id={SelectedLeagueId}";
        return null; // 全部 或 自定义无 URL
    }

    /// <summary>获取"全部"模式下要请求的联赛 ID 列表</summary>
    public static IEnumerable<string> GetAllLeagueIds()
    {
        foreach (var p in LeaguePresets)
            if (!string.IsNullOrEmpty(p.Id) && p.Id != "__custom__")
                yield return p.Id;
    }

    // ===== 简体中文翻译字典 =====

    /// <summary>联赛名称英→中</summary>
    public static readonly Dictionary<string, string> LeagueNameZh = new()
    {
        ["NBA"] = "NBA",
        ["English Premier League"] = "英超",
        ["Spanish La Liga"] = "西甲",
        ["German Bundesliga"] = "德甲",
        ["Italian Serie A"] = "意甲",
        ["French Ligue 1"] = "法甲",
        ["UEFA Champions League"] = "欧冠",
        ["NFL"] = "NFL",
        ["NHL"] = "NHL",
        ["MLB"] = "MLB",
        ["Formula 1"] = "F1",
        ["Dutch Eredivisie"] = "荷甲",
        ["Portuguese Liga"] = "葡超",
        ["Scottish Premier League"] = "苏超",
        ["English League Championship"] = "英冠",
        ["Greek Superleague Greece"] = "希超",
        ["Belgian Pro League"] = "比甲",
        ["Major League Soccer"] = "美职联",
        ["Brazilian Serie A"] = "巴甲",
        ["Argentine Primera Division"] = "阿甲",
        ["UEFA Europa League"] = "欧联杯",
        ["FIFA World Cup"] = "世界杯",
        ["UEFA Euro"] = "欧洲杯",
        ["Copa America"] = "美洲杯",
        ["AFC Asian Cup"] = "亚洲杯",
    };

    /// <summary>运动类型英→中</summary>
    public static readonly Dictionary<string, string> SportNameZh = new()
    {
        ["Soccer"] = "足球",
        ["Basketball"] = "篮球",
        ["American Football"] = "美式橄榄球",
        ["Ice Hockey"] = "冰球",
        ["Baseball"] = "棒球",
        ["Formula 1"] = "F1 赛车",
        ["Rugby"] = "橄榄球",
        ["Cricket"] = "板球",
        ["Motorsport"] = "赛车",
        ["Tennis"] = "网球",
        ["Boxing"] = "拳击",
        ["MMA"] = "综合格斗",
        ["Golf"] = "高尔夫",
        ["Cycling"] = "自行车",
        ["Volleyball"] = "排球",
    };

    /// <summary>翻译联赛名</summary>
    public static string TranslateLeague(string en)
    {
        if (string.IsNullOrEmpty(en)) return "";
        return LeagueNameZh.TryGetValue(en, out var zh) ? zh : en;
    }

    /// <summary>翻译运动类型</summary>
    public static string TranslateSport(string en)
    {
        if (string.IsNullOrEmpty(en)) return "";
        return SportNameZh.TryGetValue(en, out var zh) ? zh : en;
    }
}

/// <summary>
/// 联赛预设条目
/// </summary>
public record LeaguePreset(string Name, string Id);

/// <summary>
/// 一场比赛的数据
/// </summary>
public partial class SportsMatch : ObservableObject
{
    [ObservableProperty]
    private string _homeTeam = "";

    [ObservableProperty]
    private string _awayTeam = "";

    [ObservableProperty]
    private string _homeScore = "0";

    [ObservableProperty]
    private string _awayScore = "0";

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private string _matchTime = "";

    [ObservableProperty]
    private string _league = "";

    /// <summary>简洁模式文本："队A 3:1 队B"</summary>
    public string SimpleText => $"{HomeTeam} {HomeScore}:{AwayScore} {AwayTeam}";

    /// <summary>详细模式文本（联赛名和状态已在副标题显示，此处仅比赛信息）</summary>
    public string DetailedText
    {
        get
        {
            var time = string.IsNullOrEmpty(MatchTime) ? "" : $"{MatchTime}  ";
            return $"{time}{HomeTeam} {HomeScore}:{AwayScore} {AwayTeam}";
        }
    }
}
