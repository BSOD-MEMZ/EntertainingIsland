using System.Collections.Generic;
using Avalonia.Interactivity;
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
        $"EntertainingIsland\n" +
        $"版本: {PluginEntry.Info.Manifest.Version}\n" +
        $"2026 xxtsoft 对一切提前开学&违规补课等行为致以最强烈的谴责";

    public MySettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void ButtonReRunOobe_OnClick(object? sender, RoutedEventArgs e)
    {
        Plugin.ShowWelcomeWizard();
    }
}