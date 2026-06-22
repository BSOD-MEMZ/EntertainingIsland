using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using EntertainingIsland.Models;

namespace EntertainingIsland.Views.Components;

public partial class RssSettingsControl : ComponentBase<RssComponentSettings>
{
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
