using Avalonia.Controls;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;
using EntertainingIsland.Models;

namespace EntertainingIsland.Views.SettingsPages;

[SettingsPageInfo(
    "entertainingisland.cameramonitor",
    "摄像头安全",
    "\uE392",
    "\uE392",
    SettingsPageCategory.External
)]
public partial class CameraMonitorSettingsPage : SettingsPageBase
{
    private Plugin PluginEntry => IAppHost.GetService<Plugin>();
    public Settings Settings => PluginEntry.Settings;
    public CameraMonitorSettings Camera => Settings.CameraMonitor;

    public CameraMonitorSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }
}
