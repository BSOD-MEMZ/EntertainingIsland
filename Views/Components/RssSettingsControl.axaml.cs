using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using EntertainingIsland.Models;

namespace EntertainingIsland.Views.Components;

public partial class RssSettingsControl : ComponentBase<RssComponentSettings>
{
    public static List<string> KeyOptions { get; } = new()
    {
        "A","B","C","D","E","F","G","H","I","J","K","L","M",
        "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
        "0","1","2","3","4","5","6","7","8","9",
        "Left","Right","Up","Down","Space",
        "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12"
    };

    public ObservableCollection<PresetItem> PresetSources { get; } = new(
        RssPresets.Feeds.Select(f => new PresetItem { Name = f.Name, Url = f.Url }));

    public RssSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void OnPresetClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string url)
            Settings.RssFeedUrl = url;
    }

    public class PresetItem
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
