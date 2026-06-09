using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Shared;
using EntertainingIsland.Models;
using EntertainingIsland.Services;

namespace EntertainingIsland.Views.Components;

public partial class FortuneComponentSettingsControl : ComponentBase<FortuneComponentSettings>
{
    private Plugin PluginEntry => IAppHost.GetService<Plugin>();
    public Settings PluginSettings => PluginEntry.Settings;

    public FortuneComponentSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void AddGood_Click(object? sender, RoutedEventArgs e)
    {
        PluginSettings.Fortune.GoodFortunes.Add(new FortuneEntry { MainText = "新条目", SubText = "副标题" });
    }

    private void DeleteGood_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FortuneEntry entry)
            PluginSettings.Fortune.GoodFortunes.Remove(entry);
    }

    private void AddBad_Click(object? sender, RoutedEventArgs e)
    {
        PluginSettings.Fortune.BadFortunes.Add(new FortuneEntry { MainText = "新条目", SubText = "副标题" });
    }

    private void DeleteBad_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FortuneEntry entry)
            PluginSettings.Fortune.BadFortunes.Remove(entry);
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        IAppHost.TryGetService<FortuneService>()?.RefreshToday(forceRandom: true);
    }
}
