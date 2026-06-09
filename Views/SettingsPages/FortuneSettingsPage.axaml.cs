using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;
using EntertainingIsland.Models;
using EntertainingIsland.Services;

namespace EntertainingIsland.Views.SettingsPages;

[SettingsPageInfo(
    "entertainingisland.fortune",
    "每日运势",
    "\uE8D6",
    "\uE8D6",
    SettingsPageCategory.External
)]
public partial class FortuneSettingsPage : SettingsPageBase
{
    private Plugin PluginEntry => IAppHost.GetService<Plugin>();
    public Settings Settings => PluginEntry.Settings;
    public FortuneSettings Fortune => Settings.Fortune;

    public FortuneSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void AddGood_Click(object? sender, RoutedEventArgs e)
    {
        Fortune.GoodFortunes.Add(new FortuneEntry { MainText = "新条目", SubText = "副标题" });
    }

    private void DeleteGood_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FortuneEntry entry)
            Fortune.GoodFortunes.Remove(entry);
    }

    private void AddBad_Click(object? sender, RoutedEventArgs e)
    {
        Fortune.BadFortunes.Add(new FortuneEntry { MainText = "新条目", SubText = "副标题" });
    }

    private void DeleteBad_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FortuneEntry entry)
            Fortune.BadFortunes.Remove(entry);
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        IAppHost.TryGetService<FortuneService>()?.RefreshToday(forceRandom: true);
    }
}
