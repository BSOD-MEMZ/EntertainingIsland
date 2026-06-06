using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Controls;
using EntertainingIsland.Models;

namespace EntertainingIsland.Views.Components;

public partial class SportsSettingsControl : ComponentBase<SportsSettings>
{
    public ObservableCollection<string> KeyOptions { get; } = new()
    {
        "A","B","C","D","E","F","G","H","I","J","K","L","M",
        "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
        "0","1","2","3","4","5","6","7","8","9",
        "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
        "Left","Right","Up","Down",
        "Space","Enter","Tab","Escape"
    };

    public SportsSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // 填充联赛下拉框
        foreach (var p in SportsSettings.LeaguePresets)
            LeagueCombo.Items.Add(p.Name);

        // 同步选中项：默认选"全部"（index 0）
        var preset = SportsSettings.LeaguePresets.FirstOrDefault(p => p.Id == Settings.SelectedLeagueId);
        var idx = preset != null ? SportsSettings.LeaguePresets.IndexOf(preset) : 0;
        LeagueCombo.SelectedIndex = idx;

        LeagueCombo.SelectionChanged += (_, _) =>
        {
            if (LeagueCombo.SelectedIndex >= 0 && LeagueCombo.SelectedIndex < SportsSettings.LeaguePresets.Count)
                Settings.SelectedLeagueId = SportsSettings.LeaguePresets[LeagueCombo.SelectedIndex].Id;
        };
    }
}
