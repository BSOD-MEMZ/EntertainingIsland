using System.Collections.Generic;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;
using EntertainingIsland.Models;

namespace EntertainingIsland.Views.SettingsPages;

[SettingsPageInfo(
    "entertainingisland.settings",
    "EntertainingIsland",
    "\uEF27",
    "\uEF27",
    SettingsPageCategory.External
)]
public partial class MySettingsPage : SettingsPageBase
{
    private Plugin PluginEntry => IAppHost.GetService<Plugin>();
    public Settings Settings => PluginEntry.Settings;
    public FeatureToggles Ft => Settings.FeatureToggles;

    public static List<string> KeyOptions { get; } = new()
    {
        "A","B","C","D","E","F","G","H","I","J","K","L","M",
        "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
        "0","1","2","3","4","5","6","7","8","9",
        "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12"
    };

    public string PluginInfoText =>
        $"名称: EntertainingIsland\n" +
        $"ID: {PluginEntry.Info.Manifest.Id}\n" +
        $"版本: {PluginEntry.Info.Manifest.Version}\n" +
        $"功能: 防巡堂警报 · 小说阅读器 · 口头禅记录 · RSS 新闻 · 头像课程表 · 点名器 · 体育赛事 · 下课倒计时 · 摄像头安全 · 每日运势\n" +
        $"安全键: {PluginEntry.Settings.DismissHotkey.DisplayString}（消警+隐藏/显示所有组件）\n" +
        $"自动化: 14 个 ClassIsland 行动 + 2 个触发器";

    public MySettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }
}