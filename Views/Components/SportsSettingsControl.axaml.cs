using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Controls;
using EntertainingIsland.Models;

namespace EntertainingIsland.Views.Components;

public partial class SportsSettingsControl : ComponentBase<SportsSettings>
{
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
