using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Shared;
using EntertainingIsland.Models;

namespace EntertainingIsland.Views.Components;

public partial class CameraMonitorSettingsControl : ComponentBase<CameraMonitorSettings>
{
    private Plugin PluginEntry => IAppHost.GetService<Plugin>();
    public Settings PluginSettings => PluginEntry.Settings;

    public CameraMonitorSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
