using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EntertainingIsland.Models;

/// <summary>
/// 一条运势条目（主标题 + 副标题）
/// </summary>
public partial class FortuneEntry : ObservableObject
{
    [ObservableProperty]
    private string _mainText = "";

    [ObservableProperty]
    private string _subText = "";
}

/// <summary>
/// 每日运势设置
/// </summary>
public partial class FortuneSettings : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled = true;

    /// <summary>是否只显示宜（隐藏忌）</summary>
    [ObservableProperty]
    private bool _showOnlyGood;

    /// <summary>宜... 条目池</summary>
    public ObservableCollection<FortuneEntry> GoodFortunes { get; set; } = new()
    {
        new() { MainText = "查成绩",        SubText = "进步显著" },
        new() { MainText = "发呆",        SubText = "灵感迸发" },
        new() { MainText = "体育课",      SubText = "自由活动！" },
        new() { MainText = "考试",        SubText = "蒙的全对" },
        new() { MainText = "抄笔记",      SubText = "同桌字迹工整" },
        new() { MainText = "开班会",      SubText = "班主任有事不来了" },
        new() { MainText = "晚自习",      SubText = "效率翻倍" },
        new() { MainText = "去食堂",        SubText = "今天有给人吃的东西" },
        new() { MainText = "背单词",      SubText = "过目不忘" },
        new() { MainText = "上课睡觉",        SubText = "不被发现" },
        new() { MainText = "换座位",      SubText = "换到靠窗好位置" },
        new() { MainText = "刷抖音",      SubText = "全是搞笑视频" },
        new() { MainText = "追番",        SubText = "这季全是神作" },
        new() { MainText = "打游戏",      SubText = "排位十连胜" },
        new() { MainText = "发朋友圈",    SubText = "点赞数爆了" },
        new() { MainText = "用豆包",      SubText = "直接生成答案" },
        new() { MainText = "听歌",        SubText = "日推每首都好听" },
        new() { MainText = "刷B站",     SubText = "首页全是感兴趣的" },
        new() { MainText = "喝奶茶",      SubText = "第二杯半价" },
        new() { MainText = "看小说",      SubText = "更新了五章" },
        new() { MainText = "抢红包",      SubText = "手气最佳" },
        new() { MainText = "和同桌聊天",  SubText = "老师转过去写字了" },
        new() { MainText = "打音游",  SubText = "AP！FC！" },
        new() { MainText = "开空调",      SubText = "爽爆了" },
        new() { MainText = "玩手机",      SubText = "而且没人发现" },
        new() { MainText = "玩蛋仔派对",  SubText = "巅峰夺冠" },
        new() { MainText = "玩我的世界",  SubText = "下界挖到远古遗骸" },
        new() { MainText = "卷题",  SubText = "水平明显提升" },
        new() { MainText = "洗澡",  SubText = "洗香香" },
        new() { MainText = "交友",  SubText = "友谊地久天长" },
    };

    /// <summary>忌... 条目池</summary>
    public ObservableCollection<FortuneEntry> BadFortunes { get; set; } = new()
    {
        new() { MainText = "去食堂",      SubText = "今天的东西不是给人吃的" },
        new() { MainText = "写作业",      SubText = "上课讲了这些了吗" },
        new() { MainText = "上课睡觉",    SubText = "被老师点名" },
        new() { MainText = "传纸条",      SubText = "被班主任截获" },
        new() { MainText = "玩手机",      SubText = "后门有双眼睛" },
        new() { MainText = "体育课",      SubText = "改成教室自习" },
        new() { MainText = "早读",        SubText = "课本忘带了" },
        new() { MainText = "考试",        SubText = "考的全不会" },
        new() { MainText = "迟到",        SubText = "正好碰见年级主任" },
        new() { MainText = "周末",        SubText = "没双休" },
        new() { MainText = "吃泡面",      SubText = "被生活老师闻到" },
        new() { MainText = "换座位",      SubText = "换到讲台边" },
        new() { MainText = "打游戏",      SubText = "排位十连跪" },
        new() { MainText = "上厕所",      SubText = "刚蹲下上课铃响了" },
        new() { MainText = "看小说",      SubText = "手机被没收了" },
        new() { MainText = "点外卖",      SubText = "被门卫拦下了" },
        new() { MainText = "抄作业",      SubText = "答案发错了群" },
        new() { MainText = "讲闲话",      SubText = "被记名字" },
        new() { MainText = "追番",        SubText = "这集全是水时长" },
        new() { MainText = "装病请假",    SubText = "被班主任识破" },
        new() { MainText = "打音游",  SubText = "您有一个好" },
        new() { MainText = "出勤",  SubText = "大逼队" },
        new() { MainText = "玩我的世界",  SubText = "转角遇到苦力怕" },
        new() { MainText = "卷题",  SubText = "我咋啥都不会" },
        new() { MainText = "洗澡",  SubText = "小心着凉" },
        new() { MainText = "交友",  SubText = "交友不慎" },
        new() { MainText = "上课",  SubText = "反正你听不懂" },
    };

    /// <summary>每日宜取几条</summary>
    [ObservableProperty]
    private int _goodCount = 1;

    /// <summary>每日忌取几条</summary>
    [ObservableProperty]
    private int _badCount = 1;

    /// <summary>今日宜（内部使用，不持久化）</summary>
    [ObservableProperty]
    private ObservableCollection<FortuneEntry> _todayGood = new();

    /// <summary>今日忌（内部使用，不持久化）</summary>
    [ObservableProperty]
    private ObservableCollection<FortuneEntry> _todayBad = new();
}
